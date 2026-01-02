# Logging

Aplicações .NET modernas utilizam `ILogger<T>` do `Microsoft.Extensions.Logging` como abstração padrão de logging. O Mvp24Hours fornece extensões que integram com OpenTelemetry para correlação de rastreamento distribuído, logging estruturado e observabilidade.

## Logging Moderno com ILogger

A abordagem recomendada para aplicações .NET 9+ é usar `ILogger<T>` com as extensões de observabilidade do Mvp24Hours.

### Início Rápido

```csharp
/// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Adiciona logging do Mvp24Hours com correlação de trace
builder.Services.AddMvp24HoursLogging(options =>
{
    options.ServiceName = "MyService";
    options.ServiceVersion = "1.0.0";
    options.EnableTraceCorrelation = true;
});

// Aplica níveis de log padrão
builder.Logging.AddMvp24HoursDefaults();
```

### Usando ILogger em Services

```csharp
public class OrderService
{
    private readonly ILogger<OrderService> _logger;
    
    public OrderService(ILogger<OrderService> logger)
    {
        _logger = logger;
    }
    
    public async Task ProcessOrder(Order order)
    {
        _logger.LogInformation(
            "Processing order {OrderId} for customer {CustomerId}",
            order.Id,
            order.CustomerId);
        
        // ... processa pedido
        
        _logger.LogInformation("Order {OrderId} processed successfully", order.Id);
    }
}
```

## Structured Logging (Message Templates)

O logging estruturado permite capturar dados de log em formato consultável. Use **message templates** ao invés de interpolação de string:

### Boas Práticas

```csharp
// ✅ Bom - logging estruturado com message templates
_logger.LogInformation(
    "Processing order {OrderId} for {CustomerId}",
    order.Id,
    order.CustomerId);

// ❌ Ruim - interpolação de string (perde a estrutura)
_logger.LogInformation(
    $"Processing order {order.Id} for {order.CustomerId}");
```

### Guia de Níveis de Log

| Nível | Usar Para |
|-------|---------|
| `Trace` | Informações diagnósticas detalhadas (somente dev) |
| `Debug` | Informações de depuração para desenvolvedores |
| `Information` | Fluxo normal da aplicação, eventos de negócio |
| `Warning` | Situações incomuns mas recuperáveis |
| `Error` | Erros que impedem a conclusão da operação |
| `Critical` | Falhas de sistema que requerem atenção imediata |

## Integração com OpenTelemetry

O Mvp24Hours fornece integração profunda entre `ILogger` e OpenTelemetry, habilitando correlação automática entre logs e traces distribuídos.

### Configurar OpenTelemetry Logging

```csharp
builder.Services.AddMvp24HoursOpenTelemetryLogging(options =>
{
    options.ServiceName = "MyService";
    options.ServiceVersion = "1.0.0";
    options.EnableOtlpExporter = true;
    options.OtlpEndpoint = "http://localhost:4317";
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
});
```

### Observabilidade Completa

Para observabilidade completa (logs, traces e métricas):

```csharp
services.AddMvp24HoursObservability(options =>
{
    options.ServiceName = "MyService";
    options.ServiceVersion = "1.0.0";
    
    // Habilita todos os pilares
    options.EnableLogging = true;
    options.EnableTracing = true;
    options.EnableMetrics = true;
    
    // Opções específicas de logging
    options.Logging.EnableTraceCorrelation = true;
});
```

> 📚 Para documentação completa sobre logging com OpenTelemetry, consulte [OpenTelemetry Logging](observability/logging.md).

## Log Scopes

Use scopes para adicionar contexto a grupos de entradas de log:

```csharp
using (_logger.BeginScope(new Dictionary<string, object>
{
    ["OrderId"] = order.Id,
    ["CustomerId"] = order.CustomerId
}))
{
    // Todos os logs dentro deste scope incluem OrderId e CustomerId
    _logger.LogInformation("Starting order processing");
    // ... mais operações
    _logger.LogInformation("Order processing completed");
}
```

### Factories de Scope Integradas

