# Padrões de Banco de Dados para Agentes de IA

> **Instrução para Agente de IA**: Use estes padrões ao implementar a camada de acesso a dados. Cada padrão inclui configuração completa e exemplos de uso.

---

## Entity Framework Core (Bancos de Dados Relacionais)

### Configuração SQL Server

```csharp
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ProjectDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
  }
}

// ServiceBuilderExtensions.cs
services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

// Registro Mvp24Hours
services.AddMvp24HoursDbContext<DataContext>();
services.AddMvp24HoursRepository(options => options.MaxQtyByQueryPage = 100);

// Health Check
services.AddHealthChecks()
    .AddSqlServer(
        configuration.GetConnectionString("DefaultConnection"),
        healthQuery: "SELECT 1;",
        name: "SqlServer",
        failureStatus: HealthStatus.Degraded);
```

### Configuração PostgreSQL

```csharp
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ProjectDb;Username=postgres;Password=YourPassword;"
  }
}

// ServiceBuilderExtensions.cs
services.AddDbContext<DataContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.SetPostgresVersion(new Version(15, 0))));

// Health Check
services.AddHealthChecks()
    .AddNpgSql(
        configuration.GetConnectionString("DefaultConnection"),
        name: "PostgreSQL",
        failureStatus: HealthStatus.Degraded);
```

### Configuração MySQL

```csharp
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=ProjectDb;User=root;Password=YourPassword;"
  }
}

// ServiceBuilderExtensions.cs
services.AddDbContext<DataContext>(options =>
    options.UseMySQL(configuration.GetConnectionString("DefaultConnection")));

// Health Check
services.AddHealthChecks()
    .AddMySql(
        configuration.GetConnectionString("DefaultConnection"),
        name: "MySQL",
        failureStatus: HealthStatus.Degraded);
```

### Implementação DbContext

```csharp
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Infrastructure.Data.EFCore;

namespace NomeProjeto.Infrastructure.Data
{
    public class DataContext : Mvp24HoursContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Contato> Contatos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
        }
    }
}
```

### Configuração de Entidade (FluentAPI)

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NomeProjeto.Infrastructure.Data.Configurations
{
    public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
    {
        public void Configure(EntityTypeBuilder<Cliente> builder)
        {
            builder.ToTable("Clientes");
            
            builder.HasKey(x => x.Id);
            
            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();
            
            builder.Property(x => x.Nome)
                .HasMaxLength(100)
                .IsRequired();
            
            builder.Property(x => x.Email)
                .HasMaxLength(255)
                .IsRequired();
            
            // Relacionamento
            builder.HasMany(x => x.Contatos)
                .WithOne(x => x.Cliente)
                .HasForeignKey(x => x.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Índice
            builder.HasIndex(x => x.Email)
                .IsUnique();
        }
    }
}
```

### Uso do Repository

```csharp
// Obter repositório do UnitOfWork
var repository = _unitOfWork.GetRepository<Cliente>();

// Obter todos com paginação
var resultado = await repository.ToBusinessPagingAsync(pagina: 1, limite: 10);

// Obter por ID
var cliente = await repository.GetByIdAsync(id);

// Obter com propriedades de navegação
var cliente = await repository.GetByIdAsync(id, x => x.Contatos);

// Obter com specification
var spec = new ClientePorNomeSpec(nome);
var clientes = await repository.ListAnyAsync(spec.IsSatisfiedByExpression);

// Adicionar
await repository.AddAsync(cliente);
await _unitOfWork.SaveChangesAsync();

// Atualizar
await repository.ModifyAsync(cliente);
await _unitOfWork.SaveChangesAsync();

// Excluir
await repository.RemoveAsync(cliente);
await _unitOfWork.SaveChangesAsync();
```

---

## MongoDB (NoSQL)

### Configuração

```csharp
// appsettings.json
{
  "MongoDbOptions": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "ProjectDb"
  }
}

// ServiceBuilderExtensions.cs
services.AddMvp24HoursDbContextMongoDb(options =>
{
    options.DatabaseName = configuration["MongoDbOptions:DatabaseName"];
    options.ConnectionString = configuration["MongoDbOptions:ConnectionString"];
});

services.AddMvp24HoursRepositoryMongoDb();

// Health Check
services.AddHealthChecks()
    .AddMongoDb(
        configuration["MongoDbOptions:ConnectionString"],
        name: "MongoDB",
        failureStatus: HealthStatus.Degraded);
```

### Definição de Entidade

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Mvp24Hours.Core.Entities;

namespace NomeProjeto.Core.Entities
{
    public class Cliente : IEntityBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("nome")]
        public string Nome { get; set; }

        [BsonElement("email")]
        public string Email { get; set; }

        [BsonElement("contatos")]
        public List<Contato> Contatos { get; set; } = new();

        [BsonElement("ativo")]
        public bool Ativo { get; set; } = true;

        [BsonElement("criado")]
        public DateTime Criado { get; set; } = DateTime.UtcNow;
    }
}
```

---

## Redis (Chave-Valor / Cache)

### Configuração

```csharp
// appsettings.json
{
  "Redis": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "NomeProjeto:"
  }
}

// ServiceBuilderExtensions.cs
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration["Redis:ConnectionString"];
    options.InstanceName = configuration["Redis:InstanceName"];
});

// Health Check
services.AddHealthChecks()
    .AddRedis(
        configuration["Redis:ConnectionString"],
        name: "Redis",
        failureStatus: HealthStatus.Degraded);
```

