# Health Checks

This page is the registration catalog for Mvp24Hours health checks. Use it to select a check and understand its defaults; follow the linked module guide for provider setup and operational guidance.

Mvp24Hours checks build on [ASP.NET Core health checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks). Unless noted otherwise, a registration accepts the standard `failureStatus`, `tags`, and `timeout` values. The registration timeout is enforced by the health-check framework; option types may also define an internal timeout for the provider operation.

## WebAPI endpoints

Register the health-check framework and map the Mvp24Hours JSON endpoints:

```csharp
builder.Services.AddMvp24HoursHealthChecks(options =>
{
    options.EnableDetailedResponses = builder.Environment.IsDevelopment();
    options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
});

builder.Services.AddHealthChecks()
    // Add dependency checks here.
    ;

var app = builder.Build();
app.UseRouting();
app.UseMvp24HoursHealthChecks();
```

`AddMvp24HoursHealthChecks` calls `AddHealthChecks()` and registers `Mvp24Hours.WebAPI.Configuration.HealthCheckOptions`. `UseMvp24HoursHealthChecks` maps:

| Endpoint option | Default path | Predicate |
|---|---|---|
| `HealthPath` | `/health` | All registrations unless `HealthTags` is non-empty |
| `ReadinessPath` | `/health/ready` | Registrations containing any tag in `ReadinessTags`; default `ready` |
| `LivenessPath` | `/health/live` | Registrations containing any tag in `LivenessTags`; default `live` |

The response contains overall status and duration plus each entry's name, status, description, and duration. `EnableDetailedResponses` adds entry data; `IncludeExceptionDetails` adds exception messages. Caching is disabled.

The options type also declares `Timeout` (30 seconds), `AllowAnonymous` (`true`), `EnableUI` (`false`), and `UIPath` (`/health-ui`). Current endpoint mapping does not apply `Timeout` or `AllowAnonymous`. Enabling `EnableUI` maps another JSON health endpoint at `UIPath`; it does not install or render HealthChecks UI. Apply endpoint authorization and framework timeouts in the host as required. See [Web API Advanced](../webapi-advanced.md).

## Registration controls

For checks that expose them, the final registration values follow these rules:

- `failureStatus` controls the framework status when a check throws through the framework. Most Mvp24Hours checks also return explicit `Healthy`, `Degraded`, or `Unhealthy` results for their own conditions.
- Explicit `tags` replace option-default tags; they are not merged.
- The registration `timeout` is independent of option values such as `TimeoutSeconds`, `QueryTimeoutSeconds`, or connection timeouts.
- Readiness endpoints only include checks tagged `ready`. Most dependency checks default to `ready`; MongoDB, RabbitMQ, and the cache-package check do not add tags unless the caller supplies them.

## Catalog

### WebAPI cache

```csharp
services.AddMvp24HoursHealthChecks()
    .AddMvp24HoursCacheCheck(
        name: "cache",
        configureOptions: options =>
        {
            options.CheckDistributedCache = true;
            options.CheckMemoryCache = true;
        },
        tags: ["cache", "ready"],
        timeout: TimeSpan.FromSeconds(5));
```

| Item | Default |
|---|---|
| Registration | `AddMvp24HoursCacheCheck` |
| Name / failure status | `cache` / `Unhealthy` |
| Tags | `cache`, `ready` |
| Registration timeout | `null` |
| Options | `CheckDistributedCache = true`; `CheckMemoryCache = true` |
| Status rules | Missing or failed configured caches are `Unhealthy`; otherwise `Healthy` |

This check exercises `IDistributedCache` and `IMemoryCache`; Redis is covered when it is the registered `IDistributedCache`. See [Caching](../caching-advanced.md).

### EF Core and relational databases

All EF registrations are extensions in `Mvp24Hours.Extensions` and expose standard `failureStatus` (`Unhealthy`), tag, and registration-timeout parameters.

