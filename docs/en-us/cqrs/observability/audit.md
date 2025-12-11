# Audit Trail

## Overview

Audit Trail records all write operations (commands) for auditing, compliance, and debugging purposes.

## IAuditStore Interface

```csharp
public interface IAuditStore
{
    Task SaveAsync(AuditEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditEntry>> GetByEntityAsync(
        string entityType, 
        string entityId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditEntry>> GetByUserAsync(
        string userId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}
```

## AuditEntry

```csharp
public class AuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    // Who
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
    
    // What
    public string Operation { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    
    // Data
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? Changes { get; set; }
    
    // Context
    public string? CorrelationId { get; set; }
    public string? TenantId { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
```

## AuditBehavior

```csharp
public sealed class AuditBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorCommand<TResponse>
{
    private readonly IAuditStore _auditStore;
    private readonly IRequestContext _context;
    private readonly ILogger<AuditBehavior<TRequest, TResponse>> _logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var entry = new AuditEntry
        {
            UserId = _context.UserId,
            CorrelationId = _context.CorrelationId,
            TenantId = _context.TenantId,
            Operation = typeof(TRequest).Name,
            NewValues = JsonSerializer.Serialize(request)
        };

        try
        {
            var result = await next();
            
            // Capture EntityId from result if available
            if (result is IAuditableResult auditable)
            {
                entry.EntityType = auditable.EntityType;
                entry.EntityId = auditable.EntityId;
            }

            entry.Metadata["Success"] = true;
            await _auditStore.SaveAsync(entry, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            entry.Metadata["Success"] = false;
            entry.Metadata["Error"] = ex.Message;
            await _auditStore.SaveAsync(entry, cancellationToken);
            throw;
        }
    }
}
```

## IAuditableResult Interface

```csharp
public interface IAuditableResult
{
    string EntityType { get; }
    string EntityId { get; }
}

// Usage
public class OrderDto : IAuditableResult
{
    public Guid Id { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    
    public string EntityType => "Order";
    public string EntityId => Id.ToString();
}
```

## Auditable Command

```csharp
public interface IAuditableCommand
{
    string GetAuditDescription();
}

public record UpdateOrderStatusCommand 
    : IMediatorCommand<OrderDto>, IAuditableCommand
{
    public required Guid OrderId { get; init; }
    public required OrderStatus NewStatus { get; init; }
    public string? Reason { get; init; }
    
    public string GetAuditDescription() => 
        $"Changed order {OrderId} status to {NewStatus}. Reason: {Reason ?? "N/A"}";
}
```

## EF Core Implementation

### AuditEntry Entity

```csharp
public class AuditEntryEntity
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? CorrelationId { get; set; }
    public string? MetadataJson { get; set; }
}

// DbContext
public DbSet<AuditEntryEntity> AuditEntries { get; set; }
```

### EfCoreAuditStore

```csharp
public class EfCoreAuditStore : IAuditStore
{
    private readonly AuditDbContext _context;

    public async Task SaveAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        var entity = new AuditEntryEntity
        {
            Id = entry.Id,
            Timestamp = entry.Timestamp,
            UserId = entry.UserId,
            Operation = entry.Operation,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            OldValues = entry.OldValues,
            NewValues = entry.NewValues,
            CorrelationId = entry.CorrelationId,
            MetadataJson = JsonSerializer.Serialize(entry.Metadata)
        };

        await _context.AuditEntries.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEntry>> GetByEntityAsync(
        string entityType, 
        string entityId,
        CancellationToken cancellationToken)
    {
        return await _context.AuditEntries
            .Where(e => e.EntityType == entityType && e.EntityId == entityId)
            .OrderByDescending(e => e.Timestamp)
            .Select(e => MapToAuditEntry(e))
            .ToListAsync(cancellationToken);
    }
}
```

## Change Auditing

### Change Tracking

```csharp
public class ChangeTrackingAuditBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorCommand<TResponse>
{
    private readonly DbContext _context;
    private readonly IAuditStore _auditStore;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Snapshot before
        var beforeState = CaptureState();

        var result = await next();

        // Snapshot after and compare
        var changes = CompareStates(beforeState, CaptureState());
        
        if (changes.Any())
        {
            foreach (var change in changes)
            {
                await _auditStore.SaveAsync(new AuditEntry
                {
                    Operation = typeof(TRequest).Name,
                    EntityType = change.EntityType,
                    EntityId = change.EntityId,
                    OldValues = change.OldValues,
                    NewValues = change.NewValues,
                    Changes = JsonSerializer.Serialize(change.Properties)
                }, cancellationToken);
            }
        }

        return result;
    }
}
```

## Audit Queries

### Query API

```csharp
[ApiController]
[Route("api/audit")]
[Authorize(Policy = "AuditReader")]
public class AuditController : ControllerBase
{
    private readonly IAuditStore _auditStore;

    [HttpGet("entity/{entityType}/{entityId}")]
    public async Task<ActionResult<IReadOnlyList<AuditEntry>>> GetByEntity(
        string entityType, 
        string entityId)
    {
        var entries = await _auditStore.GetByEntityAsync(entityType, entityId);
        return Ok(entries);
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IReadOnlyList<AuditEntry>>> GetByUser(
        string userId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        var entries = await _auditStore.GetByUserAsync(userId, from, to);
        return Ok(entries);
    }
}
```

## Best Practices

1. **Separate Database**: Use separate database for auditing
2. **Retention**: Configure retention policy
3. **Immutability**: Audit logs are append-only
4. **Compression**: Compress old data
5. **Indexes**: Optimize common queries
6. **GDPR**: Consider data anonymization
7. **Performance**: Use async and batch writes

