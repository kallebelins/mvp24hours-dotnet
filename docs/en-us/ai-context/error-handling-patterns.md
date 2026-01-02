# Error Handling Patterns for AI Agents

> **AI Agent Instruction**: Use these patterns when implementing error handling, exception management, and validation in Mvp24Hours-based applications.

---

## Error Handling Strategy

| Layer | Strategy | Implementation |
|-------|----------|----------------|
| Domain | Domain Exceptions | Custom exception types |
| Application | Result Pattern | `IBusinessResult<T>` |
| API | ProblemDetails | RFC 7807 standard |
| Global | Exception Middleware | Centralized handling |

---

## Domain Exceptions

### Base Domain Exception

```csharp
// Core/Exceptions/DomainException.cs
namespace ProjectName.Core.Exceptions;

public class DomainException : Exception
{
    public string Code { get; }
    public IDictionary<string, object> Metadata { get; }

    public DomainException(string message, string code = "DOMAIN_ERROR") 
        : base(message)
    {
        Code = code;
        Metadata = new Dictionary<string, object>();
    }

    public DomainException(string message, string code, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
        Metadata = new Dictionary<string, object>();
    }

    public DomainException WithMetadata(string key, object value)
    {
        Metadata[key] = value;
        return this;
    }
}
```

### Specific Domain Exceptions

```csharp
// Core/Exceptions/EntityNotFoundException.cs
namespace ProjectName.Core.Exceptions;

public class EntityNotFoundException : DomainException
{
    public string EntityType { get; }
    public object EntityId { get; }

    public EntityNotFoundException(string entityType, object entityId)
        : base($"{entityType} with id '{entityId}' was not found.", "ENTITY_NOT_FOUND")
    {
        EntityType = entityType;
        EntityId = entityId;
        WithMetadata("entityType", entityType);
        WithMetadata("entityId", entityId);
    }

    public static EntityNotFoundException For<T>(object id) where T : class
    {
        return new EntityNotFoundException(typeof(T).Name, id);
    }
}

// Core/Exceptions/BusinessRuleException.cs
namespace ProjectName.Core.Exceptions;

public class BusinessRuleException : DomainException
{
    public string RuleName { get; }

    public BusinessRuleException(string ruleName, string message)
        : base(message, "BUSINESS_RULE_VIOLATION")
    {
        RuleName = ruleName;
        WithMetadata("ruleName", ruleName);
    }
}

// Core/Exceptions/ValidationException.cs
namespace ProjectName.Core.Exceptions;

public class ValidationException : DomainException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.", "VALIDATION_ERROR")
    {
        Errors = errors;
        WithMetadata("errors", errors);
    }

    public ValidationException(string propertyName, string errorMessage)
        : this(new Dictionary<string, string[]> { { propertyName, new[] { errorMessage } } })
    {
    }
}

// Core/Exceptions/ConcurrencyException.cs
namespace ProjectName.Core.Exceptions;

public class ConcurrencyException : DomainException
{
    public ConcurrencyException(string message)
        : base(message, "CONCURRENCY_ERROR")
    {
    }
}
```

---

## Result Pattern with Mvp24Hours

### Using IBusinessResult

```csharp
// Application/Services/CustomerService.cs
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using ProjectName.Core.Entities;
using ProjectName.Core.Exceptions;

namespace ProjectName.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly IUnitOfWorkAsync _uow;

    public CustomerService(IUnitOfWorkAsync uow)
    {
        _uow = uow;
    }

    public async Task<IBusinessResult<CustomerDto>> GetByIdAsync(int id)
    {
        var repository = _uow.GetRepository<Customer>();
        var customer = await repository.GetByIdAsync(id);

        if (customer == null)
        {
            return new CustomerDto().ToBusinessNotFound("Customer not found");
        }

        var dto = customer.ToDto();
        return dto.ToBusinessSuccess();
    }

    public async Task<IBusinessResult<CustomerDto>> CreateAsync(CustomerCreateDto dto)
    {
        // Validation
        var validationResult = await ValidateAsync(dto);
        if (validationResult.HasErrors)
        {
            return validationResult.ToBusiness<CustomerDto>();
        }

        try
        {
            var customer = Customer.Create(dto.Name, dto.Email);
            var repository = _uow.GetRepository<Customer>();
            
            await repository.AddAsync(customer);
            await _uow.SaveChangesAsync();

            return customer.ToDto().ToBusinessCreate();
        }
        catch (DomainException ex)
        {
            return new CustomerDto().ToBusinessError(ex.Message);
        }
    }

    public async Task<IBusinessResult<bool>> DeleteAsync(int id)
    {
        var repository = _uow.GetRepository<Customer>();
        var customer = await repository.GetByIdAsync(id);

        if (customer == null)
        {
            return false.ToBusinessNotFound("Customer not found");
        }

        await repository.RemoveAsync(customer);
        await _uow.SaveChangesAsync();

        return true.ToBusinessSuccess();
    }

    private async Task<IBusinessResult<bool>> ValidateAsync(CustomerCreateDto dto)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.Name))
            errors.Add("Name is required");

        if (string.IsNullOrWhiteSpace(dto.Email))
            errors.Add("Email is required");

        // Check for duplicate email
        var repository = _uow.GetRepository<Customer>();
        var existingCustomer = await repository.GetByAsync(c => c.Email.Value == dto.Email.ToLower());
        if (existingCustomer.Any())
            errors.Add("Email already exists");

        if (errors.Any())
        {
            return false.ToBusinessWithMessages(errors.ToArray());
        }

        return true.ToBusinessSuccess();
    }
}
```

