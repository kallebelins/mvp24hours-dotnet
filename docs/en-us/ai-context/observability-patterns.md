# Observability Patterns for AI Agents

> **AI Agent Instruction**: Use these patterns to implement comprehensive observability including logging, tracing, metrics, and health checks.

---

## Logging with NLog

### Package Installation

```xml
<PackageReference Include="NLog.Web.AspNetCore" Version="5.*" />
<PackageReference Include="NLog.Extensions.Logging" Version="5.*" />
```

### NLog.config

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      autoReload="true"
      throwConfigExceptions="true">

  <extensions>
    <add assembly="NLog.Web.AspNetCore"/>
  </extensions>

  <variable name="logDirectory" value="${basedir}/logs"/>

  <targets>
    <!-- Console -->
    <target xsi:type="Console" name="console"
            layout="${longdate}|${level:uppercase=true}|${logger}|${message}|${exception:format=tostring}" />

    <!-- File (All logs) -->
    <target xsi:type="File" name="allfile"
            fileName="${logDirectory}/all-${shortdate}.log"
            layout="${longdate}|${level:uppercase=true}|${logger}|${message}|${exception:format=tostring}"
            archiveEvery="Day"
            archiveNumbering="Rolling"
            maxArchiveFiles="30" />

    <!-- File (Errors only) -->
    <target xsi:type="File" name="errorfile"
            fileName="${logDirectory}/error-${shortdate}.log"
            layout="${longdate}|${level:uppercase=true}|${logger}|${message}|${exception:format=tostring}"
            archiveEvery="Day"
            archiveNumbering="Rolling"
            maxArchiveFiles="90" />

    <!-- JSON Format for structured logging -->
    <target xsi:type="File" name="jsonfile"
            fileName="${logDirectory}/structured-${shortdate}.json">
      <layout xsi:type="JsonLayout">
        <attribute name="timestamp" layout="${longdate}" />
        <attribute name="level" layout="${level:upperCase=true}" />
        <attribute name="logger" layout="${logger}" />
        <attribute name="message" layout="${message}" />
        <attribute name="exception" layout="${exception:format=tostring}" />
        <attribute name="correlationId" layout="${mdlc:item=CorrelationId}" />
        <attribute name="requestPath" layout="${aspnet-request-url}" />
        <attribute name="requestMethod" layout="${aspnet-request-method}" />
      </layout>
    </target>
  </targets>

  <rules>
    <!-- Skip Microsoft logs -->
    <logger name="Microsoft.*" maxlevel="Info" final="true" />
    <logger name="System.Net.Http.*" maxlevel="Info" final="true" />

    <!-- All logs -->
    <logger name="*" minlevel="Debug" writeTo="allfile" />
    <logger name="*" minlevel="Info" writeTo="console" />
    <logger name="*" minlevel="Error" writeTo="errorfile" />
    <logger name="*" minlevel="Info" writeTo="jsonfile" />
  </rules>
</nlog>
```

### Program.cs Configuration

```csharp
using NLog;
using NLog.Web;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    logger.Debug("Application starting...");

    var builder = WebApplication.CreateBuilder(args);

    // NLog
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // ... other configurations

    var app = builder.Build();

    logger.Info("Application started successfully");
    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Application stopped due to exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}
```

---

## OpenTelemetry Integration

### Package Installation

```xml
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.*" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.*" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.*" />
<PackageReference Include="OpenTelemetry.Instrumentation.SqlClient" Version="1.*" />
<PackageReference Include="OpenTelemetry.Exporter.Console" Version="1.*" />
<PackageReference Include="OpenTelemetry.Exporter.Jaeger" Version="1.*" />
<PackageReference Include="OpenTelemetry.Exporter.Prometheus.AspNetCore" Version="1.*" />
```

### Configuration

```csharp
// appsettings.json
{
  "OpenTelemetry": {
    "ServiceName": "ProjectName.API",
    "ServiceVersion": "1.0.0",
    "JaegerEndpoint": "http://localhost:14268/api/traces"
  }
}

