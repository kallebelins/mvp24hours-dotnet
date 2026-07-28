using Microsoft.AspNetCore.Http.HttpResults;
using Mvp24Hours.Core.Observability;
using Mvp24Hours.WebAPI.Extensions;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Collections.Concurrent;
using System.Diagnostics;

const string ServiceName = "simple-observability-customer-api";
const string ServiceVersion = "1.0.0";

var builder = WebApplication.CreateBuilder(args);

// ─── Mvp24Hours Observability options ───────────────────────────────────────
// AddMvp24HoursOpenTelemetry stores exporter option models only;
// the OpenTelemetry SDK is wired explicitly below.
builder.Services.AddMvp24HoursOpenTelemetry(opts =>
{
    opts.ServiceName = ServiceName;
    opts.ServiceVersion = ServiceVersion;
    opts.Environment = builder.Environment.EnvironmentName;
    opts.Console.Enabled = builder.Environment.IsDevelopment();
    opts.Console.EnableTracing = true;
    opts.Otlp.Enabled = true;
    opts.Otlp.Endpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317";
});

// ─── OpenTelemetry SDK wiring ────────────────────────────────────────────────
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        serviceName: ServiceName,
        serviceVersion: ServiceVersion,
        serviceInstanceId: Environment.MachineName))
    .WithTracing(tracing => tracing
        // Include all Mvp24Hours library activity sources
        .AddSource(OpenTelemetryBuilderExtensions.GetMvp24HoursActivitySourceNames())
        // Include the sample's own activity source
        .AddSource(CustomerActivitySource.Name)
        .AddAspNetCoreInstrumentation(opts =>
        {
            opts.RecordException = true;
            // Exclude health check traffic from traces
            opts.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
        })
        .AddHttpClientInstrumentation()
        .AddConsoleExporter()
        .AddOtlpExporter(opts =>
        {
            opts.Endpoint = new Uri(
                builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317");
        }))
    .WithMetrics(metrics => metrics
        // Include all Mvp24Hours library meters
        .AddMeter(OpenTelemetryMeterBuilderExtensions.GetMvp24HoursMeterNames())
        // Include the sample's own meter
        .AddMeter(CustomerMeter.Name)
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter(opts =>
        {
            opts.Endpoint = new Uri(
                builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317");
        }));

// Route logs to OTLP (the ILogger sink is kept alongside Console/Debug providers)
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeScopes = true;
    logging.IncludeFormattedMessage = true;
    logging.ParseStateValues = true;
    logging.AddOtlpExporter(opts =>
    {
        opts.Endpoint = new Uri(
            builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317");
    });
});

// ─── Native OpenAPI ──────────────────────────────────────────────────────────
builder.Services.AddMvp24HoursNativeOpenApi(opts =>
{
    opts.Title = "Simple Observability Customer API";
    opts.Description = "Demonstrates OpenTelemetry logs, traces, and metrics end-to-end with Mvp24Hours.";
    opts.Version = "v1";
});

// ─── ProblemDetails (RFC 7807) ───────────────────────────────────────────────
builder.Services.AddMvp24HoursProblemDetails(opts =>
    opts.IncludeExceptionDetails = builder.Environment.IsDevelopment());

// ─── Health checks ───────────────────────────────────────────────────────────
builder.Services.AddMvp24HoursHealthChecks();

// ─── In-memory customer store ────────────────────────────────────────────────
builder.Services.AddSingleton<CustomerStore>();

var app = builder.Build();

// ─── Middleware pipeline ──────────────────────────────────────────────────────
app.UseMvp24HoursProblemDetails();
app.UseHttpsRedirection();

// Native OpenAPI endpoints (Minimal API form; replaces Swashbuckle)
app.MapMvp24HoursNativeOpenApi();

app.UseMvp24HoursHealthChecks();

// ─── Customer endpoints ───────────────────────────────────────────────────────
var customers = app.MapGroup("/api/customers")
    .WithTags("Customers");

customers.MapGet("/", ListCustomers)
    .WithName("ListCustomers")
    .WithSummary("List all customers");

customers.MapGet("/{id:int}", GetCustomer)
    .WithName("GetCustomer")
    .WithSummary("Get a customer by ID");

customers.MapPost("/", CreateCustomer)
    .WithName("CreateCustomer")
    .WithSummary("Create a new customer");

customers.MapPut("/{id:int}", UpdateCustomer)
    .WithName("UpdateCustomer")
    .WithSummary("Update an existing customer");

customers.MapDelete("/{id:int}", DeleteCustomer)
    .WithName("DeleteCustomer")
    .WithSummary("Delete a customer");

app.Run();

// ─── Endpoint handlers ────────────────────────────────────────────────────────

static Results<Ok<IEnumerable<CustomerDto>>, ProblemHttpResult> ListCustomers(
    CustomerStore store,
    ILogger<Program> logger)
{
    using var activity = CustomerActivitySource.Source.StartActivity("ListCustomers");

    logger.LogInformation("Listing all customers. Count={Count}", store.Count);
    CustomerMeter.RequestCounter.Add(1, new KeyValuePair<string, object?>("operation", "list"));

    return TypedResults.Ok(store.GetAll());
}

static Results<Ok<CustomerDto>, NotFound> GetCustomer(
    int id,
    CustomerStore store,
    ILogger<Program> logger)
{
    using var activity = CustomerActivitySource.Source.StartActivity("GetCustomer");
    activity?.SetTag("customer.id", id);

    logger.LogInformation("Getting customer {CustomerId}", id);
    CustomerMeter.RequestCounter.Add(1, new KeyValuePair<string, object?>("operation", "get"));

    var customer = store.GetById(id);
    return customer is null ? TypedResults.NotFound() : TypedResults.Ok(customer);
}

static Results<Created<CustomerDto>, ValidationProblem> CreateCustomer(
    CustomerRequest request,
    CustomerStore store,
    ILogger<Program> logger)
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            { nameof(request.Name), ["Name is required."] }
        });
    }

    using var activity = CustomerActivitySource.Source.StartActivity("CreateCustomer");

    var created = store.Create(request.Name, request.Email);
    activity?.SetTag("customer.id", created.Id);

    logger.LogInformation("Created customer {CustomerId} with name {Name}", created.Id, created.Name);
    CustomerMeter.RequestCounter.Add(1, new KeyValuePair<string, object?>("operation", "create"));

    return TypedResults.Created($"/api/customers/{created.Id}", created);
}

