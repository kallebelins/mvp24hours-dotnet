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
