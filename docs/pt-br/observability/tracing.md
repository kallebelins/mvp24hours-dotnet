# OpenTelemetry Tracing

O framework Mvp24Hours fornece integração completa com **OpenTelemetry** para tracing distribuído em todos os módulos. Esta documentação cobre como configurar e usar tracing nas suas aplicações.

## Visão Geral

Tracing distribuído permite acompanhar o fluxo de requisições através da sua arquitetura de microserviços. O Mvp24Hours fornece:

- **ActivitySources** por módulo para criação de spans
- **Tags semânticas** seguindo convenções do OpenTelemetry
- **Activity enrichers** para adicionar contexto customizado
- **Propagação de trace** seguindo W3C Trace Context
- **Métodos auxiliares** para tracing simplificado

## Início Rápido

### 1. Instalar Pacotes OpenTelemetry

```bash
dotnet add package OpenTelemetry
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Exporter.Console
dotnet add package OpenTelemetry.Exporter.Otlp
```

### 2. Configurar OpenTelemetry

```csharp
using Mvp24Hours.Core.Observability;

var builder = WebApplication.CreateBuilder(args);

// Adicionar serviços de tracing do Mvp24Hours
builder.Services.AddMvp24HoursTracing(options =>
{
    options.EnableCorrelationIdPropagation = true;
    options.EnableUserContext = true;
    options.EnableTenantContext = true;
    options.ServiceName = "MeuServico";
});

// Configurar OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            // Adicionar todas as activity sources do Mvp24Hours
            .AddSource(Mvp24HoursActivitySources.Core.Name)
            .AddSource(Mvp24HoursActivitySources.Pipe.Name)
            .AddSource(Mvp24HoursActivitySources.Cqrs.Name)
            .AddSource(Mvp24HoursActivitySources.Data.Name)
            .AddSource(Mvp24HoursActivitySources.RabbitMQ.Name)
            .AddSource(Mvp24HoursActivitySources.WebAPI.Name)
            .AddSource(Mvp24HoursActivitySources.Caching.Name)
            // Adicionar instrumentação do ASP.NET Core
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            // Exportar para console (desenvolvimento)
            .AddConsoleExporter()
            // Exportar para OTLP (Jaeger, Tempo, etc.)
            .AddOtlpExporter(opts =>
            {
                opts.Endpoint = new Uri("http://localhost:4317");
            });
    });
```

## Activity Sources

Cada módulo tem seu próprio `ActivitySource`:

| Módulo | Nome do Source | Descrição |
|--------|---------------|-----------|
| Core | `Mvp24Hours.Core` | Operações fundamentais |
| Pipe | `Mvp24Hours.Pipe` | Pipeline e operações |
| CQRS | `Mvp24Hours.Cqrs` | Commands, Queries, Events |
| Data | `Mvp24Hours.Data` | Repository e banco de dados |
| RabbitMQ | `Mvp24Hours.RabbitMQ` | Mensageria |
| WebAPI | `Mvp24Hours.WebAPI` | Requisições HTTP |
| Caching | `Mvp24Hours.Caching` | Operações de cache |
| CronJob | `Mvp24Hours.CronJob` | Jobs agendados |
| Infrastructure | `Mvp24Hours.Infrastructure` | Preocupações transversais |

### Acessando Todos os Nomes de Source

```csharp
// Obter todos os nomes de source para registro em lote
var allSources = Mvp24HoursActivitySources.AllSourceNames;

// Ou selecionar módulos específicos
var selectedSources = OpenTelemetryBuilderExtensions.GetMvp24HoursActivitySourceNames(
    includeCore: true,
    includePipe: true,
    includeCqrs: true,
    includeData: true,
    includeRabbitMQ: false, // Excluir RabbitMQ
    includeWebAPI: true
);
```

## Criando Activities

### Usando ActivityHelper

```csharp
using Mvp24Hours.Core.Observability;
using System.Diagnostics;

public class PedidoService
{
    public async Task<Pedido> ProcessarPedidoAsync(CriarPedidoCommand command)
    {
        // Iniciar uma activity de command
        using var activity = ActivityHelper.StartCommandActivity("ProcessarPedido");
        
        try
        {
            // Adicionar tags customizadas
            activity?.SetTag(SemanticTags.OperationId, command.PedidoId);
            
            // Lógica de negócio...
            var pedido = await CriarPedido(command);
            
            // Marcar como sucesso
            activity?.SetSuccess();
            
            return pedido;
        }
        catch (Exception ex)
        {
            // Registrar erro com detalhes da exceção
            activity?.SetError(ex);
            throw;
        }
    }
}
```

