# <img  style="vertical-align:middle" width="42" height="42" src="../_media/icon.png" alt="Mvp24Hours" /> Mvp24Hours - NET9 (v9.1.200) 🚀

This project was developed to contribute to the rapid construction of services with [.NET](https://learn.microsoft.com/pt-br/training/dotnet/). I used the reference of market solutions for building microservices.

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

### Observability & Resilience
* **OpenTelemetry**: Tracing, Metrics, Logs with OTLP, Console, Prometheus exporters
* **Resilience**: Native .NET 9 resilience (Microsoft.Extensions.Resilience)
* **Health Checks**: SQL, MongoDB, Redis, RabbitMQ with unified endpoints

### Modern .NET 9
* **HybridCache**: L1 + L2 cache with stampede protection
* **Rate Limiting**: Native System.Threading.RateLimiting
* **Minimal APIs**: TypedResults, MapCommand/MapQuery for CQRS
* **Source Generators**: [LoggerMessage] and [JsonSerializable] for AOT
* **OpenAPI Native**: Microsoft.AspNetCore.OpenAPI
* **.NET Aspire 9**: Cloud-native stack integration

### Infrastructure
* **Documentation**: Swagger/OpenAPI 3.1
* **Mapping**: AutoMapper integrated
* **Validation**: FluentValidation and Data Annotations
* **Security**: API Key auth, Rate limiting, Security headers
* **Background Jobs**: CronJob with retry, circuit breaker, distributed locking

## 📚 Examples
You can study different solutions with the Mvp24Hours library. Visit the example projects at:
<br>https://github.com/kallebelins/mvp24hours-dotnet-samples

## 🔮 Next Steps
* Implement integration with Kafka (message broker)
* Create project model with Grpc over HTTP2 (server and client)
* Create project model for gateway (YARP) with service discovery
* Record training videos for the community
* Implement GraphQL support

## ✅ Recently Completed (v9.1.200)
* **CQRS Library**: Complete Mediator implementation (MediatR replacement)
* **OpenTelemetry**: Full observability stack (traces, metrics, logs)
* **.NET 9 Modernization**: HybridCache, TimeProvider, RateLimiting, Channels
* **EF Core Advanced**: Interceptors, Multi-tenancy, Bulk operations
* **MongoDB Advanced**: Change Streams, GridFS, Geospatial
* **RabbitMQ Enterprise**: Typed consumers, Sagas, Scheduling
* **Pipeline Advanced**: Typed, Fork/Join, Checkpoint/Resume
* **50+ Bilingual Docs**: PT-BR and EN-US documentation

## Donations
Please consider making a donation if you think this library is useful to you or that my work is valuable. I'm happy if you can help me [buy a cup of coffee](https://www.paypal.com/donate/?hosted_button_id=EKA2L256GJVQC). :heart:

## Community
Users, interested parties, students, enthusiasts, developers, programmers [connect on LinkedIn](https://www.linkedin.com/in/kallebelins/) to closely follow our growth!

## Sponsors
Be a sponsor by choosing this project to accelerate your products.

## What's new?
See the news and updates on this project. [News](release)

## Have you migrated your project?
Keep track of changes to keep your code working correctly. [Migration](migration)