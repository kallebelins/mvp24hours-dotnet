# NoSQL Databases

Mvp24Hours provides a MongoDB repository/unit-of-work implementation and Redis caching integration. MongoDB is a document database; Redis is exposed through .NET caching abstractions and is not a MongoDB repository provider.

## MongoDB

Install:

```powershell
dotnet add package Mvp24Hours.Infrastructure.Data.MongoDb
```

Configure a connection string:

```json
{
  "ConnectionStrings": {
    "MongoDb": "mongodb://localhost:27017"
  }
}
```

Register the context and either synchronous or asynchronous repositories:

```csharp
string connectionString = builder.Configuration.GetConnectionString("MongoDb")
    ?? throw new InvalidOperationException("ConnectionStrings:MongoDb is required.");

builder.Services.AddMvp24HoursDbContext(options =>
{
    options.DatabaseName = "customers";
    options.ConnectionString = connectionString;
    options.RetryReads = true;
    options.RetryWrites = true;
});

builder.Services.AddMvp24HoursRepositoryAsync(options =>
    options.MaxQtyByQueryPage = 100);
```

### MongoDbOptions

| Name | Type | Default | Description |
|---|---|---|---|
| DatabaseName | string | `""` | Database name. |
| ConnectionString | string | `""` | MongoDB connection string. |
| EnableTls | bool | `false` | Enables TLS. |
| EnableTransaction | bool | `false` | Enables transaction behavior. |
| Authentication | MongoDbAuthenticationOptions? | `null` | Explicit authentication settings. |
| EnableMultiTenancy | bool | `false` | Enables tenant behavior. |
| TenantValidateOnUpdate | bool | `true` | Validates tenant ownership on updates. |
| TenantValidateOnDelete | bool | `true` | Validates tenant ownership on deletes. |
| TenantThrowOnMissing | bool | `true` | Throws without tenant context. |
| EncryptionKey | string? | `null` | Base64 256-bit encryption key. |
| ReadPreference | string? | `null` | Read preference override. |
| WriteConcern | string? | `null` | Write concern override. |
| ReadConcern | string? | `null` | Read concern override. |
| ConnectionTimeoutSeconds | int? | `null` | Connection timeout override. |
| SocketTimeoutSeconds | int? | `null` | Socket timeout override. |
| MaxConnectionPoolSize | int? | `null` | Maximum pool size override. |
| MinConnectionPoolSize | int? | `null` | Minimum pool size override. |
| EnableCommandLogging | bool | `false` | Enables command logging. |
| RetryReads | bool | `true` | Enables retryable reads. |
| RetryWrites | bool | `true` | Enables retryable writes. |

MongoDB repositories support search criteria, pagination, synchronous/asynchronous commands, bulk operations, interceptors, and event-aware unit-of-work variants. They do not support EF Core navigation loading. Transactions require a replica set or sharded cluster.

See [MongoDB Advanced](mongodb-advanced.md) for resiliency, authentication, concerns, text search, time series, capped collections, collation, sharding, geospatial queries, schema validation, observability, and testing.

## Local MongoDB

Without authentication:

```bash
docker run --rm --name mongo -p 27017:27017 mongo:8
```

With a root user:

```bash
docker run --rm --name mongo -p 27017:27017 \
  -e MONGO_INITDB_ROOT_USERNAME=user \
  -e MONGO_INITDB_ROOT_PASSWORD=change-me \
  mongo:8
```

Use `mongodb://user:change-me@localhost:27017/?authSource=admin` only for local development and keep credentials outside source control.

## Redis caching

Install:

```powershell
dotnet add package Mvp24Hours.Infrastructure.Caching.Redis
```

```json
{
  "ConnectionStrings": {
    "RedisDbContext": "localhost:6379"
  }
}
```

```csharp
string redis = builder.Configuration.GetConnectionString("RedisDbContext")
    ?? throw new InvalidOperationException("ConnectionStrings:RedisDbContext is required.");

builder.Services.AddMvp24HoursCachingRedis(redis);
```

Consume the standard `IDistributedCache` abstraction:

```csharp
public sealed class CustomerCache(IDistributedCache cache)
{
    public Task SetAsync(Guid id, string json, CancellationToken cancellationToken) =>
        cache.SetStringAsync($"customer:{id}", json, cancellationToken);
}
```

For L1/L2 caching on .NET 10, see [HybridCache](../modernization/hybrid-cache.md). For repository usage, see [Repository](use-repository.md) and [Unit of Work](use-unitofwork.md).
