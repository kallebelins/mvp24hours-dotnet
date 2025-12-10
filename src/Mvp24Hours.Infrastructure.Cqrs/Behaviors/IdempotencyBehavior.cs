//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Mvp24Hours.Infrastructure.Cqrs.Behaviors;

/// <summary>
/// Marker interface for commands that should be idempotent.
/// Idempotent commands can be safely retried without causing duplicate effects.
/// </summary>
/// <remarks>
/// <para>
/// Apply this interface to commands where duplicate processing should be prevented,
/// such as payment processing, order creation, etc.
/// </para>
/// <para>
/// <strong>How it works:</strong>
/// <list type="number">
/// <item>A unique key is generated from the command</item>
/// <item>The key is checked against the cache</item>
/// <item>If found, the cached response is returned</item>
/// <item>If not found, the command is executed and result cached</item>
/// </list>
/// </para>
/// <para>
/// <strong>Important:</strong> Ensure the IdempotencyKey uniquely identifies
/// the intent of the command, not just its data.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class ProcessPaymentCommand : IMediatorCommand&lt;PaymentResult&gt;, IIdempotentCommand
/// {
///     public Guid PaymentId { get; init; }
///     public decimal Amount { get; init; }
///     
///     // Custom idempotency key based on payment ID
///     public string? IdempotencyKey => $"payment:{PaymentId}";
///     
///     // Cache result for 24 hours
///     public TimeSpan? IdempotencyDuration => TimeSpan.FromHours(24);
/// }
/// </code>
/// </example>
public interface IIdempotentCommand
{
    /// <summary>
    /// Gets the idempotency key for this command.
    /// If null, a key will be generated from the command properties.
    /// </summary>
    /// <remarks>
    /// For commands with a natural business key (like PaymentId),
    /// it's recommended to provide a custom key based on that.
    /// </remarks>
    string? IdempotencyKey => null;

    /// <summary>
    /// Gets the duration to cache the idempotency result.
    /// If null, the default duration from options will be used.
    /// </summary>
    TimeSpan? IdempotencyDuration => null;
}

/// <summary>
/// Interface for generating idempotency keys.
/// Implement this to customize key generation.
/// </summary>
public interface IIdempotencyKeyGenerator
{
    /// <summary>
    /// Generates an idempotency key for the specified request.
    /// </summary>
    /// <typeparam name="TRequest">The type of request.</typeparam>
    /// <param name="request">The request to generate a key for.</param>
    /// <returns>A unique idempotency key.</returns>
    string GenerateKey<TRequest>(TRequest request);
}

/// <summary>
/// Default implementation of idempotency key generator.
/// Generates keys based on the request type and a hash of its properties.
/// </summary>
public sealed class DefaultIdempotencyKeyGenerator : IIdempotencyKeyGenerator
{
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Creates a new instance of the DefaultIdempotencyKeyGenerator.
    /// </summary>
    public DefaultIdempotencyKeyGenerator()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <inheritdoc />
    public string GenerateKey<TRequest>(TRequest request)
    {
        var typeName = typeof(TRequest).FullName ?? typeof(TRequest).Name;
        var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
        var hash = ComputeHash(requestJson);

        return $"idempotency:{typeName}:{hash}";
    }

    private static string ComputeHash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToBase64String(hashBytes)[..16]; // Use first 16 chars
    }
}

