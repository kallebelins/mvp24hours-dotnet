# Abstrações de Infraestrutura

O módulo Mvp24Hours.Core fornece abstrações para preocupações de infraestrutura, permitindo testabilidade e desacoplamento de implementações concretas.

## IClock

Abstrai o tempo do sistema para testabilidade e manipulação de tempo.

### Por Que Usar IClock?

```csharp
// Difícil de testar - depende do tempo real
public bool Expirou(DateTime dataExpiracao)
{
    return DateTime.UtcNow > dataExpiracao; // Não dá para controlar em testes
}

// Testável - usa abstração
public bool Expirou(DateTime dataExpiracao, IClock clock)
{
    return clock.UtcNow > dataExpiracao; // Pode injetar test clock
}
```

### Interface

```csharp
public interface IClock
{
    DateTime UtcNow { get; }
    DateTime Now { get; }
    DateTime UtcToday { get; }
    DateTime Today { get; }
    DateTimeOffset UtcNowOffset { get; }
    DateTimeOffset NowOffset { get; }
}
```

### SystemClock (Produção)

```csharp
// Registro
services.AddSingleton<IClock, SystemClock>();

// Uso
public class PedidoService
{
    private readonly IClock _clock;
    
    public PedidoService(IClock clock)
    {
        _clock = clock;
    }
    
    public Pedido CriarPedido(Carrinho carrinho)
    {
        return new Pedido
        {
            CriadoEm = _clock.UtcNow,
            ExpiraEm = _clock.UtcNow.AddDays(30)
        };
    }
}
```

### TestClock (Testes)

```csharp
public class TestClock : IClock
{
    private DateTime _tempoAtual;
    
    public TestClock(DateTime tempoInicial)
    {
        _tempoAtual = tempoInicial;
    }
    
    public DateTime UtcNow => _tempoAtual;
    public DateTime Now => _tempoAtual.ToLocalTime();
    public DateTime UtcToday => _tempoAtual.Date;
    public DateTime Today => _tempoAtual.ToLocalTime().Date;
    public DateTimeOffset UtcNowOffset => new(_tempoAtual, TimeSpan.Zero);
    public DateTimeOffset NowOffset => new(_tempoAtual.ToLocalTime(), DateTimeOffset.Now.Offset);
    
    public void AvancarPor(TimeSpan duracao)
    {
        _tempoAtual = _tempoAtual.Add(duracao);
    }
    
    public void DefinirTempo(DateTime tempo)
    {
        _tempoAtual = tempo;
    }
}
```

### Exemplo de Teste

```csharp
[Fact]
public void Pedido_Deve_Expirar_Apos_30_Dias()
{
    // Arrange
    var dataInicial = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    var clock = new TestClock(dataInicial);
    var service = new PedidoService(clock);
    
    var pedido = service.CriarPedido(carrinho);
    
    // Act - avança o tempo em 31 dias
    clock.AvancarPor(TimeSpan.FromDays(31));
    
    // Assert
    Assert.True(service.PedidoExpirou(pedido));
}

[Fact]
public void Pedido_Nao_Deve_Expirar_Dentro_De_30_Dias()
{
    // Arrange
    var dataInicial = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    var clock = new TestClock(dataInicial);
    var service = new PedidoService(clock);
    
    var pedido = service.CriarPedido(carrinho);
    
    // Act - avança o tempo em 29 dias
    clock.AvancarPor(TimeSpan.FromDays(29));
    
    // Assert
    Assert.False(service.PedidoExpirou(pedido));
}
```

---

## IGuidGenerator

Abstrai a geração de GUIDs para testabilidade e estratégias especiais de geração.

### Interface

```csharp
public interface IGuidGenerator
{
    Guid NewGuid();
}
```

### DefaultGuidGenerator

Geração padrão de GUID:

```csharp
public class DefaultGuidGenerator : IGuidGenerator
{
    public Guid NewGuid() => Guid.NewGuid();
}

// Registro
services.AddSingleton<IGuidGenerator, DefaultGuidGenerator>();
```

### SequentialGuidGenerator

Gera GUIDs sequenciais otimizados para índices de banco de dados (amigável para SQL Server):

```csharp
public class SequentialGuidGenerator : IGuidGenerator
{
    public Guid NewGuid()
    {
        // Cria GUIDs que são sequenciais quando ordenados
        // Melhor performance de índice no SQL Server
        return CreateSequentialGuid();
    }
}

// Registro para SQL Server
services.AddSingleton<IGuidGenerator, SequentialGuidGenerator>();
```

### DeterministicGuidGenerator (Testes)

