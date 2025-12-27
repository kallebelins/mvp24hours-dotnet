# Exceptions

The Mvp24Hours.Core module provides a comprehensive exception hierarchy for handling different error scenarios in your application.

## Exception Hierarchy

All custom exceptions inherit from `Mvp24HoursException`:

```
Mvp24HoursException (base)
├── BusinessException        - Business rule violations
├── DomainException         - Domain logic violations
├── ValidationException     - Input validation errors
├── NotFoundException       - Resource not found
├── ConflictException       - State conflicts / concurrency
├── UnauthorizedException   - Not authenticated
├── ForbiddenException      - Not authorized (no permission)
├── ConfigurationException  - Configuration errors
├── DataException          - Data access errors
├── PipelineException      - Pipeline execution errors
└── HttpStatusCodeException - HTTP-specific errors
```

## Base Exception: Mvp24HoursException

All exceptions support error codes and context information:

```csharp
public class Mvp24HoursException : Exception
{
    public string ErrorCode { get; }
    public IDictionary<string, object> Context { get; }
}
```

---

## BusinessException

Use when business rules are violated.

### When to Use

- Insufficient balance for a transaction
- Order cannot be cancelled (already shipped)
- User quota exceeded
- Business policy violation

### Examples

```csharp
using Mvp24Hours.Core.Exceptions;

// Simple usage
throw new BusinessException("Order cannot be cancelled after shipping");

// With error code
throw new BusinessException(
    "Insufficient balance for transaction",
    "INSUFFICIENT_BALANCE"
);

// With context information
throw new BusinessException(
    "Daily transfer limit exceeded",
    "TRANSFER_LIMIT_EXCEEDED",
    new Dictionary<string, object>
    {
        ["AccountId"] = accountId,
        ["DailyLimit"] = 5000m,
        ["CurrentTotal"] = 4500m,
        ["RequestedAmount"] = 1000m
    }
);
```

### HTTP Mapping

`BusinessException` typically maps to HTTP 422 (Unprocessable Entity) or 400 (Bad Request).

---

## ValidationException

Use for input validation errors.

### When to Use

- Invalid email format
- Required field is empty
- Value out of allowed range
- Invalid data format

### Examples

```csharp
using Mvp24Hours.Core.Exceptions;

// Simple validation error
throw new ValidationException("Email is required");

// With field name
throw new ValidationException("email", "Invalid email format");

// Multiple validation errors
var errors = new Dictionary<string, string[]>
{
    ["Email"] = new[] { "Email is required", "Invalid email format" },
    ["Age"] = new[] { "Age must be between 18 and 120" }
};
throw new ValidationException(errors);
```

### Integration with FluentValidation

```csharp
public class CreateCustomerValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
            
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");
    }
}

// In handler or service
var result = await validator.ValidateAsync(command);
if (!result.IsValid)
{
    throw new ValidationException(result.Errors);
}
```

### HTTP Mapping

`ValidationException` maps to HTTP 400 (Bad Request).

---

## NotFoundException

Use when a requested resource doesn't exist.

### When to Use

- Entity not found by ID
- File not found
- Configuration key missing
- Resource doesn't exist

### Examples

```csharp
using Mvp24Hours.Core.Exceptions;

// Simple usage
throw new NotFoundException("Customer not found");

// With resource type and identifier
throw new NotFoundException("Customer", customerId);

// With context
throw new NotFoundException(
    "Order",
    orderId,
    new Dictionary<string, object>
    {
        ["SearchedBy"] = "OrderNumber",
        ["Status"] = "Active orders only"
    }
);
```

### Repository Pattern Usage

```csharp
public async Task<Customer> GetCustomerAsync(Guid id)
{
    var customer = await _repository.GetByIdAsync(id);
    
    if (customer == null)
    {
        throw new NotFoundException("Customer", id);
    }
    
    return customer;
}
```

### HTTP Mapping

`NotFoundException` maps to HTTP 404 (Not Found).

---

## ConflictException

Use for state conflicts or concurrency issues.

### When to Use

- Optimistic concurrency violation
- Duplicate key insertion
- Resource already exists
- State transition not allowed

### Examples

