# OpenTelemetry Metrics

O framework Mvp24Hours fornece instrumentação completa de métricas usando `System.Diagnostics.Metrics`, a API nativa do .NET que se integra perfeitamente com OpenTelemetry.

## Visão Geral

Métricas permitem monitorar a performance, saúde e comportamento da sua aplicação em tempo real. O framework fornece métricas pré-construídas para todos os módulos principais:

- **Pipeline** - Contagens e durações de execução de operações
- **Repository/Data** - Queries, commands e conexões de banco de dados
- **CQRS/Mediator** - Commands, queries, notificações e behaviors
- **Messaging/RabbitMQ** - Taxas de publicação/consumo e durações
- **Cache** - Taxas de hit/miss e durações de operações
- **HTTP/WebAPI** - Contagens, durações e tamanhos de requisições
- **CronJob** - Contagens e durações de execução de jobs
- **Infrastructure** - HTTP clients, email, SMS, file storage, locks, background jobs

## Instalação

Métricas estão incluídas no pacote `Mvp24Hours.Core`:

```bash
dotnet add package Mvp24Hours.Core
```

Para integração com OpenTelemetry:

```bash
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Exporter.Prometheus.AspNetCore
# Ou para OTLP:
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

## Configuração Básica

### Registrando Métricas no DI

```csharp
using Mvp24Hours.Core.Observability;

// Registrar todas as métricas
services.AddMvp24HoursMetrics();

// Ou com configuração
services.AddMvp24HoursMetrics(options =>
{
    options.EnablePipelineMetrics = true;
    options.EnableCqrsMetrics = true;
    options.EnableRepositoryMetrics = true;
    options.EnableMessagingMetrics = true;
    options.EnableCacheMetrics = true;
    options.EnableHttpMetrics = true;
    options.EnableCronJobMetrics = true;
    options.EnableInfrastructureMetrics = true;
});

// Ou registrar métricas individuais
services.AddPipelineMetrics();
services.AddCqrsMetrics();
services.AddRepositoryMetrics();
```

### Integração com OpenTelemetry

```csharp
using Mvp24Hours.Core.Observability;

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        // Adicionar todos os meters do Mvp24Hours
        foreach (var meterName in OpenTelemetryMeterBuilderExtensions.GetMvp24HoursMeterNames())
        {
            metrics.AddMeter(meterName);
        }

        // Ou adicionar módulos específicos
        metrics.AddMeter(Mvp24HoursMeters.Core.Name);
        metrics.AddMeter(Mvp24HoursMeters.Pipe.Name);
        metrics.AddMeter(Mvp24HoursMeters.Cqrs.Name);
        metrics.AddMeter(Mvp24HoursMeters.Data.Name);

        // Adicionar exportadores
        metrics.AddPrometheusExporter();
        // Ou OTLP:
        // metrics.AddOtlpExporter();
    });
```

### Endpoint Prometheus

```csharp
// Em Program.cs
var app = builder.Build();

// Expor endpoint /metrics para coleta do Prometheus
app.UseOpenTelemetryPrometheusScrapingEndpoint();
```

## Usando Métricas no Seu Código

### Métricas de Pipeline

```csharp
public class MeuPipeline
{
    private readonly PipelineMetrics _metrics;

    public MeuPipeline(PipelineMetrics metrics)
    {
        _metrics = metrics;
    }

    public async Task ExecutarAsync()
    {
        // Tracking automático com scope
        using var scope = _metrics.BeginExecution("ProcessamentoPedidoPipeline");
        try
        {
            // Executar operações do pipeline
            await ProcessarPedidoAsync();
            
            scope.Complete(); // Marcar como sucesso
        }
        catch (Exception ex)
        {
            scope.Fail(); // Marcar como falha
            throw;
        }
    }

