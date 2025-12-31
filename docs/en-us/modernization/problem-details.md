# ProblemDetails (RFC 7807)

The `ProblemDetails` specification (RFC 7807) provides a standardized way to communicate errors in HTTP APIs. Starting with .NET 7, ASP.NET Core includes native support for `ProblemDetails` through the `IProblemDetailsService` interface.

## Overview

ProblemDetails provides a consistent error response format across your entire API:

```json
{
  "type": "https://httpstatuses.com/not-found",
  "title": "Resource Not Found",
  "status": 404,
  "detail": "Customer with ID '123' was not found.",
  "instance": "/api/customers/123",
  "traceId": "00-abc123-def456-00",
  "entityName": "Customer",
  "entityId": "123"
}
```

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                        ProblemDetails Flow                          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌──────────────┐    ┌─────────────────┐    ┌──────────────────┐   │
│  │   Exception  │───▶│  Exception      │───▶│  ProblemDetails  │   │
│  │   Thrown     │    │  Handler        │    │  Response        │   │
│  └──────────────┘    └─────────────────┘    └──────────────────┘   │
│                             │                                       │
│                             ▼                                       │
│                    ┌─────────────────┐                              │
│                    │ IProblemDetails │                              │
│                    │     Service     │                              │
│                    └─────────────────┘                              │
│                             │                                       │
│         ┌──────────────────┴──────────────────┐                    │
│         ▼                                     ▼                    │
│  ┌─────────────────┐                ┌─────────────────┐            │
│  │  Mvp24Hours     │                │  Custom         │            │
│  │  Mapper         │                │  Mapper         │            │
│  └─────────────────┘                └─────────────────┘            │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

## Installation

ProblemDetails support is included in the `Mvp24Hours.WebAPI` package.

```bash
dotnet add package Mvp24Hours.WebAPI
```

## Basic Configuration

### Using Native .NET ProblemDetails (Recommended)

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add native ProblemDetails with Mvp24Hours exception mappings
builder.Services.AddNativeProblemDetails(options =>
{
    options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    options.IncludeStackTrace = builder.Environment.IsDevelopment();
    options.ProblemTypeBaseUri = "https://api.example.com/errors";
});

var app = builder.Build();

// Use native ProblemDetails exception handling
app.UseNativeProblemDetailsHandling();

app.MapControllers();
app.Run();
```

### Simplified Configuration

```csharp
// One-liner for full configuration based on environment
builder.Services.AddNativeProblemDetailsAll(builder.Environment);

var app = builder.Build();
app.UseNativeProblemDetailsHandling();
```

### Using Custom Middleware (Legacy)

For applications that need more control or are using older patterns:

```csharp
builder.Services.AddMvp24HoursProblemDetails(options =>
{
    options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
});

var app = builder.Build();
app.UseMvp24HoursProblemDetails();
```

## Exception Mappings

The framework automatically maps Mvp24Hours exceptions to appropriate HTTP status codes:

| Exception | HTTP Status | Title |
|-----------|-------------|-------|
| `NotFoundException` | 404 | Resource Not Found |
| `ValidationException` | 400 | Validation Failed |
| `UnauthorizedException` | 401 | Authentication Required |
| `ForbiddenException` | 403 | Access Denied |
| `ConflictException` | 409 | Resource Conflict |
| `DomainException` | 422 | Domain Rule Violation |
| `BusinessException` | 422 | Business Rule Violation |
| `ArgumentException` | 400 | Invalid Argument |
| `ArgumentNullException` | 400 | Missing Required Value |
| `InvalidOperationException` | 409 | Invalid Operation |
| `TimeoutException` | 408 | Request Timeout |
| `OperationCanceledException` | 499 | Request Cancelled |
| `NotImplementedException` | 501 | Not Implemented |
| Other exceptions | 500 | Internal Server Error |

## TypedResults.Problem() for Minimal APIs

The `TypedResultsExtensions` class provides helper methods for creating `ProblemDetails` responses in Minimal APIs:

### Converting Exceptions to ProblemDetails

```csharp
app.MapPost("/orders", async (CreateOrderCommand command) =>
{
    try
    {
        var result = await handler.HandleAsync(command);
        return TypedResults.Ok(result);
    }
    catch (Exception ex)
    {
        return ex.ToProblem(); // Converts exception to ProblemHttpResult
    }
});

