# Configuração de Exporters OpenTelemetry

Este guia demonstra como configurar exporters do OpenTelemetry usando a API unificada de configuração do Mvp24Hours.

## Visão Geral

O Mvp24Hours fornece uma API simplificada de configuração para exporters do OpenTelemetry através do método de extensão `AddMvp24HoursOpenTelemetry()`. Esta abordagem centraliza a configuração dos exporters OTLP, Console e Prometheus com padrões sensatos para diferentes ambientes.

### Exporters Suportados

| Exporter | Propósito | Recomendado Para |
|----------|-----------|------------------|
| **OTLP** | Exportar para backends compatíveis com OTLP | Jaeger, Tempo, Grafana, Azure Monitor, Datadog |
| **Console** | Exportar para console/stdout | Desenvolvimento e depuração |
| **Prometheus** | Expor métricas no formato Prometheus | Coleta de métricas pelo Prometheus |

## Início Rápido

### Ambiente de Desenvolvimento

```csharp
using Mvp24Hours.Core.Observability;

var builder = WebApplication.CreateBuilder(args);

// Configurar exporters com padrões de desenvolvimento
builder.Services.AddMvp24HoursOpenTelemetry(opts =>
{
    opts.ServiceName = "MeuServico";
    opts.ServiceVersion = "1.0.0";
    opts.Environment = "Development";
    
    // Console exporter para feedback imediato
    opts.Console.Enabled = true;
    opts.Console.EnableTracing = true;
    opts.Console.EnableMetrics = false; // Evitar saída excessiva
    
    // OTLP para Jaeger local
    opts.Otlp.Endpoint = "http://localhost:4317";
    opts.Otlp.Protocol = OtlpExportProtocol.Grpc;
});

// Integrar com SDK do OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("MeuServico", serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddSource(OpenTelemetryBuilderExtensions.GetMvp24HoursActivitySourceNames())
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddMeter(OpenTelemetryMeterBuilderExtensions.GetMvp24HoursMeterNames())
        .AddAspNetCoreInstrumentation()
        .AddPrometheusExporter());

var app = builder.Build();

// Expor endpoint de métricas Prometheus
app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.Run();
```

### Ambiente de Produção

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configurar exporters com padrões de produção
builder.Services.AddMvp24HoursOpenTelemetry(opts =>
{
    opts.ServiceName = "ServicoPedidos";
    opts.ServiceVersion = "2.1.0";
    opts.Environment = "Production";
    
    // Desabilitar console em produção
    opts.Console.Enabled = false;
    
    // OTLP para Grafana Tempo
    opts.Otlp.Endpoint = builder.Configuration["Observability:OtlpEndpoint"] 
                         ?? "http://tempo:4317";
    opts.Otlp.Protocol = OtlpExportProtocol.Grpc;
    opts.Otlp.BatchExportScheduledDelayMs = 5000;
    opts.Otlp.MaxExportBatchSize = 512;
    
    // Métricas Prometheus
    opts.Prometheus.Enabled = true;
    opts.Prometheus.ScrapeEndpoint = "/metrics";
    opts.Prometheus.ScrapeResponseCacheDurationMs = 5000;
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r
        .AddService("ServicoPedidos", serviceVersion: "2.1.0")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = "Production",
            ["service.namespace"] = "ECommerce"
        }))
    .WithTracing(tracing => tracing
        .AddSource(OpenTelemetryBuilderExtensions.GetMvp24HoursActivitySourceNames())
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddMeter(OpenTelemetryMeterBuilderExtensions.GetMvp24HoursMeterNames())
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter()
        .AddOtlpExporter());

// Configurar logging com OTLP
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeScopes = true;
    logging.IncludeFormattedMessage = true;
    logging.AddOtlpExporter();
});

