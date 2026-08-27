# MongoDB Advanced

Install `Mvp24Hours.Infrastructure.Data.MongoDb`. The package references the MongoDB driver; applications normally install only the Mvp24Hours package.

## Context and repository

```csharp
builder.Services.AddMvp24HoursDbContext(options =>
{
    options.DatabaseName = "customers";
    options.ConnectionString = builder.Configuration.GetConnectionString("MongoDb")
        ?? throw new InvalidOperationException("ConnectionStrings:MongoDb is required.");
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
| EnableTransaction | bool | `false` | Enables transaction behavior; requires a replica set or sharded cluster. |
| Authentication | MongoDbAuthenticationOptions? | `null` | Explicit authentication and TLS settings. |
| EnableMultiTenancy | bool | `false` | Enables tenant behavior. |
| TenantValidateOnUpdate | bool | `true` | Validates update ownership. |
| TenantValidateOnDelete | bool | `true` | Validates delete ownership. |
| TenantThrowOnMissing | bool | `true` | Throws when tenant context is absent. |
| EncryptionKey | string? | `null` | Base64 256-bit field-encryption key. |
| ReadPreference | string? | `null` | Driver read preference. |
| WriteConcern | string? | `null` | Driver write concern. |
| ReadConcern | string? | `null` | Driver read concern. |
| ConnectionTimeoutSeconds | int? | `null` | Connection timeout override. |
| SocketTimeoutSeconds | int? | `null` | Socket timeout override. |
| MaxConnectionPoolSize | int? | `null` | Maximum pool size override. |
| MinConnectionPoolSize | int? | `null` | Minimum pool size override. |
| EnableCommandLogging | bool | `false` | Enables command logging. |
| RetryReads | bool | `true` | Enables driver retryable reads. |
| RetryWrites | bool | `true` | Enables driver retryable writes. |

### MongoDbRepositoryOptions

| Name | Type | Default | Description |
|---|---|---|---|
| MaxQtyByQueryPage | int | `ContantsHelper.Data.MaxQtyByQueryPage` (300) | Page size applied when the paging criteria does not set a limit. |

### Changing the default page size

`ContantsHelper.Data.MaxQtyByQueryPage` (300) is only the framework default. Configure `MongoDbRepositoryOptions.MaxQtyByQueryPage` at registration time instead of patching the framework.

```csharp
// Before: default page size (300) applied by RepositoryBase.GetQuery
builder.Services.AddMvp24HoursRepositoryAsync();

// After: 100 documents per page for every repository resolved from this container
builder.Services.AddMvp24HoursRepositoryAsync(repositoryOptions => repositoryOptions.MaxQtyByQueryPage = 100);
```

The same `Action<MongoDbRepositoryOptions>` parameter is available on `AddMvp24HoursRepository`, `AddMvp24HoursRepositoryAsyncWithInterceptors`, `AddMvp24HoursBulkOperationsRepositoryAsync`, `AddMvp24HoursBulkOperationsRepositoryAsyncWithInterceptors`, `AddMvp24HoursReadOnlyRepository`, `AddMvp24HoursReadOnlyRepositoryAsync`, and `AddMvp24HoursReadOnlyRepositories`.

To bind the value from `appsettings.json`, read it in the registration delegate:

```csharp
builder.Services.AddMvp24HoursRepositoryAsync(repositoryOptions =>
    repositoryOptions.MaxQtyByQueryPage = builder.Configuration.GetValue("Mvp24Hours:Paging:MaxPageSize", 100));
```

How the effective page size is resolved in `RepositoryBase.GetQuery`:

| Situation | Effective limit |
|---|---|
| `criteria` is `null` | `Options.MaxQtyByQueryPage` |
| `criteria.Limit > 0` | `criteria.Limit` |
| `criteria.Limit <= 0` | `Options.MaxQtyByQueryPage` |

So the option is a default, not a hard cap: a caller passing `PagingCriteria(limit: 5000, offset: 0)` gets 5000 documents. Enforce an upper bound at the API boundary when it matters.

The `ToBusinessPaging`/`ToBusinessPagingAsync` extensions in `Mvp24Hours.Core` do not read `MongoDbRepositoryOptions`. They fall back to the constant and accept a per-call override:

```csharp
IPagingResult<IList<Customer>> result = await repository.ToBusinessPagingAsync(criteria, maxQtyByQueryDefault: 100);
```

## Interceptors and multi-tenancy

```csharp
builder.Services
    .AddMvp24HoursRepositoryAsyncWithInterceptors()
    .AddAllMongoDbInterceptors(options =>
    {
        options.EnableAuditInterceptor = true;
        options.EnableSoftDelete = true;
        options.EnableTenantInterceptor = true;
        options.TenantValidateOnUpdate = true;
        options.TenantValidateOnDelete = true;
    });
