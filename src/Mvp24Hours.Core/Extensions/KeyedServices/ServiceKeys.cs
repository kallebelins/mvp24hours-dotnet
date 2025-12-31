//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Core.Extensions.KeyedServices;

/// <summary>
/// Pre-defined keys for keyed services in the Mvp24Hours framework.
/// </summary>
/// <remarks>
/// <para>
/// Keyed Services are a .NET 8+ feature that allows registering multiple implementations
/// of the same interface with different keys. This enables scenarios like:
/// - Multiple database contexts (Read/Write)
/// - Multiple cache providers (Memory/Redis/Hybrid)
/// - Multiple file storage providers (Local/Azure/AWS)
/// - Multiple messaging providers (RabbitMQ/Azure Service Bus)
/// </para>
/// <para>
/// Use these constants with <c>[FromKeyedServices(key)]</c> attribute or
/// <c>GetRequiredKeyedService&lt;T&gt;(key)</c> method.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registration
/// services.AddKeyedSingleton&lt;IFileStorage, LocalFileStorageProvider&gt;(ServiceKeys.FileStorage.Local);
/// services.AddKeyedSingleton&lt;IFileStorage, AzureBlobStorageProvider&gt;(ServiceKeys.FileStorage.Azure);
/// 
/// // Injection via attribute
/// public class MyService([FromKeyedServices(ServiceKeys.FileStorage.Local)] IFileStorage localStorage)
/// {
/// }
/// 
/// // Resolution via service provider
/// var azureStorage = provider.GetRequiredKeyedService&lt;IFileStorage&gt;(ServiceKeys.FileStorage.Azure);
/// </code>
/// </example>
public static class ServiceKeys
{
    /// <summary>
    /// Keys for file storage providers.
    /// </summary>
    public static class FileStorage
    {
        /// <summary>
        /// Local filesystem storage provider.
        /// </summary>
        public const string Local = "FileStorage:Local";

        /// <summary>
        /// In-memory file storage provider (for testing).
        /// </summary>
        public const string InMemory = "FileStorage:InMemory";

        /// <summary>
        /// Azure Blob Storage provider.
        /// </summary>
        public const string Azure = "FileStorage:Azure";

        /// <summary>
        /// AWS S3 Storage provider.
        /// </summary>
        public const string AwsS3 = "FileStorage:AwsS3";

        /// <summary>
        /// Default file storage (uses configured default).
        /// </summary>
        public const string Default = "FileStorage:Default";
    }

    /// <summary>
    /// Keys for email service providers.
    /// </summary>
    public static class Email
    {
        /// <summary>
        /// SMTP email provider.
        /// </summary>
        public const string Smtp = "Email:Smtp";

        /// <summary>
        /// SendGrid email provider.
        /// </summary>
        public const string SendGrid = "Email:SendGrid";

        /// <summary>
        /// Azure Communication Services email provider.
        /// </summary>
        public const string Azure = "Email:Azure";

        /// <summary>
        /// In-memory email provider (for testing).
        /// </summary>
        public const string InMemory = "Email:InMemory";

        /// <summary>
        /// Default email provider (uses configured default).
        /// </summary>
        public const string Default = "Email:Default";
    }

    /// <summary>
    /// Keys for SMS service providers.
    /// </summary>
    public static class Sms
    {
        /// <summary>
        /// Twilio SMS provider.
        /// </summary>
        public const string Twilio = "Sms:Twilio";

        /// <summary>
        /// Azure Communication Services SMS provider.
        /// </summary>
        public const string Azure = "Sms:Azure";

        /// <summary>
        /// In-memory SMS provider (for testing).
        /// </summary>
        public const string InMemory = "Sms:InMemory";

        /// <summary>
        /// Default SMS provider (uses configured default).
        /// </summary>
        public const string Default = "Sms:Default";
    }

    /// <summary>
    /// Keys for cache providers.
    /// </summary>
    public static class Cache
    {
        /// <summary>
        /// In-memory cache provider.
        /// </summary>
        public const string Memory = "Cache:Memory";

