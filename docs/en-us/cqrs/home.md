# CQRS/Mediator

## Overview

The **Mvp24Hours.Infrastructure.Cqrs** module provides a complete implementation of the **CQRS (Command Query Responsibility Segregation)** pattern through the **Mediator** pattern, enabling elegant and decoupled separation of read (Queries) and write (Commands) operations.

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         Application                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐ │
│  │   Command   │  │    Query    │  │      Notification       │ │
│  └──────┬──────┘  └──────┬──────┘  └───────────┬─────────────┘ │
│         │                │                     │               │
│         └────────────────┼─────────────────────┘               │
│                          ▼                                     │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │                       IMediator                            │ │
│  │         (ISender + IPublisher + IStreamSender)             │ │
│  └───────────────────────┬───────────────────────────────────┘ │
│                          │                                     │
│  ┌───────────────────────▼───────────────────────────────────┐ │
│  │                  Pipeline Behaviors                        │ │
│  │  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐         │ │
│  │  │Logging  │→│Validate │→│ Cache   │→│ Retry   │→ ...    │ │
│  │  └─────────┘ └─────────┘ └─────────┘ └─────────┘         │ │
│  └───────────────────────┬───────────────────────────────────┘ │
│                          ▼                                     │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │                     Handler                                │ │
│  │  ┌─────────────────┐  ┌──────────────────┐                │ │
│  │  │ CommandHandler  │  │   QueryHandler   │                │ │
│  │  └─────────────────┘  └──────────────────┘                │ │
│  └───────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

## Main Components

| Component | Description |
|-----------|-------------|
| **IMediator** | Main interface combining request sending and notification publishing |
| **ISender** | Sends Commands and Queries to their respective handlers |
| **IPublisher** | Publishes notifications to multiple handlers |
| **IStreamSender** | Sends requests that return streams (IAsyncEnumerable) |
| **IPipelineBehavior** | Intercepts requests for cross-cutting concerns |

## Semantic Interfaces

### Commands (Write)
- `IMediatorCommand<TResponse>` - Command with return value
- `IMediatorCommand` - Command without return (void)
- `IMediatorCommandHandler<TCommand, TResponse>` - Command handler

### Queries (Read)
- `IMediatorQuery<TResponse>` - Query with return value
- `IMediatorQueryHandler<TQuery, TResponse>` - Query handler

### Notifications
- `IMediatorNotification` - In-process notification
- `IMediatorNotificationHandler<TNotification>` - Notification handler

## Difference: CQRS vs Repository

> ⚠️ **Important**: The Mvp24Hours project has `ICommand<T>` and `IQuery<T>` in the `Mvp24Hours.Core.Contract.Data` namespace for **Repository** operations (CRUD).

| Aspect | Repository Pattern | CQRS/Mediator |
|--------|-------------------|---------------|
| Interface | `ICommand<TEntity>` | `IMediatorCommand<TResponse>` |
| Purpose | Database CRUD | Domain operations |
| Namespace | `Mvp24Hours.Core.Contract.Data` | `Mvp24Hours.Infrastructure.Cqrs.Abstractions` |
| Usage | Data access | Application logic |

## Next Steps

- [Getting Started](getting-started.md) - Initial setup
- [Commands](commands.md) - Creating and executing commands
- [Queries](queries.md) - Implementing queries
- [Behaviors](behaviors.md) - Pipeline behaviors
- [RabbitMQ integration](integration-rabbitmq.md) - Brokered commands and events
- [Inbox/Outbox](resilience/inbox-outbox.md) - Reliable messaging
- [Event Sourcing](event-sourcing/home.md) - Aggregates and projections
- [Saga](saga/home.md) - Distributed workflows
- [API Reference](api-reference.md) - Mediator options and registration helpers
- [Best Practices](best-practices.md) - Recommended CQRS usage
- [RabbitMQ](../broker.md) - Messaging module
- [Configuration Reference](../configuration-reference.md) - Options index
- [CQRS blueprint](../guides/architecture/blueprints/template-cqrs.md) - Solution shape