```

Register an `ITenantProvider` implementation or use `AddMongoDbAsyncLocalTenantProvider()`. Enabling tenant behavior does not select a tenant by itself.

### MongoDbInterceptorOptions

| Name | Type | Default | Description |
|---|---|---|---|
| EnableAuditInterceptor | bool | `true` | Populates audit fields. |
| EnableSoftDelete | bool | `true` | Converts supported deletes to soft deletes. |
| EnableCommandLogger | bool | `false` | Logs MongoDB commands. |
| EnableAuditTrail | bool | `false` | Records audit trail entries. |
| EnableTenantInterceptor | bool | `false` | Enables tenant isolation. |
| LogSlowOperationsOnly | bool | `false` | Limits logs to slow operations. |
| SlowOperationThreshold | TimeSpan | `500 ms` | Slow-operation threshold. |
| DefaultUser | string | `"System"` | Audit fallback user. |
| LogEntityDataInAuditTrail | bool | `false` | Includes entity data in audit records. |
| TenantValidateOnUpdate | bool | `true` | Validates update ownership. |
| TenantValidateOnDelete | bool | `true` | Validates delete ownership. |
| TenantThrowOnMissing | bool | `true` | Throws when no tenant is set. |

## Authentication

```csharp
builder.Services.AddMvp24HoursDbContext(options =>
{
    options.ConnectionString = "mongodb://cluster.example:27017";
    options.DatabaseName = "orders";
    options.EnableTls = true;
    options.Authentication = new MongoDbAuthenticationOptions
    {
        Mechanism = MongoDbAuthMechanism.ScramSha256,
        Username = mongoUser,
        Password = mongoPassword,
        AuthDatabase = "admin"
    };
});
```

Supported mechanisms are `Default`, `ScramSha1`, `ScramSha256`, `X509`, `AwsIam`, `Ldap`, and `Gssapi`. With `AwsIam` and no explicit key, the driver relies on environment variables or instance metadata.

### MongoDbAuthenticationOptions

| Name | Type | Default | Description |
|---|---|---|---|
| Mechanism | MongoDbAuthMechanism | `Default` | Authentication mechanism. |
| Username | string? | `null` | SCRAM/LDAP/GSSAPI username. |
| Password | string? | `null` | User password. |
| AuthDatabase | string | `"admin"` | Authentication database. |
| CertificatePath | string? | `null` | Client PFX/PKCS#12 path. |
| CertificatePassword | string? | `null` | Client certificate password. |
| Certificate | X509Certificate2? | `null` | Client certificate instance. |
| CaCertificatePath | string? | `null` | Custom CA certificate path. |
| ValidateServerCertificate | bool | `true` | Validates server certificates. |
| AwsAccessKeyId | string? | `null` | Explicit AWS access key. |
| AwsSecretAccessKey | string? | `null` | Explicit AWS secret key. |
| AwsSessionToken | string? | `null` | Temporary AWS session token. |
| LdapBindDn | string? | `null` | LDAP bind DN. |
| KerberosServiceName | string? | `null` | GSSAPI service name. |
| AllowedTlsProtocols | SslProtocols | `Tls12 \| Tls13` | Allowed TLS versions. |

## Resiliency

```csharp
builder.Services.AddMongoDbResiliency(options =>
{
    options.EnableAutoReconnect = true;
    options.RetryCount = 3;
    options.EnableCircuitBreaker = true;
    options.DefaultOperationTimeoutSeconds = 30;
});
```

`AddMongoDbResiliencyForProduction()` and `AddMongoDbResiliencyForDevelopment()` apply the `CreateProduction()` and `CreateDevelopment()` presets. This layer complements the driver's `RetryReads` and `RetryWrites`.

### MongoDbResiliencyOptions

| Name | Type | Default | Description |
|---|---|---|---|
| EnableAutoReconnect | bool | `true` | Enables managed reconnect. |
| MaxReconnectAttempts | int | `5` | Reconnect attempts. |
| ReconnectDelayMilliseconds | int | `1000` | Initial reconnect delay. |
| MaxReconnectDelayMilliseconds | int | `30000` | Reconnect delay cap. |
| UseExponentialBackoffForReconnect | bool | `true` | Uses reconnect backoff. |
| ReconnectJitterFactor | double | `0.2` | Reconnect jitter. |
| EnableRetry | bool | `true` | Enables transient retry. |
| RetryCount | int | `3` | Retry count. |
| RetryBaseDelayMilliseconds | int | `100` | Initial retry delay. |
| RetryMaxDelayMilliseconds | int | `5000` | Retry delay cap. |
| UseExponentialBackoff | bool | `true` | Uses retry backoff. |
| RetryJitterFactor | double | `0.2` | Retry jitter. |
| AdditionalRetryableExceptions | List<Type> | empty | Extra retryable exceptions. |
| NonRetryableExceptions | List<Type> | empty | Fail-fast exceptions. |
| EnableCircuitBreaker | bool | `true` | Enables circuit breaker. |
| CircuitBreakerFailureThreshold | int | `5` | Failure count threshold. |
| CircuitBreakerSamplingDurationSeconds | int | `60` | Sampling window. |
| CircuitBreakerDurationSeconds | int | `30` | Open duration. |
| CircuitBreakerMinimumThroughput | int | `10` | Minimum sampled operations. |
| CircuitBreakerFailureRateThreshold | double? | `null` | Optional failure-rate threshold. |
| TrackCircuitBreakerMetrics | bool | `true` | Captures breaker metrics. |
| EnableOperationTimeout | bool | `true` | Enables operation timeouts. |
| DefaultOperationTimeoutSeconds | int | `30` | Default timeout. |
| ReadOperationTimeoutSeconds | int? | `null` | Read timeout override. |
| WriteOperationTimeoutSeconds | int? | `null` | Write timeout override. |
| BulkOperationTimeoutSeconds | int | `120` | Bulk timeout. |
| EnableAutomaticFailover | bool | `true` | Enables replica-set failover. |
| ServerSelectionTimeoutSeconds | int | `30` | Server selection timeout. |
| HeartbeatFrequencySeconds | int | `10` | Heartbeat interval. |
| EnableServerMonitoring | bool | `true` | Enables topology monitoring. |
| AllowReadsWithoutPrimary | bool | `true` | Allows secondary reads during election. |
| LogRetryAttempts | bool | `true` | Logs retries. |
| LogCircuitBreakerStateChanges | bool | `true` | Logs breaker transitions. |
| LogConnectionEvents | bool | `true` | Logs connection events. |
| LogTimeoutEvents | bool | `true` | Logs timeouts. |

## Bulk operations

```csharp
builder.Services.AddMvp24HoursBulkOperationsRepositoryAsync();

