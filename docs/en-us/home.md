# <img  style="vertical-align:middle" width="42" height="42" src="https://kallebelins.github.io/mvp24hours-dotnet/_media/icon.png" alt="Mvp24Hours" /> Mvp24Hours - .NET 10 (v10.8.0 source) 🚀

This project was developed to contribute to the rapid construction of services with [.NET](https://learn.microsoft.com/en-us/training/dotnet/). I used the reference of market solutions for building microservices.

The English documentation is being reorganized around a fixed product information architecture. See the [Documentation Scope and Information Architecture](documentation-ia-policy.md) policy for the locked section order, module ownership, and page template.

Start here:

- [Getting Started](getting-started.md)
- [Configuration Reference](configuration-reference.md)
- [Architecture Guides](guides/architecture/home.md)
- [Infrastructure Modules](infrastructure/home.md)
- [Testing Cookbook](testing/home.md)
- [AI & MCP Resources](ai-resources/home.md)

> **Release status:** this repository targets `net10.0`, but production package
> metadata remains at `9.1.21` and the public `Mvp24Hours.Core` feed does not
> include 10.8.0. See [Release & Migration](release.md) before upgrading.

## 🎯 Characteristics

### Data & Persistence
* **Relational Databases**: SQL Server, PostgreSQL, MySQL with EF Core (Interceptors, Multi-tenancy, Bulk Operations)
* **NoSQL Databases**: MongoDB (Change Streams, GridFS, Geospatial) and Redis
* **Repository & Unit of Work**: With Specification Pattern and Cursor Pagination

### Messaging & Events
* **Message Broker**: RabbitMQ Enterprise (Typed Consumers, Request/Response, Scheduling, Sagas)
* **CQRS & Mediator**: Complete library with Commands, Queries, Notifications, Behaviors
* **Domain Events & Integration Events**: With Outbox Pattern for reliability

### Architecture & Patterns
* **Pipeline**: Pipe and Filters pattern (Typed, Fork/Join, Saga, Checkpoint/Resume)
* **Event Sourcing**: Aggregates, Event Store, Snapshots, Projections
* **Saga/Process Manager**: With compensation and timeout
* **Architecture Guides**: Decision matrix, project structures, and blueprints

### Observability & Resilience
* **OpenTelemetry**: Tracing, Metrics, Logs with OTLP, Console, Prometheus exporters
* **Resilience**: Native .NET resilience (Microsoft.Extensions.Resilience)
* **Health Checks**: SQL, MongoDB, Redis, RabbitMQ with unified endpoints

### Modern .NET 10
* **HybridCache**: L1 + L2 cache with stampede protection
* **Rate Limiting**: Native System.Threading.RateLimiting
* **Minimal APIs**: TypedResults, MapCommand/MapQuery for CQRS
* **Source Generators**: [LoggerMessage] and [JsonSerializable] for AOT
* **OpenAPI Native**: Microsoft.AspNetCore.OpenAPI
* **.NET Aspire**: Cloud-native stack integration

### Infrastructure
* **Infrastructure Modules**: Email, SMS, File Storage, Secrets, Distributed Locking, Background Jobs
* **Documentation**: Swagger/OpenAPI 3.1
* **Mapping**: AutoMapper integrated
* **Validation**: FluentValidation and Data Annotations
* **Security**: API Key auth, Rate limiting, Security headers
* **Background Jobs**: CronJob with retry, circuit breaker, distributed locking
* **Testing**: Cookbook and library fakes under [Testing](testing/home.md)

## 📚 Examples

All **32** runnable sample solutions live in [`samples/`](https://github.com/kallebelins/mvp24hours-dotnet/blob/main/samples/README.md) in this repository. They target **`net10.0`**, use local project references to `src/` by default, and include English READMEs with run instructions.

- [Sample catalog](https://github.com/kallebelins/mvp24hours-dotnet/blob/main/samples/README.md#complete-catalog) — every sample with tier, purpose, and documentation links
- [Which sample should I open first?](https://github.com/kallebelins/mvp24hours-dotnet/blob/main/samples/README.md#which-sample-should-i-open-first) — decision-matrix guidance
- [Sample testing baseline](https://github.com/kallebelins/mvp24hours-dotnet/blob/main/samples/TESTING.md) — xUnit, FluentAssertions, and Testcontainers patterns

## 🔮 Next Steps
* Implement integration with Kafka (message broker)
* Create project model with Grpc over HTTP2 (server and client)
* Create project model for gateway (YARP) with service discovery
* Record training videos for the community
* Implement GraphQL support

## ✅ Recently Completed (v10.8.0 source)
* **Platform alignment**: All production and test projects target `net10.0`; the shared language default is `latest`
* **Build quality**: Nullable enabled solution-wide and a strict warnings-as-errors Release gate
* **Dependency management**: Central Package Management in `Directory.Packages.props`
* **Security**: Patched `System.Security.Cryptography.Xml` dependency chain with zero vulnerable packages in the recorded audit
* **Modernized internals**: `Microsoft.Data.SqlClient`, current AWS credential resolution, and non-obsolete certificate/cryptography APIs
* **Distributed locking**: Corrected lock release ordering in synchronous and asynchronous disposal
* **Verification**: CI runs unit (`Category!=Integration`) and integration (`Category=Integration`) jobs, merges coverage, and enforces a **55%** line-coverage floor; product target **95%**. Current consolidated baseline: **59.4%** — see [coverage baseline](testing/coverage-baseline.md) and `./scripts/run-ci-local.ps1`

## Donations
Please consider making a donation if you think this library is useful to you or that my work is valuable. I'm happy if you can help me [buy a cup of coffee](https://www.paypal.com/donate/?hosted_button_id=EKA2L256GJVQC). :heart:

## Community
Users, interested parties, students, enthusiasts, developers, programmers [connect on LinkedIn](https://www.linkedin.com/in/kallebelins/) to closely follow our growth!

## Sponsors
Be a sponsor by choosing this project to accelerate your products.

## What's new?
See the news and updates on this project. [News](release.md)

## Have you migrated your project?
Keep track of changes to keep your code working correctly. [Migration](migration.md)