```csharp
// Scope de requisição HTTP
using (LogScopeFactory.BeginHttpScope(_logger, "POST", "/api/orders"))
{
    _logger.LogInformation("Processing HTTP request");
}

// Scope de operação de banco de dados
using (LogScopeFactory.BeginDbScope(_logger, "sqlserver", "INSERT", "Orders"))
{
    _logger.LogInformation("Inserting order into database");
}

// Scope de mensageria
using (LogScopeFactory.BeginMessagingScope(_logger, "rabbitmq", "orders-queue", messageId))
{
    _logger.LogInformation("Processing message");
}
```

## Configuração via appsettings.json

```json
{
  "Mvp24Hours": {
    "Logging": {
      "ServiceName": "MyService",
      "ServiceVersion": "1.0.0",
      "EnableTraceCorrelation": true,
      "EnableLogSampling": false
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

---

## Legado: TelemetryHelper

> ⚠️ **Deprecado:** `TelemetryHelper` está deprecado. Use `ILogger<T>` com as extensões de logging do Mvp24Hours. Consulte o [Guia de Migração](observability/migration.md) para instruções de migração.

---

## Bibliotecas de Logging de Terceiros

### Serilog

Serilog é uma biblioteca de logging diagnóstico popular para aplicações .NET. Integra bem com OpenTelemetry.

```csharp
// Program.cs
builder.Host.UseSerilog((context, config) =>
{
    config
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("ServiceName", "MyService")
        .WriteTo.Console()
        .WriteTo.OpenTelemetry(options =>
        {
            options.Endpoint = "http://localhost:4317";
        });
});
```

Saiba mais: [Serilog](https://serilog.net/)

### NLog

NLog é uma biblioteca fácil de configurar com múltiplos destinos de saída.

Saiba mais: [NLog ASP.NET Core](https://github.com/NLog/NLog/wiki/Getting-started-with-ASP.NET-Core-3)

Siga os modelos de arquivo xml para configuração do NLog.

### Log Console
```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      autoReload="true">
	<targets>
		<target name="console"
				xsi:type="ColoredConsole"
				layout="Server-Date: ${longdate}; Level: ${level}; Message: ${message}" />
		<target name="debug"
				xsi:type="Debugger"
				layout="Server-Date: ${longdate}; Level: ${level}; Message: ${message}" />
	</targets>
	<rules>
		<logger name="*" minlevel="Trace" writeTo="console,debug" />
	</rules>
</nlog>
```

### Log Arquivo
```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      autoReload="true">
	<targets>
		<target name="logfile"
				xsi:type="File"
				layout="Server-Date: ${longdate}; Level: ${level}; Message: ${message}"
				fileName="${basedir}/logs/${date:format=yyyy-MM-dd}-webapi.log" />
	</targets>
	<rules>
		<logger name="*" minlevel="Trace" writeTo="logfile" />
	</rules>
</nlog>
```

### Log ElasticSearch
```xml
<?xml version="1.0" encoding="utf-8" ?>
<!-- 
Install-Package NLog.Targets.ElasticSearch
-->
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
	  autoReload="true">
	<extensions>
		<add assembly="NLog.Targets.ElasticSearch"/>
	</extensions>
	<targets>
		<target name="elastic" xsi:type="BufferingWrapper" flushTimeout="5000">
			<target xsi:type="ElasticSearch"
				requireAuth="true"
				username="myUserName"
				password="coolpassword"
				layout="Server-Date: ${longdate}; Level: ${level}; Message: ${message}"
				uri="http://localhost:9200" />
		</target>
	</targets>
	<rules>
		<logger name="*" minlevel="Info" writeTo="elastic" />
	</rules>
</nlog>
```

### Outras Configurações NLog
Veja outras opções em [NLog-Project](https://nlog-project.org/config/?tab=layout-renderers).

---

## Documentação Relacionada

- [OpenTelemetry Logging](observability/logging.md) - Guia completo de logging moderno com OpenTelemetry
- [Tracing com OpenTelemetry](observability/tracing.md) - Configuração de rastreamento distribuído
- [Métricas e Monitoramento](observability/metrics.md) - Métricas de aplicação
- [Migração do TelemetryHelper](observability/migration.md) - Guia de migração para código legado
