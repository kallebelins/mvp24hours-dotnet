---
templateId: cqrs
tier: Blueprint
shape: blueprint
layers: [Core, Application, Infrastructure, WebAPI]
dependencyRule: Core <- Application <- Infrastructure; use AddMvpMediator not MediatR
samplePath: samples/src/complex-cqrs-ef-customer-api
templatePath: templates/blueprints/cqrs
mvp24hoursModules: [cqrs, application-services, database, webapi, testing]
---

# CQRS Blueprint

CQRS separates state-changing commands from read-only queries. Use it when the models, scaling needs, authorization, validation, or cross-cutting behavior genuinely differ.

## Mvp24Hours shape

```csharp
public sealed record CreateOrderCommand(string Customer)
    : IMediatorCommand<OrderDto>;

public sealed class CreateOrderHandler
    : IMediatorCommandHandler<CreateOrderCommand, OrderDto>
{
    public Task<OrderDto> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        // Coordinate domain work and persistence.
        throw new NotImplementedException();
    }
}
```

```csharp
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<CreateOrderHandler>();
    options.WithDefaultBehaviors();
});
```

Inject `IMediator` and call `SendAsync`. Use `IMediatorCommand<T>`, `IMediatorQuery<T>`, and their Mvp24Hours handler interfaces. Do not use `IRequest<T>`, `IRequestHandler<,>`, `ISender`, `AddMediatR`, or MediatR pipeline APIs in this blueprint.

## Suggested feature layout

```text
Application/
└── Orders/
    ├── Commands/CreateOrder/
    ├── Queries/GetOrder/
    ├── Contracts/
    └── Validation/
```

Keep transaction boundaries in command handling. Queries must not introduce side effects. Add caching, validation, retries, idempotency, and inbox/outbox through documented mediator integrations instead of copying custom behaviors.

See [CQRS Getting Started](../../../cqrs/getting-started.md), [Behaviors](../../../cqrs/behaviors.md), [Queries](../../../cqrs/queries.md), and [Inbox/Outbox](../../../cqrs/resilience/inbox-outbox.md).

> **Sample:** [`complex-cqrs-ef-customer-api`](https://github.com/kallebelins/mvp24hours-dotnet/blob/main/samples/src/complex-cqrs-ef-customer-api/CustomerAPI.WebAPI/README.md) — runnable CQRS reference with feature folders, validation behaviors, and notifications on .NET 10.
>
> **Template:** [`templates/blueprints/cqrs`](https://github.com/kallebelins/mvp24hours-dotnet/tree/main/templates/blueprints/cqrs) — compilable `Item` scaffold.
