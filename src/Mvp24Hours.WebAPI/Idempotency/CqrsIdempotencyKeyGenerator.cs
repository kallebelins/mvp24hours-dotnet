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
using System.Text.Json;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Idempotency
{
    /// <summary>
    /// Idempotency key generator that integrates with the CQRS module's IIdempotentCommand.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This generator extracts idempotency keys from commands that implement
    /// <c>IIdempotentCommand</c> from the CQRS module. It parses the request body
    /// to check for the IdempotencyKey property on the command.
    /// </para>
    /// <para>
    /// <strong>Priority Order:</strong>
    /// <list type="number">
    /// <item>HTTP Header (Idempotency-Key)</item>
    /// <item>IdempotencyKey property from command body</item>
    /// <item>Generated from request method + path + body hash</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Integration with CQRS:</strong>
    /// When a command implements <c>IIdempotentCommand</c> and has a non-null
    /// IdempotencyKey property, that key will be used for HTTP-level idempotency.
    /// This ensures consistency between the WebAPI middleware and the CQRS
    /// <c>IdempotencyBehavior</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Command implementing IIdempotentCommand
    /// public class ProcessPaymentCommand : IMediatorCommand&lt;PaymentResult&gt;, IIdempotentCommand
    /// {
    ///     public Guid PaymentId { get; init; }
    ///     public decimal Amount { get; init; }
    ///     
    ///     // Custom idempotency key
    ///     public string? IdempotencyKey => $"payment:{PaymentId}";
    /// }
    /// 
    /// // Request
    /// POST /api/payments
    /// Content-Type: application/json
    /// 
    /// { "paymentId": "123...", "amount": 100.00, "idempotencyKey": "payment:123..." }
    /// 
    /// // The middleware will extract "payment:123..." as the idempotency key
    /// </code>
    /// </example>
    public class CqrsIdempotencyKeyGenerator : IIdempotencyKeyGenerator
    {
        private readonly IdempotencyOptions _options;
        private readonly ILogger<CqrsIdempotencyKeyGenerator>? _logger;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Creates a new instance of the CQRS-integrated idempotency key generator.
        /// </summary>
        /// <param name="options">Idempotency options.</param>
        /// <param name="logger">Optional logger.</param>
        public CqrsIdempotencyKeyGenerator(
            IOptions<IdempotencyOptions> options,
            ILogger<CqrsIdempotencyKeyGenerator>? logger = null)
        {
            _options = options?.Value ?? new IdempotencyOptions();
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<IdempotencyKeyResult> GenerateKeyAsync(HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            // 1. First check HTTP header
            var headerValue = context.Request.Headers[_options.HeaderName].ToString();
            if (!string.IsNullOrEmpty(headerValue) && IsValidKeyFormat(headerValue))
            {
                _logger?.LogDebug(
                    "[Idempotency] Using idempotency key from header: {Key}",
                    headerValue);

                return IdempotencyKeyResult.FromHeader(headerValue);
            }

            // 2. Try to extract from request body (IIdempotentCommand integration)
            if (_options.IntegrateWithCqrs)
            {
                var commandKey = await ExtractFromCommandBodyAsync(context);
                if (!string.IsNullOrEmpty(commandKey))
                {
                    _logger?.LogDebug(
                        "[Idempotency] Using idempotency key from command body: {Key}",
                        commandKey);

                    var bodyHash = await ComputeBodyHashAsync(context);
                    return IdempotencyKeyResult.Generated($"cqrs:{commandKey}", bodyHash);
                }
            }

            // 3. Fall back to generated key from body hash
            return await GenerateFromBodyAsync(context);
        }

        private async Task<string?> ExtractFromCommandBodyAsync(HttpContext context)
        {
            try
            {
                // Only process JSON content
                if (!context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return null;
                }

                // Enable buffering
                context.Request.EnableBuffering();

                var body = context.Request.Body;
                body.Position = 0;

                using var reader = new StreamReader(body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
                var bodyText = await reader.ReadToEndAsync();
                body.Position = 0;

                if (string.IsNullOrWhiteSpace(bodyText))
                {
                    return null;
                }

                // Try to parse as JSON and look for idempotencyKey property
                using var document = JsonDocument.Parse(bodyText);
                var root = document.RootElement;

                // Check for idempotencyKey property (case-insensitive)
                if (root.TryGetProperty("idempotencyKey", out var keyElement) ||
                    root.TryGetProperty("IdempotencyKey", out keyElement) ||
                    root.TryGetProperty("IDEMPOTENCYKEY", out keyElement))
                {
                    var keyValue = keyElement.GetString();
                    if (!string.IsNullOrEmpty(keyValue) && IsValidKeyFormat(keyValue))
                    {
                        return keyValue;
                    }
                }

                return null;
            }
            catch (JsonException)
            {
                // Not valid JSON - continue with other methods
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex,
                    "[Idempotency] Error extracting key from command body: {Message}",
                    ex.Message);

                return null;
            }
        }

        private async Task<IdempotencyKeyResult> GenerateFromBodyAsync(HttpContext context)
        {
            var bodyHash = await ComputeBodyHashAsync(context);

            if (string.IsNullOrEmpty(bodyHash))
            {
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

        private async Task<string> ComputeBodyHashAsync(HttpContext context)
        {
            try
            {
                context.Request.EnableBuffering();

                var body = context.Request.Body;
                body.Position = 0;

                using var reader = new StreamReader(
                    body,
                    encoding: Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 1024,
                    leaveOpen: true);

                var charBuffer = new char[_options.MaxRequestBodySizeForHashing];
                var charsRead = await reader.ReadAsync(charBuffer, 0, charBuffer.Length);

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
                    "[Idempotency] Error computing body hash: {Message}",
                    ex.Message);

                return string.Empty;
            }
        }

        private static string ComputeHash(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = SHA256.HashData(bytes);
            return Convert.ToBase64String(hashBytes)[..16]
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static bool IsValidKeyFormat(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Length > 256)
            {
                return false;
            }

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