    private async Task ExecutarOperacaoAsync(string nomeOperacao)
    {
        using var opScope = _metrics.BeginOperation("ProcessamentoPedidoPipeline", nomeOperacao);
        try
        {
            // Lógica da operação
            await Task.Delay(100);
            opScope.Complete();
        }
        catch
        {
            opScope.Fail();
            throw;
        }
    }
}
```

### Métricas de Repository

```csharp
public class ProdutoRepository
{
    private readonly RepositoryMetrics _metrics;
    private readonly DbContext _context;

    public ProdutoRepository(RepositoryMetrics metrics, DbContext context)
    {
        _metrics = metrics;
        _context = context;
    }

    public async Task<Produto?> BuscarPorIdAsync(int id)
    {
        using var scope = _metrics.BeginQuery("BuscarPorId", "Produto", "sqlserver");
        try
        {
            var produto = await _context.Produtos.FindAsync(id);
            scope.Complete();
            return produto;
        }
        catch
        {
            scope.Fail();
            throw;
        }
    }

    public async Task<int> SalvarAsync(Produto produto)
    {
        using var scope = _metrics.BeginSaveChanges("sqlserver");
        try
        {
            _context.Produtos.Add(produto);
            var linhasAfetadas = await _context.SaveChangesAsync();
            scope.Complete(linhasAfetadas);
            return linhasAfetadas;
        }
        catch
        {
            scope.Fail();
            throw;
        }
    }
}
```

### Métricas CQRS

```csharp
public class MetricsBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly CqrsMetrics _metrics;

    public MetricsBehavior(CqrsMetrics metrics)
    {
        _metrics = metrics;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var nomeRequest = typeof(TRequest).Name;
        var ehCommand = typeof(TRequest).Name.EndsWith("Command");
        
        using var scope = ehCommand 
            ? _metrics.BeginCommand(nomeRequest)
            : _metrics.BeginQuery(nomeRequest);

        try
        {
            var response = await next();
            scope.Complete();
            return response;
        }
        catch
        {
            scope.Fail();
            throw;
        }
    }
}

// Registrando eventos específicos
_metrics.RecordValidationFailure("CriarPedidoCommand");
_metrics.RecordCacheHit("BuscarProdutoQuery");
_metrics.RecordCacheMiss("BuscarProdutoQuery");
_metrics.RecordRetry("CriarPedidoCommand", attemptNumber: 2);
_metrics.RecordCircuitBreakerTrip("ApiExternaQuery");
```

### Métricas de Messaging

```csharp
public class PedidoPublisher
{
    private readonly MessagingMetrics _metrics;
    private readonly IRabbitMQClient _client;

    public PedidoPublisher(MessagingMetrics metrics, IRabbitMQClient client)
    {
        _metrics = metrics;
        _client = client;
    }

    public async Task PublicarAsync<T>(T mensagem, string exchange)
    {
        using var scope = _metrics.BeginPublish(typeof(T).Name, exchange);
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(mensagem);
            await _client.PublishAsync(exchange, mensagem);
            scope.Complete(payloadSize: bytes.Length);
        }
        catch
        {
            scope.Fail();
            throw;
        }
    }
}

public class PedidoConsumer : IMessageConsumer<PedidoCriadoEvent>
{
    private readonly MessagingMetrics _metrics;

    public async Task ConsumeAsync(ConsumeContext<PedidoCriadoEvent> context)
    {
        using var scope = _metrics.BeginConsume(
            typeof(PedidoCriadoEvent).Name,
            "pedidos-queue",
            "pedido-service");

        try
        {
            await ProcessarPedidoAsync(context.Message);
            scope.Complete();
            _metrics.RecordAcknowledge("pedidos-queue");
        }
        catch (Exception ex)
        {
            scope.Fail();
            _metrics.RecordReject("pedidos-queue", requeue: true);
            throw;
        }
    }
}
```

### Métricas de Cache

```csharp
public class ProdutoServiceComCache
{
    private readonly CacheMetrics _metrics;
    private readonly IDistributedCache _cache;

