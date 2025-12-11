# CQRS/Mediator

## Visão Geral

O módulo **Mvp24Hours.Infrastructure.Cqrs** fornece uma implementação completa do padrão **CQRS (Command Query Responsibility Segregation)** através do padrão **Mediator**, permitindo separar operações de leitura (Queries) e escrita (Commands) de forma elegante e desacoplada.

## Arquitetura

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

## Componentes Principais

| Componente | Descrição |
|------------|-----------|
| **IMediator** | Interface principal que combina envio de requests e publicação de notificações |
| **ISender** | Envia Commands e Queries para seus respectivos handlers |
| **IPublisher** | Publica notificações para múltiplos handlers |
| **IStreamSender** | Envia requests que retornam streams (IAsyncEnumerable) |
| **IPipelineBehavior** | Intercepta requests para cross-cutting concerns |

## Interfaces Semânticas

### Commands (Escrita)
- `IMediatorCommand<TResponse>` - Command com retorno
- `IMediatorCommand` - Command sem retorno (void)
- `IMediatorCommandHandler<TCommand, TResponse>` - Handler de command

### Queries (Leitura)
- `IMediatorQuery<TResponse>` - Query com retorno
- `IMediatorQueryHandler<TQuery, TResponse>` - Handler de query

### Notificações
- `IMediatorNotification` - Notificação in-process
- `IMediatorNotificationHandler<TNotification>` - Handler de notificação

## Diferença: CQRS vs Repository

> ⚠️ **Importante**: O projeto Mvp24Hours possui `ICommand<T>` e `IQuery<T>` no namespace `Mvp24Hours.Core.Contract.Data` para operações de **Repository** (CRUD).

| Aspecto | Repository Pattern | CQRS/Mediator |
|---------|-------------------|---------------|
| Interface | `ICommand<TEntity>` | `IMediatorCommand<TResponse>` |
| Propósito | CRUD no banco de dados | Operações de domínio |
| Namespace | `Mvp24Hours.Core.Contract.Data` | `Mvp24Hours.Infrastructure.Cqrs.Abstractions` |
| Uso | Acesso a dados | Lógica de aplicação |

## Próximos Passos

- [Começando](cqrs/getting-started.md) - Configuração inicial
- [Commands](cqrs/commands.md) - Criando e executando commands
- [Queries](cqrs/queries.md) - Implementando queries
- [Behaviors](cqrs/behaviors.md) - Pipeline behaviors