var result = await repository.BulkInsertAsync(
    customers,
    MongoDbBulkOperationOptions.HighIntegrity,
    cancellationToken);
```

Presets are `Default`, `HighThroughput`, and `HighIntegrity`.

### MongoDbBulkOperationOptions

| Name | Type | Default | Description |
|---|---|---|---|
| BatchSize | int | `1000` | Documents per batch. |
| UseTransaction | bool | `true` | Wraps the operation in a transaction. |
| ProgressCallback | BulkProgressCallback? | `null` | Progress callback. |
| TimeoutSeconds | int | `300` | Operation timeout. |
| IsOrdered | bool | `true` | Stops after the first failed write. |
| BypassDocumentValidation | bool | `false` | Bypasses server validation. |
| WriteConcern | string | `""` | Optional bulk write concern. |
| MaxRetryAttempts | int | `3` | Transient retries. |
| RetryDelayMilliseconds | int | `100` | Retry delay. |

## Advanced service registration

Register all core advanced services:

```csharp
builder.Services.AddMvpMongoDbAdvanced();
builder.Services.AddMvpMongoDbChangeStream<Customer>("customers");
builder.Services.AddMvpMongoDbTextSearch<Customer>("customers");
builder.Services.AddMvpMongoDbTimeSeries<SensorReading>(
    "sensor_readings", "Timestamp", "Metadata");
