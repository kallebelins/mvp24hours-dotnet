# Mediator Extensibility

## Overview

Mvp24Hours CQRS offers a robust extensibility system that allows intercepting and modifying the request pipeline behavior without changing existing code. This module provides four main mechanisms:

1. **Pre-Processors** - Execute before the handler
2. **Post-Processors** - Execute after the handler
3. **Exception Handlers** - Handle exceptions granularly
4. **Pipeline Hooks** - Extension points in the lifecycle
5. **Mediator Decorators** - Decorate the entire mediator

## Architecture

```
┌────────────────────────────────────────────────────────────────────────┐
│                            Request                                      │
└────────────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌────────────────────────────────────────────────────────────────────────┐
│                     Mediator Decorator(s)                               │
│ ┌────────────────────────────────────────────────────────────────────┐ │
│ │                      Pipeline Hooks (Start)                        │ │
│ └────────────────────────────────────────────────────────────────────┘ │
│ ┌────────────────────────────────────────────────────────────────────┐ │
│ │                    Global Pre-Processors                           │ │
│ └────────────────────────────────────────────────────────────────────┘ │
│ ┌────────────────────────────────────────────────────────────────────┐ │
│ │                    Typed Pre-Processors                            │ │
│ └────────────────────────────────────────────────────────────────────┘ │
│ ┌────────────────────────────────────────────────────────────────────┐ │
│ │                    Pipeline Behaviors                              │ │
│ │ ┌────────────────────────────────────────────────────────────────┐ │ │
│ │ │                        Handler                                 │ │ │
│ │ └────────────────────────────────────────────────────────────────┘ │ │
│ └────────────────────────────────────────────────────────────────────┘ │
│ ┌────────────────────────────────────────────────────────────────────┐ │
│ │                    Typed Post-Processors                           │ │
│ └────────────────────────────────────────────────────────────────────┘ │
│ ┌────────────────────────────────────────────────────────────────────┐ │
│ │                    Global Post-Processors                          │ │
│ └────────────────────────────────────────────────────────────────────┘ │
│ ┌────────────────────────────────────────────────────────────────────┐ │
│ │                   Pipeline Hooks (Complete/Error)                  │ │
│ └────────────────────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌────────────────────────────────────────────────────────────────────────┐
│                           Response                                      │
└────────────────────────────────────────────────────────────────────────┘
```

## Pre-Processors

Pre-processors execute before the handler and can enrich or modify the request.

### Interface

```csharp
public interface IPreProcessor<in TRequest>
{
    Task ProcessAsync(TRequest request, CancellationToken cancellationToken);
}
```

### Example

```csharp
public class TimestampPreProcessor : IPreProcessor<CreateOrderCommand>
{
    public Task ProcessAsync(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        request.CreatedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }
}

// Registration
services.AddPreProcessor<CreateOrderCommand, TimestampPreProcessor>();
```

### Global Pre-Processor

To execute on all requests:

```csharp
public class LoggingPreProcessor : IPreProcessorGlobal
{
    private readonly ILogger<LoggingPreProcessor> _logger;

    public LoggingPreProcessor(ILogger<LoggingPreProcessor> logger)
    {
        _logger = logger;
    }

    public Task ProcessAsync(object request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing {RequestType}", request.GetType().Name);
        return Task.CompletedTask;
    }
}

// Registration
services.AddGlobalPreProcessor<LoggingPreProcessor>();
```

## Post-Processors

Post-processors execute after the handler and can inspect the response.

### Interface

```csharp
public interface IPostProcessor<in TRequest, in TResponse>
{
    Task ProcessAsync(TRequest request, TResponse response, CancellationToken cancellationToken);
}
```

### Example

```csharp
public class OrderNotificationPostProcessor : IPostProcessor<CreateOrderCommand, Order>
{
    private readonly INotificationService _notifications;

    public OrderNotificationPostProcessor(INotificationService notifications)
    {
        _notifications = notifications;
    }

    public async Task ProcessAsync(
        CreateOrderCommand request, 
        Order response, 
        CancellationToken cancellationToken)
    {
        await _notifications.SendOrderConfirmationAsync(response.Id, response.Email);
    }
}

// Registration
services.AddPostProcessor<CreateOrderCommand, Order, OrderNotificationPostProcessor>();
```

