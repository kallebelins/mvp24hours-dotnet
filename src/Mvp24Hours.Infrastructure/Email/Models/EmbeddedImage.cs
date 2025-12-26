//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.Email.Models
{
    /// <summary>
    /// Represents an embedded image (inline image) in an email.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Embedded images are displayed inline within the email body rather than as attachments.
    /// They are referenced in HTML using a Content-ID (CID) URL, e.g., <c>&lt;img src="cid:logo" /&gt;</c>.
    /// </para>
    /// <para>
    /// <strong>Usage:</strong>
    /// Embedded images are useful for logos, banners, and other images that should be displayed
    /// directly in the email body. They are embedded in the email message itself, so they don't
    /// require external hosting or internet access to display.
    /// </para>
    /// <para>
    /// <strong>Content-ID:</strong>
    /// The Content-ID is a unique identifier used to reference the image in the HTML body.
    /// It should be unique within the email message and is typically a simple string like "logo"
    /// or "banner". The CID URL format is <c>cid:contentId</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create an embedded image
    /// var logo = new EmbeddedImage
    /// {
    ///     ContentId = "logo",
    ///     ContentBytes = logoBytes,
    ///     ContentType = "image/png"
    /// };
    /// 
    /// // Add to email message
    /// message.EmbeddedImages.Add(logo);
    /// 
    /// // Reference in HTML body
    /// message.HtmlBody = "&lt;img src=\"cid:logo\" alt=\"Company Logo\" /&gt;";
    /// </code>
    /// </example>
    public class EmbeddedImage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmbeddedImage"/> class.
        /// </summary>
        public EmbeddedImage()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EmbeddedImage"/> class.
        /// </summary>
        /// <param name="contentId">The Content-ID used to reference this image in HTML.</param>
        /// <param name="contentBytes">The image content as a byte array.</param>
        /// <param name="contentType">The MIME type of the image (e.g., "image/png", "image/jpeg").</param>
        /// <exception cref="ArgumentException">Thrown when contentId is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when contentBytes is null.</exception>
        public EmbeddedImage(string contentId, byte[] contentBytes, string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentId))
            {
                throw new ArgumentException("Content-ID cannot be null or empty.", nameof(contentId));
            }

            ContentId = contentId;
            ContentBytes = contentBytes ?? throw new ArgumentNullException(nameof(contentBytes));
            ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
        }

        /// <summary>
        /// Gets or sets the Content-ID used to reference this image in HTML.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The Content-ID is a unique identifier used in the HTML body to reference this image.
        /// It should be unique within the email message. The CID URL format is <c>cid:contentId</c>.
        /// </para>
        /// <para>
        /// Example: If ContentId is "logo", reference it in HTML as <c>&lt;img src="cid:logo" /&gt;</c>.
        /// </para>
        /// </remarks>
        public string ContentId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the image content as a byte array.
        /// </summary>
        /// <remarks>
        /// This is the actual image data (PNG, JPEG, GIF, etc.) as bytes.
        /// </remarks>
        public byte[] ContentBytes { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the MIME type of the image.
        /// </summary>
        /// <remarks>
        /// Common MIME types:
        /// - <c>image/png</c> for PNG images
        /// - <c>image/jpeg</c> or <c>image/jpg</c> for JPEG images
        /// - <c>image/gif</c> for GIF images
        /// - <c>image/webp</c> for WebP images
        /// </remarks>
        public string ContentType { get; set; } = "image/png";

        /// <summary>
        /// Gets or sets the file name of the image (optional, for reference).
        /// </summary>
        /// <remarks>
        /// This is optional and used primarily for reference or logging purposes.
        /// It doesn't affect how the image is displayed in the email.
        /// </remarks>
        public string? FileName { get; set; }

        /// <summary>
        /// Gets the size of the image content in bytes.
        /// </summary>
        public long ContentLength => ContentBytes?.Length ?? 0;
    }
}

