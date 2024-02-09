# ASP.NET Web API
In this topic you will find some resources to speed up building web services with ASP.NET Web API.

## Services
```csharp
// Startup.cs => ConfigureServices(IServiceCollection)

/// essencial
services.AddMvp24HoursWebEssential();

/// automapper
services.AddMvp24HoursMapService(assemblyMap: Assembly.GetExecutingAssembly());

/// json
services.AddMvp24HoursWebJson();

/// swagger
services.AddMvp24HoursWebSwagger("MyAPI");

/// compreension
services.AddMvp24HoursWebGzip();

/// exception middleware
services.AddMvp24HoursWebExceptions(options => { });

/// cors middleware
services.AddMvp24HoursWebCors(options => { });
```

### Health Checks ([AspNetCore.Diagnostics.HealthChecks](https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks))

```csharp

// Package Manager Console >
Install-Package AspNetCore.HealthChecks.UI.Client 3.1.2
/// SQLServer
Install-Package AspNetCore.HealthChecks.SqlServer 3.1.3
/// PostgreSQL
Install-Package AspNetCore.HealthChecks.NpgSql 3.1.1
/// MySQL
Install-Package AspNetCore.HealthChecks.MySql 3.2.0
/// MongoDB
Install-Package AspNetCore.HealthChecks.MongoDb 3.1.3
/// Redis
Install-Package AspNetCore.HealthChecks.Redis 3.1.2
/// RabbitMQ
Install-Package AspNetCore.HealthChecks.Rabbitmq 3.1.4

// Startup.cs

/// ConfigureServices(IServiceCollection)

services.AddHealthChecks()

    /// SQLServer
    .AddSqlServer(
        configuration.GetConnectionString("CustomerDbContext"),
        healthQuery: "SELECT 1;",
        name: "SqlServer",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded);

    /// MongoDB
    .AddMongoDb(
        configuration.GetConnectionString("MongoDbContext"),
        name: "MongoDb",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded);

    /// Redis
    .AddRedis(
        configuration.GetConnectionString("RedisDbContext"),
        name: "Redis",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded);

    /// RabbitMQ
    .AddRabbitMQ(
        configuration.GetConnectionString("RabbitMQContext"),
        name: "RabbitMQ",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded);

/// Configure(IApplicationBuilder,IWebHostEnvironment)

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
        endpoints.MapHealthChecks("/hc", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
    });

```

## Application / Middlewares

```csharp
// Startup.cs  => Configure(IApplicationBuilder,IWebHostEnvironment)

/// exception handlers
app.UseMvp24HoursExceptionHandling();

/// cors
app.UseMvp24HoursCors();

/// swagger
if (!env.IsProduction()) {
    app.UseMvp24HoursSwagger();
}

/// correlation-id
app.UseMvp24HoursCorrelationId();
```

## Resilient HTTP Requests
Use IHttpClientFactory to implement resilient HTTP requests.

```csharp
// Startup.cs  => ConfigureServices(IServiceCollection)

/// inject httpclient using custom name
services.AddHttpClient("my-api-url", client =>
{
    client.BaseAddress = new Uri("https://myexampleapi.com");
});

/// inject HttpClient using Class name
services.AddHttpClient<MyClassClient>(client =>
{
    client.BaseAddress = new Uri("https://myexampleapi.com");
});

// MyClassClient.cs

/// through the constructor, if you have registered the client class "AddHttpClient<MyClassClient>"
private readonly HttpClient _httpClient;
public MyClassClient(HttpClient httpClient)
{
    _httpClient = httpClient;
}
/// return string => "_httpClient.HttpGetAsync()"
public async Task<string> GetResults() {
    return await _httpClient.HttpGetAsync("api/myService");
}
/// return object => "_httpClient.HttpGetAsync<MyResult>()"
public async Task<MyResult> GetResults() {
    return await _httpClient.HttpGetAsync<MyResult>("api/myService");
}

/// service provider using custom name
var factory = serviceProvider.GetService<IHttpClientFactory>();
var client = factory.CreateClient("my-api-url");
var result = await client.HttpGetAsync("api/myService");

/// service provider using reference class
var factory = serviceProvider.GetService<IHttpClientFactory>();
var client = factory.CreateClient(typeof(MyClassClient).Name);
var result = await client.HttpGetAsync("api/myService");
```

## Minimal API
Use the ExtensionBinder class to handle parameter-to-object conversion:
```csharp
    public class CustomerFilter : ExtensionBinder<CustomerFilter>
    {
        public string Name { get; set; }
        public bool? Active { get; set; }
    }
```

You can also use the ModelBinder class as an Action input parameter if you don't want to update your existing classes:
```csharp
app.MapGet("/customer", async (CustomerFilter model, ModelBinder<PagingCriteriaRequest> pagingCriteriaBinder, [FromServices] IUnitOfWorkAsync uoW) =>
{
    if (pagingCriteriaBinder.Error != null)
        return Results.BadRequest(pagingCriteriaBinder.Error);

    var pagingCriteria = pagingCriteriaBinder.Data;

    //...

    return Results.Ok(result);
})
.WithName("CustomerGetBy")
.WithOpenApi();
```

If you want to implement your own conversion, use the IExtensionBinder interface:
```csharp
public class Customer : IExtensionBinder<Customer>
{
    /// <summary>
    /// 
    /// </summary>
    public string Nome { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public static ValueTask<Customer> BindAsync(HttpContext context)
    {
        return ValueTask.FromResult(context.Request.GetFromQueryString<T>() ?? new());
    }
}
```