static Results<Ok<CustomerDto>, NotFound, ValidationProblem> UpdateCustomer(
    int id,
    CustomerRequest request,
    CustomerStore store,
    ILogger<Program> logger)
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            { nameof(request.Name), ["Name is required."] }
        });
    }

    using var activity = CustomerActivitySource.Source.StartActivity("UpdateCustomer");
    activity?.SetTag("customer.id", id);

    var updated = store.Update(id, request.Name, request.Email);
    if (updated is null) return TypedResults.NotFound();

    logger.LogInformation("Updated customer {CustomerId}", id);
    CustomerMeter.RequestCounter.Add(1, new KeyValuePair<string, object?>("operation", "update"));

    return TypedResults.Ok(updated);
}

static Results<NoContent, NotFound> DeleteCustomer(
    int id,
    CustomerStore store,
    ILogger<Program> logger)
{
    using var activity = CustomerActivitySource.Source.StartActivity("DeleteCustomer");
    activity?.SetTag("customer.id", id);

    if (!store.Delete(id)) return TypedResults.NotFound();

    logger.LogInformation("Deleted customer {CustomerId}", id);
    CustomerMeter.RequestCounter.Add(1, new KeyValuePair<string, object?>("operation", "delete"));

    return TypedResults.NoContent();
}

// ─── Domain types ─────────────────────────────────────────────────────────────

/// <summary>Activity source for the Customer API's own spans.</summary>
internal static class CustomerActivitySource
{
    public const string Name = "CustomerAPI";
    public static readonly ActivitySource Source = new(Name, "1.0.0");
}

/// <summary>Meter for the Customer API's own metrics.</summary>
internal static class CustomerMeter
{
    public const string Name = "CustomerAPI";
    private static readonly System.Diagnostics.Metrics.Meter _meter = new(Name, "1.0.0");

    public static readonly System.Diagnostics.Metrics.Counter<long> RequestCounter =
        _meter.CreateCounter<long>("customer.requests", "requests", "Total customer API requests by operation");
}

public record CustomerDto(int Id, string Name, string? Email);
public record CustomerRequest(string Name, string? Email);

internal sealed class CustomerStore
{
    private readonly ConcurrentDictionary<int, CustomerDto> _data = new();
    private int _nextId = 1;

    public CustomerStore()
    {
        // Seed with a couple of entries so the API has traffic to observe immediately.
        Create("Alice Smith", "alice@example.com");
        Create("Bob Jones", "bob@example.com");
    }

    public int Count => _data.Count;

    public IEnumerable<CustomerDto> GetAll() => _data.Values.OrderBy(c => c.Id);

    public CustomerDto? GetById(int id) =>
        _data.TryGetValue(id, out var c) ? c : null;

    public CustomerDto Create(string name, string? email)
    {
        var id = Interlocked.Increment(ref _nextId);
        var dto = new CustomerDto(id, name, email);
        _data[id] = dto;
        return dto;
    }

    public CustomerDto? Update(int id, string name, string? email)
    {
        if (!_data.ContainsKey(id)) return null;
        var updated = new CustomerDto(id, name, email);
        _data[id] = updated;
        return updated;
    }

    public bool Delete(int id) => _data.TryRemove(id, out _);
}