builder.Services.AddMvpMongoDbCappedCollection<LogEntry>("logs");
builder.Services.AddMvpMongoDbGeospatial<Store>("stores");
```

`AddMvpMongoDbAdvanced()` registers transactions, GridFS, schema validation, and sharding. Feature-specific services accept the option objects below when creating collections, indexes, or running operations; registration itself does not create server resources.

### MongoDbTextSearchOptions

| Name | Type | Default | Description |
|---|---|---|---|
| Language | string? | `null` | Text-search language. |
| CaseSensitive | bool | `false` | Case-sensitive search. |
| DiacriticSensitive | bool | `false` | Diacritic-sensitive search. |
| IncludeScore | bool | `true` | Includes text score. |
| MinScore | double? | `null` | Minimum score. |
| Limit | int? | `null` | Result limit. |
| Skip | int? | `null` | Result offset. |
| SortByScore | bool | `true` | Sorts by score descending. |

### TimeSeriesOptions

| Name | Type | Default | Description |
|---|---|---|---|
| TimeField | string | `""` | Required timestamp field. |
| MetaField | string? | `null` | Metadata field. |
| Granularity | string | `"seconds"` | `seconds`, `minutes`, or `hours`. |
| BucketMaxSpanSeconds | int? | `null` | Maximum bucket span. |
| BucketRoundingSeconds | int? | `null` | Bucket rounding interval. |
| ExpireAfter | TimeSpan? | `null` | Automatic document expiration. |

### CappedCollectionOptions

| Name | Type | Default | Description |
|---|---|---|---|
| MaxSizeBytes | long | `0` | Required maximum collection size. |
| MaxDocuments | long? | `null` | Optional document limit. |
| AutoIndexId | bool | `true` | Creates the `_id` index. |

### MongoDbCollationOptions

| Name | Type | Default | Description |
|---|---|---|---|
| Locale | string | `"en"` | Collation locale. |
| CaseLevel | bool? | `null` | Enables case-level comparison. |
| CaseFirst | CollationCaseFirst | `Off` | Case ordering. |
| Strength | CollationStrength | `Tertiary` | Comparison strength. |
| NumericOrdering | bool | `false` | Sorts numeric strings numerically. |
| Alternate | CollationAlternate | `NonIgnorable` | Punctuation handling. |
| MaxVariable | CollationMaxVariable | `Punctuation` | Maximum ignored variable class. |
| Normalization | bool? | `null` | Enables normalization. |
| Backwards | bool? | `null` | Uses backwards diacritic ordering. |

Collation presets include case-insensitive English, Portuguese, and Spanish, numeric ordering, combined case-insensitive numeric ordering, and simple binary comparison.

### MongoDbShardingOptions

| Name | Type | Default | Description |
|---|---|---|---|
| ShardKeyFields | List<ShardKeyField> | empty | Ordered shard-key fields. |
| UseHashedShardKey | bool | `false` | Uses a hashed key. |
| UniqueShardKey | bool | `false` | Requires key uniqueness. |
| NumInitialChunks | int? | `null` | Initial pre-split chunk count. |

`ShardKeyField` has `FieldName` (`string`, `""`) and `Order` (`BsonValue`, `1`); use `Ascending(...)`, `Descending(...)`, or `Hashed(...)`.

### MongoDbSchemaValidationOptions

| Name | Type | Default | Description |
|---|---|---|---|
| ValidationLevel | SchemaValidationLevel | `Strict` | `Off`, `Strict`, or `Moderate`. |
| ValidationAction | SchemaValidationAction | `Error` | Rejects or warns. |
| JsonSchema | BsonDocument? | `null` | MongoDB JSON Schema document. |

## Transactions and concerns

```csharp
builder.Services.AddMvpMongoDbTransactions(options =>
{
    options.DefaultReadConcern = ReadConcern.Snapshot;
    options.DefaultWriteConcern = WriteConcern.WMajority;
    options.MaxTransactionRetries = 3;
});
```

### MongoDbTransactionOptions

| Name | Type | Default | Description |
|---|---|---|---|
| DefaultReadConcern | ReadConcern | `Snapshot` | Transaction read concern. |
| DefaultWriteConcern | WriteConcern | `WMajority` | Transaction write concern. |
| DefaultReadPreference | ReadPreference | `Primary` | Transaction read preference. |
| MaxCommitTime | TimeSpan? | `null` | Commit time limit. |
| MaxTransactionRetries | int | `3` | Transaction retries. |
| MaxCommitRetries | int | `3` | Unknown-commit retries. |
| RetryDelayMs | int | `100` | Retry delay. |
| AutoRetryReads | bool | `true` | Retries transaction reads. |
| AutoRetryWrites | bool | `true` | Retries transaction writes. |

### MongoDbConcernOptions

| Name | Type | Default | Description |
|---|---|---|---|
| ReadConcernLevel | ReadConcernLevel? | `null` | Read visibility. |
| WriteConcernMode | WriteConcernMode? | `null` | Write acknowledgment preset. |
| W | int? | `null` | Required acknowledgments; `-1` means majority. |
| WTimeout | TimeSpan? | `null` | Write acknowledgment timeout. |
| Journal | bool? | `null` | Requires journal acknowledgment. |
| ReadPreferenceMode | ReadPreferenceMode? | `null` | Node-selection preference. |
| MaxStaleness | TimeSpan? | `null` | Maximum secondary staleness. |

Concern presets are `MaxDurability`, `MaxConsistency`, `MaxPerformance`, `Balanced`, `FireAndForget`, and `Analytics`.

## Connection pool

```csharp
builder.Services.ConfigureMongoDbConnectionPool(options =>
{
    options.MinPoolSize = 10;
    options.MaxPoolSize = 200;
    options.WaitQueueTimeoutSeconds = 30;
});
```

### MongoDbConnectionPoolOptions

| Name | Type | Default | Description |
|---|---|---|---|
| MinPoolSize | int | `0` | Minimum connections. |
| MaxPoolSize | int | `100` | Maximum connections. |
| WaitQueueTimeoutSeconds | int | `120` | Wait for a pooled connection. |
| MaxConnectionIdleTimeSeconds | int | `600` | Maximum idle time. |
| MaxConnectionLifetimeSeconds | int | `1800` | Maximum connection lifetime. |
| ConnectTimeoutSeconds | int | `30` | Socket connection timeout. |
| SocketTimeoutSeconds | int | `0` | Response timeout; `0` leaves driver default. |
| ServerSelectionTimeoutSeconds | int | `30` | Server selection timeout. |
| HeartbeatFrequencySeconds | int | `10` | Heartbeat interval. |
| IPv6 | bool | `false` | Enables IPv6. |
| DirectConnection | bool | `false` | Connects directly to one server. |
| Compressors | string[]? | `null` | Requested compressor names. |
| LocalThresholdMilliseconds | int | `15` | Latency window for server selection. |

## Observability

```csharp
builder.Services.AddMongoDbObservability(options =>
{
    options.EnableSlowQueryLogging = true;
    options.EnableOpenTelemetry = true;
    options.EnableConnectionPoolMetrics = true;
});
```

### MongoDbObservabilityOptions

| Name | Type | Default | Description |
|---|---|---|---|
| EnableSlowQueryLogging | bool | `true` | Logs slow queries. |
| SlowQueryThreshold | TimeSpan | `500 ms` | Slow-query threshold. |
| LogSlowQueryFilter | bool | `false` | Logs filters; may expose data. |
| IncludeExplainForSlowQueries | bool | `false` | Adds explain output. |
| MaxSlowQueriesPerMinute | int | `100` | Rate limit. |
| EnableOpenTelemetry | bool | `false` | Enables tracing. |
| ActivitySourceName | string | `"Mvp24Hours.MongoDb"` | Activity source. |
| RecordExceptions | bool | `true` | Records trace exceptions. |
| IncludeStatementInTrace | bool | `false` | Includes statements; may expose data. |
| AdditionalTraceTags | string[] | empty | Additional trace tags. |
| EnableConnectionPoolMetrics | bool | `true` | Captures pool metrics. |
| ConnectionPoolMetricsInterval | TimeSpan | `30 seconds` | Collection interval. |
| EnableConnectionPoolAlerts | bool | `true` | Enables utilization alerts. |
| ConnectionPoolAlertThreshold | double | `0.8` | Alert utilization. |
| EnableStructuredLogging | bool | Debug: `true`; Release: `false` | Enables structured logs. |
| LogCommandParameters | bool | `false` | Logs parameters; may expose data. |
| LogResultCounts | bool | `true` | Logs result counts. |
| MaxLogMessageLength | int | `4096` | Log truncation length. |
| SensitiveFields | string[] | built-in list | Masked field names. |
| EnableDurationTracking | bool | `true` | Tracks durations. |
| TrackIndividualOperations | bool | `true` | Tracks each operation. |
| CollectDurationPercentiles | bool | `true` | Captures p50/p95/p99. |
| DurationAggregationWindow | TimeSpan | `1 minute` | Aggregation window. |
| DurationHistogramBuckets | int | `20` | Histogram buckets. |
| EnableAll | bool (set-only) | not applicable | Toggles logging, pool metrics, and duration features. |
| ServiceName | string? | `null` | Telemetry service name. |
| Environment | string? | `null` | Telemetry environment. |

## Health checks

```csharp
builder.Services.AddHealthChecks()
    .AddMongoDbHealthCheck(
        name: "mongodb",
        configureOptions: options =>
        {
            options.VerifyDatabaseAccess = true;
            options.IncludeServerStatus = false;
        },
        tags: ["database", "ready"]);
