# Extensibilidade do Mediator

## Visão Geral

O Mvp24Hours CQRS oferece um sistema robusto de extensibilidade que permite interceptar e modificar o comportamento do pipeline de requisições sem alterar o código existente. Este módulo fornece quatro mecanismos principais:

1. **Pre-Processors** - Executam antes do handler
2. **Post-Processors** - Executam após o handler
3. **Exception Handlers** - Tratam exceções de forma granular
4. **Pipeline Hooks** - Pontos de extensão no ciclo de vida
5. **Mediator Decorators** - Decoram o mediator completo

## Arquitetura

```
┌────────────────────────────────────────────────────────────────────────┐
│                          Requisição                                     │
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
│                           Resposta                                      │
└────────────────────────────────────────────────────────────────────────┘
```

## Pre-Processors

Pre-processors executam antes do handler e podem enriquecer ou modificar a requisição.

### Interface

```csharp
public interface IPreProcessor<in TRequest>
{
    Task ProcessAsync(TRequest request, CancellationToken cancellationToken);
}
```

### Exemplo

```csharp
public class TimestampPreProcessor : IPreProcessor<CreateOrderCommand>
{
    public Task ProcessAsync(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        request.CreatedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }
}

// Registro
services.AddPreProcessor<CreateOrderCommand, TimestampPreProcessor>();
```

### Pre-Processor Global

Para executar em todas as requisições:

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

// Registro
services.AddGlobalPreProcessor<LoggingPreProcessor>();
```

## Post-Processors

Post-processors executam após o handler e podem inspecionar a resposta.

### Interface

```csharp
public interface IPostProcessor<in TRequest, in TResponse>
{
    Task ProcessAsync(TRequest request, TResponse response, CancellationToken cancellationToken);
}
```

### Exemplo

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

// Registro
services.AddPostProcessor<CreateOrderCommand, Order, OrderNotificationPostProcessor>();
```

### Post-Processor Global

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

// Registro
services.AddGlobalPostProcessor<MetricsPostProcessor>();
```

## Exception Handlers

Exception handlers permitem tratar exceções de forma granular, podendo recuperar, transformar ou relançar exceções.

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

### Resultados Possíveis

```csharp
// Tratar e retornar uma resposta alternativa
ExceptionHandlingResult<TResponse>.Handled(alternativeResponse);

// Não tratar, deixar propagar
ExceptionHandlingResult<TResponse>.NotHandled;

// Relançar uma exceção diferente
ExceptionHandlingResult<TResponse>.Rethrow(newException);
```

### Exemplo - Tratando Exceção de Validação

```csharp
public class ValidationExceptionHandler 
    : IExceptionHandler<CreateOrderCommand, Order, ValidationException>
{
    public Task<ExceptionHandlingResult<Order>> HandleAsync(
        CreateOrderCommand request,
        ValidationException exception,
        CancellationToken cancellationToken)
    {
        // Retorna uma resposta de erro em vez de lançar
        return Task.FromResult(
            ExceptionHandlingResult<Order>.Handled(Order.Failed(exception.Errors)));
    }
}

// Registro
services.AddExceptionHandler<CreateOrderCommand, Order, ValidationException, ValidationExceptionHandler>();
```

### Exemplo - Transformando Exceção

```csharp
public class DatabaseExceptionHandler 
    : IExceptionHandler<CreateOrderCommand, Order, DbUpdateException>
{
    public Task<ExceptionHandlingResult<Order>> HandleAsync(
        CreateOrderCommand request,
        DbUpdateException exception,
        CancellationToken cancellationToken)
    {
        // Transforma em uma exceção de domínio
        var domainEx = new DomainException("ORDER_SAVE_FAILED", "Falha ao salvar pedido");
        return Task.FromResult(ExceptionHandlingResult<Order>.Rethrow(domainEx));
    }
}
```

### Exception Handler Global

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
        _logger.LogError(exception, "Erro ao processar {RequestType}", request.GetType().Name);
        return Task.FromResult(ExceptionHandlingResult<object?>.NotHandled);
    }
}

// Registro
services.AddGlobalExceptionHandler<Exception, GlobalExceptionHandler>();
```

## Pipeline Hooks

Hooks fornecem pontos de extensão no ciclo de vida do pipeline.

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

### Exemplo - Métricas

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

// Registro
services.AddPipelineHook<MetricsPipelineHook>();
```

### Hook Tipado

```csharp
public class OrderPipelineHook : PipelineHookBase<CreateOrderCommand>
{
    public override Task OnPipelineStartAsync(
        CreateOrderCommand request, 
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Iniciando criação do pedido: {request.CustomerId}");
        return Task.CompletedTask;
    }

    public override Task OnPipelineCompleteAsync(
        CreateOrderCommand request, 
        object? response, 
        long elapsedMilliseconds, 
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Pedido criado em {elapsedMilliseconds}ms");
        return Task.CompletedTask;
    }
}

// Registro
services.AddPipelineHook<CreateOrderCommand, OrderPipelineHook>();
```

## Mediator Decorators

Decorators envolvem o Mediator completo, permitindo interceptar todas as operações.

### Interface Base

```csharp
public interface IMediatorDecorator : IMediator
{
    IMediator InnerMediator { get; }
}
```

### Exemplo

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

// Registro - decorators são aplicados em ordem (último registrado executa primeiro)
services.AddMediatorDecorator<LoggingMediatorDecorator>();
```

### Múltiplos Decorators

```csharp
services.AddMvpMediator(typeof(Program).Assembly);
services.AddMediatorDecorator<MetricsDecorator>();      // Executa segundo
services.AddMediatorDecorator<LoggingDecorator>();      // Executa primeiro (outer)

// Ordem de execução:
// LoggingDecorator.Before -> MetricsDecorator.Before -> Handler -> 
// MetricsDecorator.After -> LoggingDecorator.After
```

## Configuração

### Habilitando Extensibilidade

```csharp
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    
    // Habilita todos os componentes de extensibilidade
    options.WithExtensibility();
});
```

### Habilitando Componentes Individuais

```csharp
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    
    // Apenas pre/post processors
    options.WithPrePostProcessors();
    
    // Apenas exception handlers
    options.WithExceptionHandlers();
    
    // Apenas pipeline hooks
    options.WithPipelineHooks();
});
```

## Quando Usar Cada Mecanismo

| Mecanismo | Uso Recomendado |
|-----------|-----------------|
| Pre-Processor | Enriquecer/modificar requisição antes do handler |
| Post-Processor | Ações após o sucesso (notificações, cache) |
| Exception Handler | Tratamento granular de exceções por tipo |
| Pipeline Hook | Métricas, logging, observabilidade |
| Mediator Decorator | Interceptação global de todas as operações |

## Diferenças de IPipelineBehavior

| Aspecto | Pre/Post Processors | IPipelineBehavior |
|---------|--------------------|--------------------|
| Complexidade | Simples, um método | Controle total do pipeline |
| Acesso à resposta | Post-processor apenas | Sim |
| Modificar resposta | Não | Sim |
| Curto-circuitar | Lançar exceção | Sim |
| Ordem de execução | Antes/depois behaviors | Configurável |

## Boas Práticas

1. **Use Pre-Processors para enriquecimento**: Adicionar timestamps, IDs de correlação, dados do usuário
2. **Use Post-Processors para side-effects**: Notificações, invalidação de cache, métricas
3. **Use Exception Handlers para recovery**: Converter exceções em respostas de erro
4. **Use Pipeline Hooks para observabilidade**: Métricas, tracing, logging centralizado
5. **Use Decorators para cross-cutting global**: Rate limiting, circuit breaker global