var app = builder.Build();
app.UseOpenTelemetryPrometheusScrapingEndpoint();
app.Run();
```

## Configuração do OTLP Exporter

O exporter OpenTelemetry Protocol (OTLP) é a forma nativa e recomendada para exportar dados de telemetria.

### Configuração Básica

```csharp
services.AddMvp24HoursOpenTelemetry(opts =>
{
    opts.ServiceName = "MeuServico";
    
    opts.Otlp.Enabled = true;
    opts.Otlp.Endpoint = "http://jaeger:4317"; // endpoint gRPC
    opts.Otlp.Protocol = OtlpExportProtocol.Grpc;
});
```

### Endpoints de Backends Comuns

#### Jaeger (All-in-One)
```csharp
opts.Otlp.Endpoint = "http://localhost:4317";  // gRPC
// ou
opts.Otlp.Endpoint = "http://localhost:4318";  // HTTP
opts.Otlp.Protocol = OtlpExportProtocol.HttpProtobuf;
```

#### Grafana Tempo
```csharp
opts.Otlp.Endpoint = "http://tempo:4317";
opts.Otlp.Protocol = OtlpExportProtocol.Grpc;
```

#### Azure Monitor
```csharp
opts.Otlp.Endpoint = "https://<ingestion-endpoint>.azure.monitor.com/v1/traces";
opts.Otlp.Protocol = OtlpExportProtocol.HttpProtobuf;
opts.Otlp.Headers["Authorization"] = $"Bearer {apiKey}";
```

#### Datadog
```csharp
opts.Otlp.Endpoint = "https://api.datadoghq.com/api/v2/otlp";
opts.Otlp.Protocol = OtlpExportProtocol.HttpProtobuf;
opts.Otlp.Headers["DD-API-KEY"] = apiKey;
```

#### AWS X-Ray (via OTEL Collector)
```csharp
opts.Otlp.Endpoint = "http://otel-collector:4317";
opts.Otlp.Protocol = OtlpExportProtocol.Grpc;
```

### Configuração Avançada do OTLP

```csharp
opts.Otlp.Enabled = true;
opts.Otlp.Endpoint = "http://tempo:4317";
opts.Otlp.Protocol = OtlpExportProtocol.Grpc;

// Controlar o que exportar
opts.Otlp.EnableTracing = true;
opts.Otlp.EnableMetrics = true;
opts.Otlp.EnableLogging = true;

// Ajuste de performance
opts.Otlp.TimeoutMs = 10000;                      // Timeout de 10 segundos
opts.Otlp.BatchExportScheduledDelayMs = 5000;     // Exportar a cada 5 segundos
opts.Otlp.MaxExportBatchSize = 512;               // Máximo de 512 itens por lote
opts.Otlp.MaxQueueSize = 2048;                    // Enfileirar até 2048 itens

// Headers de autenticação
opts.Otlp.Headers["Authorization"] = "Bearer token...";
opts.Otlp.Headers["X-Custom-Header"] = "value";
```

## Configuração do Console Exporter

O Console exporter é útil para desenvolvimento e depuração, mas **não** deve ser habilitado em produção.

```csharp
services.AddMvp24HoursOpenTelemetry(opts =>
{
    opts.Console.Enabled = builder.Environment.IsDevelopment(); // Apenas em dev
    
    opts.Console.EnableTracing = true;    // Mostrar traces
    opts.Console.EnableMetrics = false;   // Métricas podem ser verbosas
    opts.Console.EnableLogging = true;    // Mostrar logs
    
    opts.Console.EnableTimestamps = true; // Incluir timestamps
    opts.Console.UseColors = true;        // Saída colorida
});
```

### Exemplo de Saída do Console

```
Activity.TraceId:            4bf92f3577b34da6a3ce929d0e0e4736
Activity.SpanId:             00f067aa0ba902b7
Activity.TraceFlags:         Recorded
Activity.ActivitySourceName: Mvp24Hours.Core
Activity.DisplayName:        GET /api/pedidos
Activity.Kind:               Server
Activity.StartTime:          2024-12-28T10:30:45.1234567Z
Activity.Duration:           00:00:00.0234567
Activity.Tags:
    http.method: GET
    http.url: https://api.exemplo.com/api/pedidos
    http.status_code: 200
    service.name: ServicoPedidos
```

## Configuração do Prometheus Exporter

O Prometheus exporter expõe métricas via endpoint HTTP que o Prometheus pode coletar.

### Configuração Básica

```csharp
services.AddMvp24HoursOpenTelemetry(opts =>
{
    opts.Prometheus.Enabled = true;
    opts.Prometheus.ScrapeEndpoint = "/metrics";
    opts.Prometheus.ScrapeResponseCacheDurationMs = 5000; // Cache por 5 segundos
    opts.Prometheus.EnableExemplars = true; // Vincular métricas a traces
});
```

### Habilitar Endpoint de Scraping

```csharp
var app = builder.Build();

