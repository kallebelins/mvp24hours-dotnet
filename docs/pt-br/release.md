# O que há de novo?

# NET9

## 9.1.200 (Janeiro 2026) 🚀 Major Release

### ⭐ Biblioteca CQRS Completa (Mvp24Hours.Infrastructure.Cqrs)
* Implementação completa do padrão CQRS com Mediator próprio (substituto do MediatR)
* `IMediator`, `ISender`, `IPublisher` - interfaces principais
* `IMediatorCommand<T>` e `IMediatorQuery<T>` - commands e queries tipados
* `IMediatorNotification` - sistema de notificações in-process
* Pipeline Behaviors completo: Logging, Performance, Validation, Caching, Transaction, Authorization, Retry
* Domain Events e Integration Events com dispatch automático
* Event Sourcing com `IEventStore`, `AggregateRoot<T>`, Snapshots e Projections
* Saga/Process Manager com compensação e timeout
* Idempotência de commands com `IIdempotentCommand`
* Scheduled Commands com background service
* Inbox/Outbox patterns para mensageria confiável

### 🔄 Modernização para .NET 9
* **HybridCache**: Cache híbrido nativo (L1 + L2) com stampede protection
* **TimeProvider**: Abstração de tempo para testes determinísticos
* **PeriodicTimer**: Timer moderno em todos os background services
* **System.Threading.RateLimiting**: Rate limiting nativo (Fixed/Sliding Window, Token Bucket)
* **System.Threading.Channels**: Producer/Consumer de alta performance
* **Microsoft.Extensions.Http.Resilience**: Resiliência HTTP nativa
* **Microsoft.Extensions.Resilience**: Resiliência genérica para DB/messaging
* **ProblemDetails (RFC 7807)**: Erros padronizados em APIs
* **TypedResults (.NET 9)**: Minimal APIs com tipagem forte
* **Source Generators**: `[LoggerMessage]` e `[JsonSerializable]` para AOT
* **OpenAPI Nativo**: `Microsoft.AspNetCore.OpenAPI` (substitui Swashbuckle)
* **Keyed Services**: Injeção de dependência por chave
* **Output Caching**: Cache de responses HTTP nativo
* **.NET Aspire 9**: Integração com stack cloud-native

### 📊 Observabilidade Moderna (ILogger + OpenTelemetry)
* Migração completa de `TelemetryHelper` para `ILogger<T>`
* OpenTelemetry Tracing com `ActivitySource` em todos os módulos
* OpenTelemetry Metrics com `Meter` (Counters, Histograms, Gauges)
* OpenTelemetry Logs integrado com `ILogger`
* Correlation ID e W3C Trace Context propagation
* `AddMvp24HoursObservability()` - configuração all-in-one
* Exporters: OTLP (Jaeger, Tempo), Console, Prometheus

### 🗄️ Entity Framework Core Avançado
* Interceptors: Audit, SoftDelete, Concurrency, CommandLogging, SlowQuery
* Multi-tenancy com query filters automáticos e `ITenantProvider`
* Criptografia de campos com value converters
* Row-level security helpers
* Bulk Operations: BulkInsert, BulkUpdate, BulkDelete
* Specification Pattern integrado com `GetBySpecificationAsync()`
* `IReadOnlyRepository<T>` para queries (sem métodos de escrita)
* Cursor-based pagination (keyset)
* Connection resiliency com retry policies
* Health checks para SQL Server, PostgreSQL, MySQL
* Read/Write splitting para read replicas
* DbContext separado para leitura vs escrita (CQRS)

### 🍃 MongoDB Avançado
* Interceptors: Audit, SoftDelete, CommandLogger
* Multi-tenancy com filtros automáticos
* Field-level encryption (CSFLE)
* Bulk operations otimizados
* Change Streams para eventos real-time
* GridFS para arquivos grandes
* Time Series Collections
* Geospatial queries
* Text search indexes
* Health checks e replica set monitoring
* Connection resiliency com circuit breaker

### 🐇 RabbitMQ Enterprise
* Consumers tipados com `IMessageConsumer<T>` (substituto MassTransit)
* Request/Response pattern com `IRequestClient<TRequest, TResponse>`
* Message Scheduling com delayed messages
* Pipeline/Middleware de consumo e publicação
* Topologia automática e convenções de naming
* Batch consumers com `IBatchConsumer<T>`
* Transactional messaging com Outbox pattern
* Sagas integration com state machines
* Multi-tenancy com virtual hosts por tenant
* API fluente `AddMvpRabbitMQ(cfg => {...})`
* Observabilidade com OpenTelemetry e métricas

