//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.Infrastructure.FileStorage.Options
{
    /// <summary>
    /// Configuration options for file storage operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These options control various aspects of file storage behavior, including validation,
    /// path handling, and provider-specific settings. Options can be configured per provider
    /// or globally.
    /// </para>
    /// <para>
    /// <strong>Validation:</strong>
    /// File validation is performed before uploads. Files that fail validation are rejected
    /// with appropriate error messages. Multiple validators can be registered and executed in order.
    /// </para>
    /// <para>
    /// <strong>Path Handling:</strong>
    /// The base path is prepended to all file paths. This allows organizing files into logical
    /// directories or containers without requiring callers to specify the full path each time.
    /// </para>
    /// </remarks>
    public class FileStorageOptions
    {
        /// <summary>
        /// Gets or sets the base path/prefix for all file operations.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This path is prepended to all file paths passed to storage operations. For example,
        /// if BasePath is "documents" and you upload to "file.pdf", the actual path will be
        /// "documents/file.pdf".
        /// </para>
        /// <para>
        /// For cloud storage providers, this may correspond to a container/bucket name or a prefix
        /// within a container/bucket.
        /// </para>
        /// <para>
        /// Use forward slashes (/) as path separators. Trailing slashes are automatically handled.
        /// </para>
        /// <para>
        /// Default is empty string (no base path).
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// options.BasePath = "uploads/documents"; // All files will be stored under this path
        /// </code>
        /// </example>
        public string BasePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the maximum allowed file size in bytes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Files larger than this size will be rejected during validation. Set to <c>null</c>
        /// to disable size validation (not recommended for production).
        /// </para>
        /// <para>
        /// Default is 100MB (104,857,600 bytes).
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// options.MaxFileSize = 10 * 1024 * 1024; // 10MB limit
        /// </code>
        /// </example>
        public long? MaxFileSize { get; set; } = 100 * 1024 * 1024; // 100MB

        /// <summary>
        /// Gets or sets the minimum allowed file size in bytes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Files smaller than this size will be rejected during validation. Set to <c>null</c>
        /// to disable minimum size validation.
        /// </para>
        /// <para>
        /// Default is <c>null</c> (no minimum size).
        /// </para>
        /// </remarks>
        public long? MinFileSize { get; set; } = null;

        /// <summary>
        /// Gets or sets the list of allowed file extensions (without leading dot).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Files with extensions not in this list will be rejected during validation. The comparison
        /// is case-insensitive. Set to <c>null</c> or empty list to allow all extensions (not recommended
        /// for production).
        /// </para>
        /// <para>
        /// Extensions should be specified without the leading dot (e.g., "pdf", "jpg", "png").
        /// </para>
        /// <para>
        /// Default is <c>null</c> (all extensions allowed).
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// options.AllowedExtensions = new[] { "pdf", "doc", "docx", "jpg", "png" };
        /// </code>
        /// </example>
        public IList<string>? AllowedExtensions { get; set; } = null;

        /// <summary>
        /// Gets or sets the list of blocked file extensions (without leading dot).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Files with extensions in this list will be rejected during validation, even if they
        /// are in the allowed extensions list. The comparison is case-insensitive.
        /// </para>
        /// <para>
        /// Extensions should be specified without the leading dot (e.g., "exe", "bat", "sh").
        /// </para>
        /// <para>
        /// Default is <c>null</c> (no blocked extensions).
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// options.BlockedExtensions = new[] { "exe", "bat", "cmd", "sh", "ps1" }; // Block executables
        /// </code>
        /// </example>
        public IList<string>? BlockedExtensions { get; set; } = null;

        /// <summary>
        /// Gets or sets the list of allowed MIME content types.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Files with content types not in this list will be rejected during validation. The comparison
        /// is case-insensitive. Set to <c>null</c> or empty list to allow all content types (not recommended
        /// for production).
        /// </para>
        /// <para>
        /// Default is <c>null</c> (all content types allowed).
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// options.AllowedContentTypes = new[] { "application/pdf", "image/jpeg", "image/png" };
        /// </code>
        /// </example>
        public IList<string>? AllowedContentTypes { get; set; } = null;

        /// <summary>
        /// Gets or sets the list of blocked MIME content types.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Files with content types in this list will be rejected during validation, even if they
        /// are in the allowed content types list. The comparison is case-insensitive.
        /// </para>
        /// <para>
        /// Default is <c>null</c> (no blocked content types).
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// options.BlockedContentTypes = new[] { "application/x-executable", "application/x-msdownload" };
        /// </code>
        /// </example>
        public IList<string>? BlockedContentTypes { get; set; } = null;

        /// <summary>
        /// Gets or sets whether to create directories/containers automatically if they don't exist.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <c>true</c>, the storage provider will create necessary directory structures or
        /// containers/buckets automatically when uploading files. When <c>false</c>, missing directories
        /// will cause uploads to fail.
        /// </para>
        /// <para>
        /// Default is <c>true</c>.
        /// </para>
        /// </remarks>
        public bool CreateDirectoriesIfNotExists { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to overwrite existing files when uploading.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <c>true</c>, uploading a file to an existing path will overwrite the existing file.
        /// When <c>false</c>, uploading to an existing path will fail (or append a version number,
        /// depending on provider implementation).
        /// </para>
        /// <para>
        /// Default is <c>true</c>.
        /// </para>
        /// </remarks>
        public bool OverwriteExistingFiles { get; set; } = true;

        /// <summary>
        /// Gets or sets the default content type to use when none is provided.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If a file is uploaded without specifying a content type, this default will be used.
        /// If this is also <c>null</c>, the provider may attempt to detect the content type from
        /// the file extension or return an error.
        /// </para>
        /// <para>
        /// Default is "application/octet-stream".
        /// </para>
        /// </remarks>
        public string DefaultContentType { get; set; } = "application/octet-stream";

        /// <summary>
        /// Gets or sets the chunk size for streaming operations in bytes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This controls the size of chunks when uploading/downloading files in chunks via
        /// <see cref="Contract.IFileStorage.UploadFromChunksAsync"/> or
        /// <see cref="Contract.IFileStorage.DownloadAsChunksAsync"/>.
        /// </para>
        /// <para>
        /// Larger chunks reduce the number of operations but increase memory usage. Smaller chunks
        /// reduce memory usage but increase the number of operations.
        /// </para>
        /// <para>
        /// Default is 64KB (65,536 bytes).
        /// </para>
        /// </remarks>
        public int ChunkSize { get; set; } = 65536; // 64KB

        /// <summary>
        /// Gets or sets whether to validate file content (e.g., file signature verification).
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <c>true</c>, validators may perform content-based validation (e.g., verifying that
        /// a JPEG file actually contains JPEG data, not just has a .jpg extension). This requires
        /// reading file content and may impact performance.
        /// </para>
        /// <para>
        /// Default is <c>false</c> (only metadata validation).
        /// </para>
        /// </remarks>
        public bool ValidateFileContent { get; set; } = false;

        /// <summary>
        /// Gets or sets provider-specific options as a dictionary.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This dictionary allows passing provider-specific configuration options that are not
        /// covered by the standard options. The keys and values are provider-dependent.
        /// </para>
        /// <para>
        /// Common provider-specific options:
        /// </para>
        /// <list type="bullet">
        /// <item><strong>Azure Blob Storage:</strong> "PublicAccessLevel", "Metadata", "Tags"</item>
        /// <item><strong>Amazon S3:</strong> "ACL", "StorageClass", "ServerSideEncryption"</item>
        /// <item><strong>Local Storage:</strong> "Permissions", "Owner", "Group"</item>
        /// </list>
        /// </remarks>
        public IDictionary<string, object> ProviderOptions { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Validates the configuration options.
        /// </summary>
        /// <returns>A list of validation errors, or an empty list if valid.</returns>
        /// <remarks>
        /// This method checks for logical inconsistencies in the configuration (e.g., min size
        /// greater than max size, conflicting allowed/blocked lists).
        /// </remarks>
        public IList<string> Validate()
        {
            var errors = new List<string>();

            if (MinFileSize.HasValue && MaxFileSize.HasValue && MinFileSize.Value > MaxFileSize.Value)
            {
                errors.Add("Minimum file size cannot be greater than maximum file size.");
            }

            if (MinFileSize.HasValue && MinFileSize.Value < 0)
            {
                errors.Add("Minimum file size cannot be negative.");
            }

            if (MaxFileSize.HasValue && MaxFileSize.Value < 0)
            {
                errors.Add("Maximum file size cannot be negative.");
            }

            if (ChunkSize <= 0)
            {
                errors.Add("Chunk size must be greater than zero.");
            }

            if (AllowedExtensions != null && BlockedExtensions != null)
            {
                var conflicting = AllowedExtensions.Intersect(BlockedExtensions, StringComparer.OrdinalIgnoreCase).ToList();
                if (conflicting.Any())
                {
                    errors.Add($"Extensions cannot be both allowed and blocked: {string.Join(", ", conflicting)}");
                }
            }

            if (AllowedContentTypes != null && BlockedContentTypes != null)
            {
                var conflicting = AllowedContentTypes.Intersect(BlockedContentTypes, StringComparer.OrdinalIgnoreCase).ToList();
                if (conflicting.Any())
                {
                    errors.Add($"Content types cannot be both allowed and blocked: {string.Join(", ", conflicting)}");
                }
            }

            return errors;
        }

        /// <summary>
        /// Creates default file storage options suitable for most scenarios.
        /// </summary>
        /// <returns>Default options instance.</returns>
        public static FileStorageOptions Default => new();

        /// <summary>
        /// Creates file storage options optimized for image uploads.
        /// </summary>
        /// <returns>Options with image-specific validation rules.</returns>
        public static FileStorageOptions ForImages => new()
        {
            MaxFileSize = 10 * 1024 * 1024, // 10MB
            AllowedExtensions = new[] { "jpg", "jpeg", "png", "gif", "webp", "bmp" },
            AllowedContentTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp", "image/bmp" },
            DefaultContentType = "image/jpeg"
        };

        /// <summary>
        /// Creates file storage options optimized for document uploads.
        /// </summary>
        /// <returns>Options with document-specific validation rules.</returns>
        public static FileStorageOptions ForDocuments => new()
        {
            MaxFileSize = 50 * 1024 * 1024, // 50MB
            AllowedExtensions = new[] { "pdf", "doc", "docx", "xls", "xlsx", "ppt", "pptx", "txt", "rtf" },
            AllowedContentTypes = new[] {
                "application/pdf",
                "application/msword",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/vnd.ms-excel",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "application/vnd.ms-powerpoint",
                "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                "text/plain",
                "application/rtf"
            },
            DefaultContentType = "application/pdf"
        };

        /// <summary>
        /// Creates file storage options optimized for secure uploads (strict validation).
        /// </summary>
        /// <returns>Options with strict security rules.</returns>
        public static FileStorageOptions ForSecureUploads => new()
        {
            MaxFileSize = 5 * 1024 * 1024, // 5MB
            BlockedExtensions = new[] { "exe", "bat", "cmd", "sh", "ps1", "vbs", "js", "jar", "dll", "scr" },
            BlockedContentTypes = new[] {
                "application/x-executable",
                "application/x-msdownload",
                "application/x-msdos-program",
                "application/x-sh",
                "application/x-shellscript"
            },
            ValidateFileContent = true
        };
    }
}

