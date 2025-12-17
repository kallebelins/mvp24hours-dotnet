//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.WebAPI.Configuration
{
    /// <summary>
    /// Configuration options for content negotiation in WebAPI responses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Content negotiation allows clients to specify the preferred format of response data
    /// using the Accept header. This enables APIs to serve the same content in different formats.
    /// </para>
    /// </remarks>
    public class ContentNegotiationOptions
    {
        /// <summary>
        /// Gets or sets whether content negotiation is enabled.
        /// Default: true
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the default media type when Accept header is not specified or is */*.
        /// Default: "application/json"
        /// </summary>
        public string DefaultMediaType { get; set; } = "application/json";

        /// <summary>
        /// Gets or sets the list of supported media types in order of preference.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The order matters when the client accepts multiple types with equal quality.
        /// The first matching type in this list will be used.
        /// </para>
        /// </remarks>
        public List<MediaTypeMapping> SupportedMediaTypes { get; set; } = new()
        {
            new MediaTypeMapping("application/json", ContentFormat.Json),
            new MediaTypeMapping("text/json", ContentFormat.Json),
            new MediaTypeMapping("application/xml", ContentFormat.Xml),
            new MediaTypeMapping("text/xml", ContentFormat.Xml)
        };

        /// <summary>
        /// Gets or sets whether to respect the Accept header quality values.
        /// When true, responses will be formatted according to client quality preferences.
        /// Default: true
        /// </summary>
        public bool RespectQualityValues { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to return 406 Not Acceptable when no matching format is found.
        /// When false, the default format will be used instead.
        /// Default: false
        /// </summary>
        public bool Return406WhenNoMatch { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to include the format parameter in URLs.
        /// When enabled, clients can request formats via ?format=json or ?format=xml.
        /// Default: true
        /// </summary>
        public bool EnableFormatParameter { get; set; } = true;

        /// <summary>
        /// Gets or sets the query parameter name for format specification.
        /// Default: "format"
        /// </summary>
        public string FormatParameterName { get; set; } = "format";

        /// <summary>
        /// Gets or sets whether to include format suffix in routes.
        /// When enabled, clients can request formats via /api/users.json or /api/users.xml.
        /// Default: false
        /// </summary>
        public bool EnableFormatSuffix { get; set; } = false;

        /// <summary>
        /// Gets or sets custom format parameter mappings.
        /// Maps format parameter values to content types.
        /// </summary>
        /// <example>
        /// <code>
        /// options.FormatMappings["json"] = "application/json";
        /// options.FormatMappings["xml"] = "application/xml";
        /// </code>
        /// </example>
        public Dictionary<string, string> FormatMappings { get; set; } = new(StringComparer.OrdinalIgnoreCase)
        {
            { "json", "application/json" },
            { "xml", "application/xml" }
        };

        /// <summary>
        /// Gets or sets whether to use RFC 7807 content type for ProblemDetails.
        /// When true, error responses use "application/problem+json" or "application/problem+xml".
        /// Default: true
        /// </summary>
        public bool UseRfc7807ContentTypeForProblemDetails { get; set; } = true;

        /// <summary>
        /// Gets or sets charset to include in Content-Type header.
        /// Set to null to omit charset.
        /// Default: "utf-8"
        /// </summary>
        public string? Charset { get; set; } = "utf-8";

        /// <summary>
        /// Gets or sets whether to add Vary: Accept header to responses.
        /// This helps caches understand that responses vary by Accept header.
        /// Default: true
        /// </summary>
        public bool AddVaryHeader { get; set; } = true;

        /// <summary>
        /// Gets or sets paths to exclude from content negotiation.
        /// Supports wildcards (*).
        /// </summary>
        public HashSet<string> ExcludedPaths { get; set; } = new(StringComparer.OrdinalIgnoreCase)
        {
            "/health",
            "/health/*",
            "/swagger",
            "/swagger/*"
        };

        /// <summary>
        /// Gets or sets the JSON serialization options.
        /// </summary>
        public JsonSerializationOptions JsonOptions { get; set; } = new();

        /// <summary>
        /// Gets or sets the XML serialization options.
        /// </summary>
        public XmlSerializationOptions XmlOptions { get; set; } = new();
    }

    /// <summary>
    /// Represents the content format for serialization.
    /// </summary>
    public enum ContentFormat
    {
        /// <summary>
        /// JSON format (application/json, text/json).
        /// </summary>
        Json,

        /// <summary>
        /// XML format (application/xml, text/xml).
        /// </summary>
        Xml,

        /// <summary>
        /// Custom format handled by registered formatter.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Maps a media type to a content format.
    /// </summary>
    public class MediaTypeMapping
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaTypeMapping"/> class.
        /// </summary>
        public MediaTypeMapping()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaTypeMapping"/> class.
        /// </summary>
        /// <param name="mediaType">The media type string.</param>
        /// <param name="format">The content format.</param>
        public MediaTypeMapping(string mediaType, ContentFormat format)
        {
            MediaType = mediaType;
            Format = format;
        }

        /// <summary>
        /// Gets or sets the media type string (e.g., "application/json").
        /// </summary>
        public string MediaType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the content format.
        /// </summary>
        public ContentFormat Format { get; set; }

        /// <summary>
        /// Gets or sets the custom formatter type for custom formats.
        /// </summary>
        public Type? CustomFormatterType { get; set; }
    }

    /// <summary>
    /// JSON serialization configuration options.
    /// </summary>
    public class JsonSerializationOptions
    {
        /// <summary>
        /// Gets or sets whether to use camelCase property names.
        /// Default: true
        /// </summary>
        public bool UseCamelCase { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to indent the output.
        /// Default: false (minified)
        /// </summary>
        public bool WriteIndented { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to ignore null values during serialization.
        /// Default: false
        /// </summary>
        public bool IgnoreNullValues { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to handle reference loops.
        /// Default: true
        /// </summary>
        public bool HandleReferenceLoops { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum depth for serialization.
        /// Default: 32
        /// </summary>
        public int MaxDepth { get; set; } = 32;
    }

    /// <summary>
    /// XML serialization configuration options.
    /// </summary>
    public class XmlSerializationOptions
    {
        /// <summary>
        /// Gets or sets whether to include the XML declaration.
        /// Default: false
        /// </summary>
        public bool OmitXmlDeclaration { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to indent the output.
        /// Default: false (minified)
        /// </summary>
        public bool Indent { get; set; } = false;

        /// <summary>
        /// Gets or sets the root element name for collections.
        /// Default: "ArrayOfItems"
        /// </summary>
        public string CollectionRootName { get; set; } = "ArrayOfItems";

        /// <summary>
        /// Gets or sets the item element name for collections.
        /// Default: "Item"
        /// </summary>
        public string CollectionItemName { get; set; } = "Item";

        /// <summary>
        /// Gets or sets whether to use DataContract serialization.
        /// When false, uses XmlSerializer.
        /// Default: false
        /// </summary>
        public bool UseDataContractSerializer { get; set; } = false;

        /// <summary>
        /// Gets or sets the default XML namespace.
        /// </summary>
        public string? DefaultNamespace { get; set; }
    }
}

