# Exceções

O módulo Mvp24Hours.Core fornece uma hierarquia abrangente de exceções para lidar com diferentes cenários de erro na sua aplicação.

## Hierarquia de Exceções

Todas as exceções customizadas herdam de `Mvp24HoursException`:

```
Mvp24HoursException (base)
├── BusinessException        - Violações de regras de negócio
├── DomainException         - Violações de lógica de domínio
├── ValidationException     - Erros de validação de entrada
├── NotFoundException       - Recurso não encontrado
├── ConflictException       - Conflitos de estado / concorrência
├── UnauthorizedException   - Não autenticado
├── ForbiddenException      - Não autorizado (sem permissão)
├── ConfigurationException  - Erros de configuração
├── DataException          - Erros de acesso a dados
├── PipelineException      - Erros de execução do pipeline
└── HttpStatusCodeException - Erros específicos de HTTP
```

## Exceção Base: Mvp24HoursException

Todas as exceções suportam códigos de erro e informações de contexto:

```csharp
public class Mvp24HoursException : Exception
{
    public string ErrorCode { get; }
    public IDictionary<string, object> Context { get; }
}
```

---

## BusinessException

Use quando regras de negócio são violadas.

### Quando Usar

- Saldo insuficiente para uma transação
- Pedido não pode ser cancelado (já foi enviado)
- Cota do usuário excedida
- Violação de política de negócio

### Exemplos

```csharp
using Mvp24Hours.Core.Exceptions;

// Uso simples
throw new BusinessException("Pedido não pode ser cancelado após o envio");

// Com código de erro
throw new BusinessException(
    "Saldo insuficiente para a transação",
    "SALDO_INSUFICIENTE"
);

// Com informações de contexto
throw new BusinessException(
    "Limite diário de transferência excedido",
    "LIMITE_TRANSFERENCIA_EXCEDIDO",
    new Dictionary<string, object>
    {
        ["ContaId"] = contaId,
        ["LimiteDiario"] = 5000m,
        ["TotalAtual"] = 4500m,
        ["ValorSolicitado"] = 1000m
    }
);
```

### Mapeamento HTTP

`BusinessException` tipicamente mapeia para HTTP 422 (Unprocessable Entity) ou 400 (Bad Request).

---

## ValidationException

Use para erros de validação de entrada.

### Quando Usar

- Formato de email inválido
- Campo obrigatório está vazio
- Valor fora do intervalo permitido
- Formato de dados inválido

### Exemplos

```csharp
using Mvp24Hours.Core.Exceptions;

// Erro de validação simples
throw new ValidationException("Email é obrigatório");

// Com nome do campo
throw new ValidationException("email", "Formato de email inválido");

// Múltiplos erros de validação
var erros = new Dictionary<string, string[]>
{
    ["Email"] = new[] { "Email é obrigatório", "Formato de email inválido" },
    ["Idade"] = new[] { "Idade deve estar entre 18 e 120" }
};
throw new ValidationException(erros);
```

### Integração com FluentValidation

```csharp
public class CriarClienteValidator : AbstractValidator<CriarClienteCommand>
{
    public CriarClienteValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório")
            .EmailAddress().WithMessage("Formato de email inválido");
            
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .MaximumLength(100).WithMessage("Nome não pode exceder 100 caracteres");
    }
}

// No handler ou service
var resultado = await validator.ValidateAsync(command);
if (!resultado.IsValid)
{
    throw new ValidationException(resultado.Errors);
}
```

### Mapeamento HTTP

`ValidationException` mapeia para HTTP 400 (Bad Request).

---

## NotFoundException

Use quando um recurso solicitado não existe.

### Quando Usar

- Entidade não encontrada pelo ID
- Arquivo não encontrado
- Chave de configuração ausente
- Recurso não existe

### Exemplos

```csharp
using Mvp24Hours.Core.Exceptions;

// Uso simples
throw new NotFoundException("Cliente não encontrado");

// Com tipo de recurso e identificador
throw new NotFoundException("Cliente", clienteId);

// Com contexto
throw new NotFoundException(
    "Pedido",
    pedidoId,
    new Dictionary<string, object>
    {
        ["BuscadoPor"] = "NumeroPedido",
        ["Status"] = "Apenas pedidos ativos"
    }
);
```

### Uso com Repository Pattern

```csharp
public async Task<Cliente> GetClienteAsync(Guid id)
{
    var cliente = await _repository.GetByIdAsync(id);
    
    if (cliente == null)
    {
        throw new NotFoundException("Cliente", id);
    }
    
    return cliente;
}
```

### Mapeamento HTTP

`NotFoundException` mapeia para HTTP 404 (Not Found).

---

## ConflictException

Use para conflitos de estado ou problemas de concorrência.

### Quando Usar

