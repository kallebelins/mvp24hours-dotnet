# CorrelationId e Tracing

## Visão Geral

O tracing distribuído permite rastrear requisições através de múltiplos serviços, facilitando diagnóstico e debugging em sistemas distribuídos.

## Conceitos

### CorrelationId

Identificador único que acompanha uma requisição através de todos os serviços.

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

Identificador do evento/comando que causou a ação atual.

```
Event A (Id: 001)
    └── Command B (CausationId: 001, Id: 002)
            └── Event C (CausationId: 002, Id: 003)
```

## Implementação

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

## Middleware ASP.NET Core

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

// Registro
app.UseMiddleware<CorrelationIdMiddleware>();
```

## Propagação em Events

### Integration Event com Context

```csharp
public abstract record IntegrationEventBase : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; }
    public string? CausationId { get; init; }
}
```

### Publicação com Context

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
            CausationId = request.GetType().Name // Comando que causou
        }, cancellationToken);

        return OrderDto.FromEntity(order);
    }
}
```

## Integração com Logging

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

### Configuração Serilog

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

### Configuração

```csharp
services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .AddSource("Mvp24Hours.Mediator")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());
```

### Instrumentação do Mediator

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

## Visualização

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

## Boas Práticas

1. **Header Padrão**: Use `X-Correlation-Id` como padrão
2. **Geração no Gateway**: Gere ID no primeiro ponto de entrada
3. **Propagação**: Propague em todos os serviços e eventos
4. **Logging Estruturado**: Inclua CorrelationId em todos os logs
5. **Baggage**: Use para dados adicionais de contexto
6. **Retenção**: Configure retenção adequada de traces