### Padrão Repository para Redis

```csharp
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace NomeProjeto.Infrastructure.Repositories
{
    public interface IRedisRepository<T> where T : class
    {
        Task<T> ObterAsync(string chave);
        Task SalvarAsync(string chave, T valor, TimeSpan? expiracao = null);
        Task RemoverAsync(string chave);
    }

    public class RedisRepository<T> : IRedisRepository<T> where T : class
    {
        private readonly IDistributedCache _cache;
        private readonly string _prefixo;

        public RedisRepository(IDistributedCache cache)
        {
            _cache = cache;
            _prefixo = typeof(T).Name + ":";
        }

        public async Task<T> ObterAsync(string chave)
        {
            var dados = await _cache.GetStringAsync(_prefixo + chave);
            return dados == null ? null : JsonSerializer.Deserialize<T>(dados);
        }

        public async Task SalvarAsync(string chave, T valor, TimeSpan? expiracao = null)
        {
            var options = new DistributedCacheEntryOptions();
            if (expiracao.HasValue)
                options.AbsoluteExpirationRelativeToNow = expiracao;

            var dados = JsonSerializer.Serialize(valor);
            await _cache.SetStringAsync(_prefixo + chave, dados, options);
        }

        public async Task RemoverAsync(string chave)
        {
            await _cache.RemoveAsync(_prefixo + chave);
        }
    }
}
```

---

## Dapper (Queries Otimizadas)

### Configuração (Híbrido com EF)

```csharp
// Use o mesmo DbContext do EF
// Queries Dapper usam a conexão subjacente

using Dapper;
using Microsoft.EntityFrameworkCore;

namespace NomeProjeto.Infrastructure.Repositories
{
    public class ClienteDapperRepository
    {
        private readonly DataContext _context;

        public ClienteDapperRepository(DataContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ClienteDto>> ObterTodosOtimizadoAsync(int pagina, int limite)
        {
            var connection = _context.Database.GetDbConnection();
            var offset = (pagina - 1) * limite;

            return await connection.QueryAsync<ClienteDto>(@"
                SELECT c.Id, c.Nome, c.Email,
                       (SELECT COUNT(*) FROM Contatos WHERE ClienteId = c.Id) as QtdContatos
                FROM Clientes c
                WHERE c.Ativo = 1
                ORDER BY c.Nome
                OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY",
                new { Offset = offset, Limit = limite });
        }

        public async Task<ClienteDetalheDto> ObterPorIdComContatosAsync(Guid id)
        {
            var connection = _context.Database.GetDbConnection();

            var query = @"
                SELECT c.Id, c.Nome, c.Email, c.Ativo
                FROM Clientes c
                WHERE c.Id = @Id;

                SELECT ct.Id, ct.Tipo, ct.Valor, ct.ClienteId
                FROM Contatos ct
                WHERE ct.ClienteId = @Id;";

            using var multi = await connection.QueryMultipleAsync(query, new { Id = id });

            var cliente = await multi.ReadFirstOrDefaultAsync<ClienteDetalheDto>();
            if (cliente != null)
            {
                cliente.Contatos = (await multi.ReadAsync<ContatoDto>()).ToList();
            }

            return cliente;
        }
    }
}
```

---

## Padrões de Entidade

### Entidade Padrão

```csharp
using Mvp24Hours.Core.Entities;
using System.ComponentModel.DataAnnotations;

public class Cliente : EntityBase<Guid>
{
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; }

    [Required]
    [MaxLength(255)]
    public string Email { get; set; }

    public bool Ativo { get; set; } = true;

    // Navegação
    public virtual ICollection<Contato> Contatos { get; set; } = new List<Contato>();
}
```

### Entidade com Auditoria (EntityLog)

```csharp
using Mvp24Hours.Core.Entities;
using Mvp24Hours.Core.Contract.Domain;

public class Cliente : EntityBase<Guid>, IEntityLog
{
    public string Nome { get; set; }
    public string Email { get; set; }
    public bool Ativo { get; set; } = true;

    // Campos de auditoria (IEntityLog)
    public DateTime Created { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? Modified { get; set; }
    public string ModifiedBy { get; set; }
    public DateTime? Removed { get; set; }
    public string RemovedBy { get; set; }
}
```

---

## Padrão Specification

```csharp
using Mvp24Hours.Core.Contract.Domain;
using System.Linq.Expressions;

namespace NomeProjeto.Core.Specifications
{
    public class ClientePorFiltroSpec : ISpecificationQuery<Cliente>
    {
        private readonly ClienteFiltroDto _filtro;

        public ClientePorFiltroSpec(ClienteFiltroDto filtro)
        {
            _filtro = filtro;
        }

        public Expression<Func<Cliente, bool>> IsSatisfiedByExpression
        {
            get
            {
                return cliente =>
                    (string.IsNullOrEmpty(_filtro.Nome) || cliente.Nome.Contains(_filtro.Nome)) &&
                    (string.IsNullOrEmpty(_filtro.Email) || cliente.Email.Contains(_filtro.Email)) &&
                    (!_filtro.Ativo.HasValue || cliente.Ativo == _filtro.Ativo.Value);
            }
        }
    }
}
```

---

## Documentação Relacionada

- [Templates de Arquitetura](architecture-templates.md)
- [Matriz de Decisão](decision-matrix.md)
- [Estrutura de Projetos](project-structure.md)

