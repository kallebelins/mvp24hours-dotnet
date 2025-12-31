# Minimal APIs with TypedResults (.NET 9)

> Extensions for creating type-safe, AOT-friendly Minimal API endpoints using native .NET 9 TypedResults.

## Overview

The Mvp24Hours framework provides modern extensions for building Minimal APIs with the following features:

- **TypedResults** - Strongly-typed HTTP responses with compile-time checking
- **Automatic CQRS mapping** - Map commands and queries to HTTP endpoints
- **Validation integration** - FluentValidation with TypedResults responses
- **Exception handling** - Convert exceptions to RFC 7807 ProblemDetails
- **OpenAPI support** - Better documentation generation

## Why TypedResults?

| Feature | Results.* | TypedResults.* |
|---------|-----------|----------------|
| Type Safety | Runtime | Compile-time |
| OpenAPI | Basic | Enhanced metadata |
| AOT Compilation | Limited | Full support |
| IntelliSense | Generic | Specific types |
| .NET 9 Features | N/A | InternalServerError() |

## Installation

TypedResults are available in the `Mvp24Hours.WebAPI` package:

```bash
dotnet add package Mvp24Hours.WebAPI
```

## Basic Usage

### Converting IBusinessResult to TypedResults

```csharp
using Mvp24Hours.WebAPI.Endpoints;

app.MapGet("/orders/{id}", async (Guid id, ISender sender) =>
{
    var result = await sender.SendAsync<IBusinessResult<OrderDto>>(new GetOrderQuery(id));
    return result.ToNativeTypedResult();
});
```

### Using Native TypedResults Helpers

```csharp
using static Mvp24Hours.WebAPI.Endpoints.NativeTypedResultsExtensions;

app.MapGet("/orders/{id}", async (Guid id) =>
{
    var order = await repository.GetByIdAsync(id);
    return order is null 
        ? NotFound("Order", id)
        : Ok(order);
});

app.MapPost("/orders", async (CreateOrderCommand command) =>
{
    try
    {
        var order = await handler.HandleAsync(command);
        return Created($"/orders/{order.Id}", order);
    }
    catch (Exception ex)
    {
        return InternalServerError(ex.Message);
    }
});
```

## CQRS Endpoint Mapping

### Command Endpoints

```csharp
// Basic command mapping
app.MapNativeCommand<CreateOrderCommand, OrderDto>(
    "/api/orders",
    HttpMethod.Post,
    endpoint => endpoint
        .RequireAuthorization()
        .WithTags("Orders")
        .WithSummary("Create a new order")
);

// Command returning BusinessResult
app.MapNativeCommandWithResult<CreateOrderCommand, OrderDto>("/api/orders");

// Command with Created response (201)
app.MapNativeCommandCreate<CreateOrderCommand, OrderDto>(
    "/api/orders",
    "/api/orders/{0}",
    dto => dto.Id
);

// Delete command with NoContent response (204)
app.MapNativeCommandDelete<DeleteOrderCommand, bool>("/api/orders/{id}");
```

### Query Endpoints

```csharp
// Basic query mapping
app.MapNativeQuery<GetOrderByIdQuery, OrderDto>(
    "/api/orders/{id}",
    endpoint => endpoint
        .RequireAuthorization()
        .WithTags("Orders")
);

// Query returning BusinessResult
app.MapNativeQueryWithResult<GetOrderByIdQuery, OrderDto>("/api/orders/{id}");

// List query (returns 200 even for empty results)
app.MapNativeQueryList<GetOrdersQuery, IEnumerable<OrderDto>>("/api/orders");
```

## TypedResults Conversions

### IBusinessResult<T> Extensions

| Method | Description | Success | Error |
|--------|-------------|---------|-------|
| `ToNativeTypedResult()` | Standard conversion | Ok\<T\> | Based on error code |
| `ToNativeTypedResultAllowNull()` | Allows null data | Ok\<T\> or Ok\<null\> | Based on error code |
| `ToSimpleTypedResult()` | Basic conversion | Ok\<T\> | BadRequest |
| `ToCreatedTypedResult()` | For POST operations | Created\<T\> | BadRequest/Conflict |
| `ToNoContentTypedResult()` | For DELETE/PUT | NoContent | NotFound/BadRequest |
| `ToAcceptedTypedResult()` | For async operations | Accepted\<T\> | BadRequest |

### Error Code Mapping

| Error Code | HTTP Status | TypedResult |
|------------|-------------|-------------|
| NOT_FOUND | 404 | NotFound\<ProblemDetails\> |
| CONFLICT | 409 | Conflict\<ProblemDetails\> |
| UNAUTHORIZED | 401 | Unauthorized |
| FORBIDDEN | 403 | Forbid |
| VALIDATION | 400 | ValidationProblem |
| INTERNAL_ERROR | 500 | Problem (500) |
| Default | 400 | BadRequest\<ProblemDetails\> |

## Exception Handling

### Converting Exceptions to TypedResults

```csharp
app.MapPost("/orders", async (CreateOrderCommand command, ISender sender) =>
{
    try
    {
        var result = await sender.SendAsync(command);
        return TypedResults.Ok(result);
    }
    catch (Exception ex)
    {
        return ex.ToNativeTypedProblem();
    }
});

// With stack trace (development only!)
app.MapPost("/orders", async (CreateOrderCommand command, IHostEnvironment env) =>
{
    try
    {
        // ...
    }
    catch (Exception ex)
    {
        return env.IsDevelopment() 
            ? ex.ToNativeTypedProblemWithStackTrace() 
            : ex.ToNativeTypedProblem();
    }
});
```

### Exception Mapping

