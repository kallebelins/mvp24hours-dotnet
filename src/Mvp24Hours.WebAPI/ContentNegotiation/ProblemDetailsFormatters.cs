//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Mvp24Hours.WebAPI.ContentNegotiation
{
    /// <summary>
    /// Specialized formatter for ProblemDetails JSON serialization with RFC 7807 compliance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This formatter provides optimized serialization for ProblemDetails objects,
    /// ensuring proper JSON structure and content type as per RFC 7807.
    /// </para>
    /// </remarks>
    public class ProblemDetailsJsonFormatter : IProblemDetailsFormatter
    {
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProblemDetailsJsonFormatter"/> class.
        /// </summary>
        public ProblemDetailsJsonFormatter()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false
            };
            _jsonOptions.Converters.Add(new JsonStringEnumConverter());
        }

        /// <inheritdoc />
        public IReadOnlyList<string> SupportedMediaTypes { get; } = new[]
        {
            "application/problem+json"
        };

        /// <inheritdoc />
        public string PrimaryMediaType => "application/problem+json";

        /// <inheritdoc />
        public bool CanWrite(Type type)
        {
            return type != null && typeof(ProblemDetails).IsAssignableFrom(type);
        }

        /// <inheritdoc />
        public string Serialize(object? value)
        {
            if (value == null)
            {
                return "null";
            }

            return JsonSerializer.Serialize(value, _jsonOptions);
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

            await JsonSerializer.SerializeAsync(stream, value, _jsonOptions, cancellationToken);
        }

        /// <inheritdoc />
        public string GetContentType(string? charset = null)
        {
            return string.IsNullOrEmpty(charset)
                ? "application/problem+json"
                : $"application/problem+json; charset={charset}";
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
            return GetContentType(charset);
        }
    }

    /// <summary>
    /// Specialized formatter for ProblemDetails XML serialization with RFC 7807 compliance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This formatter provides XML serialization for ProblemDetails objects,
    /// using the RFC 7807 XML structure with the urn:ietf:rfc:7807 namespace.
    /// </para>
    /// </remarks>
    public class ProblemDetailsXmlFormatter : IProblemDetailsFormatter
    {
        private readonly XmlWriterSettings _xmlSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProblemDetailsXmlFormatter"/> class.
        /// </summary>
        public ProblemDetailsXmlFormatter()
        {
            _xmlSettings = new XmlWriterSettings
            {
                OmitXmlDeclaration = false,
                Indent = false,
                Encoding = Encoding.UTF8,
                CloseOutput = false,
                Async = true
            };
        }

        /// <inheritdoc />
        public IReadOnlyList<string> SupportedMediaTypes { get; } = new[]
        {
            "application/problem+xml"
        };

        /// <inheritdoc />
        public string PrimaryMediaType => "application/problem+xml";

        /// <inheritdoc />
        public bool CanWrite(Type type)
        {
            return type != null && typeof(ProblemDetails).IsAssignableFrom(type);
        }

        /// <inheritdoc />
        public string Serialize(object? value)
        {
            if (value == null)
            {
                return "<?xml version=\"1.0\" encoding=\"utf-8\"?><null />";
            }

            if (value is ProblemDetails problemDetails)
            {
                return SerializeProblemDetails(problemDetails);
            }

            throw new ArgumentException("This formatter only supports ProblemDetails objects.", nameof(value));
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

            if (value is ProblemDetails problemDetails)
            {
                await SerializeProblemDetailsAsync(stream, problemDetails, encoding, cancellationToken);
                return;
            }

            throw new ArgumentException("This formatter only supports ProblemDetails objects.", nameof(value));
        }

        /// <inheritdoc />
        public string GetContentType(string? charset = null)
        {
            return string.IsNullOrEmpty(charset)
                ? "application/problem+xml"
                : $"application/problem+xml; charset={charset}";
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
            return GetContentType(charset);
        }

        private static void WriteProblemDetailsXml(XmlWriter writer, ProblemDetails problemDetails)
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

            // Handle ValidationProblemDetails
            if (problemDetails is ValidationProblemDetails validationProblemDetails &&
                validationProblemDetails.Errors?.Count > 0)
            {
                writer.WriteStartElement("errors");
                foreach (var error in validationProblemDetails.Errors)
                {
                    writer.WriteStartElement("error");
                    writer.WriteElementString("field", error.Key);
                    writer.WriteStartElement("messages");
                    foreach (var message in error.Value)
                    {
                        writer.WriteElementString("message", message);
                    }
                    writer.WriteEndElement(); // messages
                    writer.WriteEndElement(); // error
                }
                writer.WriteEndElement(); // errors
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        private static void WriteExtensionValue(XmlWriter writer, string key, object value)
        {
            writer.WriteStartElement(key);

            if (value is string strValue)
            {
                writer.WriteString(strValue);
            }
            else if (value is System.Collections.IEnumerable enumerable and not string)
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
    }
}

