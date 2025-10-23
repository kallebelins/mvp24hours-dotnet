# Changelog

Todas as mudanças notáveis neste projeto serão documentadas neste arquivo.

O formato é baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/),
e este projeto adere ao [Versionamento Semântico](https://semver.org/lang/pt-BR/).

## [Não Lançado]

### Adicionado
- Hierarquia completa de exceções customizadas para melhor tratamento de erros
  - `Mvp24HoursException` - Exceção base com ErrorCode e Context
  - `DataException` - Erros de acesso a dados
  - `ValidationException` - Falhas de validação com lista de erros
  - `BusinessException` - Violações de regras de negócio
  - `ConfigurationException` - Problemas de configuração
  - `PipelineException` - Erros em operações de pipeline
- Documentação XML completa e profissional para interfaces principais
  - `IQuery<T>` com exemplos práticos e warnings de performance
  - `ICommand<T>` com explicações de soft delete e auditoria
  - `IUnitOfWork` com documentação do padrão e transações
  - `IRepository<T>` com referências cruzadas
- Plano estruturado de melhorias com 156 tarefas organizadas em 7 categorias
- Suporte completo a nullable reference types (C# 8+)

### Melhorado
- Documentação de todas as interfaces com tags `<remarks>`, `<example>`, `<exception>`
- Organização e clareza dos comentários de código
- IntelliSense mais informativo com exemplos executáveis
- Estrutura de exceções mais robusta e rastreável

### Documentação
- Roadmap de melhorias: `docs/tasks.md`
- 156 tarefas categorizadas e priorizadas

## [8.3.261] - 2024

### Adicionado
- **CronJob**: Implementação de suporte a tarefas agendadas
  - Configuração fluente de schedules
  - Suporte a expressões cron
  - Injeção de dependência para jobs
  - Hosted service integrado

### Detalhes
Esta versão introduz o módulo `Mvp24Hours.Infrastructure.CronJob` permitindo agendar tarefas recorrentes de forma simples e integrada ao ASP.NET Core.

**Exemplo de uso:**
```csharp
services.AddMvp24HoursCronJob(config =>
{
    config.AddJob<MyScheduledJob>("0 */5 * * * *"); // A cada 5 minutos
});
```

## [8.2.102] - 2024

### Adicionado
- **Minimal API**: Manipuladores de rotas para conversão e vinculação de parâmetros
  - Binders customizados para tipos complexos
  - Conversores para tipos primitivos
  - Suporte a validação automática
  - Integração com BusinessResult pattern

### Melhorado
- Suporte aprimorado para Minimal APIs do .NET 6+
- Binding automático de DTOs em rotas mínimas
- Validação integrada em endpoints Minimal API

### Detalhes
Facilita o uso do padrão Minimal API mantendo a robustez dos binders e validações da biblioteca.

## [8.2.101] - 2024

### Mudado
- **Migração para .NET 8**: Refatoração completa para .NET 8
  - Atualização de todas as dependências para .NET 8
  - Uso de recursos modernos do C# 12
  - Otimizações de performance do .NET 8
  - Primary constructors onde apropriado
  - Collection expressions

### Removido
- Suporte a versões anteriores ao .NET 8
- Pacotes obsoletos e substituídos

### Detalhes
Grande marco de migração para .NET 8, trazendo melhorias de performance e recursos modernos da linguagem.

---

# Histórico .NET Core / .NET 6

## [4.1.191] - 2023

### Mudado
- **Mapeamento Assíncrono**: Refatoração para mapeamento de resultados assíncronos
  - Novos métodos de extensão para `Task<T>`
  - Suporte a `ValueTask<T>`
  - Mapeamento automático de `IBusinessResult<T>` assíncrono

### Melhorado
- Performance em operações assíncronas
- Redução de alocações de memória

## [4.1.181] - 2023

### Removido
- **Anti-patterns**: Remoção de padrões problemáticos identificados
  - Eliminação de acoplamentos desnecessários
  - Remoção de dependências circulares
  - Simplificação de abstrações excessivas

### Mudado
- **Entidades de Log**: Separação de contextos de entidade de log
  - Uso apenas de contratos para melhor abstração
  - Interfaces `IEntityLog<T>` e `IEntityDateLog` separadas
  - Implementações base opcionaliz`EntityBase` separadas de `EntityBaseLog`

### Adicionado
- Documentação arquitetural detalhada
- Testes para contexto de banco de dados com log

### Corrigido
- Injeção de dependência no client do RabbitMQ
- Injeção de dependência no Pipeline
- Consumers isolados para client do RabbitMQ

### Documentação
- Atualização e detalhamento de recursos arquiteturais
- Exemplos de uso de entidades com auditoria

## [3.12.262] - 2022

### Mudado
- Refatoração completa de extensões
  - Organização por namespace
  - Remoção de duplicações
  - Padronização de nomenclatura

## [3.12.261] - 2022

### Adicionado
- Testes para middlewares customizados
- Cobertura de testes para WebAPI

## [3.12.221] - 2022

### Adicionado
- **Resiliência**: Implementação de Polly para tolerância a falhas
  - Retry policies configuráveis
  - Circuit breaker pattern
  - Timeout policies
  - Fallback strategies

- **Delegation Handlers**: Propagação de chaves no Header
  - Correlation ID automático
  - Propagação de Authorization header
  - Headers customizados configuráveis
  - Logging de requisições HTTP

### Corrigido
- Carregamento automático de classes de mapeamento com `IMapFrom`
- Bug de reflexão em assemblies dinâmicos

### Detalhes
Esta versão trouxe grande melhoria em resiliência e observabilidade de aplicações distribuídas.

## [3.12.151] - 2022

### Mudado
- **IMapFrom**: Remoção de tipagem genérica redundante
  - Simplificação da interface
  - Detecção automática de tipos
  - Configuração mais limpa

### Adicionado
- **Testcontainers**: Suporte para testes com containers Docker
  - RabbitMQ testcontainer
  - Redis testcontainer
  - MongoDB testcontainer
  - Configuração automática de testes de integração

### Detalhes
Testcontainers revolucionaram os testes de integração, permitindo testes reais contra serviços em containers Docker.

## [3.2.241] - 2021

### Mudado
- **Configuração Fluente**: Migração de configurações JSON para extensões fluentes
  - API fluente para DbContext
  - Configuração fluente para RabbitMQ
  - Configuração fluente para Redis
  - Configuração fluente para Pipeline

- **Padrão de Notificação**: Substituição do sistema de notificações
  - Novo `BusinessResult<T>` com mensagens integradas
  - Remoção de notification context separado
  - Mensagens tipadas (Info, Error, Warning, Success)

### Adicionado
- **HealthCheck**: Suporte completo a health checks
  - Health checks para SQL Server, PostgreSQL, MySQL
  - Health checks para MongoDB
  - Health checks para Redis
  - Health checks para RabbitMQ
  - WebStatus project com HealthCheckUI

- **Telemetria**: Sistema de telemetria customizável
  - Trace/Verbose em todas as bibliotecas
  - Níveis de log configuráveis
  - Filtros por operação
  - Integração com providers de logging

- **RabbitMQ Avançado**: Recursos avançados de mensageria
  - Dead Letter Queue configurável
  - Conexão persistente com Polly
  - Consumer assíncrono
  - Retry automático

### Melhorado
- **Transaction Isolation**: Configuração de nível de isolamento para EF
  - Transaction scope configurável
  - Read committed por padrão
  - Otimização para leituras

- **Pipeline**: Melhorias no sistema de pipeline
  - Adição de mensagens ao pacote durante execução
  - Suporte a operações de rollback
  - Context compartilhado entre operações

- **Validação**: Sistema de validação aprimorado
  - FluentValidation com mensagens estruturadas
  - DataAnnotations com mensagens estruturadas
  - Retorno consistente de erros

### Documentação
- Documentação completa de WebAPI
- Guias de configuração atualizados
- Exemplos de uso de todos os recursos

### Detalhes
Versão marco com grandes refatorações e novos recursos fundamentais. Introdução do padrão BusinessResult e telemetria.

## [3.1.x e anteriores] - 2020-2021

### Funcionalidades Base Implementadas
- ✅ **Banco de Dados Relacional**
  - SQL Server com Entity Framework Core
  - PostgreSQL com Npgsql
  - MySQL com Pomelo
  - Repository pattern
  - Unit of Work pattern
  - Soft delete automático
  - Auditoria automática

- ✅ **Banco de Dados NoSQL**
  - MongoDB com driver oficial
  - Redis com StackExchange.Redis
  - Repository pattern para NoSQL
  - Cache distribuído

- ✅ **Message Broker**
  - RabbitMQ com cliente oficial
  - Publisher/Subscriber pattern
  - Work Queue pattern
  - Request/Reply pattern

- ✅ **Pipeline**
  - Pipe and Filters pattern
  - Operações sequenciais
  - Rollback automático
  - Injeção de dependência

- ✅ **Documentação**
  - Swagger/OpenAPI integration
  - XML comments support
  - Customização de UI

- ✅ **Mapeamento**
  - AutoMapper integration
  - Profiles automáticos
  - IMapFrom interface

- ✅ **Logging**
  - Integração com ILogger
  - Structured logging
  - Múltiplos providers

- ✅ **Validação**
  - FluentValidation support
  - Data Annotations support
  - Validações customizadas

- ✅ **Specification Pattern**
  - Expressões LINQ reutilizáveis
  - Composição de specifications
  - AND, OR, NOT operators

---

## Tipos de Mudanças

- `Adicionado` para novos recursos
- `Mudado` para mudanças em recursos existentes
- `Descontinuado` para recursos que serão removidos
- `Removido` para recursos removidos
- `Corrigido` para correções de bugs
- `Segurança` para vulnerabilidades

## Convenções de Versionamento

Este projeto segue o [Versionamento Semântico](https://semver.org/lang/pt-BR/):
- **MAJOR**: Mudanças incompatíveis na API
- **MINOR**: Funcionalidades adicionadas de forma retrocompatível
- **PATCH**: Correções de bugs retrocompatíveis

Formato: `MAJOR.MINOR.PATCH` (ex: 8.3.261)

## Links

- [Documentação](https://mvp24hours.dev)
- [Repositório](https://github.com/kallebelins/mvp24hours-dotnet)
- [Exemplos](https://github.com/kallebelins/mvp24hours-dotnet-samples)
- [Issues](https://github.com/kallebelins/mvp24hours-dotnet/issues)
- [Releases](https://github.com/kallebelins/mvp24hours-dotnet/releases)

## Contribuindo

Veja [CONTRIBUTING.md](CONTRIBUTING.md) para detalhes sobre como contribuir com o projeto.

## Agradecimentos

Desenvolvido com ❤️ por [Kallebe Lins](https://github.com/kallebelins).

**Quer contribuir?** Veja [CONTRIBUTING.md](CONTRIBUTING.md) para começar!