```

### MongoDbHealthCheckOptions

| Name | Type | Default | Description |
|---|---|---|---|
| VerifyDatabaseAccess | bool | `false` | Executes a database-level probe. |
| IncludeServerStatus | bool | `false` | Includes server status. |
| ConnectionTimeoutSeconds | int | `5` | Connection timeout. |
| ServerSelectionTimeoutSeconds | int | `5` | Server selection timeout. |

`AddMongoDbReplicaSetHealthCheck(...)` is available for replica topology and lag checks.

### MongoDbReplicaSetHealthCheckOptions

| Name | Type | Default | Description |
|---|---|---|---|
| MinSecondaryNodes | int | `0` | Required secondary count. |
| MaxReplicationLagSeconds | int | `0` | Maximum lag; `0` disables the limit. |
| AllowUnhealthyMembers | bool | `true` | Tolerates unhealthy members. |
| AllowStandaloneMode | bool | `false` | Accepts a non-replica deployment. |
| IncludeMemberDetails | bool | `true` | Adds member details to health data. |
| ConnectionTimeoutSeconds | int | `5` | Connection timeout. |
| ServerSelectionTimeoutSeconds | int | `5` | Server selection timeout. |

## Testing

The in-memory helpers still require a reachable MongoDB-compatible connection; they provide isolated database names and fakes, not an embedded server.

```csharp
builder.Services.AddMvp24HoursMongoTestInfrastructure(
    mongoContainer.GetConnectionString(),
    options => options.EnableTransaction = false);
