//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.IO;

namespace Mvp24Hours.Infrastructure.Email.Contract
{
    /// <summary>
    /// Interface for email attachments.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface represents an attachment to be included in an email message.
    /// Attachments can be provided as byte arrays, streams, or file paths depending on
    /// the implementation and provider capabilities.
    /// </para>
    /// <para>
    /// <strong>Content Types:</strong>
    /// The content type (MIME type) should be specified to ensure proper handling by
    /// email clients. Common content types include:
    /// - "application/pdf" for PDF files
    /// - "image/jpeg" for JPEG images
    /// - "application/vnd.openxmlformats-officedocument.wordprocessingml.document" for Word documents
    /// </para>
    /// <para>
    /// <strong>File Names:</strong>
    /// The file name should include the extension to help email clients identify the file type.
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
    /// </code>
    /// </example>
    public interface IEmailAttachment
    {
        /// <summary>
        /// Gets the file name of the attachment (including extension).
        /// </summary>
        /// <remarks>
        /// This is the name that will be displayed to the recipient in the email client.
        /// </remarks>
        string FileName { get; }

        /// <summary>
        /// Gets the MIME content type of the attachment (e.g., "application/pdf", "image/jpeg").
        /// </summary>
        /// <remarks>
        /// The content type helps email clients determine how to handle the attachment.
        /// </remarks>
        string ContentType { get; }

        /// <summary>
        /// Gets the attachment content as a byte array.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property provides the attachment content as a byte array. For large files,
        /// consider using <see cref="GetContentStreamAsync"/> to avoid loading the entire
        /// file into memory.
        /// </para>
        /// <para>
        /// If the attachment was created from a stream, this may require reading the stream
        /// into memory. For large attachments, prefer stream-based access.
        /// </para>
        /// </remarks>
        byte[]? Content { get; }

        /// <summary>
        /// Gets the attachment content as a stream.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method provides stream-based access to the attachment content, which is
        /// more memory-efficient for large files.
        /// </para>
        /// <para>
        /// The stream should be disposed by the caller after use. The implementation
        /// may return a new stream or the original stream, depending on how the attachment
        /// was created.
        /// </para>
        /// </remarks>
        /// <returns>A stream containing the attachment content.</returns>
        Stream? GetContentStream();

        /// <summary>
        /// Gets the size of the attachment in bytes.
        /// </summary>
        /// <remarks>
        /// This property indicates the size of the attachment. It may be <c>null</c> if
        /// the size cannot be determined (e.g., for stream-based attachments where the
        /// stream length is unknown).
        /// </remarks>
        long? ContentLength { get; }

        /// <summary>
        /// Gets the content ID for inline attachments (embedded images).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Content ID is used for inline attachments (e.g., images embedded in HTML emails).
        /// The content ID should be referenced in the HTML body using <c>cid:contentId</c>.
        /// </para>
        /// <para>
        /// If this is <c>null</c>, the attachment is treated as a regular attachment
        /// (not inline).
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // HTML body with embedded image
        /// var htmlBody = "&lt;img src=\"cid:logo\" alt=\"Logo\" /&gt;";
        /// var logoAttachment = new EmailAttachment("logo.png", logoBytes, "image/png")
        /// {
        ///     ContentId = "logo"
        /// };
        /// </code>
        /// </example>
        string? ContentId { get; }

        /// <summary>
        /// Gets whether this attachment is inline (embedded in the email body).
        /// </summary>
        /// <remarks>
        /// Inline attachments are typically images embedded in HTML emails. They are
        /// not shown as separate attachments in the email client but are displayed within
        /// the email body.
        /// </remarks>
        bool IsInline { get; }
    }
}

