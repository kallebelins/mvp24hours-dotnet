# Padrões Funcionais

O módulo Mvp24Hours.Core fornece padrões de programação funcional para código mais seguro e expressivo.

## Maybe&lt;T&gt;

`Maybe<T>` (também conhecido como `Option`) representa um valor que pode ou não existir, fornecendo uma alternativa type-safe ao null.

### Por Que Usar Maybe&lt;T&gt;?

```csharp
// Sem Maybe - perigo de referência nula
Customer cliente = repository.GetById(id);
string nome = cliente.Name; // NullReferenceException se cliente for null!

// Com Maybe - forçado a tratar ausência
Maybe<Customer> cliente = repository.GetById(id);
string nome = cliente
    .Map(c => c.Name)
    .ValueOr("Desconhecido");
```

### Criando Valores Maybe

```csharp
using Mvp24Hours.Core.ValueObjects.Functional;

// Criar um Maybe com um valor
var some = Maybe<int>.Some(42);
var some2 = Maybe.Some(42); // Usando helper estático

// Criar um Maybe vazio
var none = Maybe<int>.None;
var none2 = Maybe.None<int>(); // Usando helper estático

// Criar a partir de um valor potencialmente null
string nome = GetNome(); // pode ser null
var maybeNome = Maybe.From(nome); // Some se não for null, None se for null

// Conversão implícita
Maybe<int> valor = 42; // Automaticamente se torna Some(42)
Maybe<string> vazio = null; // Automaticamente se torna None
```

### Verificando por Valor

```csharp
var maybe = Maybe.Some(42);

if (maybe.HasValue)
{
    Console.WriteLine($"Valor: {maybe.Value}");
}

if (maybe.HasNoValue)
{
    Console.WriteLine("Nenhum valor presente");
}
```

### Obtendo o Valor com Segurança

#### ValueOr - Valor Padrão

```csharp
var maybe = Maybe<int>.None;

// Com valor padrão literal
int valor = maybe.ValueOr(0);

// Com função factory (avaliação lazy)
int valor = maybe.ValueOr(() => CalcularPadrao());
```

#### Match - Pattern Matching

```csharp
var maybeCliente = BuscarCliente("123");

// Retornar um valor baseado na presença
string mensagem = maybeCliente.Match(
    some: cliente => $"Olá, {cliente.Nome}!",
    none: () => "Cliente não encontrado"
);

// Executar ações baseadas na presença
maybeCliente.Match(
    some: cliente => EnviarEmailBoasVindas(cliente),
    none: () => LogClienteAusente()
);
```

### Transformando Valores

#### Map - Transformar o Valor

```csharp
var maybeCliente = BuscarCliente("123");

// Transformar Customer para string
Maybe<string> maybeNome = maybeCliente.Map(c => c.Nome);

// Encadear múltiplos maps
Maybe<string> maybeNomeMaiusculo = maybeCliente
    .Map(c => c.Nome)
    .Map(nome => nome.ToUpper());
```

#### Bind - Encadear Funções que Retornam Maybe

```csharp
Maybe<Cliente> BuscarCliente(string id) { /* ... */ }
Maybe<Pedido> GetUltimoPedido(Cliente cliente) { /* ... */ }
Maybe<Produto> GetPrimeiroProduto(Pedido pedido) { /* ... */ }

// Encadear operações que retornam Maybe
Maybe<Produto> produto = BuscarCliente("123")
    .Bind(cliente => GetUltimoPedido(cliente))
    .Bind(pedido => GetPrimeiroProduto(pedido));
```

### Filtragem

#### Where - Filtragem Condicional

```csharp
var maybeNumero = Maybe.Some(42);

// Retorna None se o predicado for falso
var maybePar = maybeNumero.Where(n => n % 2 == 0); // Some(42)
var maybeGrande = maybeNumero.Where(n => n > 100); // None
```

### Efeitos Colaterais

#### Tap - Executar Ação Sem Mudar o Valor

```csharp
BuscarCliente("123")
    .Tap(cliente => Log($"Cliente encontrado: {cliente.Id}"))
    .Map(cliente => cliente.Email);
```

### Conversão

```csharp
var maybe = Maybe.Some("hello");

// Converter para nullable
string valor = maybe.ToNullable(); // "hello" ou null
```

### Exemplos do Mundo Real

#### Padrão Repository

```csharp
public interface IClienteRepository
{
    Maybe<Cliente> GetById(Guid id);
    Maybe<Cliente> GetByEmail(string email);
}

public class ClienteService
{
    private readonly IClienteRepository _repository;
    
    public string GetSaudacaoCliente(Guid id)
    {
        return _repository.GetById(id)
            .Map(c => $"Olá, {c.Nome}!")
            .ValueOr("Olá, Visitante!");
    }
    
    public async Task<IActionResult> GetCliente(Guid id)
    {
        return _repository.GetById(id)
            .Match<IActionResult>(
                some: cliente => Ok(cliente),
                none: () => NotFound()
            );
    }
}
```

#### Encadeando Operações

