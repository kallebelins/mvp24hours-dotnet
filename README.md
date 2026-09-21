# <img src="docs/_media/icon.png" width="32" height="32" alt="icon" /> Mvp24Hours - .NET 10 (v10.9.0) 🚀

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

## 🆕 What's New in v10.9.0

- 📦 **Stable publication** - `Mvp24Hours.*` packages are published on NuGet as `10.9.0`; all `.csproj` metadata (`Version`, `AssemblyVersion`, `FileVersion`) is aligned across every project
- 🧹 **Documentation cleanup** - Removed outdated "publication blocker" notices that referenced a stale `9.1.21` package line; README, release notes, and migration guides now describe a single, current, published version
- 🔄 **.NET 10 baseline carried forward** - All production and test projects target `net10.0`; C# defaults to `latest`, Nullable is enabled, and Release builds run with warnings as errors

See full changelog: [CHANGELOG.md](CHANGELOG.md) | [Release Notes](https://kallebelins.github.io/mvp24hours-dotnet/#/en-us/release)

## 💖 Support

If you find this library useful, consider [buying me a coffee](https://www.paypal.com/donate/?hosted_button_id=EKA2L256GJVQC) ☕

## 📄 License

MIT License - see [LICENSE](LICENSE) for details.