### Global Post-Processor

```csharp
public class MetricsPostProcessor : IPostProcessorGlobal
{
    private readonly IMetrics _metrics;

    public MetricsPostProcessor(IMetrics metrics)
    {
        _metrics = metrics;
    }

    public Task ProcessAsync(object request, object? response, CancellationToken cancellationToken)
    {
        _metrics.IncrementRequestCount(request.GetType().Name);
        return Task.CompletedTask;
    }
}

// Registration
services.AddGlobalPostProcessor<MetricsPostProcessor>();
```

## Exception Handlers

Exception handlers allow granular exception handling, enabling recovery, transformation, or rethrowing of exceptions.

### Interface

```csharp
public interface IExceptionHandler<in TRequest, TResponse, in TException>
    where TException : Exception
{
    Task<ExceptionHandlingResult<TResponse>> HandleAsync(
        TRequest request,
        TException exception,
        CancellationToken cancellationToken);
}
```

### Possible Results

```csharp
// Handle and return an alternative response
ExceptionHandlingResult<TResponse>.Handled(alternativeResponse);

// Don't handle, let it propagate
ExceptionHandlingResult<TResponse>.NotHandled;

// Rethrow a different exception
ExceptionHandlingResult<TResponse>.Rethrow(newException);
```

### Example - Handling Validation Exception

```csharp
public class ValidationExceptionHandler 
    : IExceptionHandler<CreateOrderCommand, Order, ValidationException>
{
    public Task<ExceptionHandlingResult<Order>> HandleAsync(
        CreateOrderCommand request,
        ValidationException exception,
        CancellationToken cancellationToken)
    {
        // Return an error response instead of throwing
        return Task.FromResult(
            ExceptionHandlingResult<Order>.Handled(Order.Failed(exception.Errors)));
    }
}

// Registration
services.AddExceptionHandler<CreateOrderCommand, Order, ValidationException, ValidationExceptionHandler>();
```

### Example - Transforming Exception

```csharp
public class DatabaseExceptionHandler 
    : IExceptionHandler<CreateOrderCommand, Order, DbUpdateException>
{
    public Task<ExceptionHandlingResult<Order>> HandleAsync(
        CreateOrderCommand request,
        DbUpdateException exception,
        CancellationToken cancellationToken)
    {
        // Transform to a domain exception
        var domainEx = new DomainException("ORDER_SAVE_FAILED", "Failed to save order");
        return Task.FromResult(ExceptionHandlingResult<Order>.Rethrow(domainEx));
    }
}
```

### Global Exception Handler

```csharp
public class GlobalExceptionHandler : IExceptionHandlerGlobal<Exception>
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public Task<ExceptionHandlingResult<object?>> HandleAsync(
        object request,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Error processing {RequestType}", request.GetType().Name);
        return Task.FromResult(ExceptionHandlingResult<object?>.NotHandled);
    }
}

// Registration
services.AddGlobalExceptionHandler<Exception, GlobalExceptionHandler>();
```

## Pipeline Hooks

Hooks provide extension points in the pipeline lifecycle.

### Interface

```csharp
public interface IPipelineHook
{
    Task OnPipelineStartAsync(object request, Type requestType, CancellationToken cancellationToken);
    
    Task OnPipelineCompleteAsync(object request, object? response, Type requestType, 
        Type responseType, long elapsedMilliseconds, CancellationToken cancellationToken);
    
    Task OnPipelineErrorAsync(object request, Exception exception, Type requestType, 
        long elapsedMilliseconds, CancellationToken cancellationToken);
}
```

### Example - Metrics