```csharp
public Maybe<decimal> CalcularDesconto(string clienteId, string produtoId)
{
    return _clienteRepository.GetById(clienteId)
        .Bind(cliente => GetNivelFidelidade(cliente))
        .Bind(nivel => _produtoRepository.GetById(produtoId)
            .Map(produto => CalcularValorDesconto(nivel, produto)));
}
```

#### Busca de Configuração

```csharp
public class ConfigService
{
    private readonly Dictionary<string, string> _config;
    
    public Maybe<string> GetValor(string chave)
    {
        return _config.TryGetValue(chave, out var valor)
            ? Maybe.Some(valor)
            : Maybe<string>.None;
    }
    
    public Maybe<int> GetValorInt(string chave)
    {
        return GetValor(chave)
            .Bind(valor => int.TryParse(valor, out var numero)
                ? Maybe.Some(numero)
                : Maybe<int>.None);
    }
}
```

---

## Either&lt;TLeft, TRight&gt;

`Either<TLeft, TRight>` representa um valor que é um de dois tipos possíveis - tipicamente usado para operações que podem falhar, onde `Left` representa falha e `Right` representa sucesso.

### Criando Valores Either

```csharp
using Mvp24Hours.Core.ValueObjects.Functional;

// Sucesso (Right)
Either<string, int> sucesso = Either<string, int>.Right(42);

// Falha (Left)
Either<string, int> falha = Either<string, int>.Left("Erro ocorreu");
```

### Pattern Matching

```csharp
Either<string, Cliente> resultado = CriarCliente(dados);

// Match para obter um valor
string mensagem = resultado.Match(
    left: erro => $"Falhou: {erro}",
    right: cliente => $"Criado: {cliente.Nome}"
);

// Match para executar ações
resultado.Match(
    left: erro => LogErro(erro),
    right: cliente => NotificarCriacao(cliente)
);
```

### Transformando

```csharp
Either<string, int> resultado = ParseNumero(entrada);

// Mapear o valor right (sucesso)
Either<string, string> formatado = resultado.MapRight(n => $"Número: {n}");

// Mapear o valor left (erro)
Either<DetalhesErro, int> detalhado = resultado.MapLeft(msg => new DetalhesErro(msg));
```

### Exemplo do Mundo Real

```csharp
public Either<ErroValidacao, Cliente> CriarCliente(CriarClienteDto dto)
{
    if (string.IsNullOrEmpty(dto.Email))
    {
        return Either<ErroValidacao, Cliente>.Left(
            new ErroValidacao("Email é obrigatório")
        );
    }
    
    if (!EmailValido(dto.Email))
    {
        return Either<ErroValidacao, Cliente>.Left(
            new ErroValidacao("Formato de email inválido")
        );
    }
    
    var cliente = new Cliente(dto.Nome, dto.Email);
    return Either<ErroValidacao, Cliente>.Right(cliente);
}

// Uso
var resultado = CriarCliente(dto);
return resultado.Match(
    left: erro => BadRequest(erro),
    right: cliente => Ok(cliente)
);
```

---

## Extensões Maybe

A classe `MaybeExtensions` fornece métodos utilitários adicionais.

### Convertendo de BusinessResult

```csharp
using Mvp24Hours.Core.Extensions.Functional;

IBusinessResult<Cliente> resultado = await _service.GetClienteAsync(id);

// Converter para Maybe (None se resultado tiver erros ou dados null)
Maybe<Cliente> maybe = resultado.ToMaybe();
```

### Trabalhando com Coleções

```csharp
// Obter primeiro elemento como Maybe
var itens = new List<int> { 1, 2, 3 };
Maybe<int> primeiro = itens.FirstOrNone();           // Some(1)
Maybe<int> encontrado = itens.FirstOrNone(x => x > 5); // None

// Obter elemento único como Maybe
Maybe<int> unico = itens.SingleOrNone(x => x == 2); // Some(2)
```

---

## Boas Práticas

### 1. Use Maybe para Valores de Retorno Opcionais

```csharp
// Bom - explícito sobre possível ausência
public Maybe<Cliente> BuscarPorEmail(string email);

// Evite - null é implícito
public Cliente BuscarPorEmail(string email);
```

### 2. Use Either para Operações que Podem Falhar

```csharp
// Bom - tipo de erro é explícito
public Either<ErroValidacao, Pedido> CriarPedido(PedidoDto dto);

// Alternativa - use padrão Result
public IBusinessResult<Pedido> CriarPedido(PedidoDto dto);
```

### 3. Prefira Map/Bind a Verificações Manuais

```csharp
// Bom - estilo funcional
var nome = maybeCliente
    .Map(c => c.Nome)
    .ValueOr("Desconhecido");

// Evite - estilo imperativo
string nome;
if (maybeCliente.HasValue)
    nome = maybeCliente.Value.Nome;
else
    nome = "Desconhecido";
```

### 4. Não Use Maybe para Valores Obrigatórios

```csharp
// Ruim - se cliente é sempre obrigatório, use Guard
public void ProcessarPedido(Maybe<Cliente> cliente)

// Bom - valide no ponto de entrada
public void ProcessarPedido(Cliente cliente)
{
    Guard.Against.Null(cliente, nameof(cliente));
}
```