- Violação de concorrência otimista
- Inserção de chave duplicada
- Recurso já existe
- Transição de estado não permitida

### Exemplos

```csharp
using Mvp24Hours.Core.Exceptions;

// Recurso duplicado
throw new ConflictException("Um cliente com este email já existe");

// Com código de erro
throw new ConflictException(
    "Email já cadastrado",
    "EMAIL_DUPLICADO"
);

// Conflito de concorrência
throw new ConflictException(
    "O registro foi modificado por outro usuário",
    "CONFLITO_CONCORRENCIA",
    new Dictionary<string, object>
    {
        ["TipoEntidade"] = "Pedido",
        ["EntidadeId"] = pedidoId,
        ["VersaoEsperada"] = versaoEsperada,
        ["VersaoAtual"] = versaoAtual
    }
);
```

### Mapeamento HTTP

`ConflictException` mapeia para HTTP 409 (Conflict).

---

## UnauthorizedException

Use quando o usuário não está autenticado.

### Quando Usar

- Token de autenticação ausente
- Token inválido ou expirado
- Sessão expirada

### Exemplos

```csharp
using Mvp24Hours.Core.Exceptions;

// Uso simples
throw new UnauthorizedException("Autenticação necessária");

// Com detalhes
throw new UnauthorizedException(
    "Token expirado",
    "TOKEN_EXPIRADO",
    new Dictionary<string, object>
    {
        ["ExpirouEm"] = dataExpiracao
    }
);
```

### Mapeamento HTTP

`UnauthorizedException` mapeia para HTTP 401 (Unauthorized).

---

## ForbiddenException

Use quando o usuário está autenticado mas não tem permissão.

### Quando Usar

- Usuário não tem a role necessária
- Acesso ao recurso negado
- Operação não permitida para este usuário

### Exemplos

```csharp
using Mvp24Hours.Core.Exceptions;

// Uso simples
throw new ForbiddenException("Você não tem permissão para excluir este recurso");

// Com informações do recurso
throw new ForbiddenException(
    "Acesso negado",
    "ACESSO_NEGADO",
    new Dictionary<string, object>
    {
        ["Recurso"] = "PainelAdmin",
        ["RoleNecessaria"] = "Administrador",
        ["RoleUsuario"] = roleAtual
    }
);
```

### Mapeamento HTTP

`ForbiddenException` mapeia para HTTP 403 (Forbidden).

---

## DomainException

Use para violações de lógica de domínio.

### Quando Usar

- Violação de invariante em um agregado
- Transição de estado inválida
- Quebra de regra de domínio

### Exemplos

```csharp
using Mvp24Hours.Core.Exceptions;

// Erro de transição de estado
throw new DomainException("Não é possível enviar um pedido que não foi pago");

// Violação de invariante
throw new DomainException(
    "Pedido deve ter pelo menos um item",
    "PEDIDO_VAZIO"
);

// Com info do agregado
throw new DomainException(
    "Não é possível adicionar mais de 100 itens a um único pedido",
    "LIMITE_ITENS_PEDIDO",
    new Dictionary<string, object>
    {
        ["PedidoId"] = pedidoId,
        ["ItensAtuais"] = 100,
        ["MaxItens"] = 100
    }
);
```

### Mapeamento HTTP

`DomainException` mapeia para HTTP 422 (Unprocessable Entity).

---

## Tratamento de Exceções na Web API

### Handler Global de Exceções

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

### Usando a Extension ToBusinessResult

```csharp
using Mvp24Hours.Core.Extensions.Exceptions;

try
{
    await ProcessarPedidoAsync(pedidoId);
}
catch (BusinessException ex)
{
    // Converte exceção para IBusinessResult
    var resultado = ex.ToBusinessResult<Pedido>();
    // resultado.HasErrors == true
    // resultado.Messages contém os detalhes do erro
}
```

---

## Boas Práticas

1. **Escolha o tipo de exceção correto** - Use a exceção mais específica para seu cenário
2. **Sempre forneça mensagens significativas** - Ajude desenvolvedores a entender o que deu errado
3. **Use códigos de erro** - Permita que clientes tratem erros específicos programaticamente
4. **Adicione contexto quando útil** - Inclua dados relevantes para debugging
5. **Não exponha dados sensíveis** - Cuidado com informações de contexto em produção
6. **Faça log de exceções apropriadamente** - Use logging estruturado com códigos de erro

```csharp
// Bom
throw new BusinessException(
    "Não foi possível processar o pagamento: cartão recusado",
    "CARTAO_RECUSADO",
    new Dictionary<string, object>
    {
        ["TransacaoId"] = transacaoId,
        ["CodigoRecusa"] = codigoRecusa
    }
);

// Ruim - muito genérico
throw new Exception("Erro");

// Ruim - expõe dados sensíveis
throw new BusinessException($"Cartão {numeroCartao} foi recusado");
```

