# Database Patterns for AI Agents

> **AI Agent Instruction**: Use these patterns when implementing database access layer. Each pattern includes complete configuration and usage examples.

---

## Entity Framework Core (Relational Databases)

### SQL Server Configuration

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

// Mvp24Hours Registration
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

### PostgreSQL Configuration

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

// Mvp24Hours Registration
services.AddMvp24HoursDbContext<DataContext>();
services.AddMvp24HoursRepository(options => options.MaxQtyByQueryPage = 100);

// Health Check
services.AddHealthChecks()
    .AddNpgSql(
        configuration.GetConnectionString("DefaultConnection"),
        name: "PostgreSQL",
        failureStatus: HealthStatus.Degraded);
```

### MySQL Configuration

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

// Mvp24Hours Registration
services.AddMvp24HoursDbContext<DataContext>();
services.AddMvp24HoursRepository(options => options.MaxQtyByQueryPage = 100);

// Health Check
services.AddHealthChecks()
    .AddMySql(
        configuration.GetConnectionString("DefaultConnection"),
        name: "MySQL",
        failureStatus: HealthStatus.Degraded);
```

### DbContext Implementation

```csharp
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Infrastructure.Data.EFCore;

namespace ProjectName.Infrastructure.Data
{
    public class DataContext : Mvp24HoursContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Contact> Contacts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
        }
    }
}
```

### Entity Configuration (FluentAPI)

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProjectName.Infrastructure.Data.Configurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("Customers");
            
            builder.HasKey(x => x.Id);
            
            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();
            
            builder.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();
            
            builder.Property(x => x.Email)
                .HasMaxLength(255)
                .IsRequired();
            
            // Relationship
            builder.HasMany(x => x.Contacts)
                .WithOne(x => x.Customer)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Index
            builder.HasIndex(x => x.Email)
                .IsUnique();
        }
    }
}
```

### Repository Usage

```csharp
// Get repository from UnitOfWork
var repository = _unitOfWork.GetRepository<Customer>();

// Get all with pagination
var result = await repository.ToBusinessPagingAsync(page: 1, limit: 10);

// Get by ID
var customer = await repository.GetByIdAsync(id);

// Get with navigation properties
var customer = await repository.GetByIdAsync(id, x => x.Contacts);

// Get with specification
var spec = new CustomerByNameSpec(name);
var customers = await repository.ListAnyAsync(spec.IsSatisfiedByExpression);

// Add
await repository.AddAsync(customer);
await _unitOfWork.SaveChangesAsync();

// Update
await repository.ModifyAsync(customer);
await _unitOfWork.SaveChangesAsync();

// Delete
await repository.RemoveAsync(customer);
await _unitOfWork.SaveChangesAsync();
```

---

## MongoDB (NoSQL)

### Configuration

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

### Entity Definition

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Mvp24Hours.Core.Entities;

namespace ProjectName.Core.Entities
{
    public class Customer : IEntityBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("email")]
        public string Email { get; set; }

        [BsonElement("contacts")]
        public List<Contact> Contacts { get; set; } = new();

        [BsonElement("active")]
        public bool Active { get; set; } = true;

        [BsonElement("created")]
        public DateTime Created { get; set; } = DateTime.UtcNow;
    }

    public class Contact
    {
        [BsonElement("type")]
        public ContactType Type { get; set; }

        [BsonElement("value")]
        public string Value { get; set; }
    }
}
```

### Repository Usage (MongoDB)

```csharp
// Get repository
var repository = _unitOfWork.GetRepository<Customer>();

// Get all with pagination
var result = await repository.ToBusinessPagingAsync(page: 1, limit: 10);

// Get by ID
var customer = await repository.GetByIdAsync(id);

// Filter with expression
var customers = await repository.ListAsync(x => x.Active && x.Name.Contains(name));

// Add
await repository.AddAsync(customer);

// Update
await repository.ModifyAsync(customer);

// Delete
await repository.RemoveAsync(customer);
```

---

## Redis (Key-Value / Cache)

### Configuration

```csharp
// appsettings.json
{
  "Redis": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "ProjectName:"
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

### Repository Pattern for Redis

```csharp
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace ProjectName.Infrastructure.Repositories
{
    public interface IRedisRepository<T> where T : class
    {
        Task<T> GetAsync(string key);
        Task SetAsync(string key, T value, TimeSpan? expiration = null);
        Task RemoveAsync(string key);
    }

    public class RedisRepository<T> : IRedisRepository<T> where T : class
    {
        private readonly IDistributedCache _cache;
        private readonly string _prefix;

        public RedisRepository(IDistributedCache cache)
        {
            _cache = cache;
            _prefix = typeof(T).Name + ":";
        }

        public async Task<T> GetAsync(string key)
        {
            var data = await _cache.GetStringAsync(_prefix + key);
            return data == null ? null : JsonSerializer.Deserialize<T>(data);
        }

        public async Task SetAsync(string key, T value, TimeSpan? expiration = null)
        {
            var options = new DistributedCacheEntryOptions();
            if (expiration.HasValue)
                options.AbsoluteExpirationRelativeToNow = expiration;

            var data = JsonSerializer.Serialize(value);
            await _cache.SetStringAsync(_prefix + key, data, options);
        }

        public async Task RemoveAsync(string key)
        {
            await _cache.RemoveAsync(_prefix + key);
        }
    }
}
```

---

## Dapper (Optimized Queries)

### Configuration (Hybrid with EF)

```csharp
// Use the same DbContext from EF
// Dapper queries use the underlying connection