```csharp
using Mvp24Hours.Core.Exceptions;

// Duplicate resource
throw new ConflictException("A customer with this email already exists");

// With error code
throw new ConflictException(
    "Email already registered",
    "DUPLICATE_EMAIL"
);

// Concurrency conflict
throw new ConflictException(
    "The record was modified by another user",
    "CONCURRENCY_CONFLICT",
    new Dictionary<string, object>
    {
        ["EntityType"] = "Order",
        ["EntityId"] = orderId,
        ["ExpectedVersion"] = expectedVersion,
        ["ActualVersion"] = actualVersion
    }
);
```

### HTTP Mapping

`ConflictException` maps to HTTP 409 (Conflict).

---

## UnauthorizedException

Use when the user is not authenticated.

### When to Use

- Missing authentication token
- Invalid or expired token
- Session expired

### Examples

```csharp
using Mvp24Hours.Core.Exceptions;

// Simple usage
throw new UnauthorizedException("Authentication required");

// With details
throw new UnauthorizedException(
    "Token expired",
    "TOKEN_EXPIRED",
    new Dictionary<string, object>
    {
        ["ExpiredAt"] = tokenExpiration
    }
);
```

### HTTP Mapping

`UnauthorizedException` maps to HTTP 401 (Unauthorized).

---

## ForbiddenException

Use when the user is authenticated but lacks permission.

### When to Use

- User doesn't have required role
- Resource access denied
- Operation not permitted for this user

### Examples

```csharp
using Mvp24Hours.Core.Exceptions;

// Simple usage
throw new ForbiddenException("You don't have permission to delete this resource");

// With resource information
throw new ForbiddenException(
    "Access denied",
    "ACCESS_DENIED",
    new Dictionary<string, object>
    {
        ["Resource"] = "AdminPanel",
        ["RequiredRole"] = "Administrator",
        ["UserRole"] = currentUserRole
    }
);
```

### HTTP Mapping

`ForbiddenException` maps to HTTP 403 (Forbidden).

---

## DomainException

Use for domain logic violations.

### When to Use

- Invariant violation in an aggregate
- Invalid state transition
- Domain rule breach

### Examples

```csharp
using Mvp24Hours.Core.Exceptions;

// State transition error
throw new DomainException("Cannot ship an order that hasn't been paid");

// Invariant violation
throw new DomainException(
    "Order must have at least one item",
    "ORDER_EMPTY"
);

// With aggregate info
throw new DomainException(
    "Cannot add more than 100 items to a single order",
    "ORDER_ITEM_LIMIT",
    new Dictionary<string, object>
    {
        ["OrderId"] = orderId,
        ["CurrentItems"] = 100,
        ["MaxItems"] = 100
    }
);
```

### HTTP Mapping

`DomainException` maps to HTTP 422 (Unprocessable Entity).

---

## Exception Handling in Web API

### Global Exception Handler

```csharp
public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, errorResponse) = exception switch
        {
            ValidationException ex => (400, CreateResponse(ex)),
            NotFoundException ex => (404, CreateResponse(ex)),
            UnauthorizedException ex => (401, CreateResponse(ex)),
            ForbiddenException ex => (403, CreateResponse(ex)),
            ConflictException ex => (409, CreateResponse(ex)),
            BusinessException ex => (422, CreateResponse(ex)),
            DomainException ex => (422, CreateResponse(ex)),
            _ => (500, CreateGenericResponse())
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(errorResponse, cancellationToken);
        return true;
    }
}
```

### Using ToBusinessResult Extension

```csharp
using Mvp24Hours.Core.Extensions.Exceptions;

try
{
    await ProcessOrderAsync(orderId);
}
catch (BusinessException ex)
{
    // Convert exception to IBusinessResult
    var result = ex.ToBusinessResult<Order>();
    // result.HasErrors == true
    // result.Messages contains the error details
}
```

---

## Best Practices

1. **Choose the right exception type** - Use the most specific exception for your scenario
2. **Always provide meaningful messages** - Help developers understand what went wrong
3. **Use error codes** - Enable clients to programmatically handle specific errors
4. **Add context when helpful** - Include relevant data for debugging
5. **Don't expose sensitive data** - Be careful with context information in production
6. **Log exceptions appropriately** - Use structured logging with error codes

```csharp
// Good
throw new BusinessException(
    "Cannot process payment: card declined",
    "CARD_DECLINED",
    new Dictionary<string, object>
    {
        ["TransactionId"] = transactionId,
        ["DeclineCode"] = declineCode
    }
);

// Bad - too generic
throw new Exception("Error");

// Bad - exposes sensitive data
throw new BusinessException($"Card {cardNumber} was declined");
```