// Adicionar middleware do endpoint de scraping Prometheus
app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.Run();
```

### Configuração do Prometheus (prometheus.yml)

```yaml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: 'meuservico'
    scrape_interval: 15s
    static_configs:
      - targets: ['meuservico:8080']
    metrics_path: '/metrics'
```

### Proteger Endpoint de Métricas

```csharp
// Requer autenticação para métricas
opts.Prometheus.RequireAuthentication = true;

// Na configuração do middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/metrics", async (HttpContext context) =>
{
    // Verificar se o usuário está autenticado
    if (!context.User.Identity?.IsAuthenticated ?? false)
    {
        return Results.Unauthorized();
    }
    
    // Servir métricas
    await context.UseOpenTelemetryPrometheusScrapingEndpoint();
    return Results.Ok();
}).RequireAuthorization();
```

## Usando Presets de Configuração

### Preset de Desenvolvimento

```csharp
var devOptions = OpenTelemetryExporterExtensions
    .GetDevelopmentDefaults("MeuServico");

services.AddMvp24HoursOpenTelemetry(opts =>
{
    opts.ServiceName = devOptions.ServiceName;
    opts.Console = devOptions.Console;
    opts.Otlp = devOptions.Otlp;
    opts.Prometheus = devOptions.Prometheus;
});
```

### Preset de Produção

```csharp
var prodOptions = OpenTelemetryExporterExtensions
    .GetProductionDefaults(
        serviceName: "ServicoPedidos",
        otlpEndpoint: "http://tempo:4317");

services.AddMvp24HoursOpenTelemetry(opts =>
{
    opts.ServiceName = prodOptions.ServiceName;
    opts.Console = prodOptions.Console;
    opts.Otlp = prodOptions.Otlp;
    opts.Prometheus = prodOptions.Prometheus;
});
```

## Configuração via appsettings.json

```json
{
  "Mvp24Hours": {
    "Observability": {
      "ServiceName": "ServicoPedidos",
      "ServiceVersion": "2.1.0",
      "Environment": "Production",
      "Otlp": {
        "Enabled": true,
        "Endpoint": "http://tempo:4317",
        "Protocol": "Grpc",
        "EnableTracing": true,
        "EnableMetrics": true,
        "EnableLogging": true,
        "BatchExportScheduledDelayMs": 5000,
        "MaxExportBatchSize": 512
      },
      "Console": {
        "Enabled": false
      },
      "Prometheus": {
        "Enabled": true,
        "ScrapeEndpoint": "/metrics",
        "ScrapeResponseCacheDurationMs": 5000
      }
    }
  }
}
```

```csharp
// Carregar configuração do appsettings.json
var config = builder.Configuration;

services.AddMvp24HoursOpenTelemetry(opts =>
{
    config.GetSection("Mvp24Hours:Observability").Bind(opts);
});
```

## Exemplo Completo com Todos os Recursos

```csharp
using Mvp24Hours.Core.Observability;
using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);

// Configurar observabilidade do Mvp24Hours
builder.Services.AddMvp24HoursOpenTelemetry(opts =>
{
    opts.ServiceName = "ServicoPedidos";
    opts.ServiceVersion = "2.1.0";
    opts.Environment = builder.Environment.EnvironmentName;
    opts.ServiceNamespace = "ECommerce";
    opts.ServiceInstanceId = Environment.MachineName;
    
    // Configuração OTLP
    opts.Otlp.Enabled = true;
    opts.Otlp.Endpoint = builder.Configuration["Observability:OtlpEndpoint"] 
                         ?? "http://localhost:4317";
    opts.Otlp.Protocol = OtlpExportProtocol.Grpc;
    opts.Otlp.EnableTracing = true;
    opts.Otlp.EnableMetrics = true;
    opts.Otlp.EnableLogging = true;
    opts.Otlp.BatchExportScheduledDelayMs = 5000;
    opts.Otlp.MaxExportBatchSize = 512;
    
    // Console apenas para desenvolvimento
    opts.Console.Enabled = builder.Environment.IsDevelopment();
    opts.Console.EnableTracing = true;
    opts.Console.EnableMetrics = false;
    
    // Métricas Prometheus
    opts.Prometheus.Enabled = true;
    opts.Prometheus.ScrapeEndpoint = "/metrics";
    opts.Prometheus.EnableExemplars = true;
    
    // Atributos de recurso customizados
    opts.ResourceAttributes["deployment.environment"] = builder.Environment.EnvironmentName;
    opts.ResourceAttributes["service.datacenter"] = "us-east-1";
});