        /// <summary>
        /// Distributed cache provider (Redis, SQL, etc.).
        /// </summary>
        public const string Distributed = "Cache:Distributed";

        /// <summary>
        /// Hybrid cache provider (L1 + L2).
        /// </summary>
        public const string Hybrid = "Cache:Hybrid";

        /// <summary>
        /// Redis cache provider.
        /// </summary>
        public const string Redis = "Cache:Redis";

        /// <summary>
        /// Default cache provider (uses configured default).
        /// </summary>
        public const string Default = "Cache:Default";
    }

    /// <summary>
    /// Keys for database contexts.
    /// </summary>
    public static class Database
    {
        /// <summary>
        /// Read-only database context (for queries).
        /// </summary>
        public const string ReadOnly = "Database:ReadOnly";

        /// <summary>
        /// Read-write database context (for commands).
        /// </summary>
        public const string ReadWrite = "Database:ReadWrite";

        /// <summary>
        /// Primary database context.
        /// </summary>
        public const string Primary = "Database:Primary";

        /// <summary>
        /// Replica database context.
        /// </summary>
        public const string Replica = "Database:Replica";

        /// <summary>
        /// Default database context.
        /// </summary>
        public const string Default = "Database:Default";
    }

    /// <summary>
    /// Keys for messaging providers.
    /// </summary>
    public static class Messaging
    {
        /// <summary>
        /// RabbitMQ messaging provider.
        /// </summary>
        public const string RabbitMQ = "Messaging:RabbitMQ";

        /// <summary>
        /// Azure Service Bus messaging provider.
        /// </summary>
        public const string AzureServiceBus = "Messaging:AzureServiceBus";

        /// <summary>
        /// In-memory messaging provider (for testing).
        /// </summary>
        public const string InMemory = "Messaging:InMemory";

        /// <summary>
        /// Default messaging provider.
        /// </summary>
        public const string Default = "Messaging:Default";
    }

    /// <summary>
    /// Keys for distributed locking providers.
    /// </summary>
    public static class DistributedLock
    {
        /// <summary>
        /// Redis-based distributed lock.
        /// </summary>
        public const string Redis = "DistributedLock:Redis";

        /// <summary>
        /// SQL Server-based distributed lock.
        /// </summary>
        public const string SqlServer = "DistributedLock:SqlServer";

        /// <summary>
        /// PostgreSQL-based distributed lock.
        /// </summary>
        public const string PostgreSql = "DistributedLock:PostgreSql";

        /// <summary>
        /// In-memory distributed lock (for testing/single-instance).
        /// </summary>
        public const string InMemory = "DistributedLock:InMemory";

        /// <summary>
        /// Default distributed lock provider.
        /// </summary>
        public const string Default = "DistributedLock:Default";
    }

    /// <summary>
    /// Keys for secret providers.
    /// </summary>
    public static class Secrets
    {
        /// <summary>
        /// Azure Key Vault secret provider.
        /// </summary>
        public const string AzureKeyVault = "Secrets:AzureKeyVault";

        /// <summary>
        /// AWS Secrets Manager provider.
        /// </summary>
        public const string AwsSecretsManager = "Secrets:AwsSecretsManager";

        /// <summary>
        /// Environment variable secret provider.
        /// </summary>
        public const string EnvironmentVariable = "Secrets:EnvironmentVariable";

        /// <summary>
        /// User secrets provider (development only).
        /// </summary>
        public const string UserSecrets = "Secrets:UserSecrets";

        /// <summary>
        /// Default secret provider.
        /// </summary>
        public const string Default = "Secrets:Default";
    }

    /// <summary>
    /// Keys for background job providers.
    /// </summary>
    public static class BackgroundJobs
    {
        /// <summary>
        /// Hangfire job provider.
        /// </summary>
        public const string Hangfire = "BackgroundJobs:Hangfire";

        /// <summary>
        /// Quartz.NET job provider.
        /// </summary>
        public const string Quartz = "BackgroundJobs:Quartz";

        /// <summary>
        /// In-memory job provider (for testing).
        /// </summary>
        public const string InMemory = "BackgroundJobs:InMemory";

