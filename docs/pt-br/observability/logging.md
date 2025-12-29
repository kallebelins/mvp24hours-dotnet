# Integração de Logging com OpenTelemetry

> Logging estruturado moderno com correlação automática de traces

O framework Mvp24Hours fornece integração profunda entre ILogger e OpenTelemetry, permitindo correlação automática entre logs e traces distribuídos, logging estruturado com convenções semânticas e amostragem configurável de logs para ambientes de alta carga.

## Visão Geral

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    Arquitetura de Logging Mvp24Hours                         │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   ┌─────────────────┐    ┌──────────────────┐    ┌─────────────────────┐   │
│   │   Seu Código    │───▶│    ILogger<T>    │───▶│  OpenTelemetry SDK  │   │
│   │                 │    │                  │    │                     │   │
│   │  _logger.Log()  │    │  + Contexto Trace│    │  + OTLP Exporter    │   │
│   │                 │    │  + Enrichers     │    │  + Console          │   │
│   └─────────────────┘    └──────────────────┘    └─────────────────────┘   │
│                                   │                         │               │
│                                   │                         ▼               │
│   ┌─────────────────┐             │              ┌─────────────────────┐   │
│   │ Activity.Current│◀────────────┘              │  Backend de         │   │
│   │                 │                            │  Observabilidade    │   │
│   │  TraceId, SpanId│                            │  (Jaeger, Seq,      │   │
│   │  CorrelationId  │                            │   Loki, etc.)       │   │
│   └─────────────────┘                             └─────────────────────┘   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Início Rápido

### 1. Configurar Serviços

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Adicionar logging Mvp24Hours com padrões
builder.Services.AddMvp24HoursLogging(options =>
{
    options.ServiceName = "MeuServico";
    options.ServiceVersion = "1.0.0";
    options.EnableTraceCorrelation = true;
});

// Configurar logging builder
builder.Logging.AddMvp24HoursDefaults();
```

### 2. Configurar OpenTelemetry (Opcional)

```csharp
// Opção A: Usar extensão Mvp24Hours OpenTelemetry Logging
builder.Services.AddMvp24HoursOpenTelemetryLogging(options =>
{
    options.ServiceName = "MeuServico";
    options.ServiceVersion = "1.0.0";
    options.EnableOtlpExporter = true;
    options.OtlpEndpoint = "http://localhost:4317";
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
});

// Opção B: Configurar OpenTelemetry SDK diretamente
builder.Logging
    .AddMvp24HoursOpenTelemetryConfig("MeuServico")
    .AddOpenTelemetry(options =>
    {
        options.IncludeFormattedMessage = true;
        options.IncludeScopes = true;
        options.ParseStateValues = true;
        
        // Exportar para endpoint OTLP (Jaeger, Grafana, etc.)
        options.AddOtlpExporter(otlp =>
        {
            otlp.Endpoint = new Uri("http://localhost:4317");
        });
    });
```

### 3. Usar Logging com Contexto de Trace

```csharp
public class PedidoService
{
    private readonly ILogger<PedidoService> _logger;
    
    public PedidoService(ILogger<PedidoService> logger)
    {
        _logger = logger;
    }
    
