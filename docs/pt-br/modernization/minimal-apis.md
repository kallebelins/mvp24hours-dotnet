# Minimal APIs com TypedResults (.NET 9)

> Extensões para criar endpoints Minimal API type-safe e AOT-friendly usando TypedResults nativos do .NET 9.

## Visão Geral

O framework Mvp24Hours fornece extensões modernas para construir Minimal APIs com as seguintes funcionalidades:

- **TypedResults** - Respostas HTTP fortemente tipadas com verificação em tempo de compilação
- **Mapeamento CQRS automático** - Mapeia commands e queries para endpoints HTTP
- **Integração com validação** - FluentValidation com respostas TypedResults
- **Tratamento de exceções** - Converte exceções para ProblemDetails RFC 7807
- **Suporte OpenAPI** - Melhor geração de documentação

## Por que TypedResults?

| Funcionalidade | Results.* | TypedResults.* |
|----------------|-----------|----------------|
| Type Safety | Runtime | Tempo de compilação |
| OpenAPI | Básico | Metadados aprimorados |
| Compilação AOT | Limitado | Suporte completo |
| IntelliSense | Genérico | Tipos específicos |
| Recursos .NET 9 | N/A | InternalServerError() |

## Instalação

TypedResults estão disponíveis no pacote `Mvp24Hours.WebAPI`:

```bash
dotnet add package Mvp24Hours.WebAPI
```

## Uso Básico

### Convertendo IBusinessResult para TypedResults

```csharp
using Mvp24Hours.WebAPI.Endpoints;

app.MapGet("/orders/{id}", async (Guid id, ISender sender) =>
{
    var result = await sender.SendAsync<IBusinessResult<OrderDto>>(new GetOrderQuery(id));
    return result.ToNativeTypedResult();
});
```

### Usando Helpers Nativos de TypedResults

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

## Mapeamento de Endpoints CQRS

### Endpoints de Command

```csharp
// Mapeamento básico de command
app.MapNativeCommand<CreateOrderCommand, OrderDto>(
    "/api/orders",
    HttpMethod.Post,
    endpoint => endpoint
        .RequireAuthorization()
        .WithTags("Orders")
        .WithSummary("Criar um novo pedido")
);

// Command retornando BusinessResult
app.MapNativeCommandWithResult<CreateOrderCommand, OrderDto>("/api/orders");

// Command com resposta Created (201)
app.MapNativeCommandCreate<CreateOrderCommand, OrderDto>(
    "/api/orders",
    "/api/orders/{0}",
    dto => dto.Id
);

// Command de exclusão com resposta NoContent (204)
app.MapNativeCommandDelete<DeleteOrderCommand, bool>("/api/orders/{id}");
```

### Endpoints de Query

```csharp
// Mapeamento básico de query
app.MapNativeQuery<GetOrderByIdQuery, OrderDto>(
    "/api/orders/{id}",
    endpoint => endpoint
        .RequireAuthorization()
        .WithTags("Orders")
);

// Query retornando BusinessResult
app.MapNativeQueryWithResult<GetOrderByIdQuery, OrderDto>("/api/orders/{id}");

// Query de listagem (retorna 200 mesmo para resultados vazios)
app.MapNativeQueryList<GetOrdersQuery, IEnumerable<OrderDto>>("/api/orders");
```

## Conversões TypedResults

### Extensões IBusinessResult<T>

| Método | Descrição | Sucesso | Erro |
|--------|-----------|---------|------|
| `ToNativeTypedResult()` | Conversão padrão | Ok\<T\> | Baseado no código de erro |
| `ToNativeTypedResultAllowNull()` | Permite dados nulos | Ok\<T\> ou Ok\<null\> | Baseado no código de erro |
| `ToSimpleTypedResult()` | Conversão básica | Ok\<T\> | BadRequest |
| `ToCreatedTypedResult()` | Para operações POST | Created\<T\> | BadRequest/Conflict |
| `ToNoContentTypedResult()` | Para DELETE/PUT | NoContent | NotFound/BadRequest |
| `ToAcceptedTypedResult()` | Para operações assíncronas | Accepted\<T\> | BadRequest |

### Mapeamento de Códigos de Erro

| Código de Erro | Status HTTP | TypedResult |
|----------------|-------------|-------------|
| NOT_FOUND | 404 | NotFound\<ProblemDetails\> |
| CONFLICT | 409 | Conflict\<ProblemDetails\> |
| UNAUTHORIZED | 401 | Unauthorized |
| FORBIDDEN | 403 | Forbid |
| VALIDATION | 400 | ValidationProblem |
| INTERNAL_ERROR | 500 | Problem (500) |
| Padrão | 400 | BadRequest\<ProblemDetails\> |

## Tratamento de Exceções

### Convertendo Exceções para TypedResults

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

// Com stack trace (apenas desenvolvimento!)
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

### Mapeamento de Exceções

| Exceção | Status | Título |
|---------|--------|--------|
| NotFoundException | 404 | Recurso Não Encontrado |
| ValidationException | 400 | Validação Falhou |
| UnauthorizedException | 401 | Autenticação Necessária |
| ForbiddenException | 403 | Acesso Negado |
| ConflictException | 409 | Conflito de Recurso |
| DomainException | 422 | Violação de Regra de Domínio |
| TimeoutException | 408 | Timeout da Requisição |
| Outras | 500 | Erro Interno do Servidor |

## Filtros de Endpoint

### Filtro de Validação

```csharp
// Usando FluentValidation com TypedResults
app.MapPost("/orders", handler)
   .WithNativeValidation<CreateOrderCommand>();
```

### Filtro de Tratamento de Exceções

