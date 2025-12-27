# Guard Clauses

Guard clauses fornecem uma API fluente para programação defensiva, validando argumentos de métodos e lançando exceções apropriadas quando as validações falham.

## Visão Geral

A classe `Guard` fornece um ponto de entrada estático `Guard.Against` que oferece vários métodos de validação:

```csharp
using Mvp24Hours.Core.Helpers;

public void ProcessarPedido(Pedido pedido, string clienteId, decimal valor)
{
    Guard.Against.Null(pedido, nameof(pedido));
    Guard.Against.NullOrEmpty(clienteId, nameof(clienteId));
    Guard.Against.NegativeOrZero(valor, nameof(valor));
    
    // Sua lógica aqui - parâmetros são garantidamente válidos
}
```

## Métodos Guard Disponíveis

### Verificações de Null

#### Guard.Against.Null&lt;T&gt;

Lança `ArgumentNullException` se o valor for null.

```csharp
public void EnviarEmail(MensagemEmail mensagem)
{
    Guard.Against.Null(mensagem, nameof(mensagem));
    // mensagem é garantidamente não-null aqui
}
```

#### Guard.Against.NullOrEmpty (string)

Lança `ArgumentNullException` se null, `ArgumentException` se vazio.

```csharp
public Usuario CriarUsuario(string nomeUsuario, string email)
{
    Guard.Against.NullOrEmpty(nomeUsuario, nameof(nomeUsuario));
    Guard.Against.NullOrEmpty(email, nameof(email));
    
    return new Usuario(nomeUsuario, email);
}
```

#### Guard.Against.NullOrWhiteSpace

Lança exceção se null, vazio, ou apenas espaços em branco.

```csharp
public void SetDescricao(string descricao)
{
    Guard.Against.NullOrWhiteSpace(descricao, nameof(descricao));
}
```

#### Guard.Against.NullOrEmpty&lt;T&gt; (coleções)

Lança exceção se a coleção for null ou vazia.

```csharp
public decimal CalcularMedia(IEnumerable<decimal> valores)
{
    Guard.Against.NullOrEmpty(valores, nameof(valores));
    return valores.Average();
}
```

### Verificações de Valor Padrão

#### Guard.Against.Default&lt;T&gt;

Lança `ArgumentException` se o valor for igual a `default(T)`. Útil para structs como `Guid.Empty`.

```csharp
public Pedido GetPedido(Guid pedidoId)
{
    Guard.Against.Default(pedidoId, nameof(pedidoId));
    // pedidoId é garantidamente diferente de Guid.Empty
    
    return _repository.GetById(pedidoId);
}
```

#### Guard.Against.EmptyGuid

Verifica especificamente por `Guid.Empty`.

```csharp
public void AtribuirParaUsuario(Guid usuarioId)
{
    Guard.Against.EmptyGuid(usuarioId, nameof(usuarioId));
}
```

### Verificações de Intervalo

#### Guard.Against.OutOfRange&lt;T&gt;

Lança `ArgumentOutOfRangeException` se o valor estiver fora do intervalo especificado.

```csharp
public void SetAvaliacao(int avaliacao)
{
    Guard.Against.OutOfRange(avaliacao, 1, 5, nameof(avaliacao));
    // avaliacao é garantidamente entre 1 e 5
}

public void SetDesconto(decimal percentual)
{
    Guard.Against.OutOfRange(percentual, 0m, 100m, nameof(percentual));
}
```

#### Guard.Against.NegativeOrZero

Lança exceção se o valor for zero ou negativo.

```csharp
public void SetQuantidade(int quantidade)
{
    Guard.Against.NegativeOrZero(quantidade, nameof(quantidade));
}

public void SetPreco(decimal preco)
{
    Guard.Against.NegativeOrZero(preco, nameof(preco));
}
```

#### Guard.Against.Negative

Lança exceção se o valor for negativo (zero é permitido).

```csharp
public void SetSaldo(decimal saldo)
{
    Guard.Against.Negative(saldo, nameof(saldo));
    // saldo pode ser 0 ou positivo
}
```

#### Guard.Against.LessThan / GreaterThan

```csharp
public void SetIdade(int idade)
{
    Guard.Against.LessThan(idade, 0, nameof(idade));
    Guard.Against.GreaterThan(idade, 150, nameof(idade));
}
```

### Verificações de Tamanho de String

#### Guard.Against.LengthLessThan / LengthGreaterThan

```csharp
public void SetSenha(string senha)
{
    Guard.Against.NullOrEmpty(senha, nameof(senha));
    Guard.Against.LengthLessThan(senha, 8, nameof(senha));
    Guard.Against.LengthGreaterThan(senha, 100, nameof(senha));
}
```

#### Guard.Against.LengthOutOfRange

```csharp
public void SetNomeUsuario(string nomeUsuario)
{
    Guard.Against.NullOrEmpty(nomeUsuario, nameof(nomeUsuario));
    Guard.Against.LengthOutOfRange(nomeUsuario, 3, 50, nameof(nomeUsuario));
}
```

### Validações de Formato

