# Compensation and Rollback

## Overview

Compensation is the process of undoing already performed operations when a saga step fails. Unlike traditional rollback, each service executes its own compensation logic.

## Compensating Transaction

### Concept

```
┌─────────────────────────────────────────────────────────────────┐
│            Original Transaction vs Compensation                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Original:        CreateOrder ──▶ ReserveStock ──▶ ChargeCard  │
│                                                                 │
│  Compensation:    CancelOrder ◀── ReleaseStock ◀── RefundCard  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Important

- Compensation **is not rollback** - it's a new operation
- May leave the system in a different state than original
- Must be **idempotent** (execute multiple times without side effects)

## ISagaStep Interface

```csharp
public interface ISagaStep<TData> where TData : class
{
    string Name { get; }
    Task ExecuteAsync(TData data, CancellationToken cancellationToken = default);
    Task CompensateAsync(TData data, CancellationToken cancellationToken = default);
    bool CanCompensate { get; }
}
```

## Step Implementations

### ReserveStockStep

```csharp
public class ReserveStockStep : ISagaStep<OrderSagaData>
{
    private readonly IInventoryService _inventoryService;
    
    public string Name => "ReserveStock";
    public bool CanCompensate => true;

    public async Task ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken)
    {
        var reservationId = await _inventoryService.ReserveAsync(
            data.Items.Select(i => new StockReservation
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }),
            cancellationToken);
        
        data.ReservationId = reservationId;
    }

    public async Task CompensateAsync(OrderSagaData data, CancellationToken cancellationToken)
    {
        if (data.ReservationId.HasValue)
        {
            await _inventoryService.ReleaseReservationAsync(
                data.ReservationId.Value, 
                cancellationToken);
        }
    }
}
```

### ProcessPaymentStep

```csharp
public class ProcessPaymentStep : ISagaStep<OrderSagaData>
{
    private readonly IPaymentService _paymentService;
    
    public string Name => "ProcessPayment";
    public bool CanCompensate => true;

    public async Task ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken)
    {
        var paymentResult = await _paymentService.ChargeAsync(
            data.CustomerId,
            data.TotalAmount,
            data.PaymentMethod,
            cancellationToken);
        
        if (!paymentResult.Success)
            throw new PaymentFailedException(paymentResult.Error);
        
        data.PaymentId = paymentResult.TransactionId;
    }

    public async Task CompensateAsync(OrderSagaData data, CancellationToken cancellationToken)
    {
        if (data.PaymentId.HasValue)
        {
            await _paymentService.RefundAsync(
                data.PaymentId.Value, 
                data.TotalAmount,
                "Order saga compensation",
                cancellationToken);
        }
    }
}
```

## Saga Orchestrator with Compensation

```csharp
public class OrderSagaOrchestrator
{
    private readonly List<ISagaStep<OrderSagaData>> _steps;
    private readonly ISagaStateStore _stateStore;
    private readonly ILogger<OrderSagaOrchestrator> _logger;

    public async Task<SagaResult> ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken)
    {
        var sagaId = Guid.NewGuid();
        var executedSteps = new Stack<ISagaStep<OrderSagaData>>();
        
        await _stateStore.SaveAsync(new SagaState
        {
            SagaId = sagaId,
            Data = data,
            Status = SagaStatus.Running
        });

        try
        {
            foreach (var step in _steps)
            {
                _logger.LogInformation(
                    "Saga {SagaId}: Executing step {Step}", 
                    sagaId, step.Name);
                
                await step.ExecuteAsync(data, cancellationToken);
                executedSteps.Push(step);
                
                await _stateStore.UpdateAsync(sagaId, state =>
                {
                    state.CurrentStep = step.Name;
                    state.Data = data;
                });
            }

            await _stateStore.UpdateAsync(sagaId, state =>
                state.Status = SagaStatus.Completed);
            
            return SagaResult.Success(sagaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Saga {SagaId}: Step failed, starting compensation", 
                sagaId);

            await CompensateAsync(sagaId, data, executedSteps, cancellationToken);
            
            return SagaResult.Failed(sagaId, ex.Message);
        }
    }

    private async Task CompensateAsync(
        Guid sagaId,
        OrderSagaData data,
        Stack<ISagaStep<OrderSagaData>> executedSteps,
        CancellationToken cancellationToken)
    {
        await _stateStore.UpdateAsync(sagaId, state =>
            state.Status = SagaStatus.Compensating);

        var compensationErrors = new List<Exception>();

        while (executedSteps.TryPop(out var step))
        {
            if (!step.CanCompensate)
            {
                _logger.LogWarning(
                    "Saga {SagaId}: Step {Step} cannot compensate", 
                    sagaId, step.Name);
                continue;
            }

            try
            {
                _logger.LogInformation(
                    "Saga {SagaId}: Compensating step {Step}", 
                    sagaId, step.Name);
                
                await step.CompensateAsync(data, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Saga {SagaId}: Compensation failed for step {Step}", 
                    sagaId, step.Name);
                compensationErrors.Add(ex);
            }
        }

        var finalStatus = compensationErrors.Any() 
            ? SagaStatus.PartiallyCompensated 
            : SagaStatus.Compensated;
        
        await _stateStore.UpdateAsync(sagaId, state =>
        {
            state.Status = finalStatus;
            state.CompensationErrors = compensationErrors
                .Select(e => e.Message)
                .ToList();
        });
    }
}
```

## Compensation Strategies

### 1. Immediate Compensation

Execute compensation immediately after failure.

```csharp
catch (Exception ex)
{
    await CompensateAsync(data, executedSteps, cancellationToken);
    throw;
}
```

### 2. Compensation with Retry

Try to compensate with retry on failure.

```csharp
public async Task CompensateWithRetryAsync(
    ISagaStep<OrderSagaData> step,
    OrderSagaData data,
    CancellationToken cancellationToken)
{
    var retryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(3, attempt => 
            TimeSpan.FromSeconds(Math.Pow(2, attempt)));

    await retryPolicy.ExecuteAsync(async () =>
        await step.CompensateAsync(data, cancellationToken));
}
```

### 3. Async Compensation

Schedule compensation for later processing.

```csharp
public async Task ScheduleCompensationAsync(
    Guid sagaId,
    OrderSagaData data,
    IEnumerable<string> stepsToCompensate)
{
    await _compensationQueue.EnqueueAsync(new CompensationJob
    {
        SagaId = sagaId,
        Data = data,
        Steps = stepsToCompensate.ToList(),
        ScheduledAt = DateTime.UtcNow
    });
}
```

## Saga State Store

```csharp
public interface ISagaStateStore
{
    Task SaveAsync(SagaState state);
    Task<SagaState?> GetAsync(Guid sagaId);
    Task UpdateAsync(Guid sagaId, Action<SagaState> update);
    Task<IReadOnlyList<SagaState>> GetPendingCompensationsAsync();
}

public class SagaState
{
    public Guid SagaId { get; set; }
    public SagaStatus Status { get; set; }
    public string CurrentStep { get; set; } = string.Empty;
    public object Data { get; set; } = null!;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<string> CompensationErrors { get; set; } = new();
}
```

## Best Practices

1. **Idempotency**: Compensations must be idempotent
2. **Timeout**: Configure timeouts for each step
3. **Retry**: Implement retry for compensations
4. **Logging**: Log all operations
5. **Alerts**: Configure alerts for failed compensations
6. **Dead Letter**: Have process for uncompensated sagas
7. **Testing**: Test failure scenarios extensively

