# Padrões de Tratamento de Erros para Agentes de IA

> **Instrução para Agente de IA**: Use estes padrões ao implementar tratamento de erros, gerenciamento de exceções e validação em aplicações baseadas em Mvp24Hours.

---

## Estratégia de Tratamento de Erros

| Camada | Estratégia | Implementação |
|--------|------------|---------------|
| Domínio | Exceções de Domínio | Tipos de exceção customizados |
| Aplicação | Padrão Result | `IBusinessResult<T>` |
| API | ProblemDetails | Padrão RFC 7807 |
| Global | Middleware de Exceções | Tratamento centralizado |

---

## Exceções de Domínio

### Exceção Base de Domínio

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

### Exceções Específicas de Domínio

```csharp
// Core/Exceptions/EntityNotFoundException.cs
namespace ProjectName.Core.Exceptions;

public class EntityNotFoundException : DomainException
{
    public string EntityType { get; }
    public object EntityId { get; }

    public EntityNotFoundException(string entityType, object entityId)
        : base($"{entityType} com id '{entityId}' não foi encontrado.", "ENTITY_NOT_FOUND")
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
        : base("Um ou mais erros de validação ocorreram.", "VALIDATION_ERROR")
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

## Padrão Result com Mvp24Hours

### Usando IBusinessResult

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
            return new CustomerDto().ToBusinessNotFound("Cliente não encontrado");
        }

        var dto = customer.ToDto();
        return dto.ToBusinessSuccess();
    }

    public async Task<IBusinessResult<CustomerDto>> CreateAsync(CustomerCreateDto dto)
    {
        // Validação
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
            return false.ToBusinessNotFound("Cliente não encontrado");
        }

        await repository.RemoveAsync(customer);
        await _uow.SaveChangesAsync();

        return true.ToBusinessSuccess();
    }

    private async Task<IBusinessResult<bool>> ValidateAsync(CustomerCreateDto dto)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.Name))
            errors.Add("Nome é obrigatório");

        if (string.IsNullOrWhiteSpace(dto.Email))
            errors.Add("E-mail é obrigatório");

        // Verificar e-mail duplicado
        var repository = _uow.GetRepository<Customer>();
        var existingCustomer = await repository.GetByAsync(c => c.Email.Value == dto.Email.ToLower());
        if (existingCustomer.Any())
            errors.Add("E-mail já existe");

        if (errors.Any())
        {
            return false.ToBusinessWithMessages(errors.ToArray());
        }

        return true.ToBusinessSuccess();
    }
}
```

---

## Tratamento Global de Exceções

### Middleware de Handler de Exceções

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
            "Erro ao processar requisição {TraceId}: {Message}",
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
                    Title = "Recurso Não Encontrado",
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
                    Title = "Erro de Validação",
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
                    Title = "Violação de Regra de Negócio",
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
                    Title = "Conflito de Concorrência",
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
                    Title = "Erro de Domínio",
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
                    Title = "Não Autorizado",
                    Status = StatusCodes.Status401Unauthorized,
                    Detail = "Autenticação é necessária para acessar este recurso.",
                    Instance = context.Request.Path,
                    Extensions = { ["traceId"] = traceId }
                }),

            _ => (
                StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    Title = "Erro Interno do Servidor",
                    Status = StatusCodes.Status500InternalServerError,
                    Detail = _environment.IsDevelopment() ? exception.Message : "Um erro inesperado ocorreu.",
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

## Integração com FluentValidation

### Definição do Validator

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
            .NotEmpty().WithMessage("Nome é obrigatório")
            .MinimumLength(2).WithMessage("Nome deve ter pelo menos 2 caracteres")
            .MaximumLength(100).WithMessage("Nome não deve exceder 100 caracteres");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail é obrigatório")
            .EmailAddress().WithMessage("Formato de e-mail inválido")
            .MaximumLength(256).WithMessage("E-mail não deve exceder 256 caracteres");
    }
}
```

### Filtro de Validação

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
                        Title = "Erro de Validação",
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

### Registro

```csharp
// Program.cs
using FluentValidation;
using ProjectName.Application.Validators;
using ProjectName.WebAPI.Filters;

var builder = WebApplication.CreateBuilder(args);

// Registrar validators
builder.Services.AddValidatorsFromAssemblyContaining<CustomerCreateDtoValidator>();

// Adicionar filtro de validação
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});
```

---

## Validação em Pipeline com Mvp24Hours

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
            input.Messages.AddMessage("Dados de requisição inválidos", MessageType.Error);
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

## Modelos de Resposta da API

### Wrapper de Resposta Padrão

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
            Message = "Um ou mais erros ocorreram",
            Errors = errors
        };
    }
}
```

### Métodos de Extensão do Controller

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
            return controller.NotFound(ApiResponse<T>.Fail("Recurso não encontrado"));
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
            ApiResponse<T>.Ok(result.Data!, "Recurso criado com sucesso"));
    }
}
```

---

## Configuração de Startup

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Adicionar serviços
builder.Services.AddValidatorsFromAssemblyContaining<CustomerCreateDtoValidator>();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});

// Configurar ProblemDetails
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});

var app = builder.Build();

// Usar tratamento de exceções
app.UseGlobalExceptionHandler();

app.MapControllers();
app.Run();
```

---

## Documentação Relacionada

- [Templates de Arquitetura](architecture-templates.md)
- [Padrões de Testes](testing-patterns.md)
- [Padrões de Segurança](security-patterns.md)