// ServiceBuilderExtensions.cs
public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
{
    var serviceName = configuration["OpenTelemetry:ServiceName"];
    var serviceVersion = configuration["OpenTelemetry:ServiceVersion"];

    // Tracing
    services.AddOpenTelemetry()
        .WithTracing(builder =>
        {
            builder
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService(serviceName, serviceVersion: serviceVersion))
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.Filter = httpContext => 
                        !httpContext.Request.Path.StartsWithSegments("/health");
                })
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation(options =>
                {
                    options.SetDbStatementForText = true;
                    options.RecordException = true;
                })
                .AddSource(serviceName)
                .AddConsoleExporter()
                .AddJaegerExporter(options =>
                {
                    options.Endpoint = new Uri(configuration["OpenTelemetry:JaegerEndpoint"]);
                });
        })
        .WithMetrics(builder =>
        {
            builder
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService(serviceName, serviceVersion: serviceVersion))
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddProcessInstrumentation()
                .AddPrometheusExporter();
        });

    return services;
}
```

### Custom Tracing

```csharp
using System.Diagnostics;

namespace ProjectName.Application.Services
{
    public class CustomerService : ICustomerService
    {
        private static readonly ActivitySource ActivitySource = new("ProjectName.API");
        private readonly ILogger<CustomerService> _logger;

        public async Task<IBusinessResult<CustomerDto>> GetByIdAsync(Guid id)
        {
            using var activity = ActivitySource.StartActivity("GetCustomerById");
            activity?.SetTag("customer.id", id.ToString());

            try
            {
                _logger.LogInformation("Getting customer {CustomerId}", id);

                var customer = await _repository.GetByIdAsync(id);
                
                if (customer == null)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, "Customer not found");
                    return new BusinessResult<CustomerDto>().AddMessage("Customer not found");
                }

                activity?.SetStatus(ActivityStatusCode.Ok);
                return new BusinessResult<CustomerDto>(_mapper.Map<CustomerDto>(customer));
            }
            catch (Exception ex)
            {
                activity?.RecordException(ex);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }
    }
}
```

---

## Health Checks

### Package Installation

```xml
<PackageReference Include="AspNetCore.HealthChecks.UI.Client" Version="8.*" />
<PackageReference Include="AspNetCore.HealthChecks.SqlServer" Version="8.*" />
<PackageReference Include="AspNetCore.HealthChecks.NpgSql" Version="8.*" />
<PackageReference Include="AspNetCore.HealthChecks.MySql" Version="8.*" />
<PackageReference Include="AspNetCore.HealthChecks.MongoDb" Version="8.*" />
<PackageReference Include="AspNetCore.HealthChecks.Redis" Version="8.*" />
<PackageReference Include="AspNetCore.HealthChecks.Rabbitmq" Version="8.*" />
```

### Configuration

```csharp
// ServiceBuilderExtensions.cs
public static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
{
    services.AddHealthChecks()
        // SQL Server
        .AddSqlServer(
            configuration.GetConnectionString("DefaultConnection"),
            healthQuery: "SELECT 1;",
            name: "sqlserver",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "db", "sql" })
        
        // PostgreSQL
        .AddNpgSql(
            configuration.GetConnectionString("PostgresConnection"),
            name: "postgresql",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "db", "postgres" })
        
        // MongoDB
        .AddMongoDb(
            configuration["MongoDbOptions:ConnectionString"],
            name: "mongodb",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "db", "nosql" })
        
        // Redis
        .AddRedis(
            configuration["Redis:ConnectionString"],
            name: "redis",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "cache" })
        
        // RabbitMQ
        .AddRabbitMQ(
            $"amqp://{configuration["RabbitMQ:UserName"]}:{configuration["RabbitMQ:Password"]}@{configuration["RabbitMQ:HostName"]}:{configuration["RabbitMQ:Port"]}",
            name: "rabbitmq",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "messaging" })
        
        // Custom health check
        .AddCheck<CustomHealthCheck>("custom", tags: new[] { "custom" });

    return services;
}

