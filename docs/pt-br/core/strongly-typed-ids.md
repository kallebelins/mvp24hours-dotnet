# IDs Fortemente Tipados

IDs fortemente tipados previnem a mistura acidental de identificadores de diferentes tipos de entidade, fornecendo segurança em tempo de compilação.

## O Problema

Com IDs primitivos, nada previne a mistura deles:

```csharp
// Perigoso - ambos são Guid
public class Pedido
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
}

public void ProcessarPedido(Guid pedidoId, Guid clienteId)
{
    // Fácil de trocar por engano - compila normalmente!
    var pedido = _repo.GetPedido(clienteId); // Bug!
}
```

## A Solução

Com IDs fortemente tipados, o compilador detecta erros:

```csharp
public class Pedido
{
    public PedidoId Id { get; set; }
    public ClienteId ClienteId { get; set; }
}

public void ProcessarPedido(PedidoId pedidoId, ClienteId clienteId)
{
    // Isso não compila - tipo incompatível!
    // var pedido = _repo.GetPedido(clienteId);
    
    var pedido = _repo.GetPedido(pedidoId); // Correto
}
```

## Criando IDs Fortemente Tipados

### Usando GuidEntityId

```csharp
using Mvp24Hours.Core.ValueObjects;

public sealed class ClienteId : GuidEntityId<ClienteId>
{
    public ClienteId(Guid value) : base(value) { }
    
    public static ClienteId New() => new(Guid.NewGuid());
    public static ClienteId Empty => new(Guid.Empty);
}

public sealed class PedidoId : GuidEntityId<PedidoId>
{
    public PedidoId(Guid value) : base(value) { }
    
    public static PedidoId New() => new(Guid.NewGuid());
    public static PedidoId Empty => new(Guid.Empty);
}

public sealed class ProdutoId : GuidEntityId<ProdutoId>
{
    public ProdutoId(Guid value) : base(value) { }
    
    public static ProdutoId New() => new(Guid.NewGuid());
}
```

### Usando IntEntityId

```csharp
public sealed class CategoriaId : IntEntityId<CategoriaId>
{
    public CategoriaId(int value) : base(value) { }
}

public sealed class NumeroSequencia : IntEntityId<NumeroSequencia>
{
    public NumeroSequencia(int value) : base(value) { }
}
```

### Usando LongEntityId

```csharp
public sealed class TransacaoId : LongEntityId<TransacaoId>
{
    public TransacaoId(long value) : base(value) { }
}
```

### Usando StringEntityId

```csharp
public sealed class Sku : StringEntityId<Sku>
{
    public Sku(string value) : base(value) { }
    
    public static Sku Create(string value)
    {
        Guard.Against.NullOrEmpty(value, nameof(value));
        Guard.Against.InvalidFormat(value, @"^[A-Z]{3}-\d{6}$", nameof(value));
        return new Sku(value);
    }
}
```

## Uso em Entidades

```csharp
public class Cliente
{
    public ClienteId Id { get; private set; }
    public string Nome { get; private set; }
    
    public Cliente(string nome)
    {
        Id = ClienteId.New();
        Nome = nome;
    }
}

public class Pedido
{
    public PedidoId Id { get; private set; }
    public ClienteId ClienteId { get; private set; }
    public List<ItemPedido> Itens { get; private set; }
    
    public Pedido(ClienteId clienteId)
    {
        Id = PedidoId.New();
        ClienteId = clienteId;
        Itens = new List<ItemPedido>();
    }
}

public class ItemPedido
{
    public ItemPedidoId Id { get; private set; }
    public PedidoId PedidoId { get; private set; }
    public ProdutoId ProdutoId { get; private set; }
    public int Quantidade { get; private set; }
}
```

## Funcionalidades

### Igualdade

```csharp
var id1 = ClienteId.New();
var id2 = new ClienteId(id1.Value);

bool saoIguais = id1 == id2;        // true
bool saoIguais2 = id1.Equals(id2);  // true
```

### Comparação

```csharp
var ids = new List<ClienteId> { id3, id1, id2 };
ids.Sort(); // Funciona - implementa IComparable
```

### Verificação de Vazio/Padrão

