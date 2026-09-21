# Configuration reference

This page is the entry point for Mvp24Hours Options types and configuration shapes. It indexes the owning module pages instead of repeating their property tables.

> A public `*Options` class does not automatically define an `appsettings.json` section. A JSON section is shown here only when Mvp24Hours source binds that section, or when an application explicitly reads a connection string and passes it to a library API.

For the complete frozen list of approximately 250 public Options types, presets, DI entry points, and name collisions, see the [Options and DI inventory](documentation-options-inventory.md). For property defaults and behavior, follow the canonical module page linked below.

## Options index

### Core, observability, and Aspire

- `AspireOptions`, `AspireTelemetryOptions`, `AspireHealthCheckOptions`, `AspireResilienceOptions`, and Aspire component options: [Aspire integration](modernization/aspire.md)
- `LoggingOptions`, `MetricsOptions`, `TracingOptions`, `ObservabilityOptions`, and OpenTelemetry exporter options: [Observability](observability/home.md), [Logging](observability/logging.md), [Tracing](observability/tracing.md), [Metrics](observability/metrics.md), and [Exporters](observability/exporters.md)
- `MvpChannelOptions` and `ProducerConsumerOptions`: [Channels](modernization/channels.md)
- `NativeRateLimiterOptions`: [Rate limiting](modernization/rate-limiting.md)
- `CacheEntryOptions`, Pipe resilience contracts, `BulkOperationOptions`, and `EncryptionOptions`: use the owning feature page; names such as `RetryOptions` and `CircuitBreakerOptions` occur in more than one namespace and must be qualified. See the [inventory collision list](documentation-options-inventory.md#ambiguous-apis).
- Options registration and validation helpers: [Options validation](core/options-validation.md)

### Application

`ApplicationModuleOptions`, application observability/event options, `ExceptionMappingOptions`, `OperationMetricsOptions`, `PaginationOptions`, query-cache options, `TransactionScopeOptions`, and validation options are indexed under [Application Services](application-services.md).

### Data and persistence

- EF Core repository, resilience, observability, CQRS, migrations, schema validation, read/write splitting, health checks, tenant, encryption, and testing options: [EF Core advanced](database/efcore-advanced.md)
- Relational provider setup and the test-proven `DataContext` connection-string key: [Relational databases](database/relational.md)
- `MongoDbOptions`, repository/resiliency options, concerns, collation, sharding, pools, authentication, observability, health checks, bulk operations, and test options: [MongoDB advanced](database/mongodb-advanced.md) and [NoSQL databases](database/nosql.md)

### CQRS and mediator

`MediatorOptions`, `MediatorCacheOptions`, `InboxOutboxOptions`, native CQRS resilience, scheduling, saga, projection, and event-sourcing options are owned by the [CQRS API reference](cqrs/api-reference.md), [Behaviors](cqrs/behaviors.md), [Inbox/outbox](cqrs/resilience/inbox-outbox.md), [Scheduled commands](cqrs/scheduled-commands.md), [Sagas](cqrs/saga/home.md), and [Event sourcing](cqrs/event-sourcing/home.md).

### RabbitMQ

Connection/client/hosted options, outbox, batching, prefetch, publisher confirms, priority, TTL, deduplication, scheduling, request clients, filters, topology, tenancy, and test-harness options are owned by [Message Broker](broker.md), [RabbitMQ advanced](broker-advanced.md), and [CQRS RabbitMQ integration](cqrs/integration-rabbitmq.md).

### Pipeline

`PipelineOptions`, `PipelineAsyncOptions`, resilience, dead-letter, observability, health, fork/join, checkpoint, dependency graph, saga, validation, telemetry, and cache-operation options are owned by [Pipeline](pipeline.md).

### Caching and Redis

`CacheOptions`, `MvpCachingOptions`, `MvpHybridCacheOptions`, multi-level/resilience/write-behind options, health/observability options, and EF Core cache options are owned by [Caching advanced](caching-advanced.md), [HybridCache](modernization/hybrid-cache.md), and [CQRS caching integration](cqrs/integration-caching.md).

### CronJob

`CronJobOptions<T>`, `CronJobGlobalOptions`, advanced options, and health-check options are owned by [CronJob](cronjob.md), [Advanced configuration](cronjob-advanced.md), [Resilience](cronjob-resilience.md), and [Observability](cronjob-observability.md).

### Identity

`KeycloakOptions`, `KeycloakAuthorizationOptions`, and `KeycloakAdminOptions` are owned by [Keycloak](identity/keycloak.md). The default bound sections are `Keycloak`, `Keycloak:Authorization`, and `Keycloak:Admin`.

### Infrastructure modules

Start from the [Infrastructure Modules](infrastructure/home.md) catalog, then open the owning page for property tables and DI examples:

- Email: `EmailOptions`, SMTP, SendGrid, Azure Communication Email, templates, bulk sending, queues, rate limiting, and health checks — [Email](infrastructure/email.md)
- SMS: `SmsOptions`, Twilio, Azure Communication SMS, rate limiting, and health checks — [SMS](infrastructure/sms.md)
- File storage: base and health-check options — [File Storage](infrastructure/file-storage.md)
- Secrets: environment variables, Azure Key Vault, AWS Secrets Manager, encryption, and masking — [Secrets & Security](infrastructure/secrets-security.md)
- Distributed locking: provider and health-check options — [Distributed Locking](infrastructure/distributed-locking.md)
- Background jobs: base jobs, Hangfire, Quartz, continuations, batches, parent/child jobs, and health checks — [Background Jobs](infrastructure/background-jobs.md)
- HTTP, resilience, and related handler options — [HTTP Clients & Resilience](infrastructure/http-resilience.md)
- Test infrastructure options and fakes — [Testing cookbook](testing/home.md)
- Consolidated health-check registrations — [Health Checks](infrastructure/health-checks.md)

A public Options type still does not imply a fixed `appsettings.json` section. Prefer the module page and the [inventory](documentation-options-inventory.md) when a binder is not shown.

### Web API

Exception handling, correlation IDs, security headers, ETags, rate limiting, API versioning, health routes, Problem Details, idempotency, caching, compression, content negotiation, request limits, sanitization, API-key authentication, CORS, telemetry, IP filtering, OpenAPI, antiforgery, request context, and request logging options are owned by [Web API advanced](webapi-advanced.md).

## Validated configuration templates

### Connection strings

`ConnectionStrings` is the standard .NET configuration container. The library APIs receive the selected string; they do not infer a database provider from the key.

The SQL Server, PostgreSQL, and MySQL test applications all read `ConnectionStrings:DataContext`. The Redis test application reads `ConnectionStrings:RedisDbContext`:

```json
{
  "ConnectionStrings": {
    "DataContext": "<SQL Server, PostgreSQL, or MySQL connection string>",
    "RedisDbContext": "localhost:6379"
  }
}
```

Use only the relational provider value required by the application. Do not store production credentials in a committed settings file.

The MongoDB module has no fixed connection-string section. The canonical MongoDB guide uses an application-owned `MongoDb` key and passes it to `MongoDbOptions`:

```json
{
  "ConnectionStrings": {
    "MongoDb": "mongodb://localhost:27017"
  }
}
```

```csharp
string connectionString = builder.Configuration.GetConnectionString("MongoDb")
    ?? throw new InvalidOperationException("ConnectionStrings:MongoDb is required.");

builder.Services.AddMvp24HoursDbContext(options =>
{
    options.ConnectionString = connectionString;
    options.DatabaseName = "customers";
});
```

`MongoDb` is therefore an application convention, not an automatically bound Mvp24Hours section.

### Base Email and SMS options

`AddMvpInfrastructure(configuration)` binds the explicit `Infrastructure` section to `InfrastructureOptions`. Its registration path currently copies only the base Email and SMS properties shown below:

```json
{
  "Infrastructure": {
    "Email": {
      "DefaultFrom": "noreply@example.com",
      "DefaultReplyTo": "support@example.com",
      "MaxAttachmentSize": 26214400
    },
    "Sms": {
      "DefaultFrom": "MyCompany",
      "DefaultCountryCode": "US"
    }
  }
}
```

```csharp
builder.Services.AddMvpInfrastructure(builder.Configuration);
```

SMTP, SendGrid, Azure Communication Email, Twilio, and Azure Communication SMS provider options are registered through provider-specific code APIs. Do not place credentials under guessed `Email`, `Smtp`, `Sms`, or provider sections and expect automatic binding.

### CronJob

CronJob source declares `CronJobs:Global` and computes each job path as `CronJobs:{JobTypeName}`:

```json
{
  "CronJobs": {
    "Global": {
      "DefaultTimeZone": "UTC",
      "EnableObservability": true,
      "EnableHealthChecks": true,
      "ValidateCronExpressionsOnStartup": true
    },
    "CleanupJob": {
      "CronExpression": "0 2 * * *",
      "TimeZone": "UTC",
      "Enabled": true,
      "EnableRetry": true,
      "MaxRetryAttempts": 3,
      "PreventOverlapping": true
    }
  }
}
```

```csharp
builder.Services.AddCronJobGlobalOptionsFromConfiguration(builder.Configuration);
builder.Services.AddCronJobFromConfiguration<CleanupJob>(builder.Configuration);
```

The job section name must exactly match `typeof(T).Name`. Multiple instances bind below `CronJobs:{JobTypeName}:Instances`; see [CronJob advanced](cronjob-advanced.md).

### Aspire

`AddMvp24HoursAspireDefaults()` binds the explicit `Aspire` section before applying a code configuration delegate:

```json
{
  "Aspire": {
    "ServiceName": "orders-api",
    "ServiceVersion": "1.0.0",
    "Environment": "Production",
    "EnableOpenTelemetry": true,
    "EnableHealthChecks": true,
    "EnableResilience": true,
    "EnableServiceDiscovery": true,
    "OtlpEndpoint": "http://localhost:4317",
    "Telemetry": {
      "EnableLogging": true,
      "EnableTracing": true,
      "EnableMetrics": true,
      "TraceSamplingRatio": 1.0
    },
    "HealthChecks": {
      "LivenessPath": "/health/live",
      "ReadinessPath": "/health/ready",
      "StartupPath": "/health/startup",
      "TimeoutSeconds": 5
    },
    "Resilience": {
      "EnableRetry": true,
      "EnableCircuitBreaker": true,
      "EnableTimeout": true,
      "MaxRetryAttempts": 3,
      "TimeoutSeconds": 30
    }
  }
}
```

```csharp
builder.AddMvp24HoursAspireDefaults();
```

The overload accepting a section name can bind a custom path, but `Aspire` is the built-in default.

## Code-only configuration

The following modules expose Options classes and DI delegates but no Mvp24Hours `IConfiguration` binder or section constant. Configure them in code, or explicitly bind an application-owned section yourself.

### RabbitMQ

```csharp
string connectionString = builder.Configuration.GetConnectionString("RabbitMQ")
    ?? throw new InvalidOperationException("ConnectionStrings:RabbitMQ is required.");

builder.Services.AddMvpRabbitMQ(connectionString, options =>
{
    options.ConfigureClient(client =>
    {
        client.PublisherConfirm.Enabled = true;
        client.ConsumerPrefetch.PrefetchCount = 32;
    });
});
```

`RabbitMQ` above is selected by application code. There is no built-in `RabbitMQ` Options section binder.

### Web API middleware

```csharp
builder.Services.AddMvp24HoursSecurityHeaders(options =>
{
    options.EnableHsts = true;
});

builder.Services.AddMvp24HoursRateLimiting(options =>
    options.AddDefaultPolicy(100, TimeSpan.FromMinutes(1)));
```

Each feature has its own registration delegate. There is no common built-in `Mvp24Hours:WebAPI` or `WebAPI` section.

### EF Core resilience

```csharp
string connectionString = builder.Configuration.GetConnectionString("DataContext")
    ?? throw new InvalidOperationException("ConnectionStrings:DataContext is required.");

builder.Services.AddMvp24HoursDbContextWithResilience<AppDbContext>(
    connectionString,
    resilience =>
    {
        resilience.EnableRetryOnFailure = true;
        resilience.MaxRetryCount = 5;
        resilience.EnableCircuitBreaker = true;
    });
```

The connection string is read from configuration, but `EFCoreResilienceOptions` itself is configured by the delegate. There is no built-in `EFCoreResilience` section.

## Binding an application-owned section

When no library binder exists, an application may deliberately define and bind its own section:

```csharp
IConfigurationSection section = builder.Configuration.GetSection("MyApplication:RabbitMQ");

builder.Services.AddOptions<RabbitMQClientOptions>()
    .Bind(section)
    .ValidateOnStart();
```

That section belongs to the application. Document it locally and do not present it as an Mvp24Hours convention. See [Options validation](core/options-validation.md) for validation helpers.

## Reading configuration outside the host: `ConfigurationHelper` is deprecated

`Mvp24Hours.Helpers.ConfigurationHelper` (`Mvp24Hours.Infrastructure`) is `[Obsolete]` since 10.8.0 and will be removed in v12. It is a process-wide service locator for configuration:

- it holds two pieces of static mutable state (the host environment and the built `IConfigurationRoot`), shared by every host in the process and impossible to isolate per test;
- when nothing has been set, `AppSettings` builds its own configuration by reading `appsettings.json` from `Directory.GetCurrentDirectory()` — the process working directory, not necessarily the content root;
- that self-built configuration ignores every source the host already composed: environment variables, user secrets, command line, and secret stores.

Bind at the host and inject instead:

```csharp
// Before (deprecated)
string? cs = ConfigurationHelper.AppSettings.GetConnectionString("DataContext");
MySettings? settings = ConfigurationHelper.GetSettings<MySettings>("MyApplication:MySection");
IConfigurationSection? section = ConfigurationHelper.GetSection("MyApplication:MySection");

// After — Program.cs
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DataContext")));

builder.Services.AddOptions<MySettings>()
    .Bind(builder.Configuration.GetSection("MyApplication:MySection"))
    .ValidateOnStart();

// After — consumer
public sealed class MyService(IOptions<MySettings> options, IConfiguration configuration)
{
    private readonly MySettings _settings = options.Value;
}
```

`SetEnvironment(IHostEnvironment)` and `SetConfiguration(IConfiguration)` have no replacement because they have no purpose once the host owns configuration: inject `IHostEnvironment` and `IConfiguration` where you need them.

## Related

- [Getting Started](getting-started.md)
- [Infrastructure Modules](infrastructure/home.md)
- [Architecture Guides](guides/architecture/home.md)
- [Options Validation](core/options-validation.md)
- [Options and DI Inventory](documentation-options-inventory.md)