    public ProdutoServiceComCache(CacheMetrics metrics, IDistributedCache cache)
    {
        _metrics = metrics;
        _cache = cache;
    }

    public async Task<Produto?> BuscarProdutoAsync(int id)
    {
        var cacheKey = $"produto:{id}";
        
        using var scope = _metrics.BeginGet("produtos");
        try
        {
            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null)
            {
                scope.SetHit();
                return JsonSerializer.Deserialize<Produto>(cached);
            }
            
            scope.SetMiss();
            return null;
        }
        catch
        {
            throw;
        }
    }

    public async Task SalvarProdutoAsync(Produto produto)
    {
        var cacheKey = $"produto:{produto.Id}";
        var json = JsonSerializer.Serialize(produto);
        var bytes = Encoding.UTF8.GetBytes(json);

        using var scope = _metrics.BeginSet("produtos");
        scope.SetItemSize(bytes.Length);
        
        await _cache.SetStringAsync(cacheKey, json);
    }
}
```

### Métricas HTTP

```csharp
public class RequestMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HttpMetrics _metrics;

    public RequestMetricsMiddleware(RequestDelegate next, HttpMetrics metrics)
    {
        _next = next;
        _metrics = metrics;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var metodo = context.Request.Method;
        var rota = context.GetEndpoint()?.DisplayName ?? context.Request.Path;

        using var scope = _metrics.BeginRequest(metodo, rota);
        try
        {
            await _next(context);
            scope.SetStatusCode(context.Response.StatusCode);
        }
        catch
        {
            scope.SetStatusCode(500);
            throw;
        }
    }
}
```

## Métricas Disponíveis

### Métricas de Pipeline

| Nome da Métrica | Tipo | Descrição |
|----------------|------|-----------|
| `mvp24hours.pipe.executions_total` | Counter | Total de execuções de pipeline |
| `mvp24hours.pipe.executions_failed_total` | Counter | Execuções de pipeline com falha |
| `mvp24hours.pipe.execution_duration_ms` | Histogram | Duração da execução do pipeline |
| `mvp24hours.pipe.operations_total` | Counter | Total de execuções de operações |
| `mvp24hours.pipe.operations_failed_total` | Counter | Execuções de operações com falha |
| `mvp24hours.pipe.operation_duration_ms` | Histogram | Duração da execução da operação |
| `mvp24hours.pipe.active_count` | UpDownCounter | Pipelines ativos no momento |

### Métricas de Repository

| Nome da Métrica | Tipo | Descrição |
|----------------|------|-----------|
| `mvp24hours.data.queries_total` | Counter | Total de queries no banco |
| `mvp24hours.data.query_duration_ms` | Histogram | Duração da execução de queries |
| `mvp24hours.data.commands_total` | Counter | Total de commands no banco |
| `mvp24hours.data.command_duration_ms` | Histogram | Duração da execução de commands |
| `mvp24hours.data.save_changes_total` | Counter | Total de operações SaveChanges |
| `mvp24hours.data.rows_affected_total` | Counter | Total de linhas afetadas |
| `mvp24hours.data.connections_active` | UpDownCounter | Conexões ativas |
| `mvp24hours.data.slow_queries_total` | Counter | Queries lentas detectadas |
| `mvp24hours.data.transactions_total` | Counter | Total de transações |

### Métricas CQRS

| Nome da Métrica | Tipo | Descrição |
|----------------|------|-----------|
| `mvp24hours.cqrs.commands_total` | Counter | Total de commands processados |
| `mvp24hours.cqrs.command_duration_ms` | Histogram | Duração do processamento de commands |
| `mvp24hours.cqrs.queries_total` | Counter | Total de queries processadas |
| `mvp24hours.cqrs.query_duration_ms` | Histogram | Duração do processamento de queries |
| `mvp24hours.cqrs.notifications_total` | Counter | Total de notificações |
| `mvp24hours.cqrs.domain_events_total` | Counter | Domain events despachados |
| `mvp24hours.cqrs.validation_failures_total` | Counter | Falhas de validação |
| `mvp24hours.cqrs.cache_hits_total` | Counter | Cache hits |
| `mvp24hours.cqrs.cache_misses_total` | Counter | Cache misses |
| `mvp24hours.cqrs.retries_total` | Counter | Tentativas de retry |

### Métricas de Messaging

| Nome da Métrica | Tipo | Descrição |
|----------------|------|-----------|
| `mvp24hours.messaging.published_total` | Counter | Mensagens publicadas |
| `mvp24hours.messaging.publish_duration_ms` | Histogram | Duração da publicação |
| `mvp24hours.messaging.consumed_total` | Counter | Mensagens consumidas |
| `mvp24hours.messaging.consume_duration_ms` | Histogram | Duração do consumo |
| `mvp24hours.messaging.acknowledged_total` | Counter | Mensagens confirmadas |
| `mvp24hours.messaging.rejected_total` | Counter | Mensagens rejeitadas |
| `mvp24hours.messaging.dead_lettered_total` | Counter | Mensagens enviadas para DLQ |
| `mvp24hours.messaging.queue_depth` | UpDownCounter | Profundidade da fila |
| `mvp24hours.messaging.payload_size_bytes` | Histogram | Tamanho do payload |

### Métricas de Cache

| Nome da Métrica | Tipo | Descrição |
|----------------|------|-----------|
| `mvp24hours.cache.gets_total` | Counter | Operações de get no cache |
| `mvp24hours.cache.hits_total` | Counter | Cache hits |
| `mvp24hours.cache.misses_total` | Counter | Cache misses |
| `mvp24hours.cache.sets_total` | Counter | Operações de set no cache |
| `mvp24hours.cache.invalidations_total` | Counter | Invalidações de cache |
| `mvp24hours.cache.hit_ratio` | ObservableGauge | Taxa de hit do cache (%) |

## Exemplos de Queries Prometheus

```promql
# Taxa de requisições por segundo
rate(mvp24hours_cqrs_commands_total[5m])

