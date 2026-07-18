//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;

namespace Mvp24Hours.WebAPI.ContentNegotiation;

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
    private readonly List<IContentFormatter> _formatters = [];
    private readonly ContentNegotiationOptions _options;
    private readonly object _lock = new();

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
            foreach (IContentFormatter formatter in customFormatters)
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
                return field ?? _formatters.First();
            }
        }

        private set;
    }

    /// <inheritdoc />
    public IContentFormatter? GetFormatter(string mediaType)
    {
        if (string.IsNullOrEmpty(mediaType))
        {
            return DefaultFormatter;
        }

        // Normalize media type (remove charset and other parameters)
        string normalizedMediaType = NormalizeMediaType(mediaType);

        lock (_lock)
        {
            // First, try exact match
            IContentFormatter? formatter = _formatters.FirstOrDefault(f =>
                f.SupportedMediaTypes.Any(mt =>
                    string.Equals(mt, normalizedMediaType, StringComparison.OrdinalIgnoreCase)));

            if (formatter != null)
            {
                return formatter;
            }

            // Handle wildcard media types
            if (normalizedMediaType is "*/*" or "*")
            {
                return DefaultFormatter;
            }

            // Handle type/* wildcards (e.g., text/* should match text/json)
            if (normalizedMediaType.EndsWith("/*", StringComparison.OrdinalIgnoreCase))
            {
                string typePrefix = normalizedMediaType[..^2];
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
                string baseType = "application/" + normalizedMediaType["application/problem+".Length..];
                return GetFormatter(baseType);
            }

            return null;
        }
    }

    /// <inheritdoc />
    public IProblemDetailsFormatter? GetProblemDetailsFormatter(string mediaType)
    {
        IContentFormatter? formatter = GetFormatter(mediaType);
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
            int existingIndex = _formatters.FindIndex(f =>
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
            DefaultFormatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        }
    }

    /// <summary>
    /// Sets the default formatter by media type.
    /// </summary>
    /// <param name="mediaType">The media type of the formatter to set as default.</param>
    public void SetDefaultFormatter(string mediaType)
    {
        IContentFormatter? formatter = GetFormatter(mediaType) ?? throw new ArgumentException($"No formatter found for media type: {mediaType}", nameof(mediaType));
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
        DefaultFormatter = _options.DefaultMediaType.Contains("xml", StringComparison.OrdinalIgnoreCase)
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
        int semicolonIndex = mediaType.IndexOf(';');
        if (semicolonIndex >= 0)
        {
            mediaType = mediaType[..semicolonIndex];
        }

        return mediaType.Trim().ToLowerInvariant();
    }
}

