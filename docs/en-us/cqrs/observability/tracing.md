# CorrelationId and Tracing

## Overview

Distributed tracing allows tracking requests across multiple services, facilitating diagnosis and debugging in distributed systems.

## Concepts

### CorrelationId

Unique identifier that accompanies a request through all services.

```
┌─────────────────────────────────────────────────────────────────┐
│                    Request Flow                                  │
├─────────────────────────────────────────────────────────────────┤
│  Client ──[CorrelationId: abc-123]──▶ API Gateway               │
│           ──[CorrelationId: abc-123]──▶ Order Service           │
│           ──[CorrelationId: abc-123]──▶ Payment Service         │
│           ──[CorrelationId: abc-123]──▶ Notification Service    │
└─────────────────────────────────────────────────────────────────┘
```

### CausationId

Identifier of the event/command that caused the current action.

```
Event A (Id: 001)
    └── Command B (CausationId: 001, Id: 002)
            └── Event C (CausationId: 002, Id: 003)
```

## Implementation

### IRequestContext

```csharp
public interface IRequestContext
{
    string CorrelationId { get; }
    string? CausationId { get; }
    string? UserId { get; }
    string? TenantId { get; }
    IDictionary<string, object> Baggage { get; }
}
```

### RequestContext

```csharp
public class RequestContext : IRequestContext
{
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public string? CausationId { get; set; }
    public string? UserId { get; set; }
    public string? TenantId { get; set; }
    public IDictionary<string, object> Baggage { get; } = new Dictionary<string, object>();
}
```

### RequestContextBehavior

```csharp
public sealed class RequestContextBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly IRequestContext _context;
    private readonly ILogger<RequestContextBehavior<TRequest, TResponse>> _logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = _context.CorrelationId,
            ["CausationId"] = _context.CausationId ?? "N/A",
            ["UserId"] = _context.UserId ?? "Anonymous",
            ["RequestType"] = typeof(TRequest).Name
        });

        _logger.LogInformation(
            "Processing {RequestType} with CorrelationId {CorrelationId}",
            typeof(TRequest).Name,
            _context.CorrelationId);

        return await next();
    }
}
```

## ASP.NET Core Middleware

### CorrelationIdMiddleware

```csharp
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IRequestContext requestContext)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        ((RequestContext)requestContext).CorrelationId = correlationId;
        
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        await _next(context);
    }
}

// Registration
app.UseMiddleware<CorrelationIdMiddleware>();
```

## Context Propagation in Events

### Integration Event with Context

```csharp
public abstract record IntegrationEventBase : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; }
    public string? CausationId { get; init; }
}
```

### Publishing with Context

```csharp
public class CreateOrderCommandHandler 
    : IMediatorCommandHandler<CreateOrderCommand, OrderDto>
{
    private readonly IRequestContext _context;
    private readonly IIntegrationEventOutbox _outbox;

    public async Task<OrderDto> Handle(
        CreateOrderCommand request, 
        CancellationToken cancellationToken)
    {
        var order = Order.Create(request.CustomerEmail, request.Items);

        await _outbox.AddAsync(new OrderCreatedIntegrationEvent
        {
            OrderId = order.Id,
            CorrelationId = _context.CorrelationId,
            CausationId = request.GetType().Name // Command that caused it
        }, cancellationToken);

        return OrderDto.FromEntity(order);
    }
}
```

## Logging Integration

### Serilog Enricher

```csharp
public class RequestContextEnricher : ILogEventEnricher
{
    private readonly IRequestContext _context;

    public RequestContextEnricher(IRequestContext context)
    {
        _context = context;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty("CorrelationId", _context.CorrelationId));
        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty("UserId", _context.UserId ?? "Anonymous"));
    }
}
```

### Serilog Configuration

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.With<RequestContextEnricher>()
    .WriteTo.Console(outputTemplate: 
        "[{Timestamp:HH:mm:ss} {Level:u3}] " +
        "[{CorrelationId}] [{UserId}] " +
        "{Message:lj}{NewLine}{Exception}")
    .CreateLogger();
```

## OpenTelemetry

### Configuration

```csharp
services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .AddSource("Mvp24Hours.Mediator")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());
```

### Mediator Instrumentation

```csharp
public sealed class TracingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private static readonly ActivitySource ActivitySource = 
        new("Mvp24Hours.Mediator");

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity(
            $"Mediator.{typeof(TRequest).Name}",
            ActivityKind.Internal);

        activity?.SetTag("mediator.request_type", typeof(TRequest).Name);
        activity?.SetTag("mediator.response_type", typeof(TResponse).Name);

        try
        {
            var result = await next();
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

## Visualization

```
┌─────────────────────────────────────────────────────────────────┐
│                    Trace View (Jaeger/Zipkin)                    │
├─────────────────────────────────────────────────────────────────┤
│  CorrelationId: abc-123-def-456                                 │
│                                                                 │
│  ├── API.CreateOrder [200ms]                                    │
│  │   ├── Mediator.CreateOrderCommand [180ms]                    │
│  │   │   ├── ValidationBehavior [5ms]                           │
│  │   │   ├── TransactionBehavior [170ms]                        │
│  │   │   │   └── CreateOrderCommandHandler [165ms]              │
│  │   │   │       ├── Database.Insert [50ms]                     │
│  │   │   │       └── Outbox.Add [10ms]                          │
│  │   │   └── LoggingBehavior [5ms]                              │
│  │   └── Response [20ms]                                        │
└─────────────────────────────────────────────────────────────────┘
```

## Best Practices

1. **Standard Header**: Use `X-Correlation-Id` as default
2. **Generate at Gateway**: Generate ID at first entry point
3. **Propagation**: Propagate across all services and events
4. **Structured Logging**: Include CorrelationId in all logs
5. **Baggage**: Use for additional context data
6. **Retention**: Configure appropriate trace retention

