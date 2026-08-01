---
templateId: clean-architecture
tier: Blueprint
shape: blueprint
layers: [Domain, Application, Infrastructure, WebAPI]
dependencyRule: Domain <- Application <- Infrastructure; inward dependency rule
samplePath: samples/src/complex-clean-architecture-customer-api
mvp24hoursModules: [core, cqrs, application-services, testing]
---

# Clean Architecture Blueprint

Clean Architecture applies an inward dependency rule: domain policy does not depend on infrastructure or delivery frameworks.

```text
Domain <- Application <- Infrastructure
                     <- WebAPI / Worker composition
```

The Domain project owns entities, value objects, invariants, and domain events. Application owns use cases and ports. Infrastructure implements persistence and external-service ports. Hosts own DI, middleware, configuration, and endpoint concerns.

Use the Mvp24Hours Mediator for request dispatch when CQRS is useful. The current API is `AddMvpMediator`, `IMediatorCommand<T>`, `IMediatorQuery<T>`, and Mvp24Hours handler interfaces—not MediatR.

Prefer `TimeProvider`, `ILogger<T>`, OpenTelemetry, native OpenAPI, and current `Program.cs` composition. Add abstractions only at real boundaries; do not hide EF Core or every framework API behind interfaces without a testing or substitution need.

See [Project Structure](../project-structure.md), [CQRS](template-cqrs.md), [Core & Domain](../../../core/home.md), and [Testing](../../../testing/home.md).

> **Sample:** [`complex-clean-architecture-customer-api`](../../../../../samples/src/complex-clean-architecture-customer-api/README.md) — inward dependency rule with Domain, Application, Infrastructure, and WebAPI projects plus a dependency diagram.
