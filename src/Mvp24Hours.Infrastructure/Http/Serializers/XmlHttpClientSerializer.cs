//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Http.Contract;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Mvp24Hours.Infrastructure.Http.Serializers
{
    /// <summary>
    /// XML serializer for HTTP content using System.Xml.Serialization.
    /// </summary>
    public class XmlHttpClientSerializer : IHttpContentSerializer
    {
        private readonly XmlWriterSettings _writerSettings;
        private readonly XmlReaderSettings _readerSettings;

        /// <summary>
        /// Initializes a new instance with default XML settings.
        /// </summary>
        public XmlHttpClientSerializer()
            : this(new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = false,
                OmitXmlDeclaration = false
            }, new XmlReaderSettings
            {
                IgnoreWhitespace = true,
                IgnoreComments = true
            })
        {
        }

        /// <summary>
        /// Initializes a new instance with custom XML settings.
        /// </summary>
        public XmlHttpClientSerializer(XmlWriterSettings writerSettings, XmlReaderSettings readerSettings)
        {
            _writerSettings = writerSettings ?? new XmlWriterSettings();
            _readerSettings = readerSettings ?? new XmlReaderSettings();
        }

        /// <inheritdoc />
        public string MediaType => "application/xml";

        /// <inheritdoc />
        public HttpContent Serialize(object? value)
        {
            if (value == null)
            {
                return new StringContent(string.Empty, Encoding.UTF8, MediaType);
            }

            try
            {
                var serializer = new XmlSerializer(value.GetType());
                using var stringWriter = new StringWriter();
                using var xmlWriter = XmlWriter.Create(stringWriter, _writerSettings);
                serializer.Serialize(xmlWriter, value);
                var xml = stringWriter.ToString();
                return new StringContent(xml, Encoding.UTF8, MediaType);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to serialize object to XML: {ex.Message}", ex);
            }
        }

        /// <inheritdoc />
        public async Task<T?> DeserializeAsync<T>(HttpContent content, CancellationToken cancellationToken = default) where T : class
        {
            var xml = await content.ReadAsStringAsync(cancellationToken);
            return Deserialize<T>(xml);
        }

        /// <inheritdoc />
        public T? Deserialize<T>(string content) where T : class
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return default;
            }

            try
            {
                var serializer = new XmlSerializer(typeof(T));
                using var stringReader = new StringReader(content);
                using var xmlReader = XmlReader.Create(stringReader, _readerSettings);
                return (T?)serializer.Deserialize(xmlReader);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to deserialize XML to {typeof(T).Name}: {ex.Message}", ex);
            }
        }
    }
}

