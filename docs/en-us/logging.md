# Logging

Modern .NET applications use `ILogger<T>` from `Microsoft.Extensions.Logging` as the standard logging abstraction. Mvp24Hours provides extensions that integrate with OpenTelemetry for distributed tracing correlation, structured logging, and observability.

## Modern Logging with ILogger

The recommended approach for .NET 9+ applications is to use `ILogger<T>` with the Mvp24Hours observability extensions.

### Quick Start

```csharp
/// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add Mvp24Hours logging with trace correlation
builder.Services.AddMvp24HoursLogging(options =>
{
    options.ServiceName = "MyService";
    options.ServiceVersion = "1.0.0";
    options.EnableTraceCorrelation = true;
});

// Apply default log levels
builder.Logging.AddMvp24HoursDefaults();
```

### Using ILogger in Services

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
        
        // ... process order
        
        _logger.LogInformation("Order {OrderId} processed successfully", order.Id);
    }
}
```

## Structured Logging (Message Templates)

Structured logging allows you to capture log data in a queryable format. Use **message templates** instead of string interpolation:

### Good Practices

```csharp
// ✅ Good - structured logging with message templates
_logger.LogInformation(
    "Processing order {OrderId} for {CustomerId}",
    order.Id,
    order.CustomerId);

// ❌ Bad - string interpolation (loses structure)
_logger.LogInformation(
    $"Processing order {order.Id} for {order.CustomerId}");
```

### Log Levels Guide

| Level | Use For |
|-------|---------|
| `Trace` | Detailed diagnostic information (dev only) |
| `Debug` | Debugging information for developers |
| `Information` | Normal application flow, business events |
| `Warning` | Unusual but recoverable situations |
| `Error` | Errors that prevent operation completion |
| `Critical` | System-wide failures requiring immediate attention |

## OpenTelemetry Integration

Mvp24Hours provides deep integration between `ILogger` and OpenTelemetry, enabling automatic correlation between logs and distributed traces.

### Configure OpenTelemetry Logging

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

### All-in-One Observability

For complete observability (logs, traces, and metrics):

```csharp
services.AddMvp24HoursObservability(options =>
{
    options.ServiceName = "MyService";
    options.ServiceVersion = "1.0.0";
    
    // Enable all pillars
    options.EnableLogging = true;
    options.EnableTracing = true;
    options.EnableMetrics = true;
    
    // Logging-specific options
    options.Logging.EnableTraceCorrelation = true;
});
```

> 📚 For complete documentation on logging with OpenTelemetry, see [OpenTelemetry Logging](observability/logging.md).

## Log Scopes

Use scopes to add context to groups of log entries:

```csharp
using (_logger.BeginScope(new Dictionary<string, object>
{
    ["OrderId"] = order.Id,
    ["CustomerId"] = order.CustomerId
}))
{
    // All logs within this scope include OrderId and CustomerId
    _logger.LogInformation("Starting order processing");
    // ... more operations
    _logger.LogInformation("Order processing completed");
}
```

### Built-in Scope Factories

```csharp
// HTTP Request scope
using (LogScopeFactory.BeginHttpScope(_logger, "POST", "/api/orders"))
{
    _logger.LogInformation("Processing HTTP request");
}

// Database operation scope
using (LogScopeFactory.BeginDbScope(_logger, "sqlserver", "INSERT", "Orders"))
{
    _logger.LogInformation("Inserting order into database");
}

// Messaging scope
using (LogScopeFactory.BeginMessagingScope(_logger, "rabbitmq", "orders-queue", messageId))
{
    _logger.LogInformation("Processing message");
}
```

## Configuration via appsettings.json

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

## Legacy: TelemetryHelper

> ⚠️ **Deprecated:** `TelemetryHelper` is deprecated. Use `ILogger<T>` with the Mvp24Hours logging extensions instead. See [Migration Guide](observability/migration.md) for migration instructions.

---

## Third-Party Logging Libraries

### Serilog

Serilog is a popular diagnostic logging library for .NET applications. It integrates well with OpenTelemetry.

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

Learn more: [Serilog](https://serilog.net/)

### NLog

NLog is an easy to configure library with multiple output targets.

Learn more: [NLog ASP.NET Core](https://github.com/NLog/NLog/wiki/Getting-started-with-ASP.NET-Core-3)

Follow the xml file templates for NLog configuration.

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

### Log File
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

### Other NLog Settings
See other options at [NLog-Project](https://nlog-project.org/config/?tab=layout-renderers).

---

## Related Documentation

- [OpenTelemetry Logging](observability/logging.md) - Complete guide to modern logging with OpenTelemetry
- [Tracing with OpenTelemetry](observability/tracing.md) - Distributed tracing setup
- [Metrics and Monitoring](observability/metrics.md) - Application metrics
- [Migration from TelemetryHelper](observability/migration.md) - Migration guide for legacy code