```

Use `AddMvp24HoursMongoFakeTestInfrastructure()` for repository-only unit tests, or `AddMvp24HoursMongoContextFactory(...)` for isolated integration contexts.

### MongoDbInMemoryOptions

| Name | Type | Default | Description |
|---|---|---|---|
| DatabaseNamePrefix | string | `"InMemoryMongoTestDb"` | Generated database prefix. |
| DatabaseName | string? | `null` | Fixed base name. |
| UseUniqueDatabaseName | bool | `true` | Appends a GUID. |
| ConnectionString | string? | `null` | Test server connection. |
| EnableLogging | bool | `true` | Enables test logging. |
| EnableTransaction | bool | `false` | Enables transactions when supported. |
| EnableMultiTenancy | bool | `false` | Enables tenant behavior. |
| TimeoutSeconds | int | `30` | Operation timeout. |
| ConfigureOptions | Action<MongoDbOptions>? | `null` | Additional context configuration. |

Presets are `ForUnitTesting()`, `ForIntegrationTesting()`, and `ForSharedDatabase(...)`.

For Testcontainers, pass the container connection string and one of the tested presets (`ForBasicTesting()`, `ForAuthenticatedTesting(...)`, or `ForReplicaSetTesting()`) to the static `MongoDbTestcontainersHelper.CreateContextFactory(...)` or `CreateOptions(...)` helper.

### MongoDbTestcontainersOptions

| Name | Type | Default | Description |
|---|---|---|---|
| ImageTag | string | `"latest"` | MongoDB image tag. |
| DatabaseName | string | `"testdb"` | Base test database name. |
| UseUniqueDatabaseName | bool | `true` | Appends a GUID. |
| Port | int? | `null` | Optional host port. |
| Username | string? | `null` | Optional root username. |
| Password | string? | `null` | Optional root password. |
| EnableReplicaSet | bool | `false` | Enables replica-set setup. |
| StartupTimeoutSeconds | int | `60` | Container startup timeout. |
| AutoRemove | bool | `true` | Removes the container after use. |
| ContainerNamePrefix | string | `"mvp24hours-mongodb-test"` | Container name prefix. |

See [NoSQL](nosql.md), [Repository](use-repository.md), and [Unit of Work](use-unitofwork.md).