---

## Global Exception Handling

### Exception Handler Middleware

```csharp
// Middlewares/GlobalExceptionMiddleware.cs
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ProjectName.Core.Exceptions;

namespace ProjectName.WebAPI.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, problemDetails) = CreateProblemDetails(context, exception);

        _logger.LogError(
            exception,
            "Error processing request {TraceId}: {Message}",
            problemDetails.Extensions["traceId"],
            exception.Message);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, jsonOptions));
    }

    private (int StatusCode, ProblemDetails Details) CreateProblemDetails(
        HttpContext context, 
        Exception exception)
    {
        var traceId = context.TraceIdentifier;

        return exception switch
        {
            EntityNotFoundException ex => (
                StatusCodes.Status404NotFound,
                new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    Title = "Resource Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = ex.Message,
                    Instance = context.Request.Path,
                    Extensions =
                    {
                        ["traceId"] = traceId,
                        ["code"] = ex.Code,
                        ["entityType"] = ex.EntityType,
                        ["entityId"] = ex.EntityId
                    }
                }),

            ValidationException ex => (
                StatusCodes.Status400BadRequest,
                new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "Validation Error",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = ex.Message,
                    Instance = context.Request.Path,
                    Extensions =
                    {
                        ["traceId"] = traceId,
                        ["code"] = ex.Code,
                        ["errors"] = ex.Errors
                    }
                }),

            BusinessRuleException ex => (
                StatusCodes.Status422UnprocessableEntity,
                new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc4918#section-11.2",
                    Title = "Business Rule Violation",
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Detail = ex.Message,
                    Instance = context.Request.Path,
                    Extensions =
                    {
                        ["traceId"] = traceId,
                        ["code"] = ex.Code,
                        ["ruleName"] = ex.RuleName
                    }
                }),

            ConcurrencyException ex => (
                StatusCodes.Status409Conflict,
                new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                    Title = "Concurrency Conflict",
                    Status = StatusCodes.Status409Conflict,
                    Detail = ex.Message,
                    Instance = context.Request.Path,
                    Extensions =
                    {
                        ["traceId"] = traceId,
                        ["code"] = ex.Code
                    }
                }),

            DomainException ex => (
                StatusCodes.Status400BadRequest,
                new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "Domain Error",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = ex.Message,
                    Instance = context.Request.Path,
                    Extensions =
                    {
                        ["traceId"] = traceId,
                        ["code"] = ex.Code,
                        ["metadata"] = ex.Metadata
                    }
                }),

            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                    Title = "Unauthorized",
                    Status = StatusCodes.Status401Unauthorized,
                    Detail = "Authentication is required to access this resource.",
                    Instance = context.Request.Path,
                    Extensions = { ["traceId"] = traceId }
                }),

            _ => (
                StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    Title = "Internal Server Error",
                    Status = StatusCodes.Status500InternalServerError,
                    Detail = _environment.IsDevelopment() ? exception.Message : "An unexpected error occurred.",
                    Instance = context.Request.Path,
                    Extensions =
                    {
                        ["traceId"] = traceId,
                        ["stackTrace"] = _environment.IsDevelopment() ? exception.StackTrace : null
                    }
                })
        };
    }
}

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
```

---

## FluentValidation Integration

### Validator Definition

```csharp
// Application/Validators/CustomerCreateDtoValidator.cs
using FluentValidation;
using ProjectName.Core.ValueObjects;

namespace ProjectName.Application.Validators;

public class CustomerCreateDtoValidator : AbstractValidator<CustomerCreateDto>
{
    public CustomerCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(2).WithMessage("Name must be at least 2 characters")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters");
    }
}
```

