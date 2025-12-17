//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Mvp24Hours.WebAPI.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.WebAPI.ContentNegotiation
{
    /// <summary>
    /// Negotiates content format based on Accept header, query parameters, and route suffixes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class implements RFC 7231 content negotiation, supporting:
    /// <list type="bullet">
    /// <item>Accept header parsing with quality values (q=0.9)</item>
    /// <item>Multiple media types in Accept header</item>
    /// <item>Wildcard media types (*/* and type/*)</item>
    /// <item>Format query parameter (?format=json)</item>
    /// <item>URL suffix (.json, .xml)</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class AcceptHeaderNegotiator
    {
        private readonly ContentNegotiationOptions _options;
        private readonly IContentFormatterRegistry _registry;

        /// <summary>
        /// Initializes a new instance of the <see cref="AcceptHeaderNegotiator"/> class.
        /// </summary>
        /// <param name="options">The content negotiation options.</param>
        /// <param name="registry">The formatter registry.</param>
        public AcceptHeaderNegotiator(
            IOptions<ContentNegotiationOptions> options,
            IContentFormatterRegistry registry)
        {
            _options = options?.Value ?? new ContentNegotiationOptions();
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AcceptHeaderNegotiator"/> class.
        /// </summary>
        /// <param name="options">The content negotiation options.</param>
        /// <param name="registry">The formatter registry.</param>
        public AcceptHeaderNegotiator(
            ContentNegotiationOptions options,
            IContentFormatterRegistry registry)
        {
            _options = options ?? new ContentNegotiationOptions();
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>
        /// Negotiates the content format based on the HTTP context.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>The negotiation result with the selected formatter.</returns>
        public ContentNegotiationResult Negotiate(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Check if content negotiation is enabled
            if (!_options.Enabled)
            {
                return new ContentNegotiationResult
                {
                    Formatter = _registry.DefaultFormatter,
                    MediaType = _options.DefaultMediaType,
                    Success = true
                };
            }

            // Check for excluded paths
            if (IsExcludedPath(context.Request.Path))
            {
                return new ContentNegotiationResult
                {
                    Formatter = _registry.DefaultFormatter,
                    MediaType = _options.DefaultMediaType,
                    Success = true
                };
            }

            // Priority: 1. Format query parameter, 2. URL suffix, 3. Accept header
            string? selectedMediaType = null;

            // Check format query parameter
            if (_options.EnableFormatParameter)
            {
                selectedMediaType = GetMediaTypeFromFormatParameter(context.Request);
            }

            // Check URL suffix
            if (string.IsNullOrEmpty(selectedMediaType) && _options.EnableFormatSuffix)
            {
                selectedMediaType = GetMediaTypeFromUrlSuffix(context.Request);
            }

            // Check Accept header
            if (string.IsNullOrEmpty(selectedMediaType))
            {
                selectedMediaType = GetMediaTypeFromAcceptHeader(context.Request);
            }

            // Resolve formatter
            if (!string.IsNullOrEmpty(selectedMediaType))
            {
                var formatter = _registry.GetFormatter(selectedMediaType);
                if (formatter != null)
                {
                    return new ContentNegotiationResult
                    {
                        Formatter = formatter,
                        MediaType = selectedMediaType,
                        Success = true
                    };
                }
            }

            // No match found - check options
            if (_options.Return406WhenNoMatch && !string.IsNullOrEmpty(selectedMediaType))
            {
                return new ContentNegotiationResult
                {
                    Success = false,
                    RequestedMediaType = selectedMediaType,
                    ErrorMessage = $"The requested media type '{selectedMediaType}' is not supported."
                };
            }

            // Fall back to default
            return new ContentNegotiationResult
            {
                Formatter = _registry.DefaultFormatter,
                MediaType = _options.DefaultMediaType,
                Success = true
            };
        }

        /// <summary>
        /// Gets the best matching media type from the Accept header.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <returns>The best matching media type, or null if no match.</returns>
        public string? GetMediaTypeFromAcceptHeader(HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!request.Headers.TryGetValue("Accept", out var acceptHeader) ||
                StringValues.IsNullOrEmpty(acceptHeader))
            {
                return null;
            }

            var acceptHeaderValue = acceptHeader.ToString();
            if (string.IsNullOrWhiteSpace(acceptHeaderValue))
            {
                return null;
            }

            try
            {
                var acceptedTypes = ParseAcceptHeader(acceptHeaderValue);

                if (acceptedTypes.Count == 0)
                {
                    return null;
                }

                if (_options.RespectQualityValues)
                {
                    // Sort by quality, then by specificity
                    acceptedTypes = acceptedTypes
                        .OrderByDescending(t => t.Quality)
                        .ThenByDescending(t => t.Specificity)
                        .ToList();
                }

                foreach (var acceptedType in acceptedTypes)
                {
                    var mediaType = acceptedType.MediaType;

                    // Skip invalid media types
                    if (string.IsNullOrWhiteSpace(mediaType))
                    {
                        continue;
                    }

                    // Check if this media type is supported
                    if (_registry.IsSupported(mediaType))
                    {
                        return mediaType;
                    }

                    // Handle wildcards
                    if (mediaType == "*/*")
                    {
                        return _options.DefaultMediaType;
                    }

                    if (mediaType.EndsWith("/*", StringComparison.OrdinalIgnoreCase))
                    {
                        var formatter = _registry.GetFormatter(mediaType);
                        if (formatter != null)
                        {
                            return formatter.PrimaryMediaType;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // If parsing fails, return null to fall back to default
                // This ensures robustness even with malformed Accept headers
                return null;
            }

            return null;
        }

        /// <summary>
        /// Gets the media type from the format query parameter.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <returns>The media type, or null if not specified.</returns>
        public string? GetMediaTypeFromFormatParameter(HttpRequest request)
        {
            if (!request.Query.TryGetValue(_options.FormatParameterName, out var formatValue) ||
                StringValues.IsNullOrEmpty(formatValue))
            {
                return null;
            }

            var format = formatValue.ToString().Trim();

            if (_options.FormatMappings.TryGetValue(format, out var mediaType))
            {
                return mediaType;
            }

            // Check if format is itself a media type
            if (format.Contains('/'))
            {
                return format;
            }

            return null;
        }

        /// <summary>
        /// Gets the media type from the URL suffix.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <returns>The media type, or null if not specified.</returns>
        public string? GetMediaTypeFromUrlSuffix(HttpRequest request)
        {
            var path = request.Path.Value;
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            var lastSegment = path.Split('/').LastOrDefault() ?? string.Empty;
            var dotIndex = lastSegment.LastIndexOf('.');

            if (dotIndex < 0 || dotIndex == lastSegment.Length - 1)
            {
                return null;
            }

            var extension = lastSegment.Substring(dotIndex + 1).ToLowerInvariant();

            if (_options.FormatMappings.TryGetValue(extension, out var mediaType))
            {
                return mediaType;
            }

            return null;
        }

        /// <summary>
        /// Parses the Accept header into a list of media type entries.
        /// </summary>
        /// <param name="acceptHeader">The Accept header value.</param>
        /// <returns>A list of parsed media type entries.</returns>
        public static List<MediaTypeEntry> ParseAcceptHeader(string acceptHeader)
        {
            var result = new List<MediaTypeEntry>();

            if (string.IsNullOrWhiteSpace(acceptHeader))
            {
                return result;
            }

            // Split by comma, handling quoted strings properly
            var types = SplitAcceptHeader(acceptHeader);

            foreach (var type in types)
            {
                var trimmedType = type.Trim();
                if (string.IsNullOrWhiteSpace(trimmedType))
                {
                    continue;
                }

                var entry = ParseMediaTypeEntry(trimmedType);
                if (entry != null)
                {
                    result.Add(entry);
                }
            }

            return result;
        }

        private static List<string> SplitAcceptHeader(string acceptHeader)
        {
            var result = new List<string>();
            var current = new System.Text.StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < acceptHeader.Length; i++)
            {
                char c = acceptHeader[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    current.Append(c);
                }
                else if (c == ',' && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        result.Add(current.ToString());
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(c);
                }
            }

            if (current.Length > 0)
            {
                result.Add(current.ToString());
            }

            return result;
        }

        private static MediaTypeEntry? ParseMediaTypeEntry(string entry)
        {
            if (string.IsNullOrWhiteSpace(entry))
            {
                return null;
            }

            // Validate media type format (type/subtype)
            var parts = entry.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var mediaType = parts[0].Trim();

            // Validate media type format
            if (!IsValidMediaType(mediaType))
            {
                return null;
            }

            double quality = 1.0;
            var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 1; i < parts.Length; i++)
            {
                var param = parts[i].Trim();
                var equalsIndex = param.IndexOf('=');

                if (equalsIndex > 0)
                {
                    var key = param.Substring(0, equalsIndex).Trim();
                    var value = param.Substring(equalsIndex + 1).Trim().Trim('"');

                    if (string.IsNullOrWhiteSpace(key))
                    {
                        continue;
                    }

                    if (key.Equals("q", StringComparison.OrdinalIgnoreCase))
                    {
                        // Parse quality value with proper validation
                        if (double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var q))
                        {
                            quality = Math.Clamp(q, 0.0, 1.0);
                        }
                    }
                    else
                    {
                        parameters[key] = value;
                    }
                }
            }

            // Calculate specificity (more specific = higher value)
            int specificity = 0;
            if (mediaType != "*/*")
            {
                specificity++;
                if (!mediaType.EndsWith("/*", StringComparison.OrdinalIgnoreCase))
                {
                    specificity++;
                }
            }
            specificity += parameters.Count;

            return new MediaTypeEntry
            {
                MediaType = mediaType,
                Quality = quality,
                Specificity = specificity,
                Parameters = parameters
            };
        }

        private static bool IsValidMediaType(string mediaType)
        {
            if (string.IsNullOrWhiteSpace(mediaType))
            {
                return false;
            }

            // Allow wildcards
            if (mediaType == "*/*" || mediaType.EndsWith("/*", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Check for type/subtype format
            var slashIndex = mediaType.IndexOf('/');
            if (slashIndex <= 0 || slashIndex >= mediaType.Length - 1)
            {
                return false;
            }

            var type = mediaType.Substring(0, slashIndex).Trim();
            var subtype = mediaType.Substring(slashIndex + 1).Trim();

            // Validate type and subtype contain only valid characters
            if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(subtype))
            {
                return false;
            }

            // Basic validation: type and subtype should contain only alphanumeric, +, -, . characters
            return IsValidToken(type) && IsValidToken(subtype);
        }

        private static bool IsValidToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            foreach (char c in token)
            {
                if (!char.IsLetterOrDigit(c) && c != '+' && c != '-' && c != '.' && c != '_')
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsExcludedPath(PathString path)
        {
            var pathValue = path.Value ?? string.Empty;

            foreach (var excludedPath in _options.ExcludedPaths)
            {
                if (MatchesPath(pathValue, excludedPath))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool MatchesPath(string path, string pattern)
        {
            if (pattern.EndsWith("*"))
            {
                var prefix = pattern.TrimEnd('*');
                return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            }

            return path.Equals(pattern, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Represents a parsed media type entry from the Accept header.
    /// </summary>
    public class MediaTypeEntry
    {
        /// <summary>
        /// Gets or sets the media type (e.g., "application/json").
        /// </summary>
        public string MediaType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the quality value (0.0 to 1.0).
        /// </summary>
        public double Quality { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the specificity value for ordering.
        /// </summary>
        public int Specificity { get; set; }

        /// <summary>
        /// Gets or sets additional parameters.
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Result of content negotiation.
    /// </summary>
    public class ContentNegotiationResult
    {
        /// <summary>
        /// Gets or sets whether negotiation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the selected formatter.
        /// </summary>
        public IContentFormatter? Formatter { get; set; }

        /// <summary>
        /// Gets or sets the selected media type.
        /// </summary>
        public string? MediaType { get; set; }

        /// <summary>
        /// Gets or sets the requested media type (when negotiation fails).
        /// </summary>
        public string? RequestedMediaType { get; set; }

        /// <summary>
        /// Gets or sets the error message (when negotiation fails).
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}