| Registration | Default name | Option type | Default internal timeout | Default thresholds | Default tags |
|---|---|---|---|---|---|
| `AddMvp24HoursDbContextCheck<TContext>` | context type name | `DbContextHealthCheckOptions` | query: 5 s | degraded 500 ms; unhealthy 2,000 ms | `db`, `database`, `efcore` |
| `AddMvp24HoursDbContextLivenessCheck<TContext>` | `{Context}-live` | `DbContextHealthCheckOptions.Liveness()` | query: 3 s | degraded 1,000 ms; unhealthy 5,000 ms | `db`, `live` |
| `AddMvp24HoursDbContextReadinessCheck<TContext>` | `{Context}-ready` | `DbContextHealthCheckOptions.Strict()` | query: 3 s | degraded 300 ms; unhealthy 1,000 ms | `db`, `database`, `efcore`, `ready` |
| `AddMvp24HoursSqlServerCheck` | `sqlserver` | `SqlServerHealthCheckOptions` | query: 5 s | degraded 500 ms; unhealthy 2,000 ms | `db`, `database`, `sqlserver`, `ready` |
| `AddMvp24HoursPostgreSqlCheck` | `postgresql` | `PostgreSqlHealthCheckOptions` | query: 5 s | degraded 500 ms; unhealthy 2,000 ms | `db`, `database`, `postgresql`, `ready` |
| `AddMvp24HoursMySqlCheck` | `mysql` | `MySqlHealthCheckOptions` | query: 5 s | degraded 500 ms; unhealthy 2,000 ms | `db`, `database`, `mysql`, `ready` |

`AddMvp24HoursDbContextAllChecks<TContext>()` adds both liveness and readiness checks. The generic check defaults to `SELECT 1`, while liveness sets `HealthQuery = null` and only calls `CanConnect`. Strict readiness checks pending migrations and degrades when any are pending.

Provider options add specialized diagnostics: SQL Server database state, blocking-session and long-query thresholds; PostgreSQL connection usage (80%), replication lag (10 MiB), database size, and blocked locks; MySQL connection usage (80%), slow queries, table locks, buffer pool, and replication lag (30 seconds). See [EF Core advanced](../database/efcore-advanced.md) and [Relational databases](../database/relational.md).

### MongoDB

| Registration | Default name | Option type and defaults | Default tags / timeout |
|---|---|---|---|
| `AddMongoDbHealthCheck` | `mongodb` | `MongoDbHealthCheckOptions`: database-access verification off, server status off, connection timeout 5 s, server-selection timeout 5 s | none / `null` |
| `AddMongoDbReplicaSetHealthCheck` | `mongodb-replicaset` | `MongoDbReplicaSetHealthCheckOptions`: minimum secondaries 0, maximum lag 0 (disabled), allow unhealthy members, disallow standalone, include member details, connection and selection timeouts 5 s | none / `null` |
| `AddMongoDbHealthChecks` | both names above | Adds connectivity and replica-set checks | caller-supplied tags; no registration timeout parameter |

Connectivity overloads accept either registered `MongoDbOptions` or explicit connection/database values. Replica-set overloads accept registered options or an explicit connection string. `failureStatus` is nullable and is passed to `HealthCheckRegistration`; with no override, framework failure behavior applies. The checks do not define response-time degraded/failure thresholds. Supply `tags: ["database", "ready"]` if MongoDB should participate in the default readiness endpoint. See [MongoDB advanced](../database/mongodb-advanced.md).

### Cache package and Redis

`Mvp24Hours.Infrastructure.Caching` has a separate `ICacheProvider` check:

```csharp
services.AddHealthChecks()
    .AddCacheHealthCheck(
        name: "cache-provider",
        tags: ["cache", "ready"],
        configure: options =>
        {
            options.MaxOperationDurationMs = 1000;
            options.TestKeyPrefix = "health_check_";
        });
```

| Item | Default |
|---|---|
| Registration | `AddCacheHealthCheck` |
| Name / failure status | `cache` / framework default when `null` |
| Tags / registration timeout | none / no timeout parameter |
| Options | `MaxOperationDurationMs = 1,000`; `TestKeyPrefix = "health_check_"`; detailed diagnostics off |
| Status rules | A slow set/get/remove is `Degraded`; wrong value or exception is `Unhealthy`; a key still present after remove is `Degraded` |

The check writes, reads, removes, and verifies a temporary key through the active `ICacheProvider`, so it also covers Redis-backed providers. It has one degraded operation-duration threshold and no separate unhealthy duration threshold. See [Caching](../caching-advanced.md).

### RabbitMQ

```csharp
services.AddMvp24HoursRabbitMQHealthCheck(
    name: "rabbitmq",
    tags: ["messaging", "ready"]);
```

| Item | Default |
|---|---|
| Registration | `AddMvp24HoursRabbitMQHealthCheck` |
| Name / tags | `rabbitmq` / none |
| Timeout / options type | no registration timeout parameter / none |
| Status rules | Connected plus channel creation, or a successful reconnect, is `Healthy`; failed reconnect or exception is `Unhealthy` |