### Usando ScopedActivity

```csharp
public async Task ProcessarAsync()
{
    using var scope = Mvp24HoursActivitySources.Core.Source
        .StartScopedActivity("MinhaOperacao");
    
    try
    {
        scope.SetTag("pedido.id", pedidoId);
        
        // Se uma exceção for lançada, será registrada automaticamente
        await ExecutarTrabalhoAsync();
        
        // Sucesso é definido automaticamente no dispose se não houver exceção
    }
    catch (Exception ex)
    {
        scope.SetException(ex);
        throw;
    }
}
```

### Helpers Específicos por Módulo

```csharp
// Activity de pipeline
using var pipelineActivity = ActivityHelper.StartPipelineActivity("PedidoPipeline", totalOperations: 5);

// Activity de banco de dados
using var dbActivity = ActivityHelper.StartDatabaseActivity(
    operationName: "ObterPedidoPorId",
    dbOperation: "SELECT",
    dbSystem: "sqlserver",
    dbName: "PedidosDb");

// Publicação de mensagem
using var publishActivity = ActivityHelper.StartMessagePublishActivity(
    destinationName: "pedidos.criados",
    routingKey: "pedidos.#");

// Operação de cache
using var cacheActivity = ActivityHelper.StartCacheActivity(
    operation: "GET",
    cacheKey: "pedido:123",
    cacheSystem: "redis");
```

## Registrando Eventos

Activities podem conter eventos marcando pontos importantes:

```csharp
using var activity = ActivityHelper.StartOperation(
    Mvp24HoursActivitySources.Core.Source,
    "ProcessarPagamento");

// Registrar tentativas de retry
activity?.RecordRetryAttempt(
    attemptNumber: 2,
    delay: TimeSpan.FromSeconds(5),
    reason: "Timeout");

// Registrar cache hit/miss
activity?.RecordCacheHit("pedido:123");
activity?.RecordCacheMiss("pedido:456");

// Registrar slow query
activity?.RecordSlowQuery(
    durationMs: 5000,
    thresholdMs: 1000,
    statement: "SELECT * FROM Pedidos WHERE...");

// Registrar falha de validação
activity?.RecordValidationFailure(new[] 
{
    "Email é obrigatório",
    "Valor deve ser positivo"
});

// Evento customizado
activity?.RecordEvent("pedido.validado", 
    ("quantidade_itens", 5),
    ("valor_total", 150.00));
```

## Activity Enrichers

Enrichers adicionam contexto automaticamente a todas as activities:

### Enrichers Nativos

```csharp
builder.Services.AddMvp24HoursTracing(options =>
{
    // Adicionar correlation ID do contexto atual
    options.AddEnricher(new CorrelationIdEnricher
    {
        GetCorrelationId = () => HttpContext.Current?.Request.Headers["X-Correlation-Id"]
    });
    
    // Adicionar contexto de usuário
    options.AddEnricher(new UserContextEnricher
    {
        GetUserId = () => currentUserService.GetUserId(),
        GetUserName = () => currentUserService.GetUserName(),
        GetUserRoles = () => currentUserService.GetRoles()
    });
    
    // Adicionar contexto de tenant
    options.AddEnricher(new TenantContextEnricher
    {
        GetTenantId = () => tenantProvider.GetTenantId(),
        GetTenantName = () => tenantProvider.GetTenantName()
    });
});
```

### Enricher Customizado

```csharp
public class PedidoContextEnricher : ActivityEnricherBase
{
    private readonly IPedidoContextAccessor _pedidoContext;
    
    public PedidoContextEnricher(IPedidoContextAccessor pedidoContext)
    {
        _pedidoContext = pedidoContext;
    }
    
    public override int Order => 10; // Executar após enrichers nativos
    
    public override void EnrichOnStart(Activity activity, object? context = null)
    {
        var pedidoId = _pedidoContext.PedidoAtualId;
        if (!string.IsNullOrEmpty(pedidoId))
        {
            activity.SetTag("pedido.id", pedidoId);
        }
    }
    
    public override void EnrichOnEnd(Activity activity, object? context = null, Exception? exception = null)
    {
        // Adicionar enriquecimento no fim da activity
        activity.SetTag("pedido.concluido", exception == null);
    }
}

// Registrar
builder.Services.AddActivityEnricher<PedidoContextEnricher>();
```

## Propagação de Trace Context

### Injetando Contexto (Requisições de Saída)

