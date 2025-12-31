# ProblemDetails (RFC 7807)

A especificação `ProblemDetails` (RFC 7807) fornece uma forma padronizada de comunicar erros em APIs HTTP. A partir do .NET 7, o ASP.NET Core inclui suporte nativo para `ProblemDetails` através da interface `IProblemDetailsService`.

## Visão Geral

ProblemDetails fornece um formato de resposta de erro consistente em toda a sua API:

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

## Arquitetura

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Fluxo ProblemDetails                         │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌──────────────┐    ┌─────────────────┐    ┌──────────────────┐   │
│  │   Exceção    │───▶│   Handler de    │───▶│  Resposta        │   │
│  │   Lançada    │    │   Exceção       │    │  ProblemDetails  │   │
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
│  │  Mapper         │                │  Mapper         │            │
│  │  Mvp24Hours     │                │  Customizado    │            │
│  └─────────────────┘                └─────────────────┘            │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

## Instalação

O suporte a ProblemDetails está incluído no pacote `Mvp24Hours.WebAPI`.

```bash
dotnet add package Mvp24Hours.WebAPI
```

## Configuração Básica

### Usando ProblemDetails Nativo do .NET (Recomendado)

```csharp
var builder = WebApplication.CreateBuilder(args);

// Adicionar ProblemDetails nativo com mapeamentos de exceção do Mvp24Hours
builder.Services.AddNativeProblemDetails(options =>
{
    options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    options.IncludeStackTrace = builder.Environment.IsDevelopment();
    options.ProblemTypeBaseUri = "https://api.example.com/errors";
});

var app = builder.Build();

// Usar tratamento de exceção nativo com ProblemDetails
app.UseNativeProblemDetailsHandling();

app.MapControllers();
app.Run();
```

### Configuração Simplificada

```csharp
// Uma linha para configuração completa baseada no ambiente
builder.Services.AddNativeProblemDetailsAll(builder.Environment);

var app = builder.Build();
app.UseNativeProblemDetailsHandling();
```

### Usando Middleware Customizado (Legado)

Para aplicações que precisam de mais controle ou usam padrões antigos:

```csharp
builder.Services.AddMvp24HoursProblemDetails(options =>
{
    options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
});

var app = builder.Build();
app.UseMvp24HoursProblemDetails();
```

## Mapeamentos de Exceção

O framework mapeia automaticamente exceções do Mvp24Hours para códigos de status HTTP apropriados:

| Exceção | Status HTTP | Título |
|---------|-------------|--------|
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
| Outras exceções | 500 | Internal Server Error |

## TypedResults.Problem() para Minimal APIs

A classe `TypedResultsExtensions` fornece métodos auxiliares para criar respostas `ProblemDetails` em Minimal APIs:

### Convertendo Exceções para ProblemDetails

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
        return ex.ToProblem(); // Converte exceção para ProblemHttpResult
    }
});

// Com stack trace (apenas desenvolvimento!)
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

### Criando Respostas de Problema Específicas

```csharp
// Não Encontrado
app.MapGet("/orders/{id}", async (Guid id) =>
{
    var order = await repository.GetByIdAsync(id);
    return order is null 
        ? TypedResultsExtensions.NotFoundProblem("Order", id)
        : TypedResults.Ok(order);
});

// Problema de Validação
app.MapPost("/orders", async (CreateOrderCommand command) =>
{
    var errors = validator.Validate(command);
    if (errors.Any())
    {
        return TypedResultsExtensions.ValidationProblem(errors);
    }
    return TypedResults.Created($"/orders/{order.Id}", order);
});

// Conflito
app.MapPost("/orders", async (CreateOrderCommand command) =>
{
    if (await repository.ExistsAsync(command.OrderNumber))
    {
        return TypedResultsExtensions.ConflictProblem(
            $"Pedido com número '{command.OrderNumber}' já existe.",
            "Order");
    }
    return TypedResults.Created($"/orders/{order.Id}", order);
});

// Proibido
app.MapDelete("/orders/{id}", async (Guid id, ClaimsPrincipal user) =>
{
    if (!user.HasPermission("orders:delete"))
    {
        return TypedResultsExtensions.ForbiddenProblem(
            "Você não tem permissão para excluir pedidos.",
            "Order",
            "orders:delete");
    }
    await repository.DeleteAsync(id);
    return TypedResults.NoContent();
});

// Não Autorizado
app.MapGet("/protected", (ClaimsPrincipal user) =>
{
    if (!user.Identity?.IsAuthenticated ?? true)
    {
        return TypedResultsExtensions.UnauthorizedProblem(
            "Autenticação é necessária para acessar este recurso.",
            "Bearer");
    }
    return TypedResults.Ok(new { message = "Bem-vindo!" });
});

// Erro de Domínio
app.MapPost("/orders/{id}/cancel", async (Guid id) =>
{
    var order = await repository.GetByIdAsync(id);
    if (order.Status == OrderStatus.Shipped)
    {
        return TypedResultsExtensions.DomainProblem(
            "Não é possível cancelar um pedido que já foi enviado.",
            "Order",
            "OrderMustNotBeShipped");
    }
    order.Cancel();
    return TypedResults.Ok(order);
});

// Erro Interno do Servidor
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

// Código de Status Customizado
app.MapPost("/upload", async () =>
{
    if (fileSizeLimitExceeded)
    {
        return TypedResultsExtensions.CustomProblem(
            StatusCodes.Status413PayloadTooLarge,
            "Payload Muito Grande",
            "O arquivo enviado excede o tamanho máximo permitido de 10MB.",
            extensions: new Dictionary<string, object?>
            {
                ["maxSize"] = "10MB",
                ["actualSize"] = "15MB"
            });
    }
    return TypedResults.Ok();
});
```

