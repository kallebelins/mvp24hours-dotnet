# Como implementar contexto de banco de dados?
Contexto representa uma sessão com o banco de dados e pode ser usado para consultar e salvar instâncias de entidades.

## Configuração Básica
```csharp
public class MyDataContext : Mvp24HoursContext
{
    public MyDataContext()
        : base()
    {
    }

    public MyDataContext(DbContextOptions options)
        : base(options)
    {
    }

    public virtual DbSet<MyEntity> MyEntity { get; set; }
}
```

## Configuração com Log
Caso queria controlar log de entidade dinamicamente, basta aplicar a configuração abaixo:
```csharp
public class MyDataContext : Mvp24HoursContext
{
    [...]
    public override bool CanApplyEntityLog => true;
}
```
Sua entidade deverá implementar interface de log. [Veja Entidade](use-entity.md)

Uma das implementações de log oferecem a possibilidade de preencher o ID do usuário que está criando, atualizando ou excluindo o registro (exclusão lógica). Para carregar os dados do usuário logado, sugiro:
```csharp
public class MyDataContext : Mvp24HoursContext
{
    private readonly IHttpContextAccessor accessor;

    public MyDataContext(IHttpContextAccessor accessor)
        : base()
    {
        this.accessor = accessor;
    }

    public MyDataContext(DbContextOptions options, IHttpContextAccessor accessor)
        : base(options)
    {
        this.accessor = accessor;
    }

    public override object EntityLogBy => this.accessor.MyExtensionGetUser();

    public override bool CanApplyEntityLog => true;

    public virtual DbSet<MyEntity> MyEntity { get; set; }
}
```

## Abordagem Moderna com ICurrentUserProvider e IClock

Para aplicações .NET 9+, use as interfaces `ICurrentUserProvider` e `IClock` (ou `TimeProvider`) para melhor testabilidade:

```csharp
public class MyDataContext : Mvp24HoursContext
{
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IClock _clock; // ou TimeProvider para .NET 9+

    public MyDataContext(
        DbContextOptions options, 
        ICurrentUserProvider currentUserProvider,
        IClock clock)
        : base(options)
    {
        _currentUserProvider = currentUserProvider;
        _clock = clock;
    }

    public override object EntityLogBy => _currentUserProvider.GetUserId();
    
    public override DateTime GetCurrentDateTime() => _clock.UtcNow.DateTime;

    public override bool CanApplyEntityLog => true;

    public virtual DbSet<MyEntity> MyEntity { get; set; }
}
```

Registre os providers no seu `Program.cs`:

```csharp
// Program.cs
builder.Services.AddScoped<ICurrentUserProvider, HttpContextUserProvider>();
builder.Services.AddSingleton<IClock, SystemClock>();
// Ou para .NET 9+:
builder.Services.AddSingleton(TimeProvider.System);
```
