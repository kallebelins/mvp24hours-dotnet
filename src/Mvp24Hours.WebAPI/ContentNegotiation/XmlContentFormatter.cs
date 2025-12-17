//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Mvp24Hours.WebAPI.ContentNegotiation
{
    /// <summary>
    /// XML content formatter using XmlSerializer or DataContractSerializer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This formatter serializes objects to XML format following the configured options.
    /// It supports both regular content and ProblemDetails with RFC 7807 compliance.
    /// </para>
    /// </remarks>
    public class XmlContentFormatter : IContentFormatter, IProblemDetailsFormatter
    {
        private readonly ContentNegotiationOptions _options;
        private readonly XmlWriterSettings _xmlSettings;
        private readonly Dictionary<Type, XmlSerializer> _serializerCache = new();
        private readonly object _cacheLock = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlContentFormatter"/> class.
        /// </summary>
        public XmlContentFormatter()
            : this(new ContentNegotiationOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlContentFormatter"/> class.
        /// </summary>
        /// <param name="options">The content negotiation options.</param>
        public XmlContentFormatter(ContentNegotiationOptions options)
        {
            _options = options ?? new ContentNegotiationOptions();
            _xmlSettings = CreateXmlSettings(_options.XmlOptions);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlContentFormatter"/> class.
        /// </summary>
        /// <param name="options">The content negotiation options.</param>
        public XmlContentFormatter(IOptions<ContentNegotiationOptions> options)
            : this(options?.Value ?? new ContentNegotiationOptions())
        {
        }

        /// <inheritdoc />
        public IReadOnlyList<string> SupportedMediaTypes { get; } = new[]
        {
            "application/xml",
            "text/xml",
            "application/problem+xml"
        };

        /// <inheritdoc />
        public string PrimaryMediaType => "application/xml";

        /// <inheritdoc />
        public bool CanWrite(Type type)
        {
            if (type == null || type.IsAbstract || type == typeof(Stream))
            {
                return false;
            }

            // Check if type can be serialized
            return IsSerializable(type);
        }

        /// <inheritdoc />
        public string Serialize(object? value)
        {
            if (value == null)
            {
                return "<?xml version=\"1.0\" encoding=\"utf-8\"?><null />";
            }

            using var stringWriter = new StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter, _xmlSettings);

            var type = value.GetType();

            if (_options.XmlOptions.UseDataContractSerializer)
            {
                var dcSerializer = new DataContractSerializer(type);
                dcSerializer.WriteObject(xmlWriter, value);
            }
            else
            {
                var serializer = GetOrCreateSerializer(type);
                serializer.Serialize(xmlWriter, value);
            }

            xmlWriter.Flush();
            return stringWriter.ToString();
        }

        /// <inheritdoc />
        public async Task SerializeAsync(Stream stream, object? value, Encoding encoding, CancellationToken cancellationToken = default)
        {
            if (value == null)
            {
                var nullXml = encoding.GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\"?><null />");
                await stream.WriteAsync(nullXml, 0, nullXml.Length, cancellationToken);
                return;
            }

            var xmlSettingsWithEncoding = _xmlSettings.Clone();
            xmlSettingsWithEncoding.Encoding = encoding;

            await using var xmlWriter = XmlWriter.Create(stream, xmlSettingsWithEncoding);

            var type = value.GetType();

            if (_options.XmlOptions.UseDataContractSerializer)
            {
                var dcSerializer = new DataContractSerializer(type);
                dcSerializer.WriteObject(xmlWriter, value);
            }
            else
            {
                var serializer = GetOrCreateSerializer(type);
                serializer.Serialize(xmlWriter, value);
            }

            await xmlWriter.FlushAsync();
        }

        /// <inheritdoc />
        public string GetContentType(string? charset = null)
        {
            return string.IsNullOrEmpty(charset)
                ? "application/xml"
                : $"application/xml; charset={charset}";
        }

        /// <inheritdoc />
        public string SerializeProblemDetails(ProblemDetails problemDetails)
        {
            using var stringWriter = new StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter, _xmlSettings);

            WriteProblemDetailsXml(xmlWriter, problemDetails);

            xmlWriter.Flush();
            return stringWriter.ToString();
        }

        /// <inheritdoc />
        public async Task SerializeProblemDetailsAsync(Stream stream, ProblemDetails problemDetails, Encoding encoding, CancellationToken cancellationToken = default)
        {
            var xmlSettingsWithEncoding = _xmlSettings.Clone();
            xmlSettingsWithEncoding.Encoding = encoding;
            xmlSettingsWithEncoding.Async = true;

            await using var xmlWriter = XmlWriter.Create(stream, xmlSettingsWithEncoding);

            WriteProblemDetailsXml(xmlWriter, problemDetails);

            await xmlWriter.FlushAsync();
        }

        /// <inheritdoc />
        public string GetProblemDetailsContentType(string? charset = null)
        {
            var mediaType = _options.UseRfc7807ContentTypeForProblemDetails
                ? "application/problem+xml"
                : "application/xml";

            return string.IsNullOrEmpty(charset)
                ? mediaType
                : $"{mediaType}; charset={charset}";
        }

        private void WriteProblemDetailsXml(XmlWriter writer, ProblemDetails problemDetails)
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("problem", "urn:ietf:rfc:7807");

            if (!string.IsNullOrEmpty(problemDetails.Type))
            {
                writer.WriteElementString("type", problemDetails.Type);
            }

            if (!string.IsNullOrEmpty(problemDetails.Title))
            {
                writer.WriteElementString("title", problemDetails.Title);
            }

            if (problemDetails.Status.HasValue)
            {
                writer.WriteElementString("status", problemDetails.Status.Value.ToString());
            }

            if (!string.IsNullOrEmpty(problemDetails.Detail))
            {
                writer.WriteElementString("detail", problemDetails.Detail);
            }

            if (!string.IsNullOrEmpty(problemDetails.Instance))
            {
                writer.WriteElementString("instance", problemDetails.Instance);
            }

            // Write extensions
            foreach (var extension in problemDetails.Extensions)
            {
                if (extension.Value != null)
                {
                    WriteExtensionValue(writer, extension.Key, extension.Value);
                }
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        private void WriteExtensionValue(XmlWriter writer, string key, object value)
        {
            writer.WriteStartElement(key);

            if (value is string strValue)
            {
                writer.WriteString(strValue);
            }
            else if (value is IEnumerable enumerable and not string)
            {
                foreach (var item in enumerable)
                {
                    writer.WriteStartElement("item");
                    writer.WriteValue(item?.ToString() ?? string.Empty);
                    writer.WriteEndElement();
                }
            }
            else if (value is IDictionary<string, object> dict)
            {
                foreach (var kvp in dict)
                {
                    WriteExtensionValue(writer, kvp.Key, kvp.Value);
                }
            }
            else
            {
                writer.WriteValue(value.ToString() ?? string.Empty);
            }

            writer.WriteEndElement();
        }

        private XmlSerializer GetOrCreateSerializer(Type type)
        {
            lock (_cacheLock)
            {
                if (_serializerCache.TryGetValue(type, out var serializer))
                {
                    return serializer;
                }

                serializer = CreateSerializer(type);
                _serializerCache[type] = serializer;
                return serializer;
            }
        }

        private XmlSerializer CreateSerializer(Type type)
        {
            if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type))
            {
                var elementType = type.GetGenericArguments()[0];
                var root = new XmlRootAttribute(_options.XmlOptions.CollectionRootName);
                return new XmlSerializer(type, root);
            }

            if (!string.IsNullOrEmpty(_options.XmlOptions.DefaultNamespace))
            {
                return new XmlSerializer(type, _options.XmlOptions.DefaultNamespace);
            }

            return new XmlSerializer(type);
        }

        private static bool IsSerializable(Type type)
        {
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) ||
                type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(Guid))
            {
                return true;
            }

            if (type.IsEnum)
            {
                return true;
            }

            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                return true;
            }

            // Check for parameterless constructor or DataContract attribute
            var hasParameterlessConstructor = type.GetConstructor(Type.EmptyTypes) != null;
            var hasDataContract = type.GetCustomAttributes(typeof(DataContractAttribute), true).Length > 0;

            return hasParameterlessConstructor || hasDataContract;
        }

        private static XmlWriterSettings CreateXmlSettings(XmlSerializationOptions options)
        {
            return new XmlWriterSettings
            {
                OmitXmlDeclaration = options.OmitXmlDeclaration,
                Indent = options.Indent,
                Encoding = Encoding.UTF8,
                CloseOutput = false,
                Async = true
            };
        }
    }
}

