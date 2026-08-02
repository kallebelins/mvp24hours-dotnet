using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Entities;
using Mvp24Hours.Infrastructure.Data.EFCore;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

public class TestDbContext : Mvp24HoursContext
{
    public TestDbContext()
    {
    }

    public TestDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public override bool CanApplyEntityLog => true;

    public DbSet<TestEntity> Entities => Set<TestEntity>();
    public DbSet<TestLogEntity> LogEntities => Set<TestLogEntity>();
    public DbSet<TestAuditableEntity> AuditableEntities => Set<TestAuditableEntity>();
    public DbSet<TestSoftDeleteEntity> SoftDeleteEntities => Set<TestSoftDeleteEntity>();
    public DbSet<TestTenantEntity> TenantEntities => Set<TestTenantEntity>();
    public DbSet<TestVersionedEntity> VersionedEntities => Set<TestVersionedEntity>();
    public DbSet<TestDomainEventEntity> DomainEventEntities => Set<TestDomainEventEntity>();
}

public class TestEntity : EntityBase<int>
{
    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public int Score { get; set; }
}

public class TestLogEntity : EntityBase<int>, IEntityDateLog
{
    public string Name { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public DateTime? Modified { get; set; }
    public DateTime? Removed { get; set; }
}

public class TestAuditableEntity : EntityBase<int>, IAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}

public class TestSoftDeleteEntity : EntityBase<int>, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string DeletedBy { get; set; } = string.Empty;
}

public class TestTenantEntity : EntityBase<int>, ITenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
}

public class TestVersionedEntity : EntityBase<int>, IVersionedEntityWithCounter
{
    public string Name { get; set; } = string.Empty;
    public long Version { get; set; }
}

public class TestDomainEventEntity : EntityBase<int>, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public string Name { get; set; } = string.Empty;

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public void Raise(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}

public sealed class TestDomainEvent(string message) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string Message { get; } = message;
}

public class TestEntityLog : EntityBaseLog<int, string>
{
    public string Name { get; set; } = string.Empty;
}

public class TestDbContextNoLog(DbContextOptions options) : Mvp24HoursContext(options)
{
    public override bool CanApplyEntityLog => false;

    public DbSet<TestLogEntity> LogEntities => Set<TestLogEntity>();
    public DbSet<TestEntity> Entities => Set<TestEntity>();
}

public class TestDbContextWithUser(DbContextOptions options, object? entityLogBy = null) : Mvp24HoursContext(options)
{
    public override bool CanApplyEntityLog => true;

    public override object? EntityLogBy { get; } = entityLogBy;

    public DbSet<TestEntityLog> EntityLogs => Set<TestEntityLog>();
    public DbSet<TestLogEntity> LogEntities => Set<TestLogEntity>();
}
