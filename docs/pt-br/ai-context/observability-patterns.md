# Padrões de Observabilidade para Agentes de IA

> **Instrução para Agente de IA**: Use estes padrões para implementar observabilidade abrangente incluindo logging, tracing, métricas e health checks.

---

## Logging com NLog

### Instalação de Pacotes

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

    <!-- Arquivo (Todos os logs) -->
    <target xsi:type="File" name="allfile"
            fileName="${logDirectory}/all-${shortdate}.log"
            layout="${longdate}|${level:uppercase=true}|${logger}|${message}|${exception:format=tostring}"
            archiveEvery="Day"
            archiveNumbering="Rolling"
            maxArchiveFiles="30" />

    <!-- Arquivo (Apenas erros) -->
    <target xsi:type="File" name="errorfile"
            fileName="${logDirectory}/error-${shortdate}.log"
            layout="${longdate}|${level:uppercase=true}|${logger}|${message}|${exception:format=tostring}"
            archiveEvery="Day"
            archiveNumbering="Rolling"
            maxArchiveFiles="90" />

    <!-- Formato JSON para logging estruturado -->
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
    <!-- Pular logs da Microsoft -->
    <logger name="Microsoft.*" maxlevel="Info" final="true" />
    <logger name="System.Net.Http.*" maxlevel="Info" final="true" />

    <!-- Todos os logs -->
    <logger name="*" minlevel="Debug" writeTo="allfile" />
    <logger name="*" minlevel="Info" writeTo="console" />
    <logger name="*" minlevel="Error" writeTo="errorfile" />
    <logger name="*" minlevel="Info" writeTo="jsonfile" />
  </rules>
</nlog>
```

### Configuração Program.cs

```csharp
using NLog;
using NLog.Web;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    logger.Debug("Aplicação iniciando...");

    var builder = WebApplication.CreateBuilder(args);

    // NLog
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // ... outras configurações

    var app = builder.Build();

    logger.Info("Aplicação iniciada com sucesso");
    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Aplicação parou devido a exceção");
    throw;
}
finally
{
    LogManager.Shutdown();
}
```

---

## Integração OpenTelemetry

### Instalação de Pacotes

```xml
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.*" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.*" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.*" />
<PackageReference Include="OpenTelemetry.Instrumentation.SqlClient" Version="1.*" />
<PackageReference Include="OpenTelemetry.Exporter.Console" Version="1.*" />
<PackageReference Include="OpenTelemetry.Exporter.Jaeger" Version="1.*" />
<PackageReference Include="OpenTelemetry.Exporter.Prometheus.AspNetCore" Version="1.*" />
```

### Configuração

```csharp
// appsettings.json
{
  "OpenTelemetry": {
    "ServiceName": "NomeProjeto.API",
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

---

## Health Checks

### Instalação de Pacotes

```xml
<PackageReference Include="AspNetCore.HealthChecks.UI.Client" Version="8.*" />
<PackageReference Include="AspNetCore.HealthChecks.SqlServer" Version="8.*" />
<PackageReference Include="AspNetCore.HealthChecks.NpgSql" Version="8.*" />
<PackageReference Include="AspNetCore.HealthChecks.MySql" Version="8.*" />
<PackageReference Include="AspNetCore.HealthChecks.MongoDb" Version="8.*" />
<PackageReference Include="AspNetCore.HealthChecks.Redis" Version="8.*" />
<PackageReference Include="AspNetCore.HealthChecks.Rabbitmq" Version="8.*" />
```

### Configuração

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
        
        // Health check customizado
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

### Health Check Customizado

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NomeProjeto.WebAPI.HealthChecks
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
                
                // Verificar se conseguimos consultar o banco de dados
                var repository = unitOfWork.GetRepository<Cliente>();
                var podeConectar = await repository.ListAnyAsync(x => true);

                return HealthCheckResult.Healthy("Conexão com banco de dados está saudável");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Conexão com banco de dados falhou", ex);
            }
        }
    }
}
```

---

## Middleware de CorrelationId

```csharp
namespace NomeProjeto.WebAPI.Middlewares
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
            var correlationId = ObterOuCriarCorrelationId(context);
            
            // Adicionar aos headers de resposta
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[CorrelationIdHeader] = correlationId;
                return Task.CompletedTask;
            });

            // Adicionar ao escopo de logging
            using (NLog.MappedDiagnosticsLogicalContext.SetScoped("CorrelationId", correlationId))
            {
                await _next(context);
            }
        }

        private string ObterOuCriarCorrelationId(HttpContext context)
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

// Uso em Program.cs
app.UseCorrelationId();
```

---

## Middleware de Tratamento de Exceções

```csharp
using System.Net;
using System.Text.Json;

namespace NomeProjeto.WebAPI.Middlewares
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
                _logger.LogError(ex, "Exceção não tratada ocorreu");
                await TratarExcecaoAsync(context, ex);
            }
        }

        private static async Task TratarExcecaoAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, message) = exception switch
            {
                ArgumentException => (HttpStatusCode.BadRequest, exception.Message),
                KeyNotFoundException => (HttpStatusCode.NotFound, exception.Message),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Não autorizado"),
                _ => (HttpStatusCode.InternalServerError, "Ocorreu um erro ao processar sua requisição")
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

// Uso em Program.cs
app.UseExceptionHandling();
```

---

## Configuração Completa de Observabilidade

```csharp
// ServiceBuilderExtensions.cs
public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
{
    // Logging (NLog é configurado via Program.cs)
    
    // Health Checks
    services.AddHealthChecks()
        .AddSqlServer(configuration.GetConnectionString("DefaultConnection"))
        .AddCheck<CustomHealthCheck>("custom");

    // OpenTelemetry
    var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "NomeProjeto.API";

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

    // Métricas Customizadas
    services.AddSingleton<ClienteMetrics>();

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

## Documentação Relacionada

- [Templates de Arquitetura](architecture-templates.md)
- [Matriz de Decisão](decision-matrix.md)
- [Padrões de Modernização](modernization-patterns.md)