// With stack trace (development only!)
app.MapPost("/orders", async (CreateOrderCommand command) =>
{
    try
    {
        return TypedResults.Ok(await handler.HandleAsync(command));
    }
    catch (Exception ex)
    {
        return builder.Environment.IsDevelopment() 
            ? ex.ToProblemWithStackTrace() 
            : ex.ToProblem();
    }
});
```

### Creating Specific Problem Responses

```csharp
// Not Found
app.MapGet("/orders/{id}", async (Guid id) =>
{
    var order = await repository.GetByIdAsync(id);
    return order is null 
        ? TypedResultsExtensions.NotFoundProblem("Order", id)
        : TypedResults.Ok(order);
});

// Validation Problem
app.MapPost("/orders", async (CreateOrderCommand command) =>
{
    var errors = validator.Validate(command);
    if (errors.Any())
    {
        return TypedResultsExtensions.ValidationProblem(errors);
    }
    return TypedResults.Created($"/orders/{order.Id}", order);
});

// Conflict
app.MapPost("/orders", async (CreateOrderCommand command) =>
{
    if (await repository.ExistsAsync(command.OrderNumber))
    {
        return TypedResultsExtensions.ConflictProblem(
            $"Order with number '{command.OrderNumber}' already exists.",
            "Order");
    }
    return TypedResults.Created($"/orders/{order.Id}", order);
});

// Forbidden
app.MapDelete("/orders/{id}", async (Guid id, ClaimsPrincipal user) =>
{
    if (!user.HasPermission("orders:delete"))
    {
        return TypedResultsExtensions.ForbiddenProblem(
            "You do not have permission to delete orders.",
            "Order",
            "orders:delete");
    }
    await repository.DeleteAsync(id);
    return TypedResults.NoContent();
});

// Unauthorized
app.MapGet("/protected", (ClaimsPrincipal user) =>
{
    if (!user.Identity?.IsAuthenticated ?? true)
    {
        return TypedResultsExtensions.UnauthorizedProblem(
            "Authentication is required to access this resource.",
            "Bearer");
    }
    return TypedResults.Ok(new { message = "Welcome!" });
});

// Domain Error
app.MapPost("/orders/{id}/cancel", async (Guid id) =>
{
    var order = await repository.GetByIdAsync(id);
    if (order.Status == OrderStatus.Shipped)
    {
        return TypedResultsExtensions.DomainProblem(
            "Cannot cancel an order that has already been shipped.",
            "Order",
            "OrderMustNotBeShipped");
    }
    order.Cancel();
    return TypedResults.Ok(order);
});

// Internal Server Error
app.MapGet("/data", async () =>
{
    try
    {
        return TypedResults.Ok(await GetDataAsync());
    }
    catch
    {
        return TypedResultsExtensions.InternalServerErrorProblem();
    }
});

// Custom Status Code
app.MapPost("/upload", async () =>
{
    if (fileSizeLimitExceeded)
    {
        return TypedResultsExtensions.CustomProblem(
            StatusCodes.Status413PayloadTooLarge,
            "Payload Too Large",
            "The uploaded file exceeds the maximum allowed size of 10MB.",
            extensions: new Dictionary<string, object?>
            {
                ["maxSize"] = "10MB",
                ["actualSize"] = "15MB"
            });
    }
    return TypedResults.Ok();
});
```

## Converting BusinessResult to ProblemDetails

```csharp
app.MapGet("/orders/{id}", async (Guid id, ISender sender) =>
{
    var result = await sender.SendAsync(new GetOrderQuery(id));
    return result.ToTypedResult(); // Automatically maps errors to ProblemDetails
});

// With null data allowed
app.MapGet("/orders/{id}/details", async (Guid id, ISender sender) =>
{
    var result = await sender.SendAsync(new GetOrderDetailsQuery(id));
    return result.ToTypedResultAllowNull(); // Returns 200 OK even if data is null
});
```

## Custom Exception Mappings

### Adding Custom Exception Types

```csharp
builder.Services.AddMvp24HoursProblemDetails(mappings =>
{
    mappings[typeof(MyCustomException)] = HttpStatusCode.BadRequest;
    mappings[typeof(RateLimitExceededException)] = (HttpStatusCode)429;
    mappings[typeof(PaymentRequiredException)] = HttpStatusCode.PaymentRequired;
});
```

### Custom Exception Mapper

```csharp
public class CustomExceptionMapper : IExceptionToProblemDetailsMapper
{
    public bool CanHandle(Exception exception) => exception is MyCustomException;

    public int GetStatusCode(Exception exception) => 422;

