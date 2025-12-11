# Implementing Sagas

## Overview

This guide shows how to implement complete sagas using the orchestration pattern.

## Saga Data Structure

```csharp
public class OrderSagaData
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public List<OrderItemData> Items { get; set; } = new();
    public string PaymentMethod { get; set; } = string.Empty;
    
    // Data collected during saga
    public Guid? ReservationId { get; set; }
    public Guid? PaymentId { get; set; }
    public string? TrackingNumber { get; set; }
}

public class OrderItemData
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
```

## ISagaStep Interface

```csharp
public interface ISagaStep<TData> where TData : class
{
    string Name { get; }
    int Order { get; }
    bool CanCompensate { get; }
    
    Task ExecuteAsync(TData data, CancellationToken cancellationToken = default);
    Task CompensateAsync(TData data, CancellationToken cancellationToken = default);
}
```

## Step Implementations

### Step 1: Reserve Stock

```csharp
public class ReserveStockStep : ISagaStep<OrderSagaData>
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<ReserveStockStep> _logger;
    
    public string Name => "ReserveStock";
    public int Order => 1;
    public bool CanCompensate => true;

    public async Task ExecuteAsync(
        OrderSagaData data, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Reserving stock for order {OrderId}", 
            data.OrderId);

        var reservations = data.Items.Select(i => new StockReservation
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            OrderId = data.OrderId
        });

        var result = await _inventoryService.ReserveAsync(
            reservations, 
            cancellationToken);

        if (!result.Success)
        {
            throw new SagaStepException(Name, result.Error);
        }

        data.ReservationId = result.ReservationId;
        
        _logger.LogInformation(
            "Stock reserved: {ReservationId}", 
            data.ReservationId);
    }

    public async Task CompensateAsync(
        OrderSagaData data, 
        CancellationToken cancellationToken)
    {
        if (data.ReservationId is null)
            return;

        _logger.LogInformation(
            "Releasing stock reservation {ReservationId}", 
            data.ReservationId);

        await _inventoryService.ReleaseReservationAsync(
            data.ReservationId.Value, 
            cancellationToken);
    }
}
```

### Step 2: Process Payment

```csharp
public class ProcessPaymentStep : ISagaStep<OrderSagaData>
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<ProcessPaymentStep> _logger;
    
    public string Name => "ProcessPayment";
    public int Order => 2;
    public bool CanCompensate => true;

    public async Task ExecuteAsync(
        OrderSagaData data, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing payment for order {OrderId}", 
            data.OrderId);

        var result = await _paymentService.ChargeAsync(new PaymentRequest
        {
            CustomerId = data.CustomerId,
            Amount = data.TotalAmount,
            Method = data.PaymentMethod,
            OrderId = data.OrderId
        }, cancellationToken);

        if (!result.Success)
        {
            throw new SagaStepException(Name, result.Error);
        }

        data.PaymentId = result.TransactionId;
        
        _logger.LogInformation(
            "Payment processed: {PaymentId}", 
            data.PaymentId);
    }

    public async Task CompensateAsync(
        OrderSagaData data, 
        CancellationToken cancellationToken)
    {
        if (data.PaymentId is null)
            return;

        _logger.LogInformation(
            "Refunding payment {PaymentId}", 
            data.PaymentId);

        await _paymentService.RefundAsync(new RefundRequest
        {
            TransactionId = data.PaymentId.Value,
            Amount = data.TotalAmount,
            Reason = "Order saga compensation"
        }, cancellationToken);
    }
}
```

### Step 3: Create Shipment

```csharp
public class CreateShipmentStep : ISagaStep<OrderSagaData>
{
    private readonly IShippingService _shippingService;
    private readonly ILogger<CreateShipmentStep> _logger;
    
    public string Name => "CreateShipment";
    public int Order => 3;
    public bool CanCompensate => true;

    public async Task ExecuteAsync(
        OrderSagaData data, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating shipment for order {OrderId}", 
            data.OrderId);

        var result = await _shippingService.CreateShipmentAsync(new ShipmentRequest
        {
            OrderId = data.OrderId,
            CustomerEmail = data.CustomerEmail,
            Items = data.Items.Select(i => new ShipmentItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList()
        }, cancellationToken);

        if (!result.Success)
        {
            throw new SagaStepException(Name, result.Error);
        }

        data.TrackingNumber = result.TrackingNumber;
        
        _logger.LogInformation(
            "Shipment created: {TrackingNumber}", 
            data.TrackingNumber);
    }

    public async Task CompensateAsync(
        OrderSagaData data, 
        CancellationToken cancellationToken)
    {
        if (data.TrackingNumber is null)
            return;

        _logger.LogInformation(
            "Canceling shipment {TrackingNumber}", 
            data.TrackingNumber);

        await _shippingService.CancelShipmentAsync(
            data.TrackingNumber, 
            cancellationToken);
    }
}
```

