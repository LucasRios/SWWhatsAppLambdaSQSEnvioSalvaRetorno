# SW WhatsApp Outbound Result Saver

Este serviço é o componente de **fechamento de ciclo** (Callback/SaveReturn) do ecossistema de mensageria WhatsApp. Ele garante que cada tentativa de envio disparada pelo sistema tenha seu status devidamente registrado no banco de dados SQL Server.

## 📌 Objetivo

Consumir eventos de uma fila SQS que contêm o status de retorno (sucesso ou erro) das APIs de mensageria e atualizar a tabela de controle operacional do sistema.

## 🛠️ Tecnologias Utilizadas

- **Runtime**: .NET 6/8 (AWS Lambda)
- **Banco de Dados**: Microsoft SQL Server / Azure SQL
- **Provedor de Dados**: `Microsoft.Data.SqlClient`
- **Mensageria**: Amazon SQS

## ⚙️ Configuração do Banco de Dados

A Lambda utiliza um modelo de persistência via ADO.NET para alta performance. O comando SQL executa uma atualização baseada no campo `CodSysFilaEnvioMensagens`.

**Campos Esperados no JSON de Entrada:**
- `CodSysFilaEnvioMensagens` (long): Chave primária do registro de envio.
- `Status` (int): Código de status da operação.
- `ResponseContent` (string): Log detalhado da resposta da API.

## ⚠️ Tratamento de Erros e Resiliência

- **Idempotência**: O código utiliza atualizações baseadas em ID único, garantindo que reprocessamentos não corrompam os dados.
- **Retry Policy**: Caso o banco de dados esteja indisponível, a Lambda lança uma exceção, permitindo que o **Amazon SQS Visibility Timeout** entre em ação para futuras tentativas.
- **DLQ (Dead Letter Queue)**: Recomenda-se configurar uma DLQ na fila de origem para capturar mensagens com erros de sintaxe ou dados inválidos.

## 🚀 Deployment

Para publicar via AWS CLI:

```bash
dotnet lambda deploy-function SWWhatsAppLambdaSQSEnvioSaveReturn