`AddMvp24HoursRabbitMQAdvanced` adds this check by default because `RabbitMQAdvancedOptions.EnableHealthCheck = true`; its default name is `rabbitmq` and tags are `null`. The check has no latency thresholds. See [RabbitMQ](../broker.md) and [RabbitMQ advanced](../broker-advanced.md).

### File storage

| Item | Default |
|---|---|
| Registration | `AddFileStorageHealthCheck` |
| Name / failure status | `file-storage` / `Unhealthy` |
| Tags / registration timeout | `file-storage`, `storage`, `ready` / `null` |
| Options | generated test path; content `Health check test content`; internal timeout 10 s; content verification enabled |
| Thresholds | degraded 1,000 ms; unhealthy 5,000 ms |

The check uploads, verifies existence, downloads, verifies content, and deletes a test file. Any operation or content failure is `Unhealthy`; total duration applies the thresholds. It is a write probe and therefore needs permission to create and delete its test object. Start at [Infrastructure modules](home.md) for provider setup.

### Email

| Item | Default |
|---|---|
| Registration | `AddEmailServiceHealthCheck` |
| Name / failure status | `email-service` / `Unhealthy` |
| Tags / registration timeout | `email`, `email-service`, `ready` / `null` |
| Options | `SendTestEmail = false`; recipient/subject/body `null`; internal timeout 10 s |
| Thresholds | degraded 2,000 ms; unhealthy 10,000 ms |

With sending disabled, the check only resolves `IEmailService` and returns `Healthy`; thresholds are evaluated only when a test message is sent. Enabling the probe sends a real message, can incur cost, and uses fallback content when fields are null. See [Email](email.md).

### SMS

| Item | Default |
|---|---|
| Registration | `AddSmsServiceHealthCheck` |
| Name / failure status | `sms-service` / `Unhealthy` |
| Tags / registration timeout | `sms`, `sms-service`, `ready` / `null` |
| Options | `SendTestSms = false`; recipient/body `null`; internal timeout 10 s |
| Thresholds | degraded 2,000 ms; unhealthy 10,000 ms |

With sending disabled, the check only resolves `ISmsService` and returns `Healthy`; thresholds are evaluated only when a test SMS is sent. Enabling it sends a real message and can incur provider charges. Start at [Infrastructure modules](home.md) for provider setup.

### Typed HTTP clients

```csharp
services.AddHealthChecks()
    .AddHttpClientHealthCheck<CatalogApi>(
        name: "catalog-api",
        configureOptions: options =>
        {
            options.HealthEndpoint = "/health";
            options.ExpectedStatusCode = HttpStatusCode.OK;
        },
        tags: ["http", "ready"],
        timeout: TimeSpan.FromSeconds(6));
```

| Item | Default |
|---|---|
| Registration | `AddHttpClientHealthCheck<TApi>` |
| Name / failure status | `httpclient-{TApi.Name}` / `Unhealthy` |
| Tags / registration timeout | `http`, `httpclient`, `ready` / `null` |
| Options | endpoint `/health`; internal timeout 5 s; expected `200 OK`; GET; content validation off |
| Thresholds | degraded 500 ms; unhealthy 2,000 ms |

Unexpected status, timeout, request failure, or unhealthy duration is `Unhealthy`; degraded duration or enabled content mismatch is `Degraded`. Register each typed `ITypedHttpClient<TApi>` separately. Start at [Infrastructure modules](home.md) for HTTP provider setup.

### Distributed locks

| Item | Default |
|---|---|
| Registration | `AddDistributedLockHealthCheck` |
| Name / failure status | `distributed-lock` / `Unhealthy` |
| Tags / registration timeout | `distributed-lock`, `locking`, `ready` / `null` |
| Options | default provider; acquisition timeout 5 s; lock duration 10 s |
| Thresholds | degraded 500 ms; unhealthy 2,000 ms |

The check acquires and releases a uniquely named lock. Acquisition failure is `Unhealthy`; release failure is `Degraded`; total duration applies the thresholds. See [Distributed Locking](distributed-locking.md).

### Background jobs

| Item | Default |
|---|---|
| Registration | `AddBackgroundJobHealthCheck` |
| Name / failure status | `background-jobs` / `Unhealthy` |
| Tags / registration timeout | `background-jobs`, `jobs`, `scheduler`, `ready` / `null` |
| Options | `ScheduleTestJob = false`; internal timeout 5 s |
| Declared thresholds | degraded 1,000 ms; unhealthy 5,000 ms |