    public ProblemDetails Map(Exception exception, HttpContext context)
    {
        var customEx = (MyCustomException)exception;
        return new ProblemDetails
        {
            Status = 422,
            Title = "Custom Error",
            Detail = customEx.Message,
            Type = "https://api.example.com/errors/custom",
            Instance = context.Request.Path,
            Extensions =
            {
                ["customField"] = customEx.CustomField,
                ["traceId"] = context.TraceIdentifier
            }
        };
    }
}

// Register the custom mapper
builder.Services.AddMvp24HoursExceptionMapper<CustomExceptionMapper>();
```

## Configuration Options

### MvpProblemDetailsOptions

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `IncludeExceptionDetails` | `bool` | `false` | Include exception type and message in response |
| `IncludeStackTrace` | `bool` | `false` | Include stack trace (dev only!) |
| `ProblemTypeBaseUri` | `string?` | `null` | Base URI for problem type documentation |
| `DefaultTitle` | `string` | "An error occurred..." | Default title for unmapped exceptions |
| `FallbackStatusCode` | `int` | `500` | Default status code for unmapped exceptions |
| `FallbackTitle` | `string` | "Internal Server Error" | Fallback title |
| `FallbackDetail` | `string` | "An unexpected error..." | Fallback detail message |
| `LogExceptions` | `bool` | `true` | Log exceptions before returning response |
| `IncludeCorrelationId` | `bool` | `true` | Include correlation ID in response |
| `CorrelationIdHeaderName` | `string` | "X-Correlation-ID" | Header name for correlation ID |
| `UseRfc7807ContentType` | `bool` | `true` | Use application/problem+json content type |
| `ExceptionMappings` | `Dictionary<Type, HttpStatusCode>` | `{}` | Custom exception type mappings |
| `StatusCodeMapper` | `Func<Exception, int>?` | `null` | Custom function to determine status code |
| `CustomMapper` | `Func<Exception, HttpContext, ProblemDetails?>?` | `null` | Custom function to map to ProblemDetails |
| `EnrichProblemDetails` | `Action<ProblemDetails, Exception, HttpContext>?` | `null` | Enrich ProblemDetails with additional data |

### Example Configuration

```csharp
builder.Services.AddNativeProblemDetails(options =>
{
    // Development settings
    options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    options.IncludeStackTrace = builder.Environment.IsDevelopment();
    
    // Custom problem type URI
    options.ProblemTypeBaseUri = "https://api.example.com/errors";
    
    // Correlation ID
    options.IncludeCorrelationId = true;
    options.CorrelationIdHeaderName = "X-Request-ID";
    
    // Custom exception mappings
    options.ExceptionMappings[typeof(RateLimitExceededException)] = (HttpStatusCode)429;
    
    // Enrich all responses
    options.EnrichProblemDetails = (problemDetails, exception, context) =>
    {
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;
        problemDetails.Extensions["environment"] = builder.Environment.EnvironmentName;
    };
});
```

## Integration with CQRS

The ProblemDetails system integrates seamlessly with the Mvp24Hours CQRS module:

```csharp
app.MapPost("/api/orders", async (CreateOrderCommand command, ISender sender) =>
{
    try
    {
        var result = await sender.SendAsync(command);
        return result.ToTypedResult();
    }
    catch (ValidationException ex)
    {
        return ex.ToProblem();
    }
})
.WithName("CreateOrder")
.Produces<OrderDto>(StatusCodes.Status201Created)
.ProducesProblem(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status404NotFound);
```

## Best Practices

1. **Use native ProblemDetails** for .NET 8+ applications
2. **Never expose stack traces** in production
3. **Use structured error codes** for client-side error handling
4. **Include correlation IDs** for distributed tracing
5. **Document your error types** with OpenAPI annotations
6. **Use TypedResults helpers** for consistent responses in Minimal APIs
7. **Map domain exceptions** to appropriate HTTP status codes

## Comparison: Native vs Custom Middleware

| Feature | Native (`AddNativeProblemDetails`) | Custom (`AddMvp24HoursProblemDetails`) |
|---------|-----------------------------------|----------------------------------------|
| .NET Version | 8+ | All |
| Integration | Built-in ASP.NET Core | Custom middleware |
| Content Negotiation | Automatic | Manual |
| Status Code Pages | Integrated | Separate |
| Performance | Optimized | Good |
| Flexibility | Standard | Maximum |

## See Also

- [RFC 7807 - Problem Details for HTTP APIs](https://datatracker.ietf.org/doc/html/rfc7807)
- [Exception Handling in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling)
- [Minimal APIs in .NET](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis)

