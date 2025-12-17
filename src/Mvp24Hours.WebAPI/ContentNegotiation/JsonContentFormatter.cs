//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.ContentNegotiation
{
    /// <summary>
    /// JSON content formatter using System.Text.Json.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This formatter serializes objects to JSON format following the configured options.
    /// It supports both regular content and ProblemDetails with RFC 7807 compliance.
    /// </para>
    /// </remarks>
    public class JsonContentFormatter : IContentFormatter, IProblemDetailsFormatter
    {
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ContentNegotiationOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonContentFormatter"/> class.
        /// </summary>
        public JsonContentFormatter()
            : this(new ContentNegotiationOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonContentFormatter"/> class.
        /// </summary>
        /// <param name="options">The content negotiation options.</param>
        public JsonContentFormatter(ContentNegotiationOptions options)
        {
            _options = options ?? new ContentNegotiationOptions();
            _jsonOptions = CreateJsonOptions(_options.JsonOptions);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonContentFormatter"/> class.
        /// </summary>
        /// <param name="options">The content negotiation options.</param>
        public JsonContentFormatter(IOptions<ContentNegotiationOptions> options)
            : this(options?.Value ?? new ContentNegotiationOptions())
        {
        }

        /// <inheritdoc />
        public IReadOnlyList<string> SupportedMediaTypes { get; } = new[]
        {
            "application/json",
            "text/json",
            "application/problem+json"
        };

        /// <inheritdoc />
        public string PrimaryMediaType => "application/json";

        /// <inheritdoc />
        public bool CanWrite(Type type)
        {
            // JSON can serialize most types
            return type != null && !type.IsAbstract && type != typeof(Stream);
        }

        /// <inheritdoc />
        public string Serialize(object? value)
        {
            if (value == null)
            {
                return "null";
            }

            return JsonSerializer.Serialize(value, value.GetType(), _jsonOptions);
        }

        /// <inheritdoc />
        public async Task SerializeAsync(Stream stream, object? value, Encoding encoding, CancellationToken cancellationToken = default)
        {
            if (value == null)
            {
                var nullBytes = encoding.GetBytes("null");
                await stream.WriteAsync(nullBytes, 0, nullBytes.Length, cancellationToken);
                return;
            }

            await JsonSerializer.SerializeAsync(stream, value, value.GetType(), _jsonOptions, cancellationToken);
        }

        /// <inheritdoc />
        public string GetContentType(string? charset = null)
        {
            return string.IsNullOrEmpty(charset)
                ? "application/json"
                : $"application/json; charset={charset}";
        }

        /// <inheritdoc />
        public string SerializeProblemDetails(ProblemDetails problemDetails)
        {
            return JsonSerializer.Serialize(problemDetails, _jsonOptions);
        }

        /// <inheritdoc />
        public async Task SerializeProblemDetailsAsync(Stream stream, ProblemDetails problemDetails, Encoding encoding, CancellationToken cancellationToken = default)
        {
            await JsonSerializer.SerializeAsync(stream, problemDetails, _jsonOptions, cancellationToken);
        }

        /// <inheritdoc />
        public string GetProblemDetailsContentType(string? charset = null)
        {
            var mediaType = _options.UseRfc7807ContentTypeForProblemDetails
                ? "application/problem+json"
                : "application/json";

            return string.IsNullOrEmpty(charset)
                ? mediaType
                : $"{mediaType}; charset={charset}";
        }

        private static JsonSerializerOptions CreateJsonOptions(JsonSerializationOptions options)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = options.WriteIndented,
                MaxDepth = options.MaxDepth,
                PropertyNamingPolicy = options.UseCamelCase ? JsonNamingPolicy.CamelCase : null,
                DefaultIgnoreCondition = options.IgnoreNullValues
                    ? JsonIgnoreCondition.WhenWritingNull
                    : JsonIgnoreCondition.Never
            };

            if (options.HandleReferenceLoops)
            {
                jsonOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            }

            // Add converters for common types
            jsonOptions.Converters.Add(new JsonStringEnumConverter());

            return jsonOptions;
        }
    }
}

