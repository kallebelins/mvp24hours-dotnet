//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Email.Contract;
using System;
using System.IO;

namespace Mvp24Hours.Infrastructure.Email.Models
{
    /// <summary>
    /// Represents an email attachment.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class implements <see cref="IEmailAttachment"/> and provides a convenient way
    /// to create email attachments from byte arrays or streams.
    /// </para>
    /// <para>
    /// <strong>Content Sources:</strong>
    /// Attachments can be created from:
    /// - Byte arrays (for small files already in memory)
    /// - Streams (for large files or files being read from disk/network)
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create attachment from byte array
    /// var pdfBytes = File.ReadAllBytes("document.pdf");
    /// var attachment = new EmailAttachment("document.pdf", pdfBytes, "application/pdf");
    /// 
    /// // Create attachment from stream
    /// using var stream = File.OpenRead("image.jpg");
    /// var imageAttachment = new EmailAttachment("image.jpg", stream, "image/jpeg");
    /// 
    /// // Create inline attachment (embedded image)
    /// var logoAttachment = new EmailAttachment("logo.png", logoBytes, "image/png")
    /// {
    ///     ContentId = "logo",
    ///     IsInline = true
    /// };
    /// </code>
    /// </example>
    public class EmailAttachment : IEmailAttachment
    {
        private readonly byte[]? _content;
        private readonly Stream? _contentStream;
        private readonly bool _disposeStream;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailAttachment"/> class from a byte array.
        /// </summary>
        /// <param name="fileName">The file name of the attachment (including extension).</param>
        /// <param name="content">The attachment content as a byte array.</param>
        /// <param name="contentType">The MIME content type (e.g., "application/pdf", "image/jpeg").</param>
        /// <exception cref="ArgumentNullException">Thrown when fileName, content, or contentType is null or empty.</exception>
        public EmailAttachment(string fileName, byte[] content, string contentType)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException(nameof(fileName), "File name cannot be null or empty.");
            }

            if (content == null)
            {
                throw new ArgumentNullException(nameof(content), "Content cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(contentType))
            {
                throw new ArgumentNullException(nameof(contentType), "Content type cannot be null or empty.");
            }

            FileName = fileName;
            _content = content;
            ContentType = contentType;
            ContentLength = content.Length;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailAttachment"/> class from a stream.
        /// </summary>
        /// <param name="fileName">The file name of the attachment (including extension).</param>
        /// <param name="contentStream">The stream containing the attachment content.</param>
        /// <param name="contentType">The MIME content type.</param>
        /// <param name="disposeStream">Whether to dispose the stream when the attachment is disposed.</param>
        /// <exception cref="ArgumentNullException">Thrown when fileName, contentStream, or contentType is null or empty.</exception>
        public EmailAttachment(string fileName, Stream contentStream, string contentType, bool disposeStream = false)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException(nameof(fileName), "File name cannot be null or empty.");
            }

            if (contentStream == null)
            {
                throw new ArgumentNullException(nameof(contentStream), "Content stream cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(contentType))
            {
                throw new ArgumentNullException(nameof(contentType), "Content type cannot be null or empty.");
            }

            FileName = fileName;
            _contentStream = contentStream;
            ContentType = contentType;
            _disposeStream = disposeStream;

            // Try to get length if stream supports it
            if (contentStream.CanSeek)
            {
                var originalPosition = contentStream.Position;
                contentStream.Position = 0;
                ContentLength = contentStream.Length;
                contentStream.Position = originalPosition;
            }
        }

        /// <summary>
        /// Gets the file name of the attachment (including extension).
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Gets the MIME content type of the attachment.
        /// </summary>
        public string ContentType { get; }

        /// <summary>
        /// Gets the attachment content as a byte array.
        /// </summary>
        /// <remarks>
        /// If the attachment was created from a stream, this will read the stream into memory.
        /// For large attachments, prefer using <see cref="GetContentStream"/>.
        /// </remarks>
        public byte[]? Content
        {
            get
            {
                if (_content != null)
                {
                    return _content;
                }

                if (_contentStream != null)
                {
                    // Read stream into memory
                    if (_contentStream.CanSeek)
                    {
                        var originalPosition = _contentStream.Position;
                        _contentStream.Position = 0;
                        using var memoryStream = new MemoryStream();
                        _contentStream.CopyTo(memoryStream);
                        _contentStream.Position = originalPosition;
                        return memoryStream.ToArray();
                    }
                    else
                    {
                        using var memoryStream = new MemoryStream();
                        _contentStream.CopyTo(memoryStream);
                        return memoryStream.ToArray();
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the attachment content as a stream.
        /// </summary>
        /// <remarks>
        /// If the attachment was created from a byte array, this returns a new MemoryStream.
        /// If created from a stream, it returns the original stream (or a new stream if the
        /// original doesn't support seeking).
        /// </remarks>
        public Stream? GetContentStream()
        {
            if (_contentStream != null)
            {
                // If stream supports seeking, reset position and return it
                if (_contentStream.CanSeek)
                {
                    _contentStream.Position = 0;
                    return _contentStream;
                }
                else
                {
                    // Stream doesn't support seeking, need to read into memory
                    var memoryStream = new MemoryStream();
                    _contentStream.CopyTo(memoryStream);
                    memoryStream.Position = 0;
                    return memoryStream;
                }
            }

            if (_content != null)
            {
                return new MemoryStream(_content);
            }

            return null;
        }

        /// <summary>
        /// Gets the size of the attachment in bytes.
        /// </summary>
        public long? ContentLength { get; }

        /// <summary>
        /// Gets or sets the content ID for inline attachments (embedded images).
        /// </summary>
        public string? ContentId { get; set; }

        /// <summary>
        /// Gets or sets whether this attachment is inline (embedded in the email body).
        /// </summary>
        public bool IsInline { get; set; }
    }
}