### 📦 Pipeline (Pipe and Filters) Avançado
* Pipeline tipado `IPipeline<TInput, TOutput>`
* API fluente `.Pipe<TIn, TOut>().Then<TNext>().Finally()`
* `IPipelineContext` com CorrelationId, Metadata, User
* Fork/Join pattern para fluxos paralelos
* Dependency Graph entre operações
* Saga Pattern com compensação orquestrada
* Checkpoint/Resume para pipelines longos
* State Snapshots para debug/auditoria
* Métricas detalhadas por operação
* Integração com FluentValidation, Cache e OpenTelemetry

### 🌐 WebAPI Melhorado
* Exception mapping para ProblemDetails (RFC 7807)
* Rate limiting nativo com políticas por IP, User, API Key
* Idempotency middleware para POST/PUT/PATCH
* Security headers (HSTS, CSP, X-Frame-Options)
* Request/Response logging com masking de dados sensíveis
* API versioning (URL, Header, Query String)
* Health checks unificados (/health, /health/ready, /health/live)
* Minimal APIs com `MapCommand<T>()` e `MapQuery<T>()`
* Model binders para DateOnly, TimeOnly, strongly-typed IDs

### 🏗️ Application Layer
* `IApplicationService<TEntity, TDto>` com AutoMapper integrado
* `QueryService` e `CommandService` separados (CQRS light)
* Validation pipeline com FluentValidation
* Transaction scope com `[Transactional]` attribute
* Specification Pattern integrado
* `ExceptionToResultMapper` configurável
* Audit trail em operações de command
* Cache com `[Cacheable]` attribute
* `PagedResult<T>` e cursor-based pagination
* Soft delete automático

### 🔧 Infrastructure Base
* HTTP Client factory com Polly resilience
* Delegating handlers: Logging, Auth, Correlation, Telemetry, Retry, CircuitBreaker
* Distributed locking (Redis, SQL Server, PostgreSQL)
* File storage abstraction (Local, Azure Blob, S3)
* Email service (SMTP, SendGrid, Azure Communication)
* SMS service (Twilio, Azure Communication)
* Background jobs abstraction (Hangfire, Quartz)
* Secret providers (Azure KeyVault, AWS Secrets Manager)

### 💾 Caching Avançado
* `ICacheProvider` abstração unificada
* Cache patterns: Cache-Aside, Read-Through, Write-Through, Write-Behind
* Multi-level cache (L1 Memory + L2 Distributed)
* Cache tags para invalidação em grupo
* Stampede prevention com locks
* Circuit breaker para cache remoto
* Compression para valores grandes
* `[Cacheable]` e `[CacheInvalidate]` attributes

### ⏰ CronJob Melhorado
* Retry policy configurável com circuit breaker
* Overlapping execution control
* Graceful shutdown com timeout
* Health checks por job
* Métricas: execuções, duração, falhas
* OpenTelemetry spans por execução
* Job dependencies (executar após outro job)
* Distributed locking para clusters
* `ICronJobStateStore` para persistência de estado
* Pausar/resumir jobs em runtime
* Expressões CRON de 6 campos (segundos)
* Configuração via appsettings.json

### 🧱 Core Fundamentals
* Guard clauses (`Guard.Against.Null`, `Guard.Against.NullOrEmpty`, etc.)
* ValueObjects: Email, Cpf, Cnpj, Money, Address, DateRange, Percentage, PhoneNumber
* Strongly-typed IDs: `EntityId<T>` com conversores EF Core e JSON
* Functional patterns: `Maybe<T>`, `Either<TLeft, TRight>`
* Smart Enums: `Enumeration<T>` base class
* Entity interfaces: `IEntity<TId>`, `IAuditableEntity`, `ISoftDeletable`, `ITenantEntity`
* `IClock` e `IGuidGenerator` para testabilidade
* Nullable reference types em todo o framework

### 📚 Documentação Bilíngue Completa
* 50+ documentos em PT-BR e EN-US
* Seções: CQRS, Core, Observability, Modernization
* Guias de migração do MediatR e TelemetryHelper
* Diagramas de arquitetura
* Exemplos de código práticos

### 🧪 Testes
* 1000+ testes unitários
* Testes de integração com Testcontainers (SQL Server, MongoDB)
* Benchmarks de performance
* Helpers de teste: FakeLogger, FakeActivityListener, FakeMeterListener

### ⚠️ Deprecated (Será removido na próxima major)
* `TelemetryHelper` - Use `ILogger<T>`
* `TelemetryLevels` - Use `LogLevel`
* `ITelemetryService` - Use `ILogger<T>`
* `AddMvp24HoursTelemetry()` - Use `AddMvp24HoursObservability()`
* `HttpClientExtensions` - Use `AddStandardResilienceHandler()`
* `MvpExecutionStrategy` - Use `ResiliencePipeline`
* `MultiLevelCache` - Use `HybridCache`

---

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