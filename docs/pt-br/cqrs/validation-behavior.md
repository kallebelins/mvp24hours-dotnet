# ValidationBehavior com FluentValidation

## Visão Geral

O `ValidationBehavior` integra o FluentValidation ao pipeline do Mediator, validando automaticamente requests antes que cheguem ao handler.

## Configuração

### Instalação

```bash
dotnet add package FluentValidation
dotnet add package FluentValidation.DependencyInjectionExtensions
```

### Registro

```csharp
// Registrar validators do assembly
services.AddValidatorsFromAssemblyContaining<Program>();

// Habilitar ValidationBehavior
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    options.RegisterValidationBehavior = true;
});
```

## Criando Validators

### Validator Básico

```csharp
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty()
            .WithMessage("Customer name is required")
            .MaximumLength(100)
            .WithMessage("Customer name must not exceed 100 characters");

        RuleFor(x => x.CustomerEmail)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("A valid email is required");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("Order must have at least one item");
    }
}
```

### Validator com Regras Complexas

```csharp
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    private readonly ICustomerRepository _customerRepository;

    public CreateOrderCommandValidator(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;

        RuleFor(x => x.CustomerEmail)
            .NotEmpty()
            .EmailAddress()
            .MustAsync(CustomerExists)
            .WithMessage("Customer not found");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .NotEmpty();

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0)
                .WithMessage("Quantity must be greater than zero");

            item.RuleFor(i => i.UnitPrice)
                .GreaterThan(0)
                .WithMessage("Unit price must be positive");
        });

        RuleFor(x => x)
            .Must(HaveValidTotal)
            .WithMessage("Order total exceeds maximum allowed");
    }

    private async Task<bool> CustomerExists(
        string email, 
        CancellationToken cancellationToken)
    {
        return await _customerRepository.ExistsByEmailAsync(email);
    }

    private bool HaveValidTotal(CreateOrderCommand command)
    {
        var total = command.Items.Sum(i => i.Quantity * i.UnitPrice);
        return total <= 10000;
    }
}
```

### Validator com Condicionais

```csharp
public class UpdateOrderCommandValidator : AbstractValidator<UpdateOrderCommand>
{
    public UpdateOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty();

        // Valida apenas se Status for fornecido
        When(x => x.Status.HasValue, () =>
        {
            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid order status");
        });

        // Valida shipping se for entrega
        When(x => x.RequiresShipping, () =>
        {
            RuleFor(x => x.ShippingAddress)
                .NotEmpty()
                .WithMessage("Shipping address is required for delivery orders");

            RuleFor(x => x.ShippingAddress!.ZipCode)
                .Matches(@"^\d{5}(-\d{4})?$")
                .WithMessage("Invalid ZIP code format");
        });
    }
}
```

## Como Funciona o ValidationBehavior

```csharp
public sealed class ValidationBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}
```

## Tratando Erros de Validação

### ValidationException

```csharp
try
{
    var result = await _mediator.SendAsync(command);
}
catch (ValidationException ex)
{
    foreach (var error in ex.Errors)
    {
        Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");
    }
}
```

### Em Controllers ASP.NET

```csharp
[ApiController]
public class OrderController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderCommand command)
    {
        try
        {
            var result = await _mediator.SendAsync(command);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );
            
            return BadRequest(new { Errors = errors });
        }
    }
}
```

### Exception Handler Global

```csharp
public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is ValidationException validationException)
        {
            context.Response.StatusCode = 400;
            
            var response = new
            {
                Type = "ValidationError",
                Errors = validationException.Errors
                    .Select(e => new { e.PropertyName, e.ErrorMessage })
            };
            
            await context.Response.WriteAsJsonAsync(response, cancellationToken);
            return true;
        }
        
        return false;
    }
}
```

## Boas Práticas

1. **Um Validator por Request**: Mantenha validators específicos
2. **Injeção de Dependências**: Use DI para regras que precisam de serviços
3. **Mensagens Claras**: Forneça mensagens de erro descritivas
4. **Regras Async**: Use `MustAsync` para validações assíncronas
5. **Validação em Cascata**: Use `CascadeMode` para controlar fluxo
6. **Localização**: Use resources para internacionalização

### Exemplo de CascadeMode

```csharp
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        // Para na primeira falha do campo
        RuleFor(x => x.CustomerEmail)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .EmailAddress()
            .MustAsync(BeUniqueEmail);
    }
}
```

