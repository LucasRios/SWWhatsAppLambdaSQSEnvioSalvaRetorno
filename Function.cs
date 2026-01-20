using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Data;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Microsoft.Data.SqlClient;

// Serializador global para garantir a comunicação correta entre o SQS e o ambiente Lambda
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SWWhatsAppLambdaSQSEnvioSaveReturn
{
    /// <summary>
    /// Classe responsável por salvar o retorno das tentativas de envio de mensagens no banco de dados.
    /// </summary>
    public class OutboundResultSaver
    {
        // String de conexão (Idealmente deve ser movida para variáveis de ambiente ou AWS Secrets Manager)
        private const string CONN_STRING = "<CONN_STRING>";

        /// <summary>
        /// Handler principal que processa o lote de mensagens enviadas pelo SQS
        /// </summary>
        public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
        {
            if (evnt?.Records == null) return;

            // Abre a conexão uma única vez por execução do Handler para economizar recursos (Pool de conexão)
            using var connection = new SqlConnection(CONN_STRING);
            await connection.OpenAsync();

            // Configuração de desserialização (ignora letras maiúsculas/minúsculas no JSON)
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var record in evnt.Records)
            {
                if (string.IsNullOrWhiteSpace(record.Body)) continue;

                try
                {
                    // Converte o corpo da mensagem SQS para o objeto de resultado C#
                    var result = JsonSerializer.Deserialize<OutboundResult>(record.Body, jsonOptions);

                    if (result == null)
                    {
                        context.Logger.LogLine($"Aviso: Não foi possível deserializar o corpo da mensagem {record.MessageId}");
                        continue;
                    }

                    // 1. Update na tabela central do sistema para registrar o sucesso/falha do envio
                    // Nota: O SQL abaixo deve ser completado com a lógica de negócio específica
                    var sql = @"UPDATE ...";

                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        // Vincula o código vindo do JSON ao parâmetro SQL para evitar SQL Injection
                        cmd.Parameters.AddWithValue("@cod", result.CodSysFilaEnvioMensagens);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    context.Logger.LogLine($"ID {result.CodSysFilaEnvioMensagens} processado e banco atualizado com sucesso.");
                }
                catch (Exception ex)
                {
                    // Em caso de erro de rede ou banco, o log é gerado e a exceção relançada
                    context.Logger.LogLine($"ERRO CRÍTICO no record {record.MessageId}: {ex.Message}");

                    // IMPORTANTE: Ao lançar o 'throw', a mensagem volta para a fila SQS. 
                    // Se falhar várias vezes, ela irá para a Dead Letter Queue (DLQ).
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Modelo de dados que representa o resultado de uma operação de envio (Outbound)
    /// </summary>
    public class OutboundResult
    {
        // ID de referência na tabela do sistema
        public long CodSysFilaEnvioMensagens { get; set; }

        // Status do retorno da API (Ex: 200 para sucesso, 400 para erro)
        public int Status { get; set; }

        // Conteúdo da resposta (JSON retornado pela Meta/Whapi)
        public string ResponseContent { get; set; }
    }
}