| Exception | Status | Title |
|-----------|--------|-------|
| NotFoundException | 404 | Resource Not Found |
| ValidationException | 400 | Validation Failed |
| UnauthorizedException | 401 | Authentication Required |
| ForbiddenException | 403 | Access Denied |
| ConflictException | 409 | Resource Conflict |
| DomainException | 422 | Domain Rule Violation |
| TimeoutException | 408 | Request Timeout |
| Other | 500 | Internal Server Error |

## Endpoint Filters

### Validation Filter

```csharp
// Using FluentValidation with TypedResults
app.MapPost("/orders", handler)
   .WithNativeValidation<CreateOrderCommand>();
```

### Exception Handling Filter

```csharp
app.MapPost("/orders", handler)
   .WithExceptionHandling();
```

### Logging Filter

```csharp
app.MapPost("/orders", handler)
   .WithLogging();
```

### Correlation ID Filter

```csharp
app.MapPost("/orders", handler)
   .WithCorrelationId();
```

### Idempotency Filter

```csharp
app.MapPost("/orders", handler)
   .WithIdempotency(); // Reads Idempotency-Key header
```

### Timeout Filter

```csharp
app.MapPost("/orders", handler)
   .WithTimeout(30); // 30 seconds timeout
```

### Combined Filters

```csharp
// Apply all standard filters
app.MapPost("/orders", handler)
   .WithStandardFilters<CreateOrderCommand>();

// Equivalent to:
app.MapPost("/orders", handler)
   .WithCorrelationId()
   .WithLogging()
   .WithNativeValidation<CreateOrderCommand>()
   .WithExceptionHandling();
```

## ProblemDetails Helpers

### Specific Problem Types

```csharp
using static Mvp24Hours.WebAPI.Endpoints.NativeTypedResultsExtensions;

// Not Found
return NotFound("Order", orderId);

// Conflict
return Conflict("Order already exists", "Order");

// Validation Problem
return ValidationProblem(new Dictionary<string, string[]>
{
    ["Name"] = ["Name is required"],
    ["Email"] = ["Email format is invalid"]
});

// Unprocessable Entity (Domain Error)
return UnprocessableEntity(
    "Cannot cancel a shipped order",
    entityName: "Order",
    ruleName: "OrderMustNotBeShipped");

// Internal Server Error (.NET 9)
return InternalServerError("Database connection failed");

// Custom Problem
return Problem(
    StatusCodes.Status413PayloadTooLarge,
    "Payload Too Large",
    "File exceeds 10MB limit",
    extensions: new Dictionary<string, object?>
    {
        ["maxSize"] = "10MB",
        ["actualSize"] = "15MB"
    });
```

## Complete Example

```csharp
using Mvp24Hours.WebAPI.Endpoints;
using Mvp24Hours.WebAPI.Endpoints.Filters;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddMvpMediator();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

// Configure API group
var api = app.MapGroup("/api")
    .WithCorrelationId()
    .WithExceptionHandling();

// Orders endpoints
var orders = api.MapGroup("/orders")
    .WithTags("Orders");

// Create order
orders.MapNativeCommandCreate<CreateOrderCommand, OrderDto>(
    "",
    "/api/orders/{0}",
    dto => dto.Id,
    endpoint => endpoint
        .WithSummary("Create a new order")
        .RequireAuthorization()
);

// Get order by ID
orders.MapNativeQueryWithResult<GetOrderByIdQuery, OrderDto>(
    "/{id}",
    endpoint => endpoint
        .WithSummary("Get order by ID")
);

// List orders
orders.MapNativeQueryList<GetOrdersQuery, IEnumerable<OrderDto>>(
    "",
    endpoint => endpoint
        .WithSummary("List all orders")
);

// Update order
orders.MapNativeCommandWithResult<UpdateOrderCommand, OrderDto>(
    "/{id}",
    HttpMethod.Put,
    endpoint => endpoint
        .WithSummary("Update an order")
        .RequireAuthorization()
);

// Delete order
orders.MapNativeCommandDelete<DeleteOrderCommand, bool>(
    "/{id}",
    endpoint => endpoint
        .WithSummary("Delete an order")
        .RequireAuthorization()
);

app.Run();
```

## Migration from Results to TypedResults

### Before (Results.*)

```csharp
app.MapGet("/orders/{id}", async (Guid id) =>
{
    var order = await GetOrderAsync(id);
    if (order is null)
        return Results.NotFound();
    return Results.Ok(order);
});
```

### After (TypedResults.*)

```csharp
app.MapGet("/orders/{id}", async Task<Results<Ok<OrderDto>, NotFound<ProblemDetails>>> (Guid id) =>
{
    var order = await GetOrderAsync(id);
    if (order is null)
        return TypedResults.NotFound(CreateProblemDetails(404, "Not Found", "Order not found"));
    return TypedResults.Ok(order);
});

// Or using extensions
app.MapNativeQuery<GetOrderByIdQuery, OrderDto>("/orders/{id}");
```

## Best Practices

1. **Use TypedResults for new endpoints** - Better type safety and OpenAPI support
2. **Apply filters at group level** - Reduce repetition
3. **Use specific HTTP methods** - POST for create, PUT/PATCH for update, DELETE for delete
4. **Return appropriate status codes** - 201 Created, 204 NoContent, etc.
5. **Include ProblemDetails** - RFC 7807 standard for errors
6. **Add trace IDs** - For distributed tracing
7. **Validate early** - Use validation filters

## See Also

- [ProblemDetails (RFC 7807)](problem-details.md)
- [Output Caching](output-caching.md)
- [HTTP Resilience](http-resilience.md)
- [CQRS Commands](../cqrs/commands.md)
- [CQRS Queries](../cqrs/queries.md)

