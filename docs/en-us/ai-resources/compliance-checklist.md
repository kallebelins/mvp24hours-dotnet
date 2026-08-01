# Mvp24Hours Compliance Checklist

Use this checklist when scaffolding or reviewing solutions with Mvp24Hours. Source code and tests under `src/` and `src/Tests/` override documentation when they disagree.

## Target framework and project setup

- [ ] Projects target `net10.0` (or the consuming solution's documented target).
- [ ] Nullable reference types enabled.
- [ ] Implicit usings enabled where the solution already uses them.
- [ ] No legacy `Startup.cs`; compose in `Program.cs` and focused `IServiceCollection` extensions.
- [ ] Secrets bound from environment variables, user secrets, or a secret store — never committed.

## Mvp24Hours APIs and modules

- [ ] CQRS uses Mvp24Hours Mediator: `AddMvpMediator`, `IMediatorCommand<T>`, `IMediatorQuery<T>`, handler interfaces, and `IMediator.SendAsync` — **not MediatR**.
- [ ] Package versions follow the solution policy; do not copy stale `9.*` pins from old examples.
- [ ] Web API uses native OpenAPI and ProblemDetails-friendly errors.
- [ ] Logging uses structured `ILogger<T>`; observability uses OpenTelemetry where applicable.
- [ ] Time-sensitive logic uses `TimeProvider`, not `DateTime.Now` directly in domain code.
- [ ] Public async APIs accept and honor `CancellationToken`.
- [ ] Options types use validation (`ValidateOnStart` or `IValidateOptions<T>`).

## Architecture boundaries

- [ ] Dependency flow: **WebAPI/Worker → Application → Core/Domain**; **Infrastructure → Core/Domain** only.
- [ ] Application must not reference Infrastructure or WebAPI (Complex/Blueprint tiers).
- [ ] Infrastructure is composed at the host; not referenced from Application.
- [ ] Cross-module communication uses explicit contracts, domain events, or integration events — not shared database tables across bounded contexts.

## Testing

- [ ] Test project mirrors the boundary under test (`{Product}.Test`).
- [ ] Integration smoke tests use `WebApplicationFactory<Program>` with `public partial class Program { }` in the host.
- [ ] EF-based APIs provide a factory that swaps persistence for EF Core InMemory in the `Testing` environment.
- [ ] OpenAPI smoke test: GET `/openapi/v1.json` returns a non-5xx status.
- [ ] Test naming: `Method_Scenario_Expected`.
- [ ] Traits: `[Trait("Category", "Unit")]` or `[Trait("Category", "Integration")]`.
- [ ] Testcontainers tests tagged `Integration` and skipped when Docker is unavailable.

## Verification against the library

- [ ] Public API claims match types under `src/Mvp24Hours.*/`.
- [ ] Behavior claims are backed by tests under `src/Tests/**` when available.
- [ ] Architecture shape matches a template in [`templates-manifest.json`](templates-manifest.json) or is explicitly documented as a justified deviation.

## Anti-patterns to reject

- [ ] `BuildServiceProvider()` during service registration.
- [ ] Direct `new HttpClient()` instead of `IHttpClientFactory`.
- [ ] Microservices chosen only for folder organization.
- [ ] CQRS wrapping simple CRUD without a demonstrated read/write split need.
- [ ] Event-driven integration where a single transactional call is required.
