//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Idempotency;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Middlewares
{
    /// <summary>
    /// Middleware that provides HTTP-level idempotency for POST, PUT, and PATCH requests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This middleware ensures that duplicate requests with the same idempotency key
    /// return the cached response instead of re-executing the request. This is critical
    /// for preventing duplicate operations in scenarios like payment processing.
    /// </para>
    /// <para>
    /// <strong>Flow:</strong>
    /// <list type="number">
    /// <item>Check if request method is idempotent (POST, PUT, PATCH by default)</item>
    /// <item>Extract or generate idempotency key</item>
    /// <item>Try to acquire lock for the key</item>
    /// <item>If existing completed response found, return cached response</item>
    /// <item>If request is in-flight, return 409 Conflict with Retry-After</item>
    /// <item>If lock acquired, execute request and cache response</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Integration with CQRS:</strong>
    /// This middleware works alongside the <c>IdempotencyBehavior</c> from the CQRS module.
    /// The WebAPI middleware handles HTTP-level idempotency (headers, responses),
    /// while the CQRS behavior handles command-level idempotency.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In Program.cs or Startup.cs
    /// app.UseMvp24HoursIdempotency();
    /// 
    /// // Client usage
    /// POST /api/orders
    /// Idempotency-Key: order-12345-create
    /// Content-Type: application/json
    /// 
    /// { "customerId": "123", "items": [...] }
    /// </code>
    /// </example>
    public class IdempotencyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IdempotencyOptions _options;
        private readonly ILogger<IdempotencyMiddleware> _logger;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Initializes a new instance of <see cref="IdempotencyMiddleware"/>.
        /// </summary>
        public IdempotencyMiddleware(
            RequestDelegate next,
            IOptions<IdempotencyOptions> options,
            ILogger<IdempotencyMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Invokes the middleware.
        /// </summary>
        public async Task InvokeAsync(
            HttpContext context,
            IIdempotencyStore store,
            IIdempotencyKeyGenerator keyGenerator)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(store);
            ArgumentNullException.ThrowIfNull(keyGenerator);

            // Check if idempotency is enabled
            if (!_options.Enabled)
            {
                await _next(context);
                return;
            }

            // Check if method should be idempotent
            if (!ShouldProcessIdempotency(context))
            {
                await _next(context);
                return;
            }

            // Generate or extract idempotency key
            var keyResult = await keyGenerator.GenerateKeyAsync(context);

            // Check if key is required but missing
            if (!keyResult.HasKey)
            {
                if (IsIdempotencyKeyRequired(context))
                {
                    await WriteMissingKeyResponse(context);
                    return;
                }

                // No key and not required - proceed without idempotency
                await _next(context);
                return;
            }

            var key = keyResult.Key!;
            var correlationId = context.TraceIdentifier;

            if (_options.EnableLogging)
            {
                _logger.LogDebug(
                    "[Idempotency] Processing request with key {Key}. Path: {Path}, Method: {Method}",
                    key, context.Request.Path, context.Request.Method);
            }

            // Try to acquire lock
            var lockResult = await store.TryAcquireLockAsync(
                key,
                context.Request.Path.Value ?? "/",
                context.Request.Method,
                keyResult.RequestBodyHash,
                _options.CacheDuration,
                correlationId,
                context.RequestAborted);

            if (!lockResult.Acquired)
            {
                if (lockResult.HasCachedResponse)
                {
                    // Return cached response
                    await WriteReplayedResponse(context, lockResult.ExistingRecord!);
                    return;
                }

                if (lockResult.IsInFlight)
                {
                    // Request is being processed
                    await WriteInFlightResponse(context, key);
                    return;
                }
            }

            // Lock acquired - execute request and capture response
            await ExecuteAndCacheResponse(context, store, key);
        }

        private bool ShouldProcessIdempotency(HttpContext context)
        {
            // Check HTTP method
            if (!_options.IdempotentMethods.Contains(context.Request.Method))
            {
                return false;
            }

            // Check excluded paths
            var path = context.Request.Path.Value ?? "/";
            foreach (var pattern in _options.ExcludedPaths)
            {
                if (PathMatches(path, pattern))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsIdempotencyKeyRequired(HttpContext context)
        {
            if (_options.RequireIdempotencyKey)
            {
                return true;
            }

            var path = context.Request.Path.Value ?? "/";
            foreach (var pattern in _options.RequiredPaths)
            {
                if (PathMatches(path, pattern))
                {
                    return true;
                }
            }

            return false;
        }

        private async Task ExecuteAndCacheResponse(
            HttpContext context,
            IIdempotencyStore store,
            string key)
        {
            // Capture the original response body
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                // Execute the request
                await _next(context);

                // Check if response should be cached
                var statusCode = context.Response.StatusCode;
                if (_options.NonCacheableStatusCodes.Contains(statusCode))
                {
                    // Don't cache server errors
                    await store.FailAsync(key, removeRecord: true, context.RequestAborted);
                }
                else
                {
                    // Cache the response
                    responseBody.Position = 0;
                    var body = responseBody.ToArray();
                    var contentType = context.Response.ContentType ?? "application/json";

                    string? headersJson = null;
                    if (_options.CacheResponseHeaders)
                    {
                        headersJson = SerializeHeaders(context.Response.Headers);
                    }

                    await store.CompleteAsync(
                        key,
                        statusCode,
                        body,
                        contentType,
                        headersJson,
                        context.RequestAborted);

                    if (_options.EnableLogging)
                    {
                        _logger.LogInformation(
                            "[Idempotency] Cached response for key {Key}. StatusCode: {StatusCode}",
                            key, statusCode);
                    }
                }

                // Add idempotency key to response
                if (_options.IncludeKeyInResponse)
                {
                    context.Response.Headers[_options.HeaderName] = key;
                }

                // Copy the captured response to the original stream
                responseBody.Position = 0;
                await responseBody.CopyToAsync(originalBodyStream, context.RequestAborted);
            }
            catch (Exception ex)
            {
                // Release the lock on error
                await store.FailAsync(key, removeRecord: true, context.RequestAborted);

                _logger.LogError(ex,
                    "[Idempotency] Error processing request with key {Key}: {Message}",
                    key, ex.Message);

                throw;
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }

        private async Task WriteReplayedResponse(HttpContext context, IdempotencyRecord record)
        {
            if (_options.EnableLogging)
            {
                _logger.LogInformation(
                    "[Idempotency] Replaying cached response for key {Key}. StatusCode: {StatusCode}",
                    record.Key, record.StatusCode);
            }

            context.Response.StatusCode = record.StatusCode;
            context.Response.ContentType = record.ContentType;

            // Add idempotency headers
            if (_options.IncludeKeyInResponse)
            {
                context.Response.Headers[_options.HeaderName] = record.Key;
            }
            context.Response.Headers[_options.ReplayedHeaderName] = "true";

            // Restore cached headers
            if (_options.CacheResponseHeaders && !string.IsNullOrEmpty(record.ResponseHeadersJson))
            {
                RestoreHeaders(context.Response.Headers, record.ResponseHeadersJson);
            }

            // Write response body
            if (record.ResponseBody.Length > 0)
            {
                await context.Response.Body.WriteAsync(record.ResponseBody, context.RequestAborted);
            }
        }

        private async Task WriteInFlightResponse(HttpContext context, string key)
        {
            if (_options.EnableLogging)
            {
                _logger.LogWarning(
                    "[Idempotency] Request in-flight for key {Key}. Returning conflict.",
                    key);
            }

            context.Response.StatusCode = _options.InFlightStatusCode;
            context.Response.Headers[_options.RetryAfterHeaderName] = _options.InFlightRetryAfterSeconds.ToString();

            if (_options.IncludeKeyInResponse)
            {
                context.Response.Headers[_options.HeaderName] = key;
            }

            if (_options.UseProblemDetails)
            {
                var problemDetails = new ProblemDetails
                {
                    Type = "https://httpstatuses.com/409",
                    Title = "Request In-Flight",
                    Status = _options.InFlightStatusCode,
                    Detail = _options.InFlightMessage,
                    Instance = context.Request.Path
                };

                problemDetails.Extensions["idempotencyKey"] = key;
                problemDetails.Extensions["retryAfter"] = _options.InFlightRetryAfterSeconds;

                if (!string.IsNullOrEmpty(context.TraceIdentifier))
                {
                    problemDetails.Extensions["traceId"] = context.TraceIdentifier;
                }

                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, JsonOptions), context.RequestAborted);
            }
            else
            {
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync(_options.InFlightMessage, context.RequestAborted);
            }
        }

        private async Task WriteMissingKeyResponse(HttpContext context)
        {
            if (_options.EnableLogging)
            {
                _logger.LogWarning(
                    "[Idempotency] Missing required idempotency key for {Path}",
                    context.Request.Path);
            }

            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            if (_options.UseProblemDetails)
            {
                var problemDetails = new ProblemDetails
                {
                    Type = "https://httpstatuses.com/400",
                    Title = "Missing Idempotency Key",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = _options.MissingKeyMessage,
                    Instance = context.Request.Path
                };

                problemDetails.Extensions["requiredHeader"] = _options.HeaderName;

                if (!string.IsNullOrEmpty(context.TraceIdentifier))
                {
                    problemDetails.Extensions["traceId"] = context.TraceIdentifier;
                }

                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, JsonOptions), context.RequestAborted);
            }
            else
            {
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync(_options.MissingKeyMessage, context.RequestAborted);
            }
        }

        private static bool PathMatches(string path, string pattern)
        {
            if (pattern.EndsWith("*"))
            {
                return path.StartsWith(pattern.TrimEnd('*'), StringComparison.OrdinalIgnoreCase);
            }
            return path.Equals(pattern, StringComparison.OrdinalIgnoreCase);
        }

        private static string? SerializeHeaders(IHeaderDictionary headers)
        {
            var headerDict = new Dictionary<string, string[]>();
            foreach (var header in headers)
            {
                // Skip certain headers that shouldn't be cached
                if (header.Key.StartsWith("X-", StringComparison.OrdinalIgnoreCase) ||
                    header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    headerDict[header.Key] = header.Value.ToArray();
                }
            }

            if (headerDict.Count == 0)
            {
                return null;
            }

            return JsonSerializer.Serialize(headerDict, JsonOptions);
        }

        private static void RestoreHeaders(IHeaderDictionary headers, string headersJson)
        {
            try
            {
                var cachedHeaders = JsonSerializer.Deserialize<Dictionary<string, string[]>>(headersJson, JsonOptions);
                if (cachedHeaders != null)
                {
                    foreach (var header in cachedHeaders)
                    {
                        // Don't overwrite existing headers
                        if (!headers.ContainsKey(header.Key))
                        {
                            headers[header.Key] = header.Value;
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors when restoring headers
            }
        }
    }
}

