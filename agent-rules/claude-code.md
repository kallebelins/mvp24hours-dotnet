# Mvp24Hours agent context

This project consumes the [Mvp24Hours](https://github.com/kallebelins/mvp24hours-dotnet) .NET library via NuGet. It is **not** the Mvp24Hours repository — do not assume paths like `docs/`, `mcp/`, `src/Tests/`, or `skills/` exist here as the library's own repo layout. If Mvp24Hours skill files were installed in this project, use them; otherwise fall back to general .NET knowledge and this file's guidance.

## Start here

- Whenever a task touches Mvp24Hours (architecture choice, CQRS, EF Core, RabbitMQ, pipelines, caching, observability, identity, testing, modernization, etc.), call **`@skill-router`** first. It classifies the request and hands off to the right specialist skill, or asks one clarifying question when more than one path fits.
- Do not pick a specialist skill yourself when the choice is ambiguous — let `@skill-router` decide.
- If skills are not installed in this project, say so and proceed with general .NET/Mvp24Hours knowledge instead of guessing at unavailable tools.

## What you can explore via skills

`@skill-router` hands off to 35 domain skills across these areas — mention the topic and let it route:

- **Architecture** — new project shape (Minimal/Simple/Complex), Clean Architecture, DDD, Hexagonal, event-driven style, microservices
- **Data & Persistence** — choosing EF Core vs MongoDB vs Redis vs Dapper, and deep implementation for each
- **Messaging** — RabbitMQ patterns, advanced broker features, saga/compensation
- **System Integration** — sync vs async with partner systems, webhooks, BFF, anti-corruption adapters
- **CQRS & Mediator** — command/query split, the Mvp24Hours mediator (`AddMvpMediator`, not MediatR), event sourcing
- **Observability & Resilience** — OpenTelemetry, circuit breakers, retries, timeouts
- **Pipeline** — pipes-and-filters workflows, rollback, checkpoints
- **Caching** — HybridCache, L1/L2, invalidation strategy
- **Infrastructure** — email, SMS, file storage, secrets, distributed locks
- **Web API** — HTTP host composition, OpenAPI/versioning, Problem Details
- **Testing** — test pyramid, fakes, Testcontainers
- **Identity & Security** — Keycloak/JWT, secrets, PII, encryption
- **CronJob** — scheduled hosted services
- **Modernization** — legacy analysis, architecture migration, porting foreign code, .NET 10 native APIs

## Accuracy rules

- Verify the Mvp24Hours package versions actually referenced in this project (e.g. its `.csproj`/`Directory.Packages.props`) before assuming a specific release's APIs or defaults.
- For CQRS use Mvp24Hours APIs: `AddMvpMediator`, `IMediatorCommand<T>`, `IMediatorQuery<T>`, their handler interfaces, and `IMediator.SendAsync` — never MediatR's `IRequest<T>`.
- Prefer current `Program.cs` composition, native OpenAPI, `ILogger<T>`, OpenTelemetry, health checks, `TimeProvider`, and native .NET resilience over older patterns.
- Treat the Mvp24Hours GitHub repository (`https://github.com/kallebelins/mvp24hours-dotnet`) and its published docs site as the source of truth for exact APIs — not assumptions carried over from other libraries.