# Duração média de commands
histogram_quantile(0.95, rate(mvp24hours_cqrs_command_duration_ms_bucket[5m]))

# Taxa de erros
sum(rate(mvp24hours_cqrs_commands_failed_total[5m])) / sum(rate(mvp24hours_cqrs_commands_total[5m]))

# Taxa de hit do cache
mvp24hours_cache_hit_ratio

# Conexões ativas no banco
mvp24hours_data_connections_active

# Mensagens na fila
mvp24hours_messaging_queue_depth{queue_name="pedidos-queue"}
```

## Dashboard Grafana

Crie dashboards com painéis para:

1. **Taxa de Requisições** - Commands e queries por segundo
2. **Latência** - Durações P50, P95, P99
3. **Taxa de Erros** - Percentual de operações com falha
4. **Performance do Cache** - Taxa de hit/miss
5. **Profundidade da Fila** - Mensagens aguardando processamento
6. **Conexões Ativas** - Conexões de banco e messaging

## Boas Práticas

1. **Use Scopes** - Sempre use scopes `BeginXxx()` para tracking automático de duração
2. **Marque Sucesso/Falha** - Chame `Complete()` ou `Fail()` antes do scope ser descartado
3. **Adicione Tags de Contexto** - Inclua dimensões relevantes (tipo de entidade, nome da operação)
4. **Monitore Métricas Chave** - Foque em métricas RED (Rate, Errors, Duration)
5. **Configure Alertas** - Configure alertas para taxas de erro e limiares de latência

## Documentação Relacionada

- [Tracing](tracing.md) - Distributed tracing com OpenTelemetry
- [Guia de Migração](migration.md) - Migrando do TelemetryHelper