### Validation Filter

```csharp
// Filters/ValidationFilter.cs
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ProjectName.WebAPI.Filters;

public class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var (key, value) in context.ActionArguments)
        {
            if (value == null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(value.GetType());
            var validator = context.HttpContext.RequestServices.GetService(validatorType) as IValidator;

            if (validator != null)
            {
                var validationContext = new ValidationContext<object>(value);
                var validationResult = await validator.ValidateAsync(validationContext);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()
                        );

                    var problemDetails = new ValidationProblemDetails(errors)
                    {
                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                        Title = "Validation Error",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.HttpContext.Request.Path
                    };

                    context.Result = new BadRequestObjectResult(problemDetails);
                    return;
                }
            }
        }

        await next();
    }
}
```

### Registration

```csharp
// Program.cs
using FluentValidation;
using ProjectName.Application.Validators;
using ProjectName.WebAPI.Filters;

var builder = WebApplication.CreateBuilder(args);

// Register validators
builder.Services.AddValidatorsFromAssemblyContaining<CustomerCreateDtoValidator>();

// Add validation filter
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});
```

---

## Pipeline Validation with Mvp24Hours

```csharp
// Application/Pipelines/Operations/ValidateCustomerOperation.cs
using FluentValidation;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Extensions;
using ProjectName.Core.ValueObjects;

namespace ProjectName.Application.Pipelines.Operations;

public class ValidateCustomerOperation : IOperationAsync<CustomerCreateDto>
{
    private readonly IValidator<CustomerCreateDto> _validator;

    public ValidateCustomerOperation(IValidator<CustomerCreateDto> validator)
    {
        _validator = validator;
    }

    public async Task<IPipelineMessage> ExecuteAsync(IPipelineMessage input, CancellationToken ct = default)
    {
        var dto = input.GetContent<CustomerCreateDto>();
        
        if (dto == null)
        {
            input.Messages.AddMessage("Invalid request data", MessageType.Error);
            input.SetLock();
            return input;
        }

        var validationResult = await _validator.ValidateAsync(dto, ct);

        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                input.Messages.AddMessage($"{error.PropertyName}: {error.ErrorMessage}", MessageType.Error);
            }
            input.SetLock();
        }

        return input;
    }
}
```

---

## API Response Models

### Standard Response Wrapper

```csharp
// Models/ApiResponse.cs
namespace ProjectName.WebAPI.Models;

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public IEnumerable<string>? Errors { get; init; }
    public string? TraceId { get; init; }

    public static ApiResponse<T> Ok(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }

    public static ApiResponse<T> Fail(IEnumerable<string> errors)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = "One or more errors occurred",
            Errors = errors
        };
    }
}
```

### Controller Extension Methods

```csharp
// Extensions/ControllerExtensions.cs
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using ProjectName.WebAPI.Models;

namespace ProjectName.WebAPI.Extensions;

public static class ControllerExtensions
{
    public static IActionResult ToActionResult<T>(
        this ControllerBase controller,
        IBusinessResult<T> result)
    {
        if (result.HasErrors)
        {
            var errors = result.Messages
                .Where(m => m.Type == Mvp24Hours.Core.Enums.Infrastructure.MessageType.Error)
                .Select(m => m.Text);

            return controller.BadRequest(ApiResponse<T>.Fail(errors));
        }

        if (!result.HasData)
        {
            return controller.NotFound(ApiResponse<T>.Fail("Resource not found"));
        }

        return controller.Ok(ApiResponse<T>.Ok(result.Data!));
    }

    public static IActionResult ToCreatedResult<T>(
        this ControllerBase controller,
        IBusinessResult<T> result,
        string routeName,
        object routeValues)
    {
        if (result.HasErrors)
        {
            var errors = result.Messages
                .Where(m => m.Type == Mvp24Hours.Core.Enums.Infrastructure.MessageType.Error)
                .Select(m => m.Text);

            return controller.BadRequest(ApiResponse<T>.Fail(errors));
        }

        return controller.CreatedAtRoute(
            routeName,
            routeValues,
            ApiResponse<T>.Ok(result.Data!, "Resource created successfully"));
    }
}
```

---

## Startup Configuration

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddValidatorsFromAssemblyContaining<CustomerCreateDtoValidator>();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});

// Configure ProblemDetails
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});

var app = builder.Build();

// Use exception handling
app.UseGlobalExceptionHandler();

app.MapControllers();
app.Run();
```

---

## Related Documentation

- [Architecture Templates](architecture-templates.md)
- [Testing Patterns](testing-patterns.md)
- [Security Patterns](security-patterns.md)