using Dapper;
using Microsoft.EntityFrameworkCore;

namespace ProjectName.Infrastructure.Repositories
{
    public class CustomerDapperRepository
    {
        private readonly DataContext _context;

        public CustomerDapperRepository(DataContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CustomerDto>> GetAllOptimizedAsync(int page, int limit)
        {
            var connection = _context.Database.GetDbConnection();
            var offset = (page - 1) * limit;

            return await connection.QueryAsync<CustomerDto>(@"
                SELECT c.Id, c.Name, c.Email,
                       (SELECT COUNT(*) FROM Contacts WHERE CustomerId = c.Id) as ContactCount
                FROM Customers c
                WHERE c.Active = 1
                ORDER BY c.Name
                OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY",
                new { Offset = offset, Limit = limit });
        }

        public async Task<CustomerDetailDto> GetByIdWithContactsAsync(Guid id)
        {
            var connection = _context.Database.GetDbConnection();

            var query = @"
                SELECT c.Id, c.Name, c.Email, c.Active
                FROM Customers c
                WHERE c.Id = @Id;

                SELECT ct.Id, ct.Type, ct.Value, ct.CustomerId
                FROM Contacts ct
                WHERE ct.CustomerId = @Id;";

            using var multi = await connection.QueryMultipleAsync(query, new { Id = id });

            var customer = await multi.ReadFirstOrDefaultAsync<CustomerDetailDto>();
            if (customer != null)
            {
                customer.Contacts = (await multi.ReadAsync<ContactDto>()).ToList();
            }

            return customer;
        }
    }
}
```

---

## Entity Patterns

### Standard Entity

```csharp
using Mvp24Hours.Core.Entities;
using System.ComponentModel.DataAnnotations;

public class Customer : EntityBase<Guid>
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [Required]
    [MaxLength(255)]
    public string Email { get; set; }

    public bool Active { get; set; } = true;

    // Navigation
    public virtual ICollection<Contact> Contacts { get; set; } = new List<Contact>();
}
```

### Entity with Audit (EntityLog)

```csharp
using Mvp24Hours.Core.Entities;
using Mvp24Hours.Core.Contract.Domain;

public class Customer : EntityBase<Guid>, IEntityLog
{
    public string Name { get; set; }
    public string Email { get; set; }
    public bool Active { get; set; } = true;

    // Audit fields (IEntityLog)
    public DateTime Created { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? Modified { get; set; }
    public string ModifiedBy { get; set; }
    public DateTime? Removed { get; set; }
    public string RemovedBy { get; set; }
}
```

### Entity with Soft Delete

```csharp
using Mvp24Hours.Core.Entities;
using Mvp24Hours.Core.Contract.Domain;

public class Customer : EntityBase<Guid>, IEntityDateLog
{
    public string Name { get; set; }
    
    // Soft delete fields (IEntityDateLog)
    public DateTime Created { get; set; }
    public DateTime? Modified { get; set; }
    public DateTime? Removed { get; set; }
}
```

---

## Specification Pattern

```csharp
using Mvp24Hours.Core.Contract.Domain;
using System.Linq.Expressions;

namespace ProjectName.Core.Specifications
{
    public class CustomerByFilterSpec : ISpecificationQuery<Customer>
    {
        private readonly CustomerFilterDto _filter;

        public CustomerByFilterSpec(CustomerFilterDto filter)
        {
            _filter = filter;
        }

        public Expression<Func<Customer, bool>> IsSatisfiedByExpression
        {
            get
            {
                return customer =>
                    (string.IsNullOrEmpty(_filter.Name) || customer.Name.Contains(_filter.Name)) &&
                    (string.IsNullOrEmpty(_filter.Email) || customer.Email.Contains(_filter.Email)) &&
                    (!_filter.Active.HasValue || customer.Active == _filter.Active.Value);
            }
        }
    }

    public class CustomerActiveSpec : ISpecificationQuery<Customer>
    {
        public Expression<Func<Customer, bool>> IsSatisfiedByExpression =>
            customer => customer.Active;
    }
}
```

---

## Migration Strategy

### Entity Framework Migrations

```bash
# Add migration
dotnet ef migrations add InitialCreate -p ProjectName.Infrastructure -s ProjectName.WebAPI

# Update database
dotnet ef database update -p ProjectName.Infrastructure -s ProjectName.WebAPI

# Generate SQL script
dotnet ef migrations script -p ProjectName.Infrastructure -s ProjectName.WebAPI -o migration.sql
```

### Seed Data

```csharp
// In DataContext or separate seeder
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<Customer>().HasData(
        new Customer { Id = Guid.NewGuid(), Name = "Customer 1", Email = "customer1@example.com" },
        new Customer { Id = Guid.NewGuid(), Name = "Customer 2", Email = "customer2@example.com" }
    );
}
```

---

## Related Documentation

- [Architecture Templates](architecture-templates.md)
- [Decision Matrix](decision-matrix.md)
- [Project Structure](project-structure.md)