    public async Task ProcessarPedido(Pedido pedido)
    {
        // Contexto de trace é incluído automaticamente
        using (_logger.BeginTraceScope())
        {
            _logger.LogInformation(
                "Processando pedido {PedidoId} para cliente {ClienteId}",
                pedido.Id,
                pedido.ClienteId);
            
            // ... processar pedido
            
            _logger.LogInformation(
                "Pedido {PedidoId} processado com sucesso",
                pedido.Id);
        }
    }
}
```

## Opções de Configuração

### LoggingOptions

```csharp
services.AddMvp24HoursLogging(options =>
{
    // Identificação do serviço
    options.ServiceName = "MeuServico";
    options.ServiceVersion = "1.0.0";
    options.Environment = "Production";
    
    // Correlação de traces (padrão: true)
    options.EnableTraceCorrelation = true;
    
    // Amostragem de logs para alta carga (padrão: false)
    options.EnableLogSampling = true;
    options.SamplingRatio = 0.1; // Amostrar 10% dos logs
    
    // Enriquecimento de contexto (padrão: true)
    options.EnableUserContextEnrichment = true;
    options.EnableTenantContextEnrichment = true;
    
    // Atributos de recurso customizados
    options.ResourceAttributes["deployment.region"] = "sa-east-1";
    options.ResourceAttributes["app.team"] = "pagamentos";
});
```

### Configuração via appsettings.json

```json
{
  "Mvp24Hours": {
    "Logging": {
      "ServiceName": "MeuServico",
      "ServiceVersion": "1.0.0",
      "EnableTraceCorrelation": true,
      "EnableLogSampling": false,
      "SamplingRatio": 1.0,
      "EnableUserContextEnrichment": true,
      "EnableTenantContextEnrichment": true
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "Mvp24Hours": "Debug"
    }
  }
}
```

## Enriquecimento de Logs

### Contexto de Trace Automático

Quando `EnableTraceCorrelation` está habilitado, os logs incluem automaticamente:

| Propriedade | Descrição |
|-------------|-----------|
| `TraceId` | ID do trace OpenTelemetry |
| `SpanId` | ID do span atual |
| `ParentSpanId` | ID do span pai |
| `correlation.id` | ID de correlação do baggage |
| `causation.id` | ID de causalidade do baggage |

### Enriquecimento de Contexto de Usuário

Quando `EnableUserContextEnrichment` está habilitado:

| Propriedade | Descrição |
|-------------|-----------|
| `enduser.id` | ID do usuário |
| `enduser.name` | Nome do usuário |
| `enduser.roles` | Papéis do usuário (separados por vírgula) |

### Enriquecimento de Contexto de Tenant

Quando `EnableTenantContextEnrichment` está habilitado:

| Propriedade | Descrição |
|-------------|-----------|
| `tenant.id` | ID do tenant |
| `tenant.name` | Nome do tenant |

### Enrichers Customizados

```csharp
public class RequestIdEnricher : ILogEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public RequestIdEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public Dictionary<string, object?> GetEnrichmentProperties()
    {
        var requestId = _httpContextAccessor.HttpContext?
            .Request.Headers["X-Request-ID"].FirstOrDefault();
            
        return new Dictionary<string, object?>
        {
            ["request.id"] = requestId
        };
    }
}

// Registrar
services.AddSingleton<ILogEnricher, RequestIdEnricher>();
```

## Escopos de Log

### Fábricas de Escopo Integradas

```csharp
// Escopo de requisição HTTP
using (LogScopeFactory.BeginHttpScope(_logger, "POST", "/api/pedidos"))
{
    _logger.LogInformation("Processando requisição HTTP");
}

// Escopo de operação de banco de dados
using (LogScopeFactory.BeginDbScope(_logger, "sqlserver", "INSERT", "Pedidos"))
{
    _logger.LogInformation("Inserindo pedido no banco de dados");
}

// Escopo de mensageria
using (LogScopeFactory.BeginMessagingScope(_logger, "rabbitmq", "fila-pedidos", messageId))
{
    _logger.LogInformation("Processando mensagem");
}

// Escopo CQRS/Mediator
using (LogScopeFactory.BeginMediatorScope(_logger, "CriarPedidoCommand", "Command"))
{
    _logger.LogInformation("Tratando comando");
}

// Escopo de pipeline
using (LogScopeFactory.BeginPipelineScope(_logger, "PipelinePedido", "ValidarPedido", 1))
{
    _logger.LogInformation("Executando operação do pipeline");
}

// Escopo de cache
using (LogScopeFactory.BeginCacheScope(_logger, "redis", "get", "pedido:123"))
{
    _logger.LogInformation("Operação de cache");
}

// Escopo de job em background
using (LogScopeFactory.BeginJobScope(_logger, "job-123", "ProcessarPedidosJob", attempt: 1))
{
    _logger.LogInformation("Executando job em background");
}

// Escopo de erro
using (LogScopeFactory.BeginErrorScope(_logger, exception, "PEDIDO_001", "Validacao"))
{
    _logger.LogError(exception, "Validação do pedido falhou");
}
```

## Amostragem de Logs

Para ambientes de alta carga, habilite a amostragem de logs para reduzir o volume:

### Amostragem Baseada em Taxa

```csharp
services.AddMvp24HoursLogging(options =>
{
    options.EnableLogSampling = true;
    options.SamplingRatio = 0.1; // 10% dos logs
});
```

### Amostragem Baseada em Nível

```csharp
// Registrar sampler customizado
services.AddSingleton<ILogSampler>(
    LevelBasedLogSampler.CreateHighLoadDefaults());