```csharp
var clienteId = ClienteId.New();
var idVazio = ClienteId.Empty;

bool vazio = idVazio.IsEmpty;       // true (GuidEntityId)
bool padrao = categoriaId.IsDefault; // true para 0 (IntEntityId)
```

### Conversão Implícita para Tipo Subjacente

```csharp
var clienteId = ClienteId.New();

// Conversão implícita para Guid
Guid guidValue = clienteId;

// Usar em LINQ
var pedidos = _context.Pedidos
    .Where(p => p.ClienteId.Value == clienteId)
    .ToList();
```

## Integração com Entity Framework Core

### Value Converter

```csharp
public class ClienteIdConverter : ValueConverter<ClienteId, Guid>
{
    public ClienteIdConverter()
        : base(
            id => id.Value,
            guid => new ClienteId(guid))
    { }
}

// Em DbContext OnModelCreating
modelBuilder.Entity<Cliente>()
    .Property(c => c.Id)
    .HasConversion<ClienteIdConverter>();

modelBuilder.Entity<Pedido>()
    .Property(p => p.ClienteId)
    .HasConversion<ClienteIdConverter>();
```

### Converter Genérico

```csharp
// Para todos os tipos GuidEntityId
public class GuidEntityIdConverter<TId> : ValueConverter<TId, Guid>
    where TId : GuidEntityId<TId>
{
    public GuidEntityIdConverter()
        : base(
            id => id.Value,
            guid => (TId)Activator.CreateInstance(typeof(TId), guid)!)
    { }
}
```

## Serialização JSON

### System.Text.Json

```csharp
public class GuidEntityIdJsonConverter<TId> : JsonConverter<TId>
    where TId : GuidEntityId<TId>
{
    public override TId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var guid = reader.GetGuid();
        return (TId)Activator.CreateInstance(typeof(TId), guid)!;
    }

    public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}
```

### Newtonsoft.Json

A classe `EntityIdNewtonsoftConverters` é fornecida em `Mvp24Hours.Core.Converters`:

```csharp
var settings = new JsonSerializerSettings
{
    Converters = { new GuidEntityIdNewtonsoftConverter<ClienteId>() }
};
```

## Padrão Repository

```csharp
public interface IClienteRepository
{
    Task<Cliente?> GetByIdAsync(ClienteId id);
    Task<IEnumerable<Cliente>> GetByIdsAsync(IEnumerable<ClienteId> ids);
}

public class ClienteRepository : IClienteRepository
{
    private readonly DbContext _context;
    
    public async Task<Cliente?> GetByIdAsync(ClienteId id)
    {
        return await _context.Clientes
            .FirstOrDefaultAsync(c => c.Id == id);
    }
    
    public async Task<IEnumerable<Cliente>> GetByIdsAsync(IEnumerable<ClienteId> ids)
    {
        var guidIds = ids.Select(id => id.Value).ToList();
        return await _context.Clientes
            .Where(c => guidIds.Contains(c.Id.Value))
            .ToListAsync();
    }
}
```

## Controllers de API

```csharp
[ApiController]
[Route("api/clientes")]
public class ClientesController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<Cliente>> Get(Guid id)
    {
        var clienteId = new ClienteId(id);
        var cliente = await _repository.GetByIdAsync(clienteId);
        
        return cliente is null ? NotFound() : Ok(cliente);
    }
    
    [HttpGet("{clienteId}/pedidos")]
    public async Task<ActionResult<IEnumerable<Pedido>>> GetPedidos(Guid clienteId)
    {
        var id = new ClienteId(clienteId);
        var pedidos = await _pedidoRepository.GetByClienteIdAsync(id);
        return Ok(pedidos);
    }
}
```

## Boas Práticas

1. **Crie um tipo de ID por entidade** - `ClienteId`, `PedidoId`, `ProdutoId`
2. **Use a classe base apropriada** - `GuidEntityId`, `IntEntityId`, `LongEntityId`, ou `StringEntityId`
3. **Adicione métodos factory** - `New()`, `Empty`, `Create()` para conveniência
4. **Configure converters do EF Core** - Garanta mapeamento correto no banco de dados
5. **Trate serialização** - Configure converters JSON para APIs

