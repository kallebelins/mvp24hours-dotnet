//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.FileStorage.Contract
{
    /// <summary>
    /// Interface for validating files before upload or after download.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface provides a pluggable validation mechanism for file storage operations.
    /// Validators can check file size, extension, content type, and even perform virus scanning
    /// or other security checks.
    /// </para>
    /// <para>
    /// Multiple validators can be chained together. If any validator fails, the upload/download
    /// operation is rejected.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class ImageFileValidator : IFileValidator
    /// {
    ///     public Task&lt;ValidationResult&gt; ValidateAsync(FileValidationContext context, CancellationToken cancellationToken)
    ///     {
    ///         var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
    ///         if (!allowedTypes.Contains(context.ContentType))
    ///         {
    ///             return Task.FromResult(ValidationResult.Failed("Only JPEG, PNG, and GIF images are allowed."));
    ///         }
    ///         
    ///         if (context.Size > 10 * 1024 * 1024) // 10MB
    ///         {
    ///             return Task.FromResult(ValidationResult.Failed("File size exceeds 10MB limit."));
    ///         }
    ///         
    ///         return Task.FromResult(ValidationResult.Success());
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IFileValidator
    {
        /// <summary>
        /// Validates a file based on the provided context.
        /// </summary>
        /// <param name="context">The validation context containing file information.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the validation.</param>
        /// <returns>A validation result indicating success or failure with error messages.</returns>
        /// <remarks>
        /// <para>
        /// The validation context provides access to file metadata (name, size, content type, extension)
        /// and optionally the file content stream. The validator should check the appropriate properties
        /// based on its validation rules.
        /// </para>
        /// <para>
        /// For performance reasons, avoid reading the entire file content unless necessary (e.g., for
        /// virus scanning or content-based validation). Prefer checking metadata first.
        /// </para>
        /// <para>
        /// If the file content stream is provided, it should not be disposed or closed by the validator.
        /// The stream position may be changed, but it's recommended to restore it to the original position
        /// if possible.
        /// </para>
        /// </remarks>
        Task<ValidationResult> ValidateAsync(
            FileValidationContext context,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Context information for file validation.
    /// </summary>
    /// <remarks>
    /// This class provides all information needed for file validation, including metadata and
    /// optionally the file content stream.
    /// </remarks>
    public class FileValidationContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileValidationContext"/> class.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="fileSize">The size of the file in bytes.</param>
        /// <param name="contentType">The MIME type of the file.</param>
        /// <param name="fileExtension">The file extension (e.g., ".pdf", ".jpg").</param>
        /// <param name="contentStream">Optional stream containing the file content.</param>
        public FileValidationContext(
            string fileName,
            long fileSize,
            string contentType,
            string fileExtension,
            Stream? contentStream = null)
        {
            FileName = fileName ?? throw new System.ArgumentNullException(nameof(fileName));
            FileSize = fileSize;
            ContentType = contentType ?? throw new System.ArgumentNullException(nameof(contentType));
            FileExtension = fileExtension ?? throw new System.ArgumentNullException(nameof(fileExtension));
            ContentStream = contentStream;
        }

        /// <summary>
        /// Gets the name of the file (without directory path).
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Gets the size of the file in bytes.
        /// </summary>
        public long FileSize { get; }

        /// <summary>
        /// Gets the MIME type of the file (e.g., "application/pdf", "image/jpeg").
        /// </summary>
        public string ContentType { get; }

        /// <summary>
        /// Gets the file extension (e.g., ".pdf", ".jpg", ".png").
        /// </summary>
        /// <remarks>
        /// The extension includes the leading dot. It may be empty if the file has no extension.
        /// </remarks>
        public string FileExtension { get; }

        /// <summary>
        /// Gets an optional stream containing the file content.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This stream is provided when content-based validation is needed (e.g., virus scanning,
        /// file signature verification). The stream is read-only and should not be disposed or closed.
        /// </para>
        /// <para>
        /// The stream position may be changed during validation, but it's recommended to restore
        /// it to the original position if possible.
        /// </para>
        /// <para>
        /// This may be <c>null</c> if content-based validation is not required or if the content
        /// is not available (e.g., for download validation).
        /// </para>
        /// </remarks>
        public Stream? ContentStream { get; }
    }

    /// <summary>
    /// Represents the result of a file validation operation.
    /// </summary>
    /// <remarks>
    /// This class encapsulates whether validation passed or failed, along with any error messages
    /// or warnings that should be communicated to the user.
    /// </remarks>
    public class ValidationResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationResult"/> class.
        /// </summary>
        /// <param name="isValid">Whether the validation passed.</param>
        /// <param name="errors">List of error messages if validation failed.</param>
        /// <param name="warnings">List of warning messages (non-blocking).</param>
        private ValidationResult(bool isValid, IList<string>? errors = null, IList<string>? warnings = null)
        {
            IsValid = isValid;
            Errors = errors ?? new List<string>();
            Warnings = warnings ?? new List<string>();
        }

        /// <summary>
        /// Gets whether the validation passed.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets the list of error messages if validation failed.
        /// </summary>
        /// <remarks>
        /// Errors are blocking - if any errors are present, the file operation should be rejected.
        /// </remarks>
        public IList<string> Errors { get; }

        /// <summary>
        /// Gets the list of warning messages (non-blocking).
        /// </summary>
        /// <remarks>
        /// Warnings are informational and do not block the file operation. They may be logged
        /// or displayed to the user for awareness.
        /// </remarks>
        public IList<string> Warnings { get; }

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        /// <param name="warnings">Optional warning messages.</param>
        /// <returns>A validation result indicating success.</returns>
        public static ValidationResult Success(params string[] warnings)
        {
            return new ValidationResult(true, warnings: warnings);
        }

        /// <summary>
        /// Creates a failed validation result.
        /// </summary>
        /// <param name="errors">Error messages describing why validation failed.</param>
        /// <returns>A validation result indicating failure.</returns>
        public static ValidationResult Failed(params string[] errors)
        {
            if (errors == null || errors.Length == 0)
            {
                throw new System.ArgumentException("At least one error message is required.", nameof(errors));
            }

            return new ValidationResult(false, errors: errors);
        }

        /// <summary>
        /// Creates a failed validation result with a single error message.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>A validation result indicating failure.</returns>
        public static ValidationResult Failed(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new System.ArgumentException("Error message cannot be null or empty.", nameof(errorMessage));
            }

            return new ValidationResult(false, errors: new[] { errorMessage });
        }
    }
}

