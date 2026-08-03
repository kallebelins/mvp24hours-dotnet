---
templateId: complex-nlayers
tier: Complex
shape: structure
layers: [Module.Domain, Module.Application, Module.Infrastructure, Module.Contracts, Host.WebAPI, Tests]
dependencyRule: Module boundaries; Application must not reference Infrastructure
samplePath: samples/src/complex-crud-ef-customer-api
templatePath: templates/blueprints/complex-nlayers
mvp24hoursModules: [core, application-services, database/efcore-advanced, cqrs, testing]
---

---
templateId: complex-nlayers
tier: Complex
shape: Complex N-Layers modular monolith
dependencyRule: WebAPI -> Application -> Core; Infrastructure -> Core; composed at host
samplePath: samples/src/complex-crud-ef-customer-api
templatePath: templates/blueprints/complex-nlayers
mvp24hoursModules:
  - webapi
  - database
  - application-services
  - observability
layers:
  - Core
  - Application
  - Infrastructure
  - WebAPI
  - Tests
---

# Complex N-Layers Structure

Use this shape for a modular monolith with substantial domain and application boundaries. Organize by business module inside each layer so the solution can evolve without becoming one global Core/Application bucket.

```text
Solution/
├── Modules/
│   ├── Sales/
│   │   ├── Sales.Domain/
│   │   ├── Sales.Application/
│   │   ├── Sales.Infrastructure/
│   │   └── Sales.Contracts/
│   └── Billing/
│       └── ...
├── Hosts/
│   ├── Product.WebAPI/
│   └── Product.Worker/
├── BuildingBlocks/
│   └── narrowly shared technical abstractions
└── Tests/
    ├── Unit/
    └── Integration/
```

Each module owns its model and persistence boundary. Cross-module work should use explicit application contracts, domain events inside a boundary, or integration events where eventual consistency is acceptable.

Add [CQRS](../blueprints/template-cqrs.md), [event-driven integration](../blueprints/template-event-driven.md), or [DDD](../blueprints/template-ddd.md) per module; they are not mandatory for the whole solution.

Canonical references: [CQRS](../../../cqrs/home.md), [EF Core Advanced](../../../database/efcore-advanced.md), [RabbitMQ](../../../broker.md), [Application Services](../../../application-services.md), and [Testing](../../../testing/home.md).

> **Sample:** [`complex-crud-ef-customer-api`](https://github.com/kallebelins/mvp24hours-dotnet/blob/main/samples/src/complex-crud-ef-customer-api/CustomerAPI.WebAPI/README.md) — canonical Complex EF Customer API.
>
> **Template:** [`templates/blueprints/complex-nlayers`](https://github.com/kallebelins/mvp24hours-dotnet/tree/main/templates/blueprints/complex-nlayers) — compilable `Item` scaffold.