        /// <summary>
        /// Default job provider.
        /// </summary>
        public const string Default = "BackgroundJobs:Default";
    }

    /// <summary>
    /// Keys for HTTP clients.
    /// </summary>
    public static class HttpClient
    {
        /// <summary>
        /// Default HTTP client with standard resilience.
        /// </summary>
        public const string Default = "HttpClient:Default";

        /// <summary>
        /// HTTP client optimized for high availability.
        /// </summary>
        public const string HighAvailability = "HttpClient:HighAvailability";

        /// <summary>
        /// HTTP client optimized for low latency.
        /// </summary>
        public const string LowLatency = "HttpClient:LowLatency";

        /// <summary>
        /// HTTP client for batch processing.
        /// </summary>
        public const string BatchProcessing = "HttpClient:BatchProcessing";
    }

    /// <summary>
    /// Keys for template renderers.
    /// </summary>
    public static class TemplateRenderer
    {
        /// <summary>
        /// Scriban template renderer (Liquid-like syntax).
        /// </summary>
        public const string Scriban = "TemplateRenderer:Scriban";

        /// <summary>
        /// Razor template renderer (C# syntax).
        /// </summary>
        public const string Razor = "TemplateRenderer:Razor";

        /// <summary>
        /// Default template renderer.
        /// </summary>
        public const string Default = "TemplateRenderer:Default";
    }

    /// <summary>
    /// Keys for validators.
    /// </summary>
    public static class Validator
    {
        /// <summary>
        /// FluentValidation validator.
        /// </summary>
        public const string FluentValidation = "Validator:FluentValidation";

        /// <summary>
        /// Data Annotations validator.
        /// </summary>
        public const string DataAnnotations = "Validator:DataAnnotations";

        /// <summary>
        /// Default validator.
        /// </summary>
        public const string Default = "Validator:Default";
    }

    /// <summary>
    /// Keys for serializers.
    /// </summary>
    public static class Serializer
    {
        /// <summary>
        /// System.Text.Json serializer.
        /// </summary>
        public const string SystemTextJson = "Serializer:SystemTextJson";

        /// <summary>
        /// Newtonsoft.Json serializer.
        /// </summary>
        public const string NewtonsoftJson = "Serializer:NewtonsoftJson";

        /// <summary>
        /// MessagePack serializer.
        /// </summary>
        public const string MessagePack = "Serializer:MessagePack";

        /// <summary>
        /// Default serializer.
        /// </summary>
        public const string Default = "Serializer:Default";
    }

    /// <summary>
    /// Keys for tenant-specific services.
    /// </summary>
    public static class Tenant
    {
        /// <summary>
        /// Creates a tenant-specific service key.
        /// </summary>
        /// <param name="serviceCategory">The service category (e.g., "Database", "Cache").</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <returns>A tenant-specific service key.</returns>
        public static string ForTenant(string serviceCategory, string tenantId)
            => $"Tenant:{tenantId}:{serviceCategory}";

        /// <summary>
        /// Creates a tenant-specific database key.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <returns>A tenant-specific database key.</returns>
        public static string DatabaseForTenant(string tenantId)
            => ForTenant("Database", tenantId);

        /// <summary>
        /// Creates a tenant-specific cache key.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <returns>A tenant-specific cache key.</returns>
        public static string CacheForTenant(string tenantId)
            => ForTenant("Cache", tenantId);

        /// <summary>
        /// Creates a tenant-specific file storage key.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <returns>A tenant-specific file storage key.</returns>
        public static string FileStorageForTenant(string tenantId)
            => ForTenant("FileStorage", tenantId);
    }

    /// <summary>
    /// Keys for environment-specific services.
    /// </summary>
    public static class Environment
    {
        /// <summary>
        /// Development environment services.
        /// </summary>
        public const string Development = "Environment:Development";

        /// <summary>
        /// Staging environment services.
        /// </summary>
        public const string Staging = "Environment:Staging";

        /// <summary>
        /// Production environment services.
        /// </summary>
        public const string Production = "Environment:Production";

        /// <summary>
        /// Test environment services.
        /// </summary>
        public const string Test = "Environment:Test";
    }
}

