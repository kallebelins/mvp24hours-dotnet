# Infrastructure Modules

Mvp24Hours infrastructure modules provide adapters for messaging, storage, notifications, secrets, distributed coordination, background work, HTTP clients, and cross-cutting runtime concerns. Start with the module that owns the external dependency, then follow its page for registration, options, health checks, and test doubles.

> The source currently targets .NET 10, but package publication must be verified before pinning a `10.0.0` version. The examples below intentionally omit a package version.

## Install

The modules on this page are primarily distributed by `Mvp24Hours.Infrastructure`. Data, caching, CQRS, RabbitMQ, Pipeline, WebAPI, and CronJob features have dedicated packages documented by their own guides.

```bash
dotnet add package Mvp24Hours.Infrastructure
```

## Module catalog

| Need | Start here | Primary capability |
|------|------------|--------------------|
| Message broker | [RabbitMQ](../broker.md) | Publish/consume, topology, request/reply, scheduling, and test harnesses |
| Processing pipeline | [Pipeline](../pipeline.md) | Ordered operations, validation, rollback, resilience, and advanced flows |
| Application caching | [Caching](../caching-advanced.md) | Memory, distributed, Redis, HybridCache, invalidation, and resilience |
| Email delivery | [Email](email.md) | SMTP, SendGrid, Azure Communication Services, templates, queues, and test fakes |
| SMS delivery | [SMS](sms.md) | Twilio, Azure Communication Services, throttling, validation, and test fakes |
| File storage | [File Storage](file-storage.md) | Local, Azure Blob, AWS S3, and in-memory providers |
| Secrets and security | [Secrets & Security](secrets-security.md) | Environment variables, Azure Key Vault, AWS Secrets Manager, rotation, and masking |
| Identity provider | [Keycloak](../identity/keycloak.md) | JWT bearer authentication, UMA/RPT authorization, Admin REST clients, and user sync |
| Cross-process coordination | [Distributed Locking](distributed-locking.md) | In-memory, Redis, RedLock, SQL Server, and PostgreSQL locks |
| Durable or provider-backed jobs | [Background Jobs](background-jobs.md) | In-memory, Hangfire, and Quartz job scheduling |
| Scheduled hosted services | [CronJob](../cronjob.md) | Cron-based recurring work hosted directly by the application |
| HTTP integrations | [HTTP Clients & Resilience](http-resilience.md) | Typed clients, handlers, certificates, proxies, timeout, retry, and circuit breaking |
| Operational probes | [Health Checks](health-checks.md) | Consolidated registration catalog across infrastructure modules |
| Test doubles and assertions | [Testing](../testing/home.md) | Clocks, providers, handlers, listeners, stores, containers, and messaging harnesses |

## Choose the owning module

- Use **Background Jobs** when work needs a job provider, persistence, continuations, batches, dashboards, or provider-specific scheduling.
- Use **CronJob** when a hosted service with a cron schedule and the module's overlap, resilience, state, and observability features is sufficient.
- Use **Pipeline** for an in-process operation flow. Use **CQRS** for commands, queries, notifications, and mediator behaviors.
- Use the dedicated **RabbitMQ** package for brokered application messaging; it is not part of the generic background-job abstraction.
- Use **Health Checks** as the catalog, then configure the check on the owning module page.

## Common registration shape

Each module exposes its own extension methods because provider requirements differ. Register only one default provider for an abstraction unless the module explicitly supports keyed providers.

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add the provider-specific registrations required by this application.
// See each module page for verified extension methods and options.

var app = builder.Build();
```

Avoid copying registration names between modules: several infrastructure entry points intentionally use names such as `AddEmailService`, `AddFileStorage`, `AddDistributedLocking`, or `AddBackgroundJobs` instead of the `AddMvp24Hours*` prefix.

## Configuration and validation

Most options can be configured in a DI lambda. Configuration-section binding is documented only where the source provides a binder or a verified section name.

- [Configuration Reference](../configuration-reference.md)
- [Options Validation](../core/options-validation.md)
- [Keyed Services](../modernization/keyed-services.md)

## Observability and testing

Use the module-specific health registration together with the consolidated [Health Checks](health-checks.md) catalog. For traces, metrics, and logs, start at [Observability](../observability/home.md). The [Testing](../testing/home.md) cookbook lists in-memory implementations, fakes, assertion helpers, and integration-test harnesses.

## Related documentation

- [Getting Started](../getting-started.md)
- [Application Services](../application-services.md)
- [CQRS & Mediator](../cqrs/home.md)
- [Web API Advanced](../webapi-advanced.md)
- [Modernization and native APIs](../modernization/dotnet9-features.md)
- [Architecture Guides](../guides/architecture/home.md)
