# Interfaces de Entidade

O módulo Mvp24Hours.Core fornece um conjunto de interfaces e classes base para construir entidades de domínio seguindo princípios DDD.

## Hierarquia de Interfaces

```
IEntity<TId>
├── IAuditableEntity    - Rastreia criação e modificação
├── ISoftDeletable      - Suporta exclusão lógica
├── ITenantEntity       - Suporte a multi-tenancy
└── IVersionedEntity    - Concorrência otimista
```

## IEntity&lt;TId&gt;

Interface base para todas as entidades com identificador fortemente tipado.

```csharp
public interface IEntity<TId> where TId : IEquatable<TId>
{
    TId Id { get; }
}
```

### Uso

```csharp
public class Cliente : IEntity<Guid>
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    
    public Cliente(string nome)
    {
        Id = Guid.NewGuid();
        Nome = nome;
    }
}
```

---

## IAuditableEntity

Rastreia quando e por quem uma entidade foi criada e modificada.

```csharp
public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    string CreatedBy { get; set; }
    DateTime? ModifiedAt { get; set; }
    string ModifiedBy { get; set; }
}
```

### Uso

```csharp
public class Produto : IEntity<Guid>, IAuditableEntity
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    
    // Campos de auditoria
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = string.Empty;
}
```

### Preenchimento Automático com EF Core

```csharp
public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var usuarioAtual = _currentUserProvider.UserId;
    var agora = _clock.UtcNow;
    
    foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
    {
        switch (entry.State)
        {
            case EntityState.Added:
                entry.Entity.CreatedAt = agora;
                entry.Entity.CreatedBy = usuarioAtual;
                break;
            case EntityState.Modified:
                entry.Entity.ModifiedAt = agora;
                entry.Entity.ModifiedBy = usuarioAtual;
                break;
        }
    }
    
    return base.SaveChangesAsync(cancellationToken);
}
```

---

## ISoftDeletable

Suporta exclusão lógica (soft delete) ao invés de exclusão física.

```csharp
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}
```

### Uso

```csharp
public class Documento : IEntity<Guid>, ISoftDeletable
{
    public Guid Id { get; private set; }
    public string Titulo { get; private set; }
    
    // Campos de soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    
    public void Excluir(string excluidoPor)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = excluidoPor;
    }
    
    public void Restaurar()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }
}
```

### Filtro Global de Query

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Automaticamente exclui entidades soft-deleted
    modelBuilder.Entity<Documento>()
        .HasQueryFilter(d => !d.IsDeleted);
}

// Para incluir itens deletados
var todosDocs = await _context.Documentos
    .IgnoreQueryFilters()
    .ToListAsync();
```

---

## ITenantEntity

Habilita multi-tenancy associando entidades com tenants.

```csharp
public interface ITenantEntity
{
    string TenantId { get; set; }
}
```

### Uso

```csharp
public class Fatura : IEntity<Guid>, ITenantEntity
{
    public Guid Id { get; private set; }
    public string Numero { get; private set; }
    public decimal Total { get; private set; }
    
    // Multi-tenancy
    public string TenantId { get; set; } = string.Empty;
}
```

### Filtro Automático por Tenant

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Fatura>()
        .HasQueryFilter(f => f.TenantId == _tenantProvider.TenantId);
}
```

---

## IVersionedEntity

Suporta concorrência otimista usando um campo de versão/row version.

```csharp
public interface IVersionedEntity
{
    byte[] RowVersion { get; set; }
}
```

### Uso

```csharp
public class Pedido : IEntity<Guid>, IVersionedEntity
{
    public Guid Id { get; private set; }
    public decimal Total { get; private set; }
    
    // Token de concorrência
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
```

### Configuração EF Core

```csharp
modelBuilder.Entity<Pedido>()
    .Property(p => p.RowVersion)
    .IsRowVersion();
```

### Tratando Conflitos de Concorrência

```csharp
try
{
    await _context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException ex)
{
    throw new ConflictException(
        "O registro foi modificado por outro usuário",
        "CONFLITO_CONCORRENCIA"
    );
}
```

---

## Classes Base

### EntityBase&lt;TId&gt;

Classe base implementando `IEntity<TId>` com suporte a igualdade.

```csharp
public class Cliente : EntityBase<Guid>
{
    public string Nome { get; private set; }
    
    public Cliente(string nome)
    {
        Id = Guid.NewGuid();
        Nome = nome;
    }
}
```

Funcionalidades:
- Implementa `IEquatable<EntityBase<TId>>`
- Igualdade baseada em Id
- Implementação correta de `GetHashCode()`

### AuditableEntity&lt;TId&gt;

Combina identidade de entidade com rastreamento de auditoria.

```csharp
public class Produto : AuditableEntity<Guid>
{
    public string Nome { get; private set; }
    public decimal Preco { get; private set; }
    
    public Produto(string nome, decimal preco)
    {
        Id = Guid.NewGuid();
        Nome = nome;
        Preco = preco;
    }
}
```

### Classes de Conveniência

```csharp
// Para IDs Guid
public class MinhaEntidade : AuditableGuidEntity { }

// Para IDs int
public class MinhaEntidade : AuditableIntEntity { }

// Para IDs long
public class MinhaEntidade : AuditableLongEntity { }
```

### SoftDeletableEntity&lt;TId&gt;

Combina entidade, auditoria e soft delete.

```csharp
public class Artigo : SoftDeletableEntity<Guid>
{
    public string Titulo { get; private set; }
    public string Conteudo { get; private set; }
}
```

---

## Combinando Interfaces

Você pode combinar múltiplas interfaces:

```csharp
public class DocumentoTenant : EntityBase<Guid>, 
    IAuditableEntity, 
    ISoftDeletable, 
    ITenantEntity,
    IVersionedEntity
{
    public string Titulo { get; private set; }
    
    // IAuditableEntity
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = string.Empty;
    
    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    
    // ITenantEntity
    public string TenantId { get; set; } = string.Empty;
    
    // IVersionedEntity
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
```

---

## Padrão Interceptor EF Core

Use interceptors para preencher automaticamente todos os campos das interfaces:

```csharp
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserProvider _userProvider;
    private readonly IClock _clock;
    
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null) return new ValueTask<InterceptionResult<int>>(result);
        
        var agora = _clock.UtcNow;
        var usuarioId = _userProvider.UserId;
        
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is IAuditableEntity auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedAt = agora;
                    auditable.CreatedBy = usuarioId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditable.ModifiedAt = agora;
                    auditable.ModifiedBy = usuarioId;
                }
            }
            
            if (entry.Entity is ISoftDeletable softDeletable && 
                entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                softDeletable.IsDeleted = true;
                softDeletable.DeletedAt = agora;
                softDeletable.DeletedBy = usuarioId;
            }
        }
        
        return new ValueTask<InterceptionResult<int>>(result);
    }
}
```

## Boas Práticas

1. **Use interfaces apropriadas** - Não adicione campos de auditoria se não precisar deles
2. **Prefira classes base** - Use `AuditableEntity<TId>` ao invés de implementar tudo manualmente
3. **Configure EF Core corretamente** - Configure query filters e interceptors
4. **Mantenha entidades focadas** - Entidade deve representar conceitos de domínio, não preocupações de infraestrutura