// Padrões:
// - Trace: 1%
// - Debug: 5%
// - Information: 10%
// - Warning: 50%
// - Error: 100%
// - Critical: 100%
```

### Amostragem Baseada em Contexto de Trace

```csharp
// Amostrar logs apenas para traces amostrados
services.AddSingleton<ILogSampler>(
    new TraceContextLogSampler(fallbackRatio: 0.1));
```

## Acessor de Contexto de Log

Acesse o contexto de log programaticamente:

```csharp
public class MeuServico
{
    private readonly ILogContextAccessor _logContext;
    private readonly ILogger<MeuServico> _logger;
    
    public MeuServico(ILogContextAccessor logContext, ILogger<MeuServico> logger)
    {
        _logContext = logContext;
        _logger = logger;
    }
    
    public void FazerTrabalho()
    {
        // Acessar contexto de trace
        var traceId = _logContext.TraceId;
        var correlationId = _logContext.CorrelationId;
        
        // Iniciar escopo com enriquecimento
        using (_logContext.BeginTraceScope(_logger))
        {
            _logger.LogInformation("Trabalhando com trace {TraceId}", traceId);
        }
    }
}
```

## Logging de Alta Performance

### Métodos de Log Gerados por Source

Para melhor performance, use logging gerado por source:

```csharp
public static partial class PedidoLogMessages
{
    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Information,
        Message = "Processando pedido {PedidoId} para cliente {ClienteId}")]
    public static partial void ProcessandoPedido(
        ILogger logger,
        string pedidoId,
        string clienteId);
    
    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Error,
        Message = "Processamento do pedido {PedidoId} falhou: {MensagemErro}")]
    public static partial void ProcessamentoPedidoFalhou(
        ILogger logger,
        string pedidoId,
        string mensagemErro,
        Exception exception);
}

// Uso
PedidoLogMessages.ProcessandoPedido(_logger, pedido.Id, pedido.ClienteId);
```

### Convenções de Event ID

| Módulo | Faixa de Event ID |
|--------|-------------------|
| Core | 1000-1999 |
| Pipe | 2000-2999 |
| CQRS | 3000-3999 |
| Data | 4000-4999 |
| RabbitMQ | 5000-5999 |
| WebAPI | 6000-6999 |
| Caching | 7000-7999 |
| CronJob | 8000-8999 |
| Infrastructure | 9000-9999 |

## Atributos de Recurso

### Atributos Padrão

```csharp
var attributes = OpenTelemetryLoggingExtensions
    .GetMvp24HoursResourceAttributes(
        serviceName: "MeuServico",
        serviceVersion: "1.0.0",
        environment: "Production");

// Inclui:
// - service.name
// - service.version
// - service.instance.id
// - deployment.environment
// - host.name
// - process.pid
// - process.runtime.name
// - process.runtime.version
// - telemetry.sdk.name
// - telemetry.sdk.language
// - telemetry.sdk.version
```

## Padrões Específicos por Ambiente

Aplique configurações de nível de log pré-configuradas para diferentes ambientes:

```csharp
// Para Desenvolvimento - logging detalhado
builder.Logging.ApplyMvp24HoursDevelopmentDefaults();
// Define: nível Debug, com filtros para namespaces Microsoft e System

// Para Produção - otimizado para performance
builder.Logging.ApplyMvp24HoursProductionDefaults();
// Define: nível Information, com filtros mais rigorosos para namespaces de framework
```

## Helpers de Logging Estruturado

Use os métodos de extensão de logging estruturado para logs consistentes com contexto de trace:

```csharp
// Log com contexto de trace automático
_logger.LogInformationWithTrace("Processando pedido {PedidoId}", pedidoId);
_logger.LogWarningWithTrace("Pedido {PedidoId} com estoque baixo", pedidoId);
_logger.LogErrorWithTrace(exception, "Pedido {PedidoId} falhou", pedidoId);
_logger.LogCriticalWithTrace(exception, "Falha no sistema de processamento de pedidos");