The current implementation returns `Healthy` after resolving `IJobScheduler`; it does not contact provider storage. Setting `ScheduleTestJob = true` still does not schedule a job, and the declared latency thresholds are not evaluated. See [Background Jobs](background-jobs.md) for provider status.

### Keycloak

```csharp
using Mvp24Hours.Infrastructure.Identity.Keycloak.HealthChecks;

services.AddHealthChecks()
    .AddKeycloakHealthCheck(
        name: "keycloak",
        tags: ["identity", "ready"],
        timeout: TimeSpan.FromSeconds(5));
```

| Item | Default |
|---|---|
| Registration | `AddKeycloakHealthCheck` |
| Name / failure status | `keycloak` / framework default when `null` |
| Tags / registration timeout | none / `null` |
| Options | none; uses registered `IKeycloakDiscoveryService` |
| Status rules | Successful OIDC discovery is `Healthy`; discovery failures map to the registration failure status |

Register Keycloak authentication or services first so discovery is available. The check verifies metadata reachability; it does not prove Admin API permissions or authorization policies. See [Keycloak](../identity/keycloak.md).

### CronJob

```csharp
services.AddHealthChecks()
    .AddCronJobHealthCheck(
        options =>
        {
            options.MaxFailureRate = 0.10;
            options.CriticalFailureRate = 0.50;
            options.MaxExecutionAge = TimeSpan.FromHours(2);
        },
        name: "cronjobs",
        tags: ["cronjob", "ready"]);
```

| Item | Default |
|---|---|
| Registration | `AddCronJobHealthCheck` (with or without options lambda) |
| Name / failure status | `cronjobs` / `Unhealthy` |
| Tags / registration timeout | `cronjob`, `scheduled`, `background` / `null` |
| Options | maximum failure rate 10%; critical rate 50%; minimum executions 10; maximum execution age 2 h; recent-failure window 15 min |
| Status rules | Open circuit or unexpectedly stopped job is `Unhealthy`; elevated failure rate, stale execution, or recent failure is `Degraded`; rate above 50% is `Unhealthy` |

The extension ensures `CronJobMetricsService` is registered. No metrics service and no registered jobs both return `Healthy`. `CriticalJobs` and `IgnoreStoppedJobs` are present on `CronJobHealthCheckOptions`, but the current evaluation method does not consult them. See [CronJob observability](../cronjob-observability.md), [CronJob advanced configuration](../cronjob-advanced.md), and [CronJob resilience](../cronjob-resilience.md).

## Aggregate infrastructure registration

`AddInfrastructureHealthChecks` conditionally adds distributed lock, file storage, email, SMS, and background-job checks when their required service interface is already registered:

```csharp
services.AddHealthChecks()
    .AddInfrastructureHealthChecks(options =>
    {
        options.FileStorage.TimeoutSeconds = 10;
        options.Email.SendTestEmail = false;
        options.Sms.SendTestSms = false;
        options.BackgroundJobs.ScheduleTestJob = false;
    });
```

It does not add HTTP checks because each requires a typed client. It also does not add WebAPI cache, `ICacheProvider`, EF Core, MongoDB, RabbitMQ, CronJob, or Keycloak checks; register those explicitly.

## Probe design guidance

- Put process-only checks on liveness. Avoid external dependencies there so an outage does not cause restart loops.
- Put dependencies required to accept traffic on readiness and tag them `ready`.
- Set the framework registration timeout slightly above the check's internal timeout so the check can return its diagnostic result first.
- Keep write probes isolated. File, cache, lock, email, and SMS checks can mutate external systems; use dedicated keys, paths, recipients, and least-privilege credentials.
- Keep detailed data and exception messages off public production endpoints. Mvp24Hours endpoint options do not configure authorization for you.

## Related pages

- [Infrastructure modules](home.md)
- [Web API Advanced](../webapi-advanced.md)
- [Observability](../observability/home.md)
- [Caching](../caching-advanced.md)
- [EF Core advanced](../database/efcore-advanced.md)
- [MongoDB advanced](../database/mongodb-advanced.md)
- [RabbitMQ](../broker.md)
- [Background Jobs](background-jobs.md)
- [Keycloak](../identity/keycloak.md)
- [CronJob observability](../cronjob-observability.md)
