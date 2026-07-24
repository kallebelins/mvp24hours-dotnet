# <img src="docs/_media/icon.png" width="32" height="32" alt="icon" /> Mvp24Hours - .NET 9 (v9.1.200) 🚀

Enterprise-ready library for rapid .NET application development with CQRS, Event Sourcing, Domain Events, and modern observability.

## ✨ Key Features

| Category | Features |
|----------|----------|
| **CQRS & Mediator** | Commands, Queries, Notifications, Pipeline Behaviors, Domain Events |
| **Data** | SQL Server, PostgreSQL, MySQL (EF Core), MongoDB, Redis |
| **Messaging** | RabbitMQ (Typed Consumers, Request/Response, Sagas, Scheduling) |
| **Observability** | OpenTelemetry (Tracing, Metrics, Logs), ILogger integration |
| **Resilience** | Native .NET 9 resilience, Circuit Breaker, Retry, Rate Limiting |
| **Modern .NET 9** | HybridCache, TimeProvider, Channels, TypedResults, Source Generators |
| **Patterns** | Repository, Unit of Work, Specification, Pipeline (Pipe & Filters) |

## 📦 Quick Start

```bash
dotnet add package Mvp24Hours.Core
dotnet add package Mvp24Hours.Infrastructure.Data.EFCore
dotnet add package Mvp24Hours.Infrastructure.Cqrs
dotnet add package Mvp24Hours.WebAPI
```

## 📚 Documentation

- 🌐 **Website**: [mvp24hours.dev](https://mvp24hours.dev/#/)
- 📖 **Documentation**: [English Documentation](https://mvp24hours.dev/#/en-us/home)
- 🧪 **Samples**: [mvp24hours-dotnet-samples](https://github.com/kallebelins/mvp24hours-dotnet-samples)

## 🆕 What's New in v9.1.200

- ⭐ **Complete CQRS Library** - Full Mediator implementation (MediatR replacement)
- 📊 **OpenTelemetry** - Tracing, Metrics, Logs with OTLP/Prometheus exporters
- 🔄 **.NET 9 Modernization** - HybridCache, TimeProvider, RateLimiting, Channels
- 🗄️ **Advanced EF Core** - Interceptors, Multi-tenancy, Bulk Operations
- 🐇 **Enterprise RabbitMQ** - Typed Consumers, Sagas, Scheduling
- 📦 **Advanced Pipeline** - Typed, Fork/Join, Checkpoint/Resume

See full changelog: [CHANGELOG.md](CHANGELOG.md) | [Release Notes](https://mvp24hours.dev/#/en-us/release)

## 💖 Support

If you find this library useful, consider [buying me a coffee](https://www.paypal.com/donate/?hosted_button_id=EKA2L256GJVQC) ☕

## 📄 License

MIT License - see [LICENSE](LICENSE) for details.