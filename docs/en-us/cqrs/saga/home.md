# Saga Pattern - Overview

## What is a Saga?

A Saga is a pattern for managing distributed transactions through a sequence of local transactions. Each transaction updates a service and publishes an event to trigger the next transaction.

## Why Sagas?

```
┌─────────────────────────────────────────────────────────────────┐
│            Traditional Distributed Transaction (2PC)             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌───────────┐    ┌───────────┐    ┌───────────┐               │
│  │  Service  │    │  Service  │    │  Service  │               │
│  │     A     │◄──▶│     B     │◄──▶│     C     │               │
│  └───────────┘    └───────────┘    └───────────┘               │
│         │               │               │                       │
│         └───────────────┼───────────────┘                       │
│                         │                                       │
│              ┌──────────┴──────────┐                            │
│              │  Transaction       │  ❌ Complex                 │
│              │  Coordinator (2PC) │  ❌ Low availability        │
│              └─────────────────────┘  ❌ Doesn't scale         │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                        Saga Pattern                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌───────────┐    ┌───────────┐    ┌───────────┐               │
│  │  Service  │───▶│  Service  │───▶│  Service  │               │
│  │     A     │    │     B     │    │     C     │               │
│  └───────────┘    └───────────┘    └───────────┘               │
│         │               │               │                       │
│         ▼               ▼               ▼                       │
│    ┌─────────┐    ┌─────────┐    ┌─────────┐                   │
│    │ Event A │───▶│ Event B │───▶│ Event C │                   │
│    └─────────┘    └─────────┘    └─────────┘                   │
│                                                                 │
│    ✅ Simple        ✅ High availability      ✅ Scales        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Saga Types

### Choreography

Each service knows the next step and publishes events.

```
┌──────────────────────────────────────────────────────────────┐
│                    Choreography Saga                          │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  Order         Inventory        Payment         Shipping    │
│  Service       Service          Service         Service     │
│    │              │                │               │        │
│    │ OrderCreated │                │               │        │
│    │─────────────▶│                │               │        │
│    │              │ StockReserved  │               │        │
│    │              │───────────────▶│               │        │
│    │              │                │ PaymentDone   │        │
│    │              │                │──────────────▶│        │
│    │              │                │               │        │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### Orchestration

A central orchestrator controls the flow.

```
┌──────────────────────────────────────────────────────────────┐
│                    Orchestration Saga                         │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│                    Saga Orchestrator                         │
│                          │                                   │
│         ┌────────────────┼────────────────┐                 │
│         │                │                │                 │
│         ▼                ▼                ▼                 │
│    ┌─────────┐     ┌─────────┐     ┌─────────┐             │
│    │Inventory│     │ Payment │     │Shipping │             │
│    │ Service │     │ Service │     │ Service │             │
│    └─────────┘     └─────────┘     └─────────┘             │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

## Compensation

When a step fails, previous ones must be reverted.

```
┌─────────────────────────────────────────────────────────────────┐
│                    Saga with Compensation                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Step 1: Reserve Stock     ───────────▶  Compensation: Release │
│  Step 2: Process Payment   ───────────▶  Compensation: Refund  │
│  Step 3: Ship Order        ❌ FAILURE                          │
│                                                                 │
│  Rollback:                                                      │
│    1. Refund Payment                                            │
│    2. Release Stock                                             │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## ISaga Interface

```csharp
public interface ISaga<TData> where TData : class
{
    Guid SagaId { get; }
    TData Data { get; }
    SagaStatus Status { get; }
    int CurrentStep { get; }
    
    Task StartAsync(TData data, CancellationToken cancellationToken = default);
    Task HandleEventAsync(IDomainEvent @event, CancellationToken cancellationToken = default);
    Task CompensateAsync(CancellationToken cancellationToken = default);
}

public enum SagaStatus
{
    NotStarted,
    Running,
    Completed,
    Failed,
    Compensating,
    Compensated
}
```

## Example: Order Saga

```csharp
public class OrderSaga : ISaga<OrderSagaData>
{
    private readonly List<ISagaStep<OrderSagaData>> _steps;
    
    public OrderSaga()
    {
        _steps = new List<ISagaStep<OrderSagaData>>
        {
            new ReserveStockStep(),
            new ProcessPaymentStep(),
            new ShipOrderStep()
        };
    }

    public async Task StartAsync(OrderSagaData data, CancellationToken cancellationToken)
    {
        Data = data;
        Status = SagaStatus.Running;
        
        foreach (var step in _steps)
        {
            try
            {
                await step.ExecuteAsync(Data, cancellationToken);
                CurrentStep++;
            }
            catch (Exception)
            {
                Status = SagaStatus.Failed;
                await CompensateAsync(cancellationToken);
                throw;
            }
        }
        
        Status = SagaStatus.Completed;
    }

    public async Task CompensateAsync(CancellationToken cancellationToken)
    {
        Status = SagaStatus.Compensating;
        
        // Compensate in reverse order
        for (var i = CurrentStep - 1; i >= 0; i--)
        {
            await _steps[i].CompensateAsync(Data, cancellationToken);
        }
        
        Status = SagaStatus.Compensated;
    }
}
```

## Next Steps

- [Implementation](implementation.md) - Implementing Sagas
- [Compensation](compensation.md) - Rollback strategies

