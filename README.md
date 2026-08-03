# <img src="docs/_media/icon.png" width="32" height="32" alt="icon" /> Mvp24Hours - .NET 10 (v10.0.0 source) 🚀

Enterprise-ready library for rapid .NET application development with CQRS, Event Sourcing, Domain Events, and modern observability.

## ✨ Key Features

| Category | Features |
|----------|----------|
| **CQRS & Mediator** | Commands, Queries, Notifications, Pipeline Behaviors, Domain Events |
| **Data** | SQL Server, PostgreSQL, MySQL (EF Core), MongoDB, Redis |
| **Messaging** | RabbitMQ (Typed Consumers, Request/Response, Sagas, Scheduling) |
| **Observability** | OpenTelemetry (Tracing, Metrics, Logs), ILogger integration |
| **Resilience** | Native .NET resilience, Circuit Breaker, Retry, Rate Limiting |
| **Modern .NET 10** | HybridCache, TimeProvider, Channels, TypedResults, Source Generators |
| **Patterns** | Repository, Unit of Work, Specification, Pipeline (Pipe & Filters) |

## 📦 Quick Start

```bash
dotnet add package Mvp24Hours.Core
dotnet add package Mvp24Hours.Infrastructure.Data.EFCore
dotnet add package Mvp24Hours.Infrastructure.Cqrs
dotnet add package Mvp24Hours.WebAPI
```

## 📚 Documentation

- 🌐 **Website**: [kallebelins.github.io/mvp24hours-dotnet](https://kallebelins.github.io/mvp24hours-dotnet/#/)
- 📖 **Documentation**: [English Documentation](https://kallebelins.github.io/mvp24hours-dotnet/#/en-us/home)
- 🧪 **Samples**: [`samples/`](samples/README.md) — 32 runnable .NET 10 solutions (Minimal, Simple, Complex, Blueprints, Capabilities); see the [catalog](samples/README.md#complete-catalog) and [decision matrix](samples/README.md#which-sample-should-i-open-first)

> **Release status:** the repository targets `net10.0`, but its package project metadata
> remains at `9.1.21` and the public `Mvp24Hours.Core` feed does not include 10.0.0.
> Treat 10.0.0 as the source release until metadata is finalized and publication is confirmed.

## 🆕 What's New in v10.0.0

- 🔄 **.NET 10** - All production and test projects target `net10.0`; C# defaults to `latest`
- ✅ **Strict quality gate** - Nullable enabled and Release builds run with warnings as errors
- 🔐 **Security updates** - Patched `System.Security.Cryptography.Xml` dependency chain
- 🧰 **Modernized internals** - `Microsoft.Data.SqlClient`, current AWS credential resolution, and non-obsolete cryptography APIs
- 🐛 **Distributed lock fix** - Lock handles now release the resource before marking themselves disposed
- 🧪 **Expanded test coverage** - 19 test projects on .NET 10, split unit/integration CI jobs, **55%** line-coverage floor (product target **95%**), consolidated baseline **59.4%**

See full changelog: [CHANGELOG.md](CHANGELOG.md) | [Release Notes](https://kallebelins.github.io/mvp24hours-dotnet/#/en-us/release)

## 💖 Support

If you find this library useful, consider [buying me a coffee](https://www.paypal.com/donate/?hosted_button_id=EKA2L256GJVQC) ☕

## 📄 License

MIT License - see [LICENSE](LICENSE) for details.