```csharp
app.MapPost("/orders", handler)
   .WithExceptionHandling();
```

### Filtro de Logging

```csharp
app.MapPost("/orders", handler)
   .WithLogging();
```

### Filtro de Correlation ID

```csharp
app.MapPost("/orders", handler)
   .WithCorrelationId();
```

### Filtro de Idempotência

```csharp
app.MapPost("/orders", handler)
   .WithIdempotency(); // Lê o header Idempotency-Key
```

### Filtro de Timeout

```csharp
app.MapPost("/orders", handler)
   .WithTimeout(30); // Timeout de 30 segundos
```

### Filtros Combinados

```csharp
// Aplica todos os filtros padrão
app.MapPost("/orders", handler)
   .WithStandardFilters<CreateOrderCommand>();

// Equivalente a:
app.MapPost("/orders", handler)
   .WithCorrelationId()
   .WithLogging()
   .WithNativeValidation<CreateOrderCommand>()
   .WithExceptionHandling();
```

## Helpers de ProblemDetails

### Tipos Específicos de Problemas

```csharp
using static Mvp24Hours.WebAPI.Endpoints.NativeTypedResultsExtensions;

// Não Encontrado
return NotFound("Order", orderId);

// Conflito
return Conflict("Pedido já existe", "Order");

// Problema de Validação
return ValidationProblem(new Dictionary<string, string[]>
{
    ["Name"] = ["Nome é obrigatório"],
    ["Email"] = ["Formato de email inválido"]
});

// Entidade Não Processável (Erro de Domínio)
return UnprocessableEntity(
    "Não é possível cancelar um pedido enviado",
    entityName: "Order",
    ruleName: "PedidoNaoDeveEstarEnviado");

// Erro Interno do Servidor (.NET 9)
return InternalServerError("Falha na conexão com o banco de dados");

// Problema Personalizado
return Problem(
    StatusCodes.Status413PayloadTooLarge,
    "Payload Muito Grande",
    "Arquivo excede o limite de 10MB",
    extensions: new Dictionary<string, object?>
    {
        ["tamanhoMaximo"] = "10MB",
        ["tamanhoAtual"] = "15MB"
    });
```

## Exemplo Completo

```csharp
using Mvp24Hours.WebAPI.Endpoints;
using Mvp24Hours.WebAPI.Endpoints.Filters;

var builder = WebApplication.CreateBuilder(args);

// Adicionar serviços
builder.Services.AddMvpMediator();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

// Configurar grupo de API
var api = app.MapGroup("/api")
    .WithCorrelationId()
    .WithExceptionHandling();

// Endpoints de pedidos
var orders = api.MapGroup("/orders")
    .WithTags("Orders");

// Criar pedido
orders.MapNativeCommandCreate<CreateOrderCommand, OrderDto>(
    "",
    "/api/orders/{0}",
    dto => dto.Id,
    endpoint => endpoint
        .WithSummary("Criar um novo pedido")
        .RequireAuthorization()
);

// Obter pedido por ID
orders.MapNativeQueryWithResult<GetOrderByIdQuery, OrderDto>(
    "/{id}",
    endpoint => endpoint
        .WithSummary("Obter pedido por ID")
);

// Listar pedidos
orders.MapNativeQueryList<GetOrdersQuery, IEnumerable<OrderDto>>(
    "",
    endpoint => endpoint
        .WithSummary("Listar todos os pedidos")
);

// Atualizar pedido
orders.MapNativeCommandWithResult<UpdateOrderCommand, OrderDto>(
    "/{id}",
    HttpMethod.Put,
    endpoint => endpoint
        .WithSummary("Atualizar um pedido")
        .RequireAuthorization()
);

// Excluir pedido
orders.MapNativeCommandDelete<DeleteOrderCommand, bool>(
    "/{id}",
    endpoint => endpoint
        .WithSummary("Excluir um pedido")
        .RequireAuthorization()
);

app.Run();
```

## Migração de Results para TypedResults

### Antes (Results.*)

```csharp
app.MapGet("/orders/{id}", async (Guid id) =>
{
    var order = await GetOrderAsync(id);
    if (order is null)
        return Results.NotFound();
    return Results.Ok(order);
});
```

### Depois (TypedResults.*)

```csharp
app.MapGet("/orders/{id}", async Task<Results<Ok<OrderDto>, NotFound<ProblemDetails>>> (Guid id) =>
{
    var order = await GetOrderAsync(id);
    if (order is null)
        return TypedResults.NotFound(CreateProblemDetails(404, "Não Encontrado", "Pedido não encontrado"));
    return TypedResults.Ok(order);
});

// Ou usando extensões
app.MapNativeQuery<GetOrderByIdQuery, OrderDto>("/orders/{id}");
```

## Boas Práticas

1. **Use TypedResults para novos endpoints** - Melhor type safety e suporte a OpenAPI
2. **Aplique filtros no nível de grupo** - Reduza repetição
3. **Use métodos HTTP específicos** - POST para criar, PUT/PATCH para atualizar, DELETE para excluir
4. **Retorne códigos de status apropriados** - 201 Created, 204 NoContent, etc.
5. **Inclua ProblemDetails** - Padrão RFC 7807 para erros
6. **Adicione trace IDs** - Para rastreamento distribuído
7. **Valide cedo** - Use filtros de validação

## Veja Também

- [ProblemDetails (RFC 7807)](problem-details.md)
- [Output Caching](output-caching.md)
- [Resiliência HTTP](http-resilience.md)
- [Commands CQRS](../cqrs/commands.md)
- [Queries CQRS](../cqrs/queries.md)

