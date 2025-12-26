//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.FileStorage.Contract
{
    /// <summary>
    /// Represents metadata about a stored file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface provides standard file metadata that is common across different storage
    /// providers. Provider-specific metadata can be accessed via the <see cref="CustomProperties"/>
    /// dictionary.
    /// </para>
    /// <para>
    /// All dates and times are in UTC. File sizes are in bytes.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var metadata = await fileStorage.GetMetadataAsync("documents/file.pdf", cancellationToken);
    /// if (metadata != null)
    /// {
    ///     Console.WriteLine($"File: {metadata.Name}");
    ///     Console.WriteLine($"Size: {metadata.Size} bytes");
    ///     Console.WriteLine($"Content Type: {metadata.ContentType}");
    ///     Console.WriteLine($"Created: {metadata.CreatedAt}");
    ///     Console.WriteLine($"Modified: {metadata.ModifiedAt}");
    /// }
    /// </code>
    /// </example>
    public interface IFileMetadata
    {
        /// <summary>
        /// Gets the full path of the file in the storage system.
        /// </summary>
        /// <remarks>
        /// This is the path used to identify the file in storage operations (upload, download, delete, etc.).
        /// </remarks>
        string FilePath { get; }

        /// <summary>
        /// Gets the name of the file (without directory path).
        /// </summary>
        /// <remarks>
        /// This is typically the last segment of the file path (e.g., "document.pdf" from "documents/document.pdf").
        /// </remarks>
        string Name { get; }

        /// <summary>
        /// Gets the size of the file in bytes.
        /// </summary>
        long Size { get; }

        /// <summary>
        /// Gets the MIME type of the file (e.g., "application/pdf", "image/jpeg").
        /// </summary>
        /// <remarks>
        /// This may be null if the content type is not available or not set.
        /// </remarks>
        string? ContentType { get; }

        /// <summary>
        /// Gets the date and time when the file was created (in UTC).
        /// </summary>
        /// <remarks>
        /// This may be the same as <see cref="ModifiedAt"/> if the provider doesn't track creation time separately.
        /// </remarks>
        DateTimeOffset CreatedAt { get; }

        /// <summary>
        /// Gets the date and time when the file was last modified (in UTC).
        /// </summary>
        /// <remarks>
        /// This is typically updated whenever the file content is changed.
        /// </remarks>
        DateTimeOffset ModifiedAt { get; }

        /// <summary>
        /// Gets the ETag (entity tag) of the file, if available.
        /// </summary>
        /// <remarks>
        /// <para>
        /// ETags are used for optimistic concurrency control. They change whenever the file content changes.
        /// </para>
        /// <para>
        /// This may be null if the provider doesn't support ETags.
        /// </para>
        /// </remarks>
        string? ETag { get; }

        /// <summary>
        /// Gets custom properties/metadata associated with the file.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This dictionary contains provider-specific or user-defined metadata. Common keys include:
        /// </para>
        /// <list type="bullet">
        /// <item><c>"CacheControl"</c> - Cache control header value</item>
        /// <item><c>"ContentEncoding"</c> - Content encoding (e.g., "gzip")</item>
        /// <item><c>"ContentLanguage"</c> - Content language (e.g., "en-US")</item>
        /// <item><c>"ContentDisposition"</c> - Content disposition header value</item>
        /// </list>
        /// <para>
        /// Provider-specific metadata (e.g., Azure Blob Storage tags, S3 metadata) are also included here.
        /// </para>
        /// </remarks>
        IDictionary<string, string> CustomProperties { get; }
    }
}