// Log de requisições HTTP com atributos estruturados
_logger.LogHttpRequest("POST", "/api/pedidos", 201, durationMs: 45);

// Log de operações de banco de dados com atributos estruturados
_logger.LogDatabaseOperation("sqlserver", "INSERT", "Pedidos", durationMs: 12, rowsAffected: 1);

// Log de operações de mensageria com atributos estruturados
_logger.LogMessagingOperation("rabbitmq", "fila-pedidos", "publish", messageId);

// Log de requisições CQRS/Mediator com atributos estruturados
_logger.LogMediatorRequest("CriarPedidoCommand", "Command", durationMs: 150, success: true);
```

## Configuração Tudo-em-Um

Use `AddMvp24HoursObservability` para configurar logging, tracing e metrics juntos:

```csharp
services.AddMvp24HoursObservability(options =>
{
    options.ServiceName = "MeuServico";
    options.ServiceVersion = "1.0.0";
    options.Environment = "Production";
    
    // Habilitar todos os pilares
    options.EnableLogging = true;
    options.EnableTracing = true;
    options.EnableMetrics = true;
    
    // Opções específicas de logging
    options.Logging.EnableTraceCorrelation = true;
    options.Logging.EnableLogSampling = false;
    
    // Opções específicas de tracing
    options.Tracing.EnableCorrelationIdPropagation = true;
    options.Tracing.AddDefaultEnrichers = true;
    
    // Opções específicas de métricas
    options.Metrics.EnablePipelineMetrics = true;
    options.Metrics.EnableCqrsMetrics = true;
});
```

## Integração com Serilog

```csharp
// Program.cs
builder.Host.UseSerilog((context, config) =>
{
    config
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("ServiceName", "MeuServico")
        .Enrich.With<TraceIdEnricher>()
        .WriteTo.Console()
        .WriteTo.OpenTelemetry(options =>
        {
            options.Endpoint = "http://localhost:4317";
            options.Protocol = OtlpProtocol.Grpc;
        });
});

// Enricher Serilog customizado para contexto de trace
public class TraceIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            logEvent.AddPropertyIfAbsent(
                factory.CreateProperty("TraceId", activity.TraceId.ToString()));
            logEvent.AddPropertyIfAbsent(
                factory.CreateProperty("SpanId", activity.SpanId.ToString()));
        }
    }
}
```

## Boas Práticas

### 1. Sempre Use Message Templates

```csharp
// Bom - logging estruturado
_logger.LogInformation(
    "Processando pedido {PedidoId} para {ClienteId}",
    pedido.Id,
    pedido.ClienteId);

// Ruim - interpolação de string
_logger.LogInformation(
    $"Processando pedido {pedido.Id} para {pedido.ClienteId}");
```

### 2. Use Níveis de Log Apropriados

| Nível | Use Para |
|-------|----------|
| Trace | Informação diagnóstica detalhada |
| Debug | Informação de depuração de desenvolvimento |
| Information | Fluxo normal da aplicação |
| Warning | Situações incomuns mas recuperáveis |
| Error | Erros que impedem a conclusão da operação |
| Critical | Falhas em todo o sistema |

### 3. Inclua Contexto em Escopos

```csharp
using (_logger.BeginScope(new Dictionary<string, object>
{
    ["PedidoId"] = pedido.Id,
    ["ClienteId"] = pedido.ClienteId
}))
{
    // Todos os logs dentro deste escopo incluem PedidoId e ClienteId
    _logger.LogInformation("Iniciando processamento do pedido");
    // ... mais operações
    _logger.LogInformation("Processamento do pedido concluído");
}
```

### 4. Não Registre Dados Sensíveis

```csharp
// Ruim
_logger.LogInformation("Senha do usuário: {Senha}", usuario.Senha);

// Bom - mascarar dados sensíveis
_logger.LogInformation("Usuário autenticado: {UsuarioId}", usuario.Id);
```

## Veja Também

- [Tracing com OpenTelemetry](tracing.md)
- [Métricas e Monitoramento](metrics.md)
- [Migração do TelemetryHelper](migration.md)


