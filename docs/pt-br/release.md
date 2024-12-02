# O que há de novo?

# NET9
## 9.0.101
    * Migração e refatoração de evolução para NET9.

# NET8

## 8.3.261
* Implementação de CronJob.

## 8.2.102
* Implementação de manipuladores de rotas para conversão e vinculação de parâmetros para Minimal API.

## 8.2.101
* Migração e refatoração de evolução para NET8.

# NETCORE

## 4.1.191
* Refatoração para mapeamento de resultados assíncronos;

## 4.1.181
* Remoção de Anti-patterns;
* Separação de contextos de entidade de log para uso apenas de contratos;
* Atualização e detalhamento de recuros arquiteturais na documentação;
* Correção de injeção de dependência no client do RabbitMQ e Pipeline;
* Configuração de consumers isolados para client do RabbitMQ;
* Implementação de testes para contexto de banco de dados com log;

## 3.12.262
* Refatoração de extensões.

## 3.12.261
* Implementação de teste de middleware.

## 3.12.221
* Implementação de Delegation Handlers para propagação de chaves no Header (correlation-id, authorization, etc);
* Implementação de Polly para aplicar conceitos de resiliência e tolerância a falhas;
* Correção de carregamento automático de classes de mapeamento com IMapFrom;

## 3.12.151
* Remoção de tipagem genérica da classe IMapFrom;
* Implmentação de Testcontainers para projetos RabbitMQ, Redis e MongoDb;

## 3.2.241
* Refatoração para migrar configurações de arquivo json para extensões fluentes;
* Substituição do padrão de notificação;
* Revisão dos templates;
* Adição de HealthCheck em todos os samples;
* Criação de projeto básico de WebStatus com HealthCheckUI;
* Substituição de depências de logging para injeção de trace através de actions;
* Trace/Verbose em todas as bibliotecas e camadas principais;
* Configuração de nível de isolamento de transação para consultas com EF;
* Refatoração da biblioteca do RabbitMQ para injeção de consumers e configuração fluída para "DeadLetterQueue";
* Conexão persistente e resiliência com Polly para RabbitMQ;
* Implementação de consumidor assíncrono para RabbitMQ;
* Ajuste de pipeline para permitir adicionar mensagens no pacote (info, error, warning, success) - substituição do padrão de notificação;
* Alteração de validação (FluentValidation ou DataAnnotations) para retornar lista de mensagens - substituição do padrão de notificação;
* Alteração de documentação e adição de configuração para WebAPI;
* Refatoração do teste de bibliotecas;
* Refatoração para migração do Core para o .NET 6.

## Outras versões...
* Banco de dados relacional (SQL Server, PostgreSql e MySql)
* Banco de dados NoSql (MongoDb e Redis)
* Message Broker (RabbitMQ)
* Pipeline (Pipe and Filters pattern)
* Documentação (Swagger)
* Mapeamento (AutoMapper)
* Logging
* Padrões para validação de dados (FluentValidation e Data Annotations), especificações (Specification pattern), unidade de trabalho, repositório, entre outros.