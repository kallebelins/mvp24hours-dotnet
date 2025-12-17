//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Idempotency
{
    /// <summary>
    /// Default implementation of idempotency key generator.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation follows the configured <see cref="IdempotencyKeySource"/>:
    /// <list type="bullet">
    /// <item><strong>Header</strong> - Extracts key from Idempotency-Key header</item>
    /// <item><strong>RequestBody</strong> - Generates key from method + path + body hash</item>
    /// <item><strong>HeaderOrRequestBody</strong> - Uses header if present, falls back to body hash</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Key Format:</strong>
    /// Generated keys follow the format: <c>{method}:{path}:{bodyHash}</c>
    /// </para>
    /// </remarks>
    public class DefaultIdempotencyKeyGenerator : IIdempotencyKeyGenerator
    {
        private readonly IdempotencyOptions _options;
        private readonly ILogger<DefaultIdempotencyKeyGenerator>? _logger;

        /// <summary>
        /// Creates a new instance of the default idempotency key generator.
        /// </summary>
        /// <param name="options">Idempotency options.</param>
        /// <param name="logger">Optional logger.</param>
        public DefaultIdempotencyKeyGenerator(
            IOptions<IdempotencyOptions> options,
            ILogger<DefaultIdempotencyKeyGenerator>? logger = null)
        {
            _options = options?.Value ?? new IdempotencyOptions();
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<IdempotencyKeyResult> GenerateKeyAsync(HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            return _options.KeySource switch
            {
                IdempotencyKeySource.Header => ExtractFromHeader(context),
                IdempotencyKeySource.RequestBody => await GenerateFromBodyAsync(context),
                IdempotencyKeySource.HeaderOrRequestBody => await ExtractOrGenerateAsync(context),
                IdempotencyKeySource.Custom => IdempotencyKeyResult.NoKey(), // Custom generator should be used
                _ => IdempotencyKeyResult.NoKey()
            };
        }

        private IdempotencyKeyResult ExtractFromHeader(HttpContext context)
        {
            var headerValue = context.Request.Headers[_options.HeaderName].ToString();

            if (string.IsNullOrEmpty(headerValue))
            {
                _logger?.LogDebug(
                    "[Idempotency] No idempotency key found in header {HeaderName}",
                    _options.HeaderName);

                return IdempotencyKeyResult.NoKey();
            }

            // Validate the key format (prevent injection attacks)
            if (!IsValidKeyFormat(headerValue))
            {
                _logger?.LogWarning(
                    "[Idempotency] Invalid idempotency key format: {Key}",
                    headerValue);

                return IdempotencyKeyResult.NoKey();
            }

            _logger?.LogDebug(
                "[Idempotency] Extracted idempotency key from header: {Key}",
                headerValue);

            return IdempotencyKeyResult.FromHeader(headerValue);
        }

        private async Task<IdempotencyKeyResult> GenerateFromBodyAsync(HttpContext context)
        {
            var bodyHash = await ComputeBodyHashAsync(context);

            if (string.IsNullOrEmpty(bodyHash))
            {
                // Empty body - generate from method and path only
                bodyHash = "empty";
            }

            var method = context.Request.Method.ToUpperInvariant();
            var path = context.Request.Path.Value ?? "/";
            var key = $"{method}:{path}:{bodyHash}";

            _logger?.LogDebug(
                "[Idempotency] Generated idempotency key from body: {Key}",
                key);

            return IdempotencyKeyResult.Generated(key, bodyHash);
        }

        private async Task<IdempotencyKeyResult> ExtractOrGenerateAsync(HttpContext context)
        {
            // First try header
            var headerResult = ExtractFromHeader(context);
            if (headerResult.HasKey)
            {
                return headerResult;
            }

            // Fall back to body generation
            return await GenerateFromBodyAsync(context);
        }

        private async Task<string> ComputeBodyHashAsync(HttpContext context)
        {
            try
            {
                // Enable buffering so the body can be read multiple times
                context.Request.EnableBuffering();

                var body = context.Request.Body;
                body.Position = 0;

                using var reader = new StreamReader(
                    body,
                    encoding: Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 1024,
                    leaveOpen: true);

                // Read up to max size
                var charBuffer = new char[_options.MaxRequestBodySizeForHashing];
                var charsRead = await reader.ReadAsync(charBuffer, 0, charBuffer.Length);

                // Reset the body position for subsequent reads
                body.Position = 0;

                if (charsRead == 0)
                {
                    return string.Empty;
                }

                var content = new string(charBuffer, 0, charsRead);
                return ComputeHash(content);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex,
                    "[Idempotency] Error reading request body for hash: {Message}",
                    ex.Message);

                return string.Empty;
            }
        }

        private static string ComputeHash(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = SHA256.HashData(bytes);
            // Use first 16 characters of base64 for reasonable uniqueness
            return Convert.ToBase64String(hashBytes)[..16]
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static bool IsValidKeyFormat(string key)
        {
            // Key should be non-empty, reasonable length, and contain safe characters
            if (string.IsNullOrWhiteSpace(key) || key.Length > 256)
            {
                return false;
            }

            // Allow alphanumeric, dash, underscore, colon, and period
            foreach (var c in key)
            {
                if (!char.IsLetterOrDigit(c) && c != '-' && c != '_' && c != ':' && c != '.')
                {
                    return false;
                }
            }

            return true;
        }
    }
}