// Program.cs / Startup.cs
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    Predicate = _ => true
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    Predicate = _ => false
});
```

### Custom Health Check

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ProjectName.WebAPI.HealthChecks
{
    public class CustomHealthCheck : IHealthCheck
    {
        private readonly IServiceProvider _serviceProvider;

        public CustomHealthCheck(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();
                
                // Check if we can query the database
                var repository = unitOfWork.GetRepository<Customer>();
                var canConnect = await repository.ListAnyAsync(x => true);

                return HealthCheckResult.Healthy("Database connection is healthy");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Database connection failed", ex);
            }
        }
    }
}
```

---

## Correlation ID Middleware

```csharp
namespace ProjectName.WebAPI.Middlewares
{
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private const string CorrelationIdHeader = "X-Correlation-ID";

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = GetOrCreateCorrelationId(context);
            
            // Add to response headers
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[CorrelationIdHeader] = correlationId;
                return Task.CompletedTask;
            });

            // Add to logging scope
            using (NLog.MappedDiagnosticsLogicalContext.SetScoped("CorrelationId", correlationId))
            {
                await _next(context);
            }
        }

        private string GetOrCreateCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId))
            {
                return correlationId.ToString();
            }

            return Guid.NewGuid().ToString();
        }
    }

    public static class CorrelationIdMiddlewareExtensions
    {
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CorrelationIdMiddleware>();
        }
    }
}

// Usage in Program.cs
app.UseCorrelationId();
```

---

## Exception Handling Middleware

```csharp
using System.Net;
using System.Text.Json;

namespace ProjectName.WebAPI.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, message) = exception switch
            {
                ArgumentException => (HttpStatusCode.BadRequest, exception.Message),
                KeyNotFoundException => (HttpStatusCode.NotFound, exception.Message),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized"),
                _ => (HttpStatusCode.InternalServerError, "An error occurred processing your request")
            };

            context.Response.StatusCode = (int)statusCode;

            var response = new
            {
                StatusCode = (int)statusCode,
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }

    public static class ExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionMiddleware>();
        }
    }
}

// Usage in Program.cs
app.UseExceptionHandling();
```

---

## Metrics Collection

### Custom Metrics

```csharp
using System.Diagnostics.Metrics;

namespace ProjectName.Application.Metrics
{
    public class CustomerMetrics
    {
        private readonly Counter<long> _customersCreated;
        private readonly Counter<long> _customersDeleted;
        private readonly Histogram<double> _operationDuration;

        public CustomerMetrics(IMeterFactory meterFactory)
        {
            var meter = meterFactory.Create("ProjectName.Customers");

            _customersCreated = meter.CreateCounter<long>(
                "customers_created_total",
                description: "Total number of customers created");

            _customersDeleted = meter.CreateCounter<long>(
                "customers_deleted_total",
                description: "Total number of customers deleted");

            _operationDuration = meter.CreateHistogram<double>(
                "customer_operation_duration_seconds",
                unit: "s",
                description: "Duration of customer operations");
        }

        public void RecordCustomerCreated() => _customersCreated.Add(1);
        public void RecordCustomerDeleted() => _customersDeleted.Add(1);
        public void RecordOperationDuration(double seconds) => _operationDuration.Record(seconds);
    }
}

// Registration
services.AddSingleton<CustomerMetrics>();
```

---

## Complete Observability Setup

```csharp
// ServiceBuilderExtensions.cs
public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
{
    // Logging (NLog is configured via Program.cs)
    
    // Health Checks
    services.AddHealthChecks()
        .AddSqlServer(configuration.GetConnectionString("DefaultConnection"))
        .AddCheck<CustomHealthCheck>("custom");

    // OpenTelemetry
    var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "ProjectName.API";

    services.AddOpenTelemetry()
        .WithTracing(builder => builder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSqlClientInstrumentation()
            .AddConsoleExporter())
        .WithMetrics(builder => builder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddPrometheusExporter());

    // Custom Metrics
    services.AddSingleton<CustomerMetrics>();

    return services;
}

// Program.cs
app.UseCorrelationId();
app.UseExceptionHandling();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapPrometheusScrapingEndpoint("/metrics");
```

---

## Related Documentation

- [Architecture Templates](architecture-templates.md)
- [Decision Matrix](decision-matrix.md)
- [Modernization Patterns](modernization-patterns.md)