/// <summary>
/// Pipeline behavior that ensures idempotency for commands.
/// Only applies to commands that implement <see cref="IIdempotentCommand"/>.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <remarks>
/// <para>
/// This behavior uses <see cref="IDistributedCache"/> to store command results,
/// allowing integration with various cache providers (Memory, Redis, etc.).
/// </para>
/// <para>
/// <strong>Thread Safety:</strong> Multiple concurrent requests with the same
/// idempotency key may execute in parallel. For strict deduplication, consider
/// using distributed locks.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI
/// services.AddTransient(typeof(IPipelineBehavior&lt;,&gt;), typeof(IdempotencyBehavior&lt;,&gt;));
/// services.AddSingleton&lt;IIdempotencyKeyGenerator, DefaultIdempotencyKeyGenerator&gt;();
/// </code>
/// </example>
public sealed class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly IDistributedCache _cache;
    private readonly IIdempotencyKeyGenerator _keyGenerator;
    private readonly ILogger<IdempotencyBehavior<TRequest, TResponse>>? _logger;
    private readonly TimeSpan _defaultDuration;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Creates a new instance of the IdempotencyBehavior.
    /// </summary>
    /// <param name="cache">The distributed cache.</param>
    /// <param name="keyGenerator">The idempotency key generator.</param>
    /// <param name="logger">Optional logger for recording operations.</param>
    /// <param name="defaultDurationHours">Default cache duration in hours (default: 24).</param>
    public IdempotencyBehavior(
        IDistributedCache cache,
        IIdempotencyKeyGenerator? keyGenerator = null,
        ILogger<IdempotencyBehavior<TRequest, TResponse>>? logger = null,
        int defaultDurationHours = 24)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _keyGenerator = keyGenerator ?? new DefaultIdempotencyKeyGenerator();
        _logger = logger;
        _defaultDuration = TimeSpan.FromHours(defaultDurationHours);
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Only apply idempotency if the request implements IIdempotentCommand
        if (request is not IIdempotentCommand idempotent)
        {
            return await next();
        }

        var requestName = typeof(TRequest).Name;
        var idempotencyKey = GetIdempotencyKey(request, idempotent);

        // Try to get cached response
        try
        {
            var cachedValue = await _cache.GetStringAsync(idempotencyKey, cancellationToken);

            if (!string.IsNullOrEmpty(cachedValue))
            {
                _logger?.LogInformation(
                    "[Idempotency] Duplicate request detected for {RequestName} (Key: {IdempotencyKey}). Returning cached response.",
                    requestName,
                    idempotencyKey);

                var cachedResponse = JsonSerializer.Deserialize<TResponse>(cachedValue, _jsonOptions);
                if (cachedResponse != null)
                {
                    return cachedResponse;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(
                ex,
                "[Idempotency] Error reading from cache for {RequestName}: {Message}",
                requestName,
                ex.Message);
            // Continue with execution if cache read fails
        }

        _logger?.LogDebug(
            "[Idempotency] Processing new request {RequestName} (Key: {IdempotencyKey})",
            requestName,
            idempotencyKey);

        // Execute the handler
        var response = await next();

        // Cache the response
        try
        {
            if (response != null)
            {
                var duration = idempotent.IdempotencyDuration ?? _defaultDuration;
                var serialized = JsonSerializer.Serialize(response, _jsonOptions);

                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = duration
                };

                await _cache.SetStringAsync(idempotencyKey, serialized, options, cancellationToken);

                _logger?.LogDebug(
                    "[Idempotency] Cached response for {RequestName} (Key: {IdempotencyKey}, Duration: {Duration})",
                    requestName,
                    idempotencyKey,
                    duration);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(
                ex,
                "[Idempotency] Error writing to cache for {RequestName}: {Message}",
                requestName,
                ex.Message);
            // Don't fail the request if cache write fails
        }

        return response;
    }

    private string GetIdempotencyKey(TRequest request, IIdempotentCommand idempotent)
    {
        // Use custom key if provided
        if (!string.IsNullOrEmpty(idempotent.IdempotencyKey))
        {
            return $"idempotency:{typeof(TRequest).Name}:{idempotent.IdempotencyKey}";
        }

        // Generate key from request
        return _keyGenerator.GenerateKey(request);
    }
}

/// <summary>
/// Model for storing idempotency information.
/// </summary>
internal sealed class IdempotencyRecord
{
    public string Key { get; init; } = string.Empty;
    public string RequestType { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public string ResponseJson { get; init; } = string.Empty;
}

