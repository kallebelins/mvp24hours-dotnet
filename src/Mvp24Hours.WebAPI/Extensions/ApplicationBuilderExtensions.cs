//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Middlewares;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="IApplicationBuilder"/> to add Mvp24Hours WebAPI middleware.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        #region Request Context

        /// <summary>
        /// Adds the request context middleware for Correlation/Causation ID propagation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This middleware establishes request context for distributed tracing:
        /// <list type="bullet">
        /// <item>Extracts or generates Correlation ID</item>
        /// <item>Extracts Causation ID from incoming requests</item>
        /// <item>Generates unique Request ID</item>
        /// <item>Makes context available via HttpContext.Items</item>
        /// <item>Adds context IDs to response headers</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// Call <c>services.AddMvp24HoursRequestContext()</c> to register required services.
        /// </para>
        /// <para>
        /// <strong>Pipeline Position:</strong>
        /// Should be added very early in the pipeline, before any other Mvp24Hours middleware,
        /// to ensure context is available for all downstream components.
        /// </para>
        /// <para>
        /// <strong>Integration with CQRS:</strong>
        /// When using the CQRS module's <c>RequestContextBehavior</c>, it will automatically
        /// pick up the context established by this middleware via <c>IHttpContextAccessor</c>.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // In Program.cs
        /// builder.Services.AddMvp24HoursRequestContext();
        /// 
        /// var app = builder.Build();
        /// app.UseMvp24HoursRequestContext(); // First in pipeline
        /// app.UseMvp24HoursRequestTelemetry();
        /// app.UseMvp24HoursProblemDetails();
        /// app.UseAuthentication();
        /// </code>
        /// </example>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursRequestContext(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestContextMiddleware>();
        }

        #endregion

        #region Request Logging and Telemetry

        /// <summary>
        /// Adds the request/response logging middleware.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This middleware provides structured logging for HTTP requests and responses with:
        /// <list type="bullet">
        /// <item>Configurable logging levels (Basic, Standard, Detailed, Full)</item>
        /// <item>Automatic masking of sensitive headers and body properties</item>
        /// <item>Slow request detection and warning</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// Call <c>services.AddMvp24HoursRequestLogging()</c> to register required services.
        /// </para>
        /// <para>
        /// <strong>Pipeline Position:</strong>
        /// Should be added early in the pipeline, after exception handling but before authentication.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // In Program.cs
        /// builder.Services.AddMvp24HoursRequestLogging(options =>
        /// {
        ///     options.LoggingLevel = RequestLoggingLevel.Standard;
        ///     options.LogRequestHeaders = true;
        /// });
        /// 
        /// var app = builder.Build();
        /// app.UseMvp24HoursProblemDetails(); // Exception handling first
        /// app.UseMvp24HoursRequestLogging(); // Then logging
        /// app.UseAuthentication();
        /// app.UseAuthorization();
        /// </code>
        /// </example>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursRequestLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }

        /// <summary>
        /// Adds the request telemetry middleware for OpenTelemetry-compatible tracing and metrics.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This middleware provides:
        /// <list type="bullet">
        /// <item>Distributed tracing using Activity API</item>
        /// <item>Request/response metrics (counters, histograms, gauges)</item>
        /// <item>Correlation ID propagation</item>
        /// <item>User and tenant context enrichment</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>OpenTelemetry Integration:</strong>
        /// Configure OpenTelemetry to include Mvp24Hours WebAPI activities and metrics:
        /// <code>
        /// builder.Services.AddOpenTelemetry()
        ///     .WithTracing(builder => builder.AddSource("Mvp24Hours.WebAPI"))
        ///     .WithMetrics(builder => builder.AddMeter("Mvp24Hours.WebAPI"));
        /// </code>
        /// </para>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// Call <c>services.AddMvp24HoursRequestTelemetry()</c> to register required services.
        /// </para>
        /// <para>
        /// <strong>Pipeline Position:</strong>
        /// Should be added as early as possible in the pipeline to capture full request duration.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // In Program.cs
        /// builder.Services.AddMvp24HoursRequestTelemetry(options =>
        /// {
        ///     options.EnableTracing = true;
        ///     options.EnableMetrics = true;
        /// });
        /// 
        /// var app = builder.Build();
        /// app.UseMvp24HoursRequestTelemetry(); // First in pipeline
        /// app.UseMvp24HoursProblemDetails();
        /// app.UseAuthentication();
        /// </code>
        /// </example>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursRequestTelemetry(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestTelemetryMiddleware>();
        }

        /// <summary>
        /// Adds both request logging and telemetry middlewares.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is a convenience method that adds telemetry first (for accurate timing)
        /// followed by logging.
        /// </para>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// Call <c>services.AddMvp24HoursRequestObservability()</c> to register required services.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// builder.Services.AddMvp24HoursRequestObservability();
        /// 
        /// var app = builder.Build();
        /// app.UseMvp24HoursRequestObservability();
        /// app.UseMvp24HoursProblemDetails();
        /// </code>
        /// </example>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursRequestObservability(this IApplicationBuilder builder)
        {
            builder.UseMvp24HoursRequestTelemetry();
            builder.UseMvp24HoursRequestLogging();
            return builder;
        }

        /// <summary>
        /// Adds all observability middlewares including request context, telemetry, and logging.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is a convenience method that adds all observability components in the correct order:
        /// <list type="number">
        /// <item>Request Context - Establishes Correlation/Causation IDs</item>
        /// <item>Request Telemetry - OpenTelemetry metrics and tracing</item>
        /// <item>Request Logging - Structured request/response logging</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// <code>
        /// builder.Services.AddMvp24HoursRequestContext();
        /// builder.Services.AddMvp24HoursRequestObservability();
        /// </code>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // In Program.cs
        /// builder.Services.AddMvp24HoursRequestContext();
        /// builder.Services.AddMvp24HoursRequestObservability();
        /// 
        /// var app = builder.Build();
        /// app.UseMvp24HoursFullObservability();
        /// app.UseMvp24HoursProblemDetails();
        /// </code>
        /// </example>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursFullObservability(this IApplicationBuilder builder)
        {
            builder.UseMvp24HoursRequestContext();
            builder.UseMvp24HoursRequestTelemetry();
            builder.UseMvp24HoursRequestLogging();
            return builder;
        }

        #endregion

        #region Exception Handling
        /// <summary>
        /// Adds the legacy exception handling middleware.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This middleware provides basic exception handling with customizable status code mapping.
        /// For RFC 7807 compliant ProblemDetails responses, use <see cref="UseMvp24HoursProblemDetails"/> instead.
        /// </para>
        /// </remarks>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursExceptionHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionMiddleware>();
        }

        #endregion

        #region Correlation ID

        /// <summary>
        /// Adds the ProblemDetails exception handling middleware following RFC 7807.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This middleware catches unhandled exceptions and returns standardized
        /// ProblemDetails responses. It provides:
        /// <list type="bullet">
        /// <item>Automatic mapping of Mvp24Hours exceptions to appropriate HTTP status codes</item>
        /// <item>Detailed validation error responses for <see cref="Core.Exceptions.ValidationException"/></item>
        /// <item>Correlation ID and trace ID in responses</item>
        /// <item>Configurable exception details exposure</item>
        /// </list>
        /// </para>
        /// <para>
        /// Requires prior registration via <see cref="ServiceCollectionExtentions.AddMvp24HoursProblemDetails"/>.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // In Program.cs
        /// builder.Services.AddMvp24HoursProblemDetails(options =>
        /// {
        ///     options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
        /// });
        /// 
        /// var app = builder.Build();
        /// app.UseMvp24HoursProblemDetails();
        /// </code>
        /// </example>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursProblemDetails(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ProblemDetailsMiddleware>();
        }

        /// <summary>
        /// Adds the CORS middleware.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Requires prior configuration via <see cref="ServiceCollectionExtentions.AddMvp24HoursWebCors"/>.
        /// </para>
        /// </remarks>
        #endregion

        #region CORS

        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursCors(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CorsMiddleware>();
        }

        #endregion

        #region Performance and Caching

        /// <summary>
        /// Adds the response compression middleware.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// Call <c>services.AddMvp24HoursCompression()</c> to configure options.
        /// </para>
        /// </remarks>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursResponseCompression(this IApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            return builder.UseResponseCompression();
        }

        /// <summary>
        /// Adds the request decompression middleware.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// Call <c>services.AddMvp24HoursRequestDecompression()</c> to configure options.
        /// </para>
        /// </remarks>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursRequestDecompression(this IApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            return builder.UseMiddleware<RequestDecompressionMiddleware>();
        }

        /// <summary>
        /// Adds the response caching middleware with configurable cache profiles.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// Call <c>services.AddMvp24HoursResponseCaching()</c> to configure options.
        /// </para>
        /// </remarks>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursResponseCaching(this IApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.UseResponseCaching();
            return builder.UseMiddleware<CachingMiddleware>();
        }

        /// <summary>
        /// Adds the output caching middleware (.NET 7+).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>DEPRECATED:</strong> Use <see cref="OutputCachingExtensions.UseMvp24HoursOutputCache"/> instead.
        /// </para>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// Call <c>services.AddMvp24HoursOutputCache()</c> to configure options.
        /// </para>
        /// </remarks>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        [Obsolete("Use UseMvp24HoursOutputCache from OutputCachingExtensions instead. This method will be removed in a future version.")]
        public static IApplicationBuilder UseMvp24HoursOutputCaching(this IApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            // Delegate to the new implementation
            return builder.UseMvp24HoursOutputCache();
        }

        /// <summary>
        /// Adds the ETag middleware for conditional request support.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// Call <c>services.AddMvp24HoursETag()</c> to configure options.
        /// </para>
        /// </remarks>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursETag(this IApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            return builder.UseMiddleware<ETagMiddleware>();
        }

        /// <summary>
        /// Adds the request timeout middleware.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// Call <c>services.AddMvp24HoursRequestTimeout()</c> to configure options.
        /// </para>
        /// </remarks>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursRequestTimeout(this IApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            return builder.UseMiddleware<RequestTimeoutMiddleware>();
        }

        /// <summary>
        /// Adds the Cache-Control header middleware.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// Call <c>services.AddMvp24HoursCacheControl()</c> to configure options.
        /// </para>
        /// </remarks>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursCacheControl(this IApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            return builder.UseMiddleware<CacheControlMiddleware>();
        }

        /// <summary>
        /// Adds the legacy caching middleware (for backward compatibility).
        /// </summary>
        /// <param name="builder">The application builder.</param>
        /// <param name="varyByQueryKeys">Query string keys to vary the cache by.</param>
        /// <returns>The application builder for chaining.</returns>
        [Obsolete("Use UseMvp24HoursResponseCaching instead. This method is kept for backward compatibility.")]
        public static IApplicationBuilder UseMvp24HoursCaching(this IApplicationBuilder builder, params string[] varyByQueryKeys)
        {
            ArgumentNullException.ThrowIfNull(builder);
            // For backward compatibility, create a simple cache profile
            return builder.UseMvp24HoursResponseCaching();
        }

        #endregion

        #region Security

        /// <summary>
        /// Adds the security headers middleware.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This middleware adds essential security headers to HTTP responses including:
        /// <list type="bullet">
        /// <item><strong>HSTS</strong> - Forces HTTPS connections</item>
        /// <item><strong>CSP</strong> - Content Security Policy</item>
        /// <item><strong>X-Frame-Options</strong> - Clickjacking protection</item>
        /// <item><strong>X-Content-Type-Options</strong> - MIME sniffing protection</item>
        /// <item><strong>X-XSS-Protection</strong> - Legacy XSS protection</item>
        /// <item><strong>Referrer-Policy</strong> - Controls referrer information</item>
        /// <item><strong>Permissions-Policy</strong> - Controls browser features</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// Call <c>services.AddMvp24HoursSecurityHeaders()</c> to configure options.
        /// </para>
        /// <para>
        /// <strong>Pipeline Position:</strong>
        /// Should be added early in the pipeline, typically before exception handling.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// builder.Services.AddMvp24HoursSecurityHeaders(options =>
        /// {
        ///     options.EnableHsts = true;
        ///     options.EnableContentSecurityPolicy = true;
        /// });
        /// 
        /// var app = builder.Build();
        /// app.UseMvp24HoursSecurityHeaders();
        /// </code>
        /// </example>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursSecurityHeaders(this IApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            return builder.UseMiddleware<SecurityHeadersMiddleware>();
        }

        /// <summary>
        /// Adds the API Key authentication middleware.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This middleware validates API keys from headers or query strings:
        /// <list type="bullet">
        /// <item>Header-based API key (configurable header name)</item>
        /// <item>Optional query string API key</item>
        /// <item>Multiple valid API keys support</item>
        /// <item>Custom validator function support</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// Call <c>services.AddMvp24HoursApiKeyAuthentication()</c> to configure options.
        /// </para>
        /// <para>
        /// <strong>Pipeline Position:</strong>
        /// Should be added after exception handling but before authorization.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// builder.Services.AddMvp24HoursApiKeyAuthentication(options =>
        /// {
        ///     options.ApiKeys.Add("my-secret-api-key");
        /// });
        /// 
        /// var app = builder.Build();
        /// app.UseMvp24HoursApiKeyAuthentication();
        /// </code>
        /// </example>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursApiKeyAuthentication(this IApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            return builder.UseMiddleware<ApiKeyAuthenticationMiddleware>();
        }

        /// <summary>
        /// Adds the request size limiting middleware.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This middleware enforces request body size limits to prevent DoS attacks:
        /// <list type="bullet">
        /// <item>Global default size limit</item>
        /// <item>Per-endpoint size limits</item>
        /// <item>Per-content-type size limits</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// Call <c>services.AddMvp24HoursRequestSizeLimit()</c> to configure options.
        /// </para>
        /// <para>
        /// <strong>Pipeline Position:</strong>
        /// Should be added early in the pipeline, before body parsing.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// builder.Services.AddMvp24HoursRequestSizeLimit(options =>
        /// {
        ///     options.DefaultMaxBodySize = 10 * 1024 * 1024; // 10MB
        /// });
        /// 
        /// var app = builder.Build();
        /// app.UseMvp24HoursRequestSizeLimit();
        /// </code>
        /// </example>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursRequestSizeLimit(this IApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            return builder.UseMiddleware<RequestSizeLimitMiddleware>();
        }

        /// <summary>
        /// Adds the IP filtering middleware.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This middleware provides IP-based access control:
        /// <list type="bullet">
        /// <item>Whitelist mode - only allow specified IPs</item>
        /// <item>Blacklist mode - block specified IPs</item>
        /// <item>CIDR notation for IP ranges</item>
        /// <item>Path-specific IP rules</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// Call <c>services.AddMvp24HoursIpFiltering()</c> to configure options.
        /// </para>
        /// <para>
        /// <strong>Pipeline Position:</strong>
        /// Should be added very early in the pipeline, before authentication.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// builder.Services.AddMvp24HoursIpFiltering(options =>
        /// {
        ///     options.Enabled = true;
        ///     options.Mode = IpFilteringMode.Whitelist;
        ///     options.WhitelistedIps.Add("192.168.1.0/24");
        /// });
        /// 
        /// var app = builder.Build();
        /// app.UseMvp24HoursIpFiltering();
        /// </code>
        /// </example>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursIpFiltering(this IApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            return builder.UseMiddleware<IpFilteringMiddleware>();
        }

        /// <summary>
        /// Adds the anti-forgery (CSRF) protection middleware for SPAs.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This middleware implements the double-submit cookie pattern:
        /// <list type="bullet">
        /// <item>Sets token in a cookie readable by JavaScript</item>
        /// <item>Validates token from custom header matches cookie</item>
        /// <item>Optionally provides a token endpoint</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// Call <c>services.AddMvp24HoursAntiForgery()</c> to configure options.
        /// </para>
        /// <para>
        /// <strong>Pipeline Position:</strong>
        /// Should be added after authentication but before endpoint routing.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// builder.Services.AddMvp24HoursAntiForgery(options =>
        /// {
        ///     options.CookieName = "XSRF-TOKEN";
        ///     options.HeaderName = "X-XSRF-TOKEN";
        /// });
        /// 
        /// var app = builder.Build();
        /// app.UseMvp24HoursAntiForgery();
        /// </code>
        /// </example>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursAntiForgery(this IApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            return builder.UseMiddleware<AntiForgeryMiddleware>();
        }

        /// <summary>
        /// Adds the input sanitization middleware.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This middleware validates and sanitizes input to protect against:
        /// <list type="bullet">
        /// <item>XSS (Cross-Site Scripting) attacks</item>
        /// <item>SQL Injection patterns</item>
        /// <item>Path Traversal attempts</item>
        /// <item>Command Injection patterns</item>
        /// <item>LDAP Injection patterns</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// Call <c>services.AddMvp24HoursInputSanitization()</c> to configure options.
        /// </para>
        /// <para>
        /// <strong>Pipeline Position:</strong>
        /// Should be added early in the pipeline, before body parsing.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// builder.Services.AddMvp24HoursInputSanitization(options =>
        /// {
        ///     options.Mode = SanitizationMode.Validate;
        ///     options.EnableXssSanitization = true;
        /// });
        /// 
        /// var app = builder.Build();
        /// app.UseMvp24HoursInputSanitization();
        /// </code>
        /// </example>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursInputSanitization(this IApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            return builder.UseMiddleware<InputSanitizationMiddleware>();
        }

        /// <summary>
        /// Adds all security middlewares in the recommended order.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is a convenience method that adds all security middlewares:
        /// <list type="number">
        /// <item>Security Headers - adds protection headers</item>
        /// <item>IP Filtering - blocks unauthorized IPs</item>
        /// <item>Request Size Limit - prevents oversized payloads</item>
        /// <item>Input Sanitization - detects malicious input</item>
        /// <item>Anti-Forgery - protects against CSRF</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// Call <c>services.AddMvp24HoursSecurity()</c> to configure all options.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// builder.Services.AddMvp24HoursSecurity();
        /// 
        /// var app = builder.Build();
        /// app.UseMvp24HoursSecurity();
        /// </code>
        /// </example>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursSecurity(this IApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            
            builder.UseMvp24HoursSecurityHeaders();
            builder.UseMvp24HoursIpFiltering();
            builder.UseMvp24HoursRequestSizeLimit();
            builder.UseMvp24HoursInputSanitization();
            builder.UseMvp24HoursAntiForgery();
            
            return builder;
        }

        #endregion

        #region Swagger

        /// <summary>
        /// Adds Swagger UI middleware.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Requires prior configuration via <see cref="ServiceCollectionExtentions.AddMvp24HoursWebSwagger"/>.
        /// </para>
        /// </remarks>
        /// <param name="builder">The application builder.</param>
        /// <param name="name">The display name for the API documentation.</param>
        /// <param name="version">The API version (default: "v1").</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursSwagger(this IApplicationBuilder builder, string name, string version = "v1")
        {
            builder.UseSwagger();
            builder.UseSwaggerUI(opt =>
            {
                string swaggerJsonBasePath = string.IsNullOrWhiteSpace(opt.RoutePrefix) ? "." : "..";
                opt.SwaggerEndpoint($"{swaggerJsonBasePath}/swagger/{version}/swagger.json", name);
            });
            return builder;
        }

        /// <summary>
        /// Adds Swagger UI middleware with support for multiple API versions.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method configures Swagger UI to display all API versions discovered via API versioning.
        /// Requires prior configuration via <see cref="ServiceCollectionExtentions.AddMvp24HoursSwaggerWithVersioning"/>.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // In Program.cs
        /// services.AddMvp24HoursApiVersioning();
        /// services.AddMvp24HoursSwaggerWithVersioning(options => { ... });
        /// 
        /// var app = builder.Build();
        /// app.UseMvp24HoursSwaggerWithVersioning("My API");
        /// </code>
        /// </example>
        /// <param name="builder">The application builder.</param>
        /// <param name="apiTitle">The API title.</param>
        /// <param name="swaggerRoutePrefix">The Swagger UI route prefix (default: "swagger").</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursSwaggerWithVersioning(
            this IApplicationBuilder builder,
            string apiTitle,
            string swaggerRoutePrefix = "swagger")
        {
            builder.UseSwagger();

            var apiVersionDescriptionProvider = builder.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>();

            builder.UseSwaggerUI(options =>
            {
                options.RoutePrefix = swaggerRoutePrefix;

                // Add endpoint for each API version
                foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions.OrderByDescending(d => d.ApiVersion))
                {
                    var versionInfo = description.IsDeprecated 
                        ? $"{description.GroupName} (Deprecated)" 
                        : description.GroupName;

                    options.SwaggerEndpoint(
                        $"/swagger/{description.GroupName}/swagger.json",
                        $"{apiTitle} {versionInfo}");
                }

                // Enable deep linking
                options.EnableDeepLinking();
                options.EnableFilter();
                options.EnableTryItOutByDefault();
                options.DisplayRequestDuration();
            });

            return builder;
        }

        /// <summary>
        /// Adds ReDoc UI middleware for API documentation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// ReDoc provides an alternative, more readable documentation interface.
        /// Requires prior configuration via <see cref="ServiceCollectionExtentions.AddMvp24HoursSwaggerWithVersioning"/>.
        /// </para>
        /// <para>
        /// This implementation serves ReDoc via static HTML that loads the OpenAPI specification.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // In Program.cs
        /// services.AddMvp24HoursSwaggerWithVersioning(options =>
        /// {
        ///     options.EnableReDoc = true;
        ///     options.ReDocRoutePrefix = "redoc";
        /// });
        /// 
        /// var app = builder.Build();
        /// app.UseMvp24HoursReDoc("My API");
        /// </code>
        /// </example>
        /// <param name="builder">The application builder.</param>
        /// <param name="apiTitle">The API title.</param>
        /// <param name="reDocRoutePrefix">The ReDoc route prefix (default: "redoc").</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursReDoc(
            this IApplicationBuilder builder,
            string apiTitle,
            string reDocRoutePrefix = "redoc")
        {
            var apiVersionDescriptionProvider = builder.ApplicationServices.GetService<IApiVersionDescriptionProvider>();

            if (apiVersionDescriptionProvider != null)
            {
                // Use the latest non-deprecated version, or the latest version if all are deprecated
                var latestVersion = apiVersionDescriptionProvider.ApiVersionDescriptions
                    .OrderByDescending(d => d.ApiVersion)
                    .FirstOrDefault();

                if (latestVersion != null)
                {
                    var specUrl = $"/swagger/{latestVersion.GroupName}/swagger.json";
                    
                    // Map ReDoc route to serve HTML
                    builder.Map($"/{reDocRoutePrefix}", appBuilder =>
                    {
                        appBuilder.Run(async context =>
                        {
                            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>{apiTitle} - ReDoc</title>
    <meta charset=""utf-8""/>
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <link href=""https://fonts.googleapis.com/css?family=Montserrat:300,400,700|Roboto:300,400,700"" rel=""stylesheet"">
    <style>
        body {{
            margin: 0;
            padding: 0;
        }}
    </style>
</head>
<body>
    <redoc spec-url=""{specUrl}""
            scroll-y-offset=""10""
            hide-hostname=""
            hide-download-button=""
            expand-responses=""200,201""
            required-props-first=""
            sort-props-alphabetically=""></redoc>
    <script src=""https://cdn.redoc.ly/redoc/latest/bundles/redoc.standalone.js""></script>
</body>
</html>";
                            context.Response.ContentType = "text/html";
                            await context.Response.WriteAsync(html);
                        });
                    });
                }
            }

            return builder;
        }

        #endregion

        #region Rate Limiting

        /// <summary>
        /// Adds the rate limiting middleware using .NET 7+ built-in RateLimiter.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This middleware provides rate limiting with support for:
        /// <list type="bullet">
        /// <item><strong>Fixed Window</strong> - Counts requests in fixed time windows</item>
        /// <item><strong>Sliding Window</strong> - Smooths fixed window boundaries</item>
        /// <item><strong>Token Bucket</strong> - Allows controlled bursts with smooth refill</item>
        /// <item><strong>Concurrency</strong> - Limits concurrent requests</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Rate Limit Headers:</strong>
        /// When enabled, the middleware adds the following headers to responses:
        /// <list type="bullet">
        /// <item><c>X-RateLimit-Limit</c> - The maximum number of requests allowed</item>
        /// <item><c>X-RateLimit-Remaining</c> - The number of requests remaining in the window</item>
        /// <item><c>X-RateLimit-Reset</c> - The time when the rate limit resets (Unix timestamp)</item>
        /// <item><c>Retry-After</c> - Seconds to wait before retrying (when rate limited)</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// Call <c>services.AddMvp24HoursRateLimiting()</c> to configure rate limiting options.
        /// </para>
        /// <para>
        /// <strong>Pipeline Position:</strong>
        /// Should be added early in the pipeline, after exception handling and logging,
        /// but before authentication and authorization.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // In Program.cs
        /// builder.Services.AddMvp24HoursRateLimiting(options =>
        /// {
        ///     options.AddSlidingWindowPolicy("default", 100, TimeSpan.FromMinutes(1));
        ///     options.IncludeRateLimitHeaders = true;
        /// });
        /// 
        /// var app = builder.Build();
        /// 
        /// app.UseMvp24HoursProblemDetails();
        /// app.UseMvp24HoursRateLimiting(); // After exception handling
        /// app.UseAuthentication();
        /// app.UseAuthorization();
        /// app.MapControllers();
        /// </code>
        /// </example>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursRateLimiting(this IApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            return builder.UseMiddleware<RateLimitingMiddleware>();
        }

        /// <summary>
        /// Conditionally adds the rate limiting middleware.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this method when you want to conditionally enable rate limiting,
        /// for example, only in production environments.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Only enable rate limiting in production
        /// app.UseMvp24HoursRateLimiting(app.Environment.IsProduction());
        /// </code>
        /// </example>
        /// <param name="builder">The application builder.</param>
        /// <param name="enabled">Whether to enable rate limiting.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursRateLimiting(this IApplicationBuilder builder, bool enabled)
        {
            if (enabled)
            {
                return builder.UseMvp24HoursRateLimiting();
            }
            return builder;
        }

        #endregion

        #region Idempotency

        /// <summary>
        /// Adds the idempotency middleware for POST, PUT, and PATCH requests.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This middleware ensures that duplicate requests with the same idempotency key
        /// return the cached response instead of re-executing the request.
        /// </para>
        /// <para>
        /// <strong>Response Headers:</strong>
        /// <list type="bullet">
        /// <item><strong>Idempotency-Key</strong> - Echo of the request's idempotency key</item>
        /// <item><strong>Idempotency-Replayed</strong> - Set to "true" when returning cached response</item>
        /// <item><strong>Retry-After</strong> - Seconds to wait when a duplicate request is in-flight</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// Call <c>services.AddMvp24HoursIdempotency()</c> to register required services.
        /// </para>
        /// <para>
        /// <strong>Pipeline Position:</strong>
        /// Should be added after authentication and authorization, but before controllers.
        /// This ensures the user context is available for idempotency key generation.
        /// </para>
        /// <para>
        /// <strong>Integration with CQRS:</strong>
        /// When configured with <c>IntegrateWithCqrs = true</c>, the middleware will
        /// look for <c>IdempotencyKey</c> property in the request body, matching the
        /// <c>IIdempotentCommand</c> interface from the CQRS module.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // In Program.cs
        /// builder.Services.AddMvp24HoursIdempotency(options =>
        /// {
        ///     options.HeaderName = "Idempotency-Key";
        ///     options.CacheDuration = TimeSpan.FromHours(24);
        /// });
        /// 
        /// var app = builder.Build();
        /// app.UseMvp24HoursRequestContext();
        /// app.UseAuthentication();
        /// app.UseAuthorization();
        /// app.UseMvp24HoursIdempotency(); // After auth, before controllers
        /// app.MapControllers();
        /// </code>
        /// </example>
        /// <example>
        /// <code>
        /// // Client usage
        /// POST /api/payments
        /// Idempotency-Key: payment-123-create
        /// Content-Type: application/json
        /// 
        /// { "amount": 100.00, "customerId": "cust-456" }
        /// 
        /// // First request: processes payment, returns 201
        /// // Duplicate request: returns cached 201 with Idempotency-Replayed: true
        /// // Concurrent request: returns 409 with Retry-After: 1
        /// </code>
        /// </example>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursIdempotency(this IApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            return builder.UseMiddleware<IdempotencyMiddleware>();
        }

        /// <summary>
        /// Conditionally adds the idempotency middleware.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this method when you want to conditionally enable idempotency,
        /// for example, only in production environments.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Only enable idempotency in production
        /// app.UseMvp24HoursIdempotency(app.Environment.IsProduction());
        /// </code>
        /// </example>
        /// <param name="builder">The application builder.</param>
        /// <param name="enabled">Whether to enable idempotency.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursIdempotency(this IApplicationBuilder builder, bool enabled)
        {
            if (enabled)
            {
                return builder.UseMvp24HoursIdempotency();
            }
            return builder;
        }

        #endregion

        #region Health Checks

        /// <summary>
        /// Maps health check endpoints with Mvp24Hours configuration.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method maps three health check endpoints:
        /// <list type="bullet">
        /// <item><strong>/health</strong> - Overall health status (all checks)</item>
        /// <item><strong>/health/ready</strong> - Readiness probe (checks with "ready" tag)</item>
        /// <item><strong>/health/live</strong> - Liveness probe (checks with "live" tag)</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// Call <c>services.AddMvp24HoursHealthChecks()</c> to register health checks.
        /// </para>
        /// <para>
        /// <strong>Pipeline Position:</strong>
        /// Should be added before <c>MapControllers()</c> but after routing.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // In Program.cs
        /// builder.Services.AddMvp24HoursHealthChecks(options =>
        /// {
        ///     options.HealthPath = "/health";
        ///     options.ReadinessPath = "/health/ready";
        ///     options.LivenessPath = "/health/live";
        /// });
        /// 
        /// var app = builder.Build();
        /// app.UseRouting();
        /// app.UseMvp24HoursHealthChecks(); // Map health check endpoints
        /// app.MapControllers();
        /// </code>
        /// </example>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursHealthChecks(this IApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            var options = builder.ApplicationServices.GetService<Microsoft.Extensions.Options.IOptions<Configuration.HealthCheckOptions>>()?.Value
                ?? new Configuration.HealthCheckOptions();

            var healthCheckOptions = new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = check => options.HealthTags.Count == 0 || 
                                     options.HealthTags.Any(tag => check.Tags.Contains(tag)),
                AllowCachingResponses = false,
                ResponseWriter = async (context, result) =>
                {
                    context.Response.ContentType = "application/json";

                    var response = new
                    {
                        status = result.Status.ToString(),
                        totalDuration = result.TotalDuration.TotalMilliseconds,
                        entries = result.Entries.Select(e => new
                        {
                            name = e.Key,
                            status = e.Value.Status.ToString(),
                            description = e.Value.Description,
                            duration = e.Value.Duration.TotalMilliseconds,
                            data = options.EnableDetailedResponses ? e.Value.Data : null,
                            exception = options.IncludeExceptionDetails && e.Value.Exception != null
                                ? e.Value.Exception.Message
                                : null
                        })
                    };

                    var json = System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                    await context.Response.WriteAsync(json);
                }
            };

            var readinessOptions = new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = check => options.ReadinessTags.Count == 0 || 
                                     options.ReadinessTags.Any(tag => check.Tags.Contains(tag)),
                AllowCachingResponses = false,
                ResponseWriter = healthCheckOptions.ResponseWriter
            };

            var livenessOptions = new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = check => options.LivenessTags.Count == 0 || 
                                     options.LivenessTags.Any(tag => check.Tags.Contains(tag)),
                AllowCachingResponses = false,
                ResponseWriter = healthCheckOptions.ResponseWriter
            };

            // Map health check endpoints using endpoint routing
            // Note: This requires UseRouting() to be called before this method
            var endpointRouteBuilder = builder as Microsoft.AspNetCore.Routing.IEndpointRouteBuilder;
            if (endpointRouteBuilder != null)
            {
                endpointRouteBuilder.MapHealthChecks(options.HealthPath, healthCheckOptions);
                endpointRouteBuilder.MapHealthChecks(options.ReadinessPath, readinessOptions);
                endpointRouteBuilder.MapHealthChecks(options.LivenessPath, livenessOptions);

                // Map Health Check UI if enabled
                if (options.EnableUI)
                {
                    // Health Check UI requires AspNetCore.HealthChecks.UI package
                    // This is a placeholder - the actual UI mapping should be done by the application
                    // if they have the package installed
                    endpointRouteBuilder.MapHealthChecks(options.UIPath, new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
                    {
                        Predicate = _ => true,
                        ResponseWriter = healthCheckOptions.ResponseWriter
                    });
                }
            }
            else
            {
                // Fallback: Use endpoint routing via Map
                builder.Map(options.HealthPath, app =>
                {
                    var routeBuilder = app as Microsoft.AspNetCore.Routing.IEndpointRouteBuilder;
                    routeBuilder?.MapHealthChecks("", healthCheckOptions);
                });
                builder.Map(options.ReadinessPath, app =>
                {
                    var routeBuilder = app as Microsoft.AspNetCore.Routing.IEndpointRouteBuilder;
                    routeBuilder?.MapHealthChecks("", readinessOptions);
                });
                builder.Map(options.LivenessPath, app =>
                {
                    var routeBuilder = app as Microsoft.AspNetCore.Routing.IEndpointRouteBuilder;
                    routeBuilder?.MapHealthChecks("", livenessOptions);
                });
            }

            return builder;
        }

        #endregion

        #region Content Negotiation

        /// <summary>
        /// Adds the content negotiation middleware for supporting multiple response formats.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This middleware performs content negotiation based on:
        /// <list type="bullet">
        /// <item><strong>Accept header</strong> - With quality value support (q=0.9)</item>
        /// <item><strong>Format parameter</strong> - ?format=json or ?format=xml</item>
        /// <item><strong>URL suffix</strong> - .json or .xml (when enabled)</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Supported Formats:</strong>
        /// <list type="bullet">
        /// <item><strong>JSON</strong> - application/json, text/json</item>
        /// <item><strong>XML</strong> - application/xml, text/xml</item>
        /// <item><strong>ProblemDetails</strong> - application/problem+json, application/problem+xml</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// Call <c>services.AddMvp24HoursContentNegotiation()</c> to register required services.
        /// </para>
        /// <para>
        /// <strong>Pipeline Position:</strong>
        /// Should be added early in the pipeline, after exception handling but before authentication.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // In Program.cs
        /// builder.Services.AddMvp24HoursContentNegotiation(options =>
        /// {
        ///     options.DefaultMediaType = "application/json";
        ///     options.RespectQualityValues = true;
        /// });
        /// 
        /// var app = builder.Build();
        /// app.UseMvp24HoursProblemDetails();
        /// app.UseMvp24HoursContentNegotiation(); // After exception handling
        /// app.UseAuthentication();
        /// </code>
        /// </example>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursContentNegotiation(this IApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            return builder.UseMiddleware<ContentNegotiationMiddleware>();
        }

        /// <summary>
        /// Conditionally adds the content negotiation middleware.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this method when you want to conditionally enable content negotiation.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Only enable content negotiation in certain environments
        /// app.UseMvp24HoursContentNegotiation(app.Environment.IsProduction());
        /// </code>
        /// </example>
        /// <param name="builder">The application builder.</param>
        /// <param name="enabled">Whether to enable content negotiation.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseMvp24HoursContentNegotiation(this IApplicationBuilder builder, bool enabled)
        {
            if (enabled)
            {
                return builder.UseMvp24HoursContentNegotiation();
            }
            return builder;
        }

        #endregion
    }
}