Gera GUIDs previsíveis para testes:

```csharp
public class DeterministicGuidGenerator : IGuidGenerator
{
    private int _contador;
    
    public Guid NewGuid()
    {
        _contador++;
        return new Guid($"00000000-0000-0000-0000-{_contador:D12}");
    }
    
    public void Resetar()
    {
        _contador = 0;
    }
}

// Uso em testes
[Fact]
public void Deve_Criar_Cliente_Com_Id()
{
    // Arrange
    var guidGenerator = new DeterministicGuidGenerator();
    var service = new ClienteService(guidGenerator);
    
    // Act
    var cliente1 = service.CriarCliente("João");
    var cliente2 = service.CriarCliente("Maria");
    
    // Assert - IDs previsíveis
    Assert.Equal(new Guid("00000000-0000-0000-0000-000000000001"), cliente1.Id);
    Assert.Equal(new Guid("00000000-0000-0000-0000-000000000002"), cliente2.Id);
}
```

### Uso em Services

```csharp
public class ClienteService
{
    private readonly IGuidGenerator _guidGenerator;
    
    public ClienteService(IGuidGenerator guidGenerator)
    {
        _guidGenerator = guidGenerator;
    }
    
    public Cliente CriarCliente(string nome)
    {
        return new Cliente
        {
            Id = _guidGenerator.NewGuid(),
            Nome = nome
        };
    }
}
```

---

## ICurrentUserProvider

Abstrai o acesso ao contexto do usuário atual.

```csharp
public interface ICurrentUserProvider
{
    string UserId { get; }
    string UserName { get; }
    IEnumerable<string> Roles { get; }
    bool IsAuthenticated { get; }
}
```

### Implementação para ASP.NET Core

```csharp
public class HttpContextUserProvider : ICurrentUserProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public HttpContextUserProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public string UserId => 
        _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
        ?? "anonimo";
    
    public string UserName =>
        _httpContextAccessor.HttpContext?.User?.Identity?.Name 
        ?? "Anônimo";
    
    public IEnumerable<string> Roles =>
        _httpContextAccessor.HttpContext?.User?.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value) 
        ?? Enumerable.Empty<string>();
    
    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
```

### Implementação para Testes

```csharp
public class TestUserProvider : ICurrentUserProvider
{
    public string UserId { get; set; } = "id-usuario-teste";
    public string UserName { get; set; } = "Usuário Teste";
    public IEnumerable<string> Roles { get; set; } = new[] { "Usuario" };
    public bool IsAuthenticated { get; set; } = true;
}
```

---

## ITenantProvider

Abstrai a resolução de contexto multi-tenant.

```csharp
public interface ITenantProvider
{
    string TenantId { get; }
    string TenantName { get; }
}
```

### Implementação

```csharp
public class HttpHeaderTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public string TenantId =>
        _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault()
        ?? throw new InvalidOperationException("Header de tenant não encontrado");
    
    public string TenantName =>
        _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Name"].FirstOrDefault()
        ?? TenantId;
}
```

---

## Registro

```csharp
public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructureAbstractions(
        this IServiceCollection services)
    {
        // Clock
        services.AddSingleton<IClock, SystemClock>();
        
        // Gerador de GUID (use SequentialGuidGenerator para SQL Server)
        services.AddSingleton<IGuidGenerator, DefaultGuidGenerator>();
        
        // Provider de Usuário
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserProvider, HttpContextUserProvider>();
        
        return services;
    }
    
    public static IServiceCollection AddTestInfrastructure(
        this IServiceCollection services,
        DateTime? tempoInicial = null)
    {
        var clock = new TestClock(tempoInicial ?? DateTime.UtcNow);
        services.AddSingleton<IClock>(clock);
        services.AddSingleton(clock); // Também como tipo concreto para controle de teste
        
        var guidGen = new DeterministicGuidGenerator();
        services.AddSingleton<IGuidGenerator>(guidGen);
        services.AddSingleton(guidGen);
        
        services.AddSingleton<ICurrentUserProvider>(new TestUserProvider());
        
        return services;
    }
}
```

---

## Boas Práticas

1. **Sempre injete IClock** - Nunca use `DateTime.Now` ou `DateTime.UtcNow` diretamente
2. **Use IGuidGenerator para IDs de entidade** - Permite testes previsíveis
3. **Considere SequentialGuidGenerator** - Melhor performance de banco de dados para SQL Server
4. **Crie implementações de teste** - Torne os testes determinísticos e rápidos
5. **Registre com o lifetime apropriado** - Singletons para stateless, Scoped para específico de request

