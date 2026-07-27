# Mvp24Hours.Infrastructure.Cqrs

A native CQRS and mediator implementation for .NET 10 applications, without a dependency on MediatR.

## Install

```bash
dotnet add package Mvp24Hours.Infrastructure.Cqrs
```

## Features

- Commands, queries, request/response handlers, and notifications.
- Logging, validation, retry, and resilience pipeline behaviors.
- Domain events and durable inbox/outbox processing.
- Event sourcing, sagas, projections, and scheduled commands.
- Multi-tenant request processing.

## Quick start

Register handlers from an assembly:

```csharp
builder.Services.AddMvpMediator(typeof(Program).Assembly);
```

Optional modules include `AddMvpInbox`, `AddMvpOutbox`, `AddEventSourcingInMemory`, `AddSagaOrchestration`, and `AddProjections`.

## Documentation

See [kallebelins.github.io/mvp24hours-dotnet](https://kallebelins.github.io/mvp24hours-dotnet) and the [project repository](https://github.com/kallebelins/mvp24hours-dotnet).

## License

Licensed under the [MIT License](../../LICENSE).
