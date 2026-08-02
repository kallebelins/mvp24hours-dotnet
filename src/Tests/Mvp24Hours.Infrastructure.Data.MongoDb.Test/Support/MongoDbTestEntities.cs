using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.Geospatial;
using Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Attributes;
using Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Indexes;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

public class TestArticle : IEntityBase
{
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string[] Tags { get; set; } = [];

    public object EntityKey => Id;

    public IReadOnlyCollection<MessageResult> GetNotifications()
    {
        return [];
    }

    public bool HasNotifications()
    {
        return false;
    }
}

public class TestPlace : IEntityBase
{
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    public string Name { get; set; } = string.Empty;

    public GeoPoint Location { get; set; } = null!;

    public object EntityKey => Id;

    public IReadOnlyCollection<MessageResult> GetNotifications()
    {
        return [];
    }

    public bool HasNotifications()
    {
        return false;
    }
}

public class TestLogEntry : IEntityBase
{
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    public string Message { get; set; } = string.Empty;

    public object EntityKey => Id;

    public IReadOnlyCollection<MessageResult> GetNotifications()
    {
        return [];
    }

    public bool HasNotifications()
    {
        return false;
    }
}

[BsonCollection("indexed_customers")]
[MongoCompoundIndex(Fields = "Email:asc,Active:desc", Name = "idx_email_active")]
public class IndexedCustomer : IEntityBase
{
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    [MongoIndex(Unique = true)]
    public string Email { get; set; } = string.Empty;

    [MongoIndex]
    public bool Active { get; set; }

    [MongoTtlIndex(ExpireAfterSeconds = 3600)]
    public DateTime ExpiresAt { get; set; }

    [MongoIndex(CompoundIndexGroup = "tenant_status", Order = 0)]
    public string TenantCode { get; set; } = string.Empty;

    [MongoIndex(CompoundIndexGroup = "tenant_status", Order = 1, IndexType = MongoIndexType.Descending)]
    public int Status { get; set; }

    public object EntityKey => Id;

    public IReadOnlyCollection<MessageResult> GetNotifications()
    {
        return [];
    }

    public bool HasNotifications()
    {
        return false;
    }
}

public class ValidatedUser
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Range(0, 150)]
    public int Age { get; set; }
}

public class TenantInvoice : IEntityBase, ITenantEntity
{
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    public string TenantId { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public object EntityKey => Id;

    public IReadOnlyCollection<MessageResult> GetNotifications()
    {
        return [];
    }

    public bool HasNotifications()
    {
        return false;
    }
}

public class TenantOrder : IEntityBase, ITenantEntity<Guid>
{
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    public Guid TenantId { get; set; }

    public string Code { get; set; } = string.Empty;

    public object EntityKey => Id;

    public IReadOnlyCollection<MessageResult> GetNotifications()
    {
        return [];
    }

    public bool HasNotifications()
    {
        return false;
    }
}

public class TestEntity : IEntityBase
{
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    public string Name { get; set; } = string.Empty;

    public object EntityKey => Id;

    public IReadOnlyCollection<MessageResult> GetNotifications()
    {
        return [];
    }

    public bool HasNotifications()
    {
        return false;
    }
}

public class CustomerDto
{
    public string Email { get; set; } = string.Empty;

    public bool Active { get; set; }
}

public class OrderDocument : IEntityBase
{
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    [MongoIndex]
    public string Status { get; set; } = string.Empty;

    public object EntityKey => Id;

    public IReadOnlyCollection<MessageResult> GetNotifications()
    {
        return [];
    }

    public bool HasNotifications()
    {
        return false;
    }
}

public class FakeTenantProvider(string? tenantId) : ITenantProvider
{
    public string TenantId { get; set; } = tenantId ?? string.Empty;

    public bool HasTenant => !string.IsNullOrEmpty(TenantId);

    public string ConnectionString { get; set; } = string.Empty;

    public string Schema { get; set; } = string.Empty;
}