## Convertendo BusinessResult para ProblemDetails

```csharp
app.MapGet("/orders/{id}", async (Guid id, ISender sender) =>
{
    var result = await sender.SendAsync(new GetOrderQuery(id));
    return result.ToTypedResult(); // Mapeia erros automaticamente para ProblemDetails
});

// Com dados nulos permitidos
app.MapGet("/orders/{id}/details", async (Guid id, ISender sender) =>
{
    var result = await sender.SendAsync(new GetOrderDetailsQuery(id));
    return result.ToTypedResultAllowNull(); // Retorna 200 OK mesmo se dados forem nulos
});
```

## Mapeamentos de Exceção Customizados

### Adicionando Tipos de Exceção Customizados

```csharp
builder.Services.AddMvp24HoursProblemDetails(mappings =>
{
    mappings[typeof(MyCustomException)] = HttpStatusCode.BadRequest;
    mappings[typeof(RateLimitExceededException)] = (HttpStatusCode)429;
    mappings[typeof(PaymentRequiredException)] = HttpStatusCode.PaymentRequired;
});
```

### Mapper de Exceção Customizado

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
            Title = "Erro Customizado",
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

// Registrar o mapper customizado
builder.Services.AddMvp24HoursExceptionMapper<CustomExceptionMapper>();
```

## Opções de Configuração

### MvpProblemDetailsOptions

| Opção | Tipo | Padrão | Descrição |
|-------|------|--------|-----------|
| `IncludeExceptionDetails` | `bool` | `false` | Incluir tipo e mensagem da exceção na resposta |
| `IncludeStackTrace` | `bool` | `false` | Incluir stack trace (apenas dev!) |
| `ProblemTypeBaseUri` | `string?` | `null` | URI base para documentação de tipos de problema |
| `DefaultTitle` | `string` | "An error occurred..." | Título padrão para exceções não mapeadas |
| `FallbackStatusCode` | `int` | `500` | Código de status padrão para exceções não mapeadas |
| `FallbackTitle` | `string` | "Internal Server Error" | Título de fallback |
| `FallbackDetail` | `string` | "An unexpected error..." | Mensagem de detalhe de fallback |
| `LogExceptions` | `bool` | `true` | Registrar exceções antes de retornar resposta |
| `IncludeCorrelationId` | `bool` | `true` | Incluir ID de correlação na resposta |
| `CorrelationIdHeaderName` | `string` | "X-Correlation-ID" | Nome do header para ID de correlação |
| `UseRfc7807ContentType` | `bool` | `true` | Usar content type application/problem+json |
| `ExceptionMappings` | `Dictionary<Type, HttpStatusCode>` | `{}` | Mapeamentos de tipo de exceção customizados |
| `StatusCodeMapper` | `Func<Exception, int>?` | `null` | Função customizada para determinar código de status |
| `CustomMapper` | `Func<Exception, HttpContext, ProblemDetails?>?` | `null` | Função customizada para mapear para ProblemDetails |
| `EnrichProblemDetails` | `Action<ProblemDetails, Exception, HttpContext>?` | `null` | Enriquecer ProblemDetails com dados adicionais |

### Exemplo de Configuração

```csharp
builder.Services.AddNativeProblemDetails(options =>
{
    // Configurações de desenvolvimento
    options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    options.IncludeStackTrace = builder.Environment.IsDevelopment();
    
    // URI de tipo de problema customizado
    options.ProblemTypeBaseUri = "https://api.example.com/errors";
    
    // ID de Correlação
    options.IncludeCorrelationId = true;
    options.CorrelationIdHeaderName = "X-Request-ID";
    
    // Mapeamentos de exceção customizados
    options.ExceptionMappings[typeof(RateLimitExceededException)] = (HttpStatusCode)429;
    
    // Enriquecer todas as respostas
    options.EnrichProblemDetails = (problemDetails, exception, context) =>
    {
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;
        problemDetails.Extensions["environment"] = builder.Environment.EnvironmentName;
    };
});
```

## Integração com CQRS

O sistema ProblemDetails integra-se perfeitamente com o módulo CQRS do Mvp24Hours:

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

## Melhores Práticas

1. **Use ProblemDetails nativo** para aplicações .NET 8+
2. **Nunca exponha stack traces** em produção
3. **Use códigos de erro estruturados** para tratamento de erros no cliente
4. **Inclua IDs de correlação** para rastreamento distribuído
5. **Documente seus tipos de erro** com anotações OpenAPI
6. **Use helpers TypedResults** para respostas consistentes em Minimal APIs
7. **Mapeie exceções de domínio** para códigos de status HTTP apropriados

## Comparação: Nativo vs Middleware Customizado

| Recurso | Nativo (`AddNativeProblemDetails`) | Customizado (`AddMvp24HoursProblemDetails`) |
|---------|-----------------------------------|---------------------------------------------|
| Versão .NET | 8+ | Todas |
| Integração | ASP.NET Core nativo | Middleware customizado |
| Negociação de Conteúdo | Automática | Manual |
| Páginas de Código de Status | Integrada | Separada |
| Performance | Otimizada | Boa |
| Flexibilidade | Padrão | Máxima |

## Veja Também

- [RFC 7807 - Problem Details for HTTP APIs](https://datatracker.ietf.org/doc/html/rfc7807)
- [Tratamento de Exceções no ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling)
- [Minimal APIs no .NET](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis)

