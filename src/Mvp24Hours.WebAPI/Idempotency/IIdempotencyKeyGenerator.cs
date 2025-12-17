//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Idempotency
{
    /// <summary>
    /// Result of idempotency key extraction from an HTTP request.
    /// </summary>
    public class IdempotencyKeyResult
    {
        /// <summary>
        /// Gets the extracted or generated idempotency key.
        /// </summary>
        public string? Key { get; init; }

        /// <summary>
        /// Gets whether a key was successfully extracted or generated.
        /// </summary>
        public bool HasKey => !string.IsNullOrEmpty(Key);

        /// <summary>
        /// Gets whether the key came from the request header.
        /// </summary>
        public bool IsFromHeader { get; init; }

        /// <summary>
        /// Gets whether the key was generated from the request body.
        /// </summary>
        public bool IsGenerated { get; init; }

        /// <summary>
        /// Gets the hash of the request body (if generated from body).
        /// </summary>
        public string? RequestBodyHash { get; init; }

        /// <summary>
        /// Creates a result with no key.
        /// </summary>
        public static IdempotencyKeyResult NoKey() => new();

        /// <summary>
        /// Creates a result with a key from header.
        /// </summary>
        public static IdempotencyKeyResult FromHeader(string key) => new()
        {
            Key = key,
            IsFromHeader = true
        };

        /// <summary>
        /// Creates a result with a generated key from request body.
        /// </summary>
        public static IdempotencyKeyResult Generated(string key, string requestBodyHash) => new()
        {
            Key = key,
            IsGenerated = true,
            RequestBodyHash = requestBodyHash
        };
    }

    /// <summary>
    /// Interface for generating idempotency keys from HTTP requests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implement this interface to customize how idempotency keys are extracted
    /// or generated from HTTP requests.
    /// </para>
    /// <para>
    /// <strong>Default Behavior:</strong>
    /// The default implementation (<see cref="DefaultIdempotencyKeyGenerator"/>):
    /// <list type="number">
    /// <item>First checks for an Idempotency-Key header</item>
    /// <item>If not found, generates a key from the request method, path, and body hash</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Custom key generator that includes user ID
    /// public class UserScopedIdempotencyKeyGenerator : IIdempotencyKeyGenerator
    /// {
    ///     public async Task&lt;IdempotencyKeyResult&gt; GenerateKeyAsync(HttpContext context)
    ///     {
    ///         var userId = context.User.FindFirst("sub")?.Value ?? "anonymous";
    ///         var headerKey = context.Request.Headers["Idempotency-Key"].ToString();
    ///         
    ///         if (!string.IsNullOrEmpty(headerKey))
    ///         {
    ///             return IdempotencyKeyResult.FromHeader($"{userId}:{headerKey}");
    ///         }
    ///         
    ///         // Fall back to default generation
    ///         return IdempotencyKeyResult.NoKey();
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IIdempotencyKeyGenerator
    {
        /// <summary>
        /// Generates or extracts an idempotency key from the HTTP context.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>The idempotency key result.</returns>
        Task<IdempotencyKeyResult> GenerateKeyAsync(HttpContext context);
    }
}

