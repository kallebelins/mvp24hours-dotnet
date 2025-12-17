//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.WebAPI.ContentNegotiation
{
    /// <summary>
    /// Registry for content formatters that manages formatter registration and lookup.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This registry maintains a collection of content formatters and provides methods
    /// to retrieve formatters based on media type.
    /// </para>
    /// </remarks>
    public class ContentFormatterRegistry : IContentFormatterRegistry
    {
        private readonly List<IContentFormatter> _formatters = new();
        private readonly ContentNegotiationOptions _options;
        private readonly object _lock = new();
        private IContentFormatter? _defaultFormatter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentFormatterRegistry"/> class.
        /// </summary>
        public ContentFormatterRegistry()
            : this(new ContentNegotiationOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentFormatterRegistry"/> class.
        /// </summary>
        /// <param name="options">The content negotiation options.</param>
        /// <param name="customFormatters">Optional custom formatters to register.</param>
        public ContentFormatterRegistry(ContentNegotiationOptions options, IEnumerable<IContentFormatter>? customFormatters = null)
        {
            _options = options ?? new ContentNegotiationOptions();
            InitializeDefaultFormatters();
            
            // Register custom formatters if provided
            if (customFormatters != null)
            {
                foreach (var formatter in customFormatters)
                {
                    RegisterFormatter(formatter);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentFormatterRegistry"/> class.
        /// </summary>
        /// <param name="options">The content negotiation options.</param>
        /// <param name="customFormatters">Optional custom formatters to register.</param>
        public ContentFormatterRegistry(IOptions<ContentNegotiationOptions> options, IEnumerable<IContentFormatter>? customFormatters = null)
            : this(options?.Value ?? new ContentNegotiationOptions(), customFormatters)
        {
        }

        /// <inheritdoc />
        public IReadOnlyList<IContentFormatter> Formatters
        {
            get
            {
                lock (_lock)
                {
                    return _formatters.ToList().AsReadOnly();
                }
            }
        }

        /// <inheritdoc />
        public IContentFormatter DefaultFormatter
        {
            get
            {
                lock (_lock)
                {
                    return _defaultFormatter ?? _formatters.First();
                }
            }
        }

        /// <inheritdoc />
        public IContentFormatter? GetFormatter(string mediaType)
        {
            if (string.IsNullOrEmpty(mediaType))
            {
                return DefaultFormatter;
            }

            // Normalize media type (remove charset and other parameters)
            var normalizedMediaType = NormalizeMediaType(mediaType);

            lock (_lock)
            {
                // First, try exact match
                var formatter = _formatters.FirstOrDefault(f =>
                    f.SupportedMediaTypes.Any(mt =>
                        string.Equals(mt, normalizedMediaType, StringComparison.OrdinalIgnoreCase)));

                if (formatter != null)
                {
                    return formatter;
                }

                // Handle wildcard media types
                if (normalizedMediaType == "*/*" || normalizedMediaType == "*")
                {
                    return DefaultFormatter;
                }

                // Handle type/* wildcards (e.g., text/* should match text/json)
                if (normalizedMediaType.EndsWith("/*", StringComparison.OrdinalIgnoreCase))
                {
                    var typePrefix = normalizedMediaType.Substring(0, normalizedMediaType.Length - 2);
                    formatter = _formatters.FirstOrDefault(f =>
                        f.SupportedMediaTypes.Any(mt =>
                            mt.StartsWith(typePrefix + "/", StringComparison.OrdinalIgnoreCase)));

                    if (formatter != null)
                    {
                        return formatter;
                    }
                }

                // Handle problem details media types
                if (normalizedMediaType.StartsWith("application/problem+", StringComparison.OrdinalIgnoreCase))
                {
                    var baseType = "application/" + normalizedMediaType.Substring("application/problem+".Length);
                    return GetFormatter(baseType);
                }

                return null;
            }
        }

        /// <inheritdoc />
        public IProblemDetailsFormatter? GetProblemDetailsFormatter(string mediaType)
        {
            var formatter = GetFormatter(mediaType);
            return formatter as IProblemDetailsFormatter;
        }

        /// <inheritdoc />
        public bool IsSupported(string mediaType)
        {
            return GetFormatter(mediaType) != null;
        }

        /// <inheritdoc />
        public void RegisterFormatter(IContentFormatter formatter)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            lock (_lock)
            {
                // Check if formatter for this media type already exists
                var existingIndex = _formatters.FindIndex(f =>
                    f.PrimaryMediaType.Equals(formatter.PrimaryMediaType, StringComparison.OrdinalIgnoreCase));

                if (existingIndex >= 0)
                {
                    // Replace existing formatter
                    _formatters[existingIndex] = formatter;
                }
                else
                {
                    _formatters.Add(formatter);
                }
            }
        }

        /// <summary>
        /// Sets the default formatter.
        /// </summary>
        /// <param name="formatter">The formatter to set as default.</param>
        public void SetDefaultFormatter(IContentFormatter formatter)
        {
            lock (_lock)
            {
                _defaultFormatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            }
        }

        /// <summary>
        /// Sets the default formatter by media type.
        /// </summary>
        /// <param name="mediaType">The media type of the formatter to set as default.</param>
        public void SetDefaultFormatter(string mediaType)
        {
            var formatter = GetFormatter(mediaType);
            if (formatter == null)
            {
                throw new ArgumentException($"No formatter found for media type: {mediaType}", nameof(mediaType));
            }

            SetDefaultFormatter(formatter);
        }

        private void InitializeDefaultFormatters()
        {
            // Register JSON formatter
            var jsonFormatter = new JsonContentFormatter(_options);
            _formatters.Add(jsonFormatter);

            // Register XML formatter
            var xmlFormatter = new XmlContentFormatter(_options);
            _formatters.Add(xmlFormatter);

            // Set default formatter based on options
            _defaultFormatter = _options.DefaultMediaType.Contains("xml", StringComparison.OrdinalIgnoreCase)
                ? xmlFormatter
                : jsonFormatter;
        }

        private static string NormalizeMediaType(string mediaType)
        {
            if (string.IsNullOrEmpty(mediaType))
            {
                return string.Empty;
            }

            // Remove parameters (e.g., charset=utf-8)
            var semicolonIndex = mediaType.IndexOf(';');
            if (semicolonIndex >= 0)
            {
                mediaType = mediaType.Substring(0, semicolonIndex);
            }

            return mediaType.Trim().ToLowerInvariant();
        }
    }
}