## Saga Orchestrator

```csharp
public class OrderSagaOrchestrator
{
    private readonly IEnumerable<ISagaStep<OrderSagaData>> _steps;
    private readonly ISagaStateStore _stateStore;
    private readonly ILogger<OrderSagaOrchestrator> _logger;

    public OrderSagaOrchestrator(
        IEnumerable<ISagaStep<OrderSagaData>> steps,
        ISagaStateStore stateStore,
        ILogger<OrderSagaOrchestrator> logger)
    {
        _steps = steps.OrderBy(s => s.Order).ToList();
        _stateStore = stateStore;
        _logger = logger;
    }

    public async Task<SagaResult> ExecuteAsync(
        OrderSagaData data,
        CancellationToken cancellationToken = default)
    {
        var sagaId = Guid.NewGuid();
        var executedSteps = new Stack<ISagaStep<OrderSagaData>>();

        _logger.LogInformation(
            "Starting saga {SagaId} for order {OrderId}", 
            sagaId, data.OrderId);

        await SaveStateAsync(sagaId, data, SagaStatus.Running);

        try
        {
            foreach (var step in _steps)
            {
                _logger.LogInformation(
                    "Saga {SagaId}: Executing step {Step}", 
                    sagaId, step.Name);

                await step.ExecuteAsync(data, cancellationToken);
                executedSteps.Push(step);

                await UpdateStateAsync(sagaId, data, step.Name);
            }

            await SaveStateAsync(sagaId, data, SagaStatus.Completed);
            
            _logger.LogInformation(
                "Saga {SagaId} completed successfully", 
                sagaId);

            return SagaResult.Success(sagaId, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Saga {SagaId} failed at step", 
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
        _logger.LogInformation(
            "Saga {SagaId}: Starting compensation", 
            sagaId);

        await SaveStateAsync(sagaId, data, SagaStatus.Compensating);

        while (executedSteps.TryPop(out var step))
        {
            if (!step.CanCompensate)
            {
                _logger.LogWarning(
                    "Saga {SagaId}: Step {Step} cannot be compensated", 
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
            }
        }

        await SaveStateAsync(sagaId, data, SagaStatus.Compensated);
        
        _logger.LogInformation(
            "Saga {SagaId}: Compensation completed", 
            sagaId);
    }

    private async Task SaveStateAsync(
        Guid sagaId, 
        OrderSagaData data, 
        SagaStatus status)
    {
        await _stateStore.SaveAsync(new SagaState
        {
            SagaId = sagaId,
            Status = status,
            Data = JsonSerializer.Serialize(data),
            UpdatedAt = DateTime.UtcNow
        });
    }

    private async Task UpdateStateAsync(
        Guid sagaId, 
        OrderSagaData data, 
        string currentStep)
    {
        await _stateStore.UpdateAsync(sagaId, state =>
        {
            state.CurrentStep = currentStep;
            state.Data = JsonSerializer.Serialize(data);
            state.UpdatedAt = DateTime.UtcNow;
        });
    }
}
```

## Command Handler

```csharp
public class CreateOrderSagaCommandHandler 
    : IMediatorCommandHandler<CreateOrderSagaCommand, SagaResult>
{
    private readonly OrderSagaOrchestrator _orchestrator;

    public async Task<SagaResult> Handle(
        CreateOrderSagaCommand request,
        CancellationToken cancellationToken)
    {
        var data = new OrderSagaData
        {
            OrderId = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            CustomerEmail = request.CustomerEmail,
            TotalAmount = request.Items.Sum(i => i.Quantity * i.UnitPrice),
            Items = request.Items.Select(i => new OrderItemData
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList(),
            PaymentMethod = request.PaymentMethod
        };

        return await _orchestrator.ExecuteAsync(data, cancellationToken);
    }
}
```

## DI Configuration

```csharp
services.AddScoped<ISagaStep<OrderSagaData>, ReserveStockStep>();
services.AddScoped<ISagaStep<OrderSagaData>, ProcessPaymentStep>();
services.AddScoped<ISagaStep<OrderSagaData>, CreateShipmentStep>();
services.AddScoped<OrderSagaOrchestrator>();
services.AddScoped<ISagaStateStore, SqlSagaStateStore>();
```

## Best Practices

1. **Clear Order**: Use `Order` to define sequence
2. **Idempotency**: Steps must be idempotent
3. **Logging**: Log each step and compensation
4. **Persisted State**: Save state for recovery
5. **Timeout**: Configure timeout for each step
6. **Retry**: Implement retry for transient failures

