//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.Sms.Models
{
    /// <summary>
    /// Represents a multimedia attachment for MMS messages.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MMS (Multimedia Messaging Service) allows sending media files (images, videos, audio)
    /// along with text messages. This class represents a single media attachment.
    /// </para>
    /// <para>
    /// <strong>Supported Media Types:</strong>
    /// Common supported formats include:
    /// - Images: JPEG, PNG, GIF, BMP
    /// - Audio: MP3, AAC, AMR, WAV
    /// - Video: MP4, 3GP, AVI
    /// </para>
    /// <para>
    /// <strong>Size Limitations:</strong>
    /// MMS messages typically have size limits (e.g., 300KB-1MB depending on carrier).
    /// Check provider documentation for specific limits.
    /// </para>
    /// </remarks>
    public class MmsAttachment
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MmsAttachment"/> class.
        /// </summary>
        public MmsAttachment()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MmsAttachment"/> class.
        /// </summary>
        /// <param name="content">The binary content of the attachment.</param>
        /// <param name="contentType">The MIME type of the content (e.g., "image/jpeg", "video/mp4").</param>
        /// <param name="fileName">Optional file name for the attachment.</param>
        /// <exception cref="ArgumentNullException">Thrown when content or contentType is null.</exception>
        public MmsAttachment(byte[] content, string contentType, string? fileName = null)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
            FileName = fileName;
        }

        /// <summary>
        /// Gets or sets the binary content of the attachment.
        /// </summary>
        /// <remarks>
        /// This is the raw binary data of the media file (image, video, or audio).
        /// </remarks>
        public byte[]? Content { get; set; }

        /// <summary>
        /// Gets or sets the MIME type of the content.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The MIME type identifies the format of the media file. Common examples:
        /// - Images: "image/jpeg", "image/png", "image/gif"
        /// - Audio: "audio/mpeg", "audio/aac", "audio/amr"
        /// - Video: "video/mp4", "video/3gpp", "video/avi"
        /// </para>
        /// <para>
        /// The provider uses this to determine how to handle the attachment.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// attachment.ContentType = "image/jpeg";
        /// attachment.ContentType = "video/mp4";
        /// </code>
        /// </example>
        public string? ContentType { get; set; }

        /// <summary>
        /// Gets or sets the optional file name for the attachment.
        /// </summary>
        /// <remarks>
        /// This is the name that will be displayed to the recipient (if supported by the provider).
        /// If not specified, the provider may generate a default name based on the content type.
        /// </remarks>
        public string? FileName { get; set; }

        /// <summary>
        /// Gets the size of the attachment in bytes.
        /// </summary>
        public long Size => Content?.Length ?? 0;

        /// <summary>
        /// Validates the attachment.
        /// </summary>
        /// <returns>A list of validation errors, or an empty list if valid.</returns>
        public System.Collections.Generic.IList<string> Validate()
        {
            var errors = new System.Collections.Generic.List<string>();

            if (Content == null || Content.Length == 0)
            {
                errors.Add("Attachment content is required.");
            }

            if (string.IsNullOrWhiteSpace(ContentType))
            {
                errors.Add("Content type is required.");
            }

            return errors;
        }
    }
}

