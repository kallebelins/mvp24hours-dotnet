# Saga Pattern - Visão Geral

## O que é uma Saga?

Uma Saga é um padrão para gerenciar transações distribuídas através de uma sequência de transações locais. Cada transação atualiza um serviço e publica um evento para disparar a próxima transação.

## Por que Sagas?

```
┌─────────────────────────────────────────────────────────────────┐
│            Transação Distribuída Tradicional (2PC)               │
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
│              │  Transaction       │  ❌ Complexo                │
│              │  Coordinator (2PC) │  ❌ Baixa disponibilidade   │
│              └─────────────────────┘  ❌ Não escala             │
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
│    ✅ Simples       ✅ Alta disponibilidade    ✅ Escala       │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Tipos de Saga

### Choreography (Coreografia)

Cada serviço conhece o próximo passo e publica eventos.

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

### Orchestration (Orquestração)

Um orquestrador central controla o fluxo.

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

## Compensação

Quando uma etapa falha, as anteriores devem ser revertidas.

```
┌─────────────────────────────────────────────────────────────────┐
│                    Saga com Compensação                          │
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

## Interface ISaga

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

## Exemplo: Order Saga

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
        
        // Compensar em ordem reversa
        for (var i = CurrentStep - 1; i >= 0; i--)
        {
            await _steps[i].CompensateAsync(Data, cancellationToken);
        }
        
        Status = SagaStatus.Compensated;
    }
}
```

## Próximos Passos

- [Implementação](saga/implementation.md) - Implementando Sagas
- [Compensação](saga/compensation.md) - Estratégias de rollback