// Validar configuração (lança exceção se inválida)
var exporterOpts = builder.Services.BuildServiceProvider()
    .GetRequiredService<OpenTelemetryExporterOptions>();
exporterOpts.Validate();

// Configurar SDK do OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
    {
        var attrs = exporterOpts.GetResourceAttributes();
        resource.AddAttributes(attrs);
    })
    .WithTracing(tracing => tracing
        .AddSource(OpenTelemetryBuilderExtensions.GetMvp24HoursActivitySourceNames())
        .AddAspNetCoreInstrumentation(opts =>
        {
            opts.RecordException = true;
            opts.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
        })
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation(opts =>
        {
            opts.SetDbStatementForText = true;
        })
        .AddConsoleExporter() // Apenas se Console.Enabled
        .AddOtlpExporter(opts =>
        {
            opts.Endpoint = new Uri(exporterOpts.Otlp.Endpoint);
            opts.Protocol = exporterOpts.Otlp.Protocol == OtlpExportProtocol.Grpc
                ? OpenTelemetry.Exporter.OtlpExportProtocol.Grpc
                : OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
        }))
    .WithMetrics(metrics => metrics
        .AddMeter(OpenTelemetryMeterBuilderExtensions.GetMvp24HoursMeterNames())
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter()
        .AddOtlpExporter());

// Configurar logging com OpenTelemetry
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeScopes = true;
    logging.IncludeFormattedMessage = true;
    logging.ParseStateValues = true;
    
    if (exporterOpts.Otlp.EnableLogging)
    {
        logging.AddOtlpExporter(opts =>
        {
            opts.Endpoint = new Uri(exporterOpts.Otlp.Endpoint);
        });
    }
    
    if (exporterOpts.Console.EnableLogging)
    {
        logging.AddConsoleExporter();
    }
});

var app = builder.Build();

// Health checks
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");

// Endpoint de métricas Prometheus
app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.Run();
```

## Solução de Problemas

### Problemas de Conexão OTLP

```csharp
// Habilitar logging detalhado para depuração
builder.Logging.AddFilter("OpenTelemetry", LogLevel.Debug);

// Aumentar timeout
opts.Otlp.TimeoutMs = 30000; // 30 segundos

// Verificar conectividade
curl -v http://localhost:4317
```

### Alto Uso de Memória

```csharp
// Reduzir tamanhos de lote
opts.Otlp.MaxExportBatchSize = 256;
opts.Otlp.MaxQueueSize = 1024;

// Aumentar frequência de exportação
opts.Otlp.BatchExportScheduledDelayMs = 2000; // Exportar a cada 2 segundos
```

### Métricas Ausentes no Prometheus

```csharp
// Verificar se o endpoint de scrape está acessível
curl http://localhost:8080/metrics

// Verificar logs do Prometheus para erros de scrape
docker logs prometheus

// Garantir que as métricas estão sendo coletadas
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMeter("*") // Coletar todos os meters para depuração
        .AddPrometheusExporter());
```

## Melhores Práticas

1. **Use Configuração Específica por Ambiente**: Habilite Console exporter apenas em Development
2. **Configurações de Exportação em Lote**: Ajuste tamanho do lote e atraso baseado em seus padrões de tráfego
3. **Autenticação**: Proteja endpoints OTLP com headers de autenticação apropriados
4. **Atributos de Recurso**: Adicione ambiente de deployment, datacenter e ID da instância para melhor filtragem
5. **Validação**: Sempre chame `Validate()` nas opções em produção para detectar erros de configuração cedo
6. **Amostragem**: Considere amostragem de traces para serviços de alto tráfego para reduzir custos

## Veja Também

- [Guia de Logging](logging.md)
- [Guia de Tracing](tracing.md)
- [Guia de Métricas](metrics.md)
- [Migração do TelemetryHelper](migration.md)