```csharp
// Para requisições HTTP
var headers = new Dictionary<string, string>();
TracePropagation.InjectTraceContext(headers);

httpClient.DefaultRequestHeaders.Add("traceparent", headers["traceparent"]);
if (headers.TryGetValue("tracestate", out var tracestate))
{
    httpClient.DefaultRequestHeaders.Add("tracestate", tracestate);
}

// Usando injeção tipada
TracePropagation.InjectTraceContext(
    httpRequest.Headers, 
    (headers, key, value) => headers.Add(key, value));
```

### Extraindo Contexto (Requisições de Entrada)

```csharp
// De headers HTTP
var headers = new Dictionary<string, string?>
{
    ["traceparent"] = request.Headers["traceparent"],
    ["tracestate"] = request.Headers["tracestate"],
    ["baggage"] = request.Headers["baggage"]
};

var traceContext = TracePropagation.ExtractTraceContext(headers);

if (traceContext != null)
{
    // Iniciar activity com contexto pai extraído
    using var activity = Mvp24HoursActivitySources.WebAPI.Source
        .StartActivityWithParent("TratarRequisicao", traceContext, ActivityKind.Server);
    
    // Processar requisição...
}
```

### Usando ITraceContextAccessor

```csharp
public class MeuServico
{
    private readonly ITraceContextAccessor _traceContext;
    
    public MeuServico(ITraceContextAccessor traceContext)
    {
        _traceContext = traceContext;
    }
    
    public void ExecutarTrabalho()
    {
        // Obter informações de trace atuais
        var traceId = _traceContext.TraceId;
        var spanId = _traceContext.SpanId;
        var correlationId = _traceContext.CorrelationId;
        
        // Obter/definir baggage
        var valorCustomizado = _traceContext.GetBaggageItem("chave.customizada");
        _traceContext.SetBaggageItem("chave.customizada", "valor");
    }
}
```

## Tags Semânticas

Use `SemanticTags` para nomeação consistente de tags:

```csharp
using static Mvp24Hours.Core.Observability.SemanticTags;

activity?.SetTag(CorrelationId, "abc-123");
activity?.SetTag(EnduserId, "usuario-456");
activity?.SetTag(TenantId, "tenant-789");
activity?.SetTag(DbOperation, "SELECT");
activity?.SetTag(MessagingSystem, "rabbitmq");
activity?.SetTag(CacheHit, true);
```

### Categorias de Tags Disponíveis

- **Geral**: `CorrelationId`, `CausationId`, `OperationId`, `OperationName`, `OperationType`
- **Usuário**: `EnduserId`, `EnduserName`, `EnduserRoles`
- **Tenant**: `TenantId`, `TenantName`
- **Erro**: `ErrorType`, `ErrorMessage`, `ErrorCode`
- **Banco de Dados**: `DbSystem`, `DbName`, `DbStatement`, `DbOperation`
- **HTTP**: `HttpMethod`, `HttpStatusCode`, `UrlPath`
- **Mensageria**: `MessagingSystem`, `MessagingDestinationName`, `MessagingMessageId`
- **Cache**: `CacheSystem`, `CacheKey`, `CacheHit`
- **Pipeline**: `PipelineName`, `PipelineOperationName`, `PipelineOperationIndex`

## Integração com Exporters

### Jaeger

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(Mvp24HoursActivitySources.AllSourceNames)
            .AddJaegerExporter(opts =>
            {
                opts.AgentHost = "localhost";
                opts.AgentPort = 6831;
            });
    });
```

### OTLP (Grafana Tempo, etc.)

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(Mvp24HoursActivitySources.AllSourceNames)
            .AddOtlpExporter(opts =>
            {
                opts.Endpoint = new Uri("http://tempo:4317");
                opts.Protocol = OtlpExportProtocol.Grpc;
            });
    });
```

### Azure Application Insights

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(Mvp24HoursActivitySources.AllSourceNames)
            .AddAzureMonitorTraceExporter(opts =>
            {
                opts.ConnectionString = configuration["ApplicationInsights:ConnectionString"];
            });
    });
```

## Boas Práticas

1. **Sempre descarte activities** - Use instruções `using`
2. **Defina status ao concluir** - Chame `SetSuccess()` ou `SetError()`
3. **Use tags semânticas** - Para nomeação consistente entre serviços
4. **Propague contexto** - Passe trace context para serviços downstream
5. **Não trace demais** - Foque em operações significativas
6. **Mascare dados sensíveis** - Nunca registre senhas, tokens ou PII em tags

## Veja Também

- [Documentação OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)
- [W3C Trace Context](https://www.w3.org/TR/trace-context/)
- [Migração do TelemetryHelper](migration.md)