#### Guard.Against.InvalidFormat

Valida contra um padrão regex.

```csharp
public void SetCodigoProduto(string codigo)
{
    Guard.Against.InvalidFormat(codigo, @"^[A-Z]{2}-\d{4}$", nameof(codigo));
    // codigo deve corresponder ao padrão como "AB-1234"
}
```

#### Guard.Against.InvalidEmail

Valida formato de email (compatível com RFC 5322).

```csharp
public void SetEmail(string email)
{
    Guard.Against.InvalidEmail(email, nameof(email));
}
```

### Validações de Documentos Brasileiros

#### Guard.Against.InvalidCpf

Valida CPF brasileiro (Cadastro de Pessoa Física).

```csharp
public void SetCpf(string cpf)
{
    Guard.Against.InvalidCpf(cpf, nameof(cpf));
    // Valida formato e dígitos verificadores
    // Aceita: "123.456.789-09" ou "12345678909"
}
```

#### Guard.Against.InvalidCnpj

Valida CNPJ brasileiro (Cadastro Nacional da Pessoa Jurídica).

```csharp
public void SetCnpj(string cnpj)
{
    Guard.Against.InvalidCnpj(cnpj, nameof(cnpj));
    // Valida formato e dígitos verificadores
    // Aceita: "12.345.678/0001-95" ou "12345678000195"
}
```

### Verificações de Tipo

#### Guard.Against.NotOfType&lt;T&gt;

Valida que um objeto é de um tipo específico.

```csharp
public void ProcessarPagamento(IMetodoPagamento metodo)
{
    var cartaoCredito = Guard.Against.NotOfType<PagamentoCartaoCredito>(
        metodo, 
        nameof(metodo)
    );
    // cartaoCredito agora está tipado como PagamentoCartaoCredito
}
```

### Verificações de Condição

#### Guard.Against.Condition

Lança `ArgumentException` se a condição for verdadeira.

```csharp
public void Transferir(Conta origem, Conta destino, decimal valor)
{
    Guard.Against.Condition(
        origem.Id == destino.Id,
        nameof(destino),
        "Não é possível transferir para a mesma conta"
    );
}
```

#### Guard.Against.InvalidOperation

Lança `InvalidOperationException` se a condição for verdadeira. Use para validação de estado.

```csharp
public void Enviar(Pedido pedido)
{
    Guard.Against.InvalidOperation(
        pedido.Status != StatusPedido.Pago,
        "Não é possível enviar um pedido não pago"
    );
}
```

## Mensagens de Erro Customizadas

Todos os métodos guard aceitam uma mensagem customizada opcional:

```csharp
Guard.Against.Null(
    cliente, 
    nameof(cliente), 
    "Cliente deve ser fornecido para criar um pedido"
);

Guard.Against.NegativeOrZero(
    valor, 
    nameof(valor),
    "Valor do pedido deve ser maior que zero"
);
```

## Retornos Fluentes

Métodos guard retornam o valor validado, permitindo uso fluente:

```csharp
public class Pedido
{
    public string ClienteId { get; }
    public decimal Valor { get; }
    
    public Pedido(string clienteId, decimal valor)
    {
        ClienteId = Guard.Against.NullOrEmpty(clienteId, nameof(clienteId));
        Valor = Guard.Against.NegativeOrZero(valor, nameof(valor));
    }
}
```

## Boas Práticas

### 1. Guard nos Pontos de Entrada

Coloque guards no início de métodos públicos:

```csharp
public async Task<Pedido> CriarPedidoAsync(
    string clienteId, 
    IEnumerable<ItemPedido> itens,
    Endereco enderecoEntrega)
{
    // Todos os guards primeiro
    Guard.Against.NullOrEmpty(clienteId, nameof(clienteId));
    Guard.Against.NullOrEmpty(itens, nameof(itens));
    Guard.Against.Null(enderecoEntrega, nameof(enderecoEntrega));
    
    // Depois a lógica de negócio
    var cliente = await _clienteService.GetAsync(clienteId);
    // ...
}
```

### 2. Use Guards Específicos

Escolha o guard mais específico para sua validação:

```csharp
// Bom - específico
Guard.Against.EmptyGuid(pedidoId, nameof(pedidoId));

// Menos bom - genérico
Guard.Against.Default(pedidoId, nameof(pedidoId));
```

### 3. Combine com Value Objects

Use guards em construtores de Value Objects:

```csharp
public class Email
{
    public string Value { get; }
    
    public Email(string value)
    {
        Value = Guard.Against.InvalidEmail(value, nameof(value));
    }
}
```

### 4. Validação de Domínio vs Guard Clauses

- **Guard clauses**: Para validação de argumentos (erros de programação)
- **Validação de domínio**: Para regras de negócio (erros de entrada do usuário)

```csharp
// Guard clause - erro de programação se null
Guard.Against.Null(pedido, nameof(pedido));

// Validação de domínio - erro de entrada do usuário
if (pedido.Total > cliente.LimiteCredito)
{
    throw new BusinessException("Pedido excede o limite de crédito");
}
```