```csharp
public class MetricsPipelineHook : PipelineHookBase
{
    private readonly IMetrics _metrics;

    public MetricsPipelineHook(IMetrics metrics)
    {
        _metrics = metrics;
    }

    public override Task OnPipelineCompleteAsync(
        object request,
        object? response,
        Type requestType,
        Type responseType,
        long elapsedMilliseconds,
        CancellationToken cancellationToken)
    {
        _metrics.RecordRequestDuration(requestType.Name, elapsedMilliseconds);
        return Task.CompletedTask;
    }

    public override Task OnPipelineErrorAsync(
        object request,
        Exception exception,
        Type requestType,
        long elapsedMilliseconds,
        CancellationToken cancellationToken)
    {
        _metrics.IncrementErrorCount(requestType.Name, exception.GetType().Name);
        return Task.CompletedTask;
    }
}

// Registration
services.AddPipelineHook<MetricsPipelineHook>();
```

### Typed Hook

```csharp
public class OrderPipelineHook : PipelineHookBase<CreateOrderCommand>
{
    public override Task OnPipelineStartAsync(
        CreateOrderCommand request, 
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Starting order creation: {request.CustomerId}");
        return Task.CompletedTask;
    }

    public override Task OnPipelineCompleteAsync(
        CreateOrderCommand request, 
        object? response, 
        long elapsedMilliseconds, 
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Order created in {elapsedMilliseconds}ms");
        return Task.CompletedTask;
    }
}

// Registration
services.AddPipelineHook<CreateOrderCommand, OrderPipelineHook>();
```

## Mediator Decorators

Decorators wrap the entire Mediator, allowing interception of all operations.

### Base Interface

```csharp
public interface IMediatorDecorator : IMediator
{
    IMediator InnerMediator { get; }
}
```

### Example

```csharp
public class LoggingMediatorDecorator : MediatorDecoratorBase
{
    private readonly ILogger<LoggingMediatorDecorator> _logger;

    public LoggingMediatorDecorator(IMediator inner, ILogger<LoggingMediatorDecorator> logger) 
        : base(inner)
    {
        _logger = logger;
    }

    public override async Task<TResponse> SendAsync<TResponse>(
        IMediatorRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending {RequestType}", request.GetType().Name);
        
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            _logger.LogInformation("Completed {RequestType}", request.GetType().Name);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed {RequestType}", request.GetType().Name);
            throw;
        }
    }
}

// Registration - decorators are applied in order (last registered executes first)
services.AddMediatorDecorator<LoggingMediatorDecorator>();
```

### Multiple Decorators

```csharp
services.AddMvpMediator(typeof(Program).Assembly);
services.AddMediatorDecorator<MetricsDecorator>();      // Executes second
services.AddMediatorDecorator<LoggingDecorator>();      // Executes first (outer)

// Execution order:
// LoggingDecorator.Before -> MetricsDecorator.Before -> Handler -> 
// MetricsDecorator.After -> LoggingDecorator.After
```

## Configuration

### Enabling Extensibility

```csharp
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    
    // Enable all extensibility components
    options.WithExtensibility();
});
```

### Enabling Individual Components

```csharp
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    
    // Only pre/post processors
    options.WithPrePostProcessors();
    
    // Only exception handlers
    options.WithExceptionHandlers();
    
    // Only pipeline hooks
    options.WithPipelineHooks();
});
```

## When to Use Each Mechanism

| Mechanism | Recommended Use |
|-----------|-----------------|
| Pre-Processor | Enrich/modify request before handler |
| Post-Processor | Actions after success (notifications, cache) |
| Exception Handler | Granular exception handling by type |
| Pipeline Hook | Metrics, logging, observability |
| Mediator Decorator | Global interception of all operations |

## Differences from IPipelineBehavior

| Aspect | Pre/Post Processors | IPipelineBehavior |
|--------|--------------------|--------------------|
| Complexity | Simple, single method | Full pipeline control |
| Response access | Post-processor only | Yes |
| Modify response | No | Yes |
| Short-circuit | Throw exception | Yes |
| Execution order | Before/after behaviors | Configurable |

## Best Practices

1. **Use Pre-Processors for enrichment**: Add timestamps, correlation IDs, user data
2. **Use Post-Processors for side-effects**: Notifications, cache invalidation, metrics
3. **Use Exception Handlers for recovery**: Convert exceptions to error responses
4. **Use Pipeline Hooks for observability**: Metrics, tracing, centralized logging
5. **Use Decorators for global cross-cutting**: Rate limiting, global circuit breaker

