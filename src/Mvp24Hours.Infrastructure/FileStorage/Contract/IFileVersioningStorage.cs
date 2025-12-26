//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.FileStorage.Contract
{
    /// <summary>
    /// Interface for file versioning operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// File versioning allows maintaining multiple versions of the same file, enabling:
    /// - Rollback to previous versions
    /// - History tracking
    /// - Recovery from accidental overwrites
    /// </para>
    /// <para>
    /// When versioning is enabled, each update creates a new version while preserving
    /// previous versions. Versions can be listed, retrieved, and restored.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Upload a file (creates version 1)
    /// await fileStorage.UploadAsync("document.pdf", content1, "application/pdf");
    /// 
    /// // Update the file (creates version 2)
    /// await fileStorage.UploadAsync("document.pdf", content2, "application/pdf");
    /// 
    /// // List all versions
    /// var versions = await versioningStorage.ListVersionsAsync("document.pdf");
    /// foreach (var version in versions)
    /// {
    ///     Console.WriteLine($"Version {version.VersionNumber}: {version.CreatedAt}");
    /// }
    /// 
    /// // Restore a previous version
    /// await versioningStorage.RestoreVersionAsync("document.pdf", versionId: 1);
    /// </code>
    /// </example>
    public interface IFileVersioningStorage
    {
        /// <summary>
        /// Enables versioning for a file.
        /// </summary>
        /// <param name="filePath">The path of the file.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns><c>true</c> if versioning was enabled; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// Once enabled, all subsequent updates to the file will create new versions.
        /// Existing versions are preserved.
        /// </remarks>
        Task<bool> EnableVersioningAsync(
            string filePath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Disables versioning for a file.
        /// </summary>
        /// <param name="filePath">The path of the file.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns><c>true</c> if versioning was disabled; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// Disabling versioning does not delete existing versions, but prevents new versions
        /// from being created. Existing versions can still be accessed and restored.
        /// </remarks>
        Task<bool> DisableVersioningAsync(
            string filePath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all versions of a file.
        /// </summary>
        /// <param name="filePath">The path of the file.</param>
        /// <param name="includeDeleted">Whether to include deleted versions.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>An async enumerable of file versions, ordered by version number (newest first).</returns>
        /// <remarks>
        /// Versions are typically numbered sequentially (1, 2, 3, ...) with higher numbers
        /// representing newer versions. The current version is usually the highest number.
        /// </remarks>
        IAsyncEnumerable<IFileVersion> ListVersionsAsync(
            string filePath,
            bool includeDeleted = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets metadata for a specific version of a file.
        /// </summary>
        /// <param name="filePath">The path of the file.</param>
        /// <param name="versionId">The version identifier (version number or version ID).</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The file version metadata, or null if the version doesn't exist.</returns>
        Task<IFileVersion?> GetVersionAsync(
            string filePath,
            string versionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads a specific version of a file.
        /// </summary>
        /// <param name="filePath">The path of the file.</param>
        /// <param name="versionId">The version identifier.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The file download result, or null if the version doesn't exist.</returns>
        Task<Results.FileDownloadResult?> DownloadVersionAsync(
            string filePath,
            string versionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Restores a specific version of a file, making it the current version.
        /// </summary>
        /// <param name="filePath">The path of the file.</param>
        /// <param name="versionId">The version identifier to restore.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns><c>true</c> if the version was restored; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// Restoring a version creates a new version (the restored content becomes the new current version).
        /// The original version being restored is preserved in the version history.
        /// </remarks>
        Task<bool> RestoreVersionAsync(
            string filePath,
            string versionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Permanently deletes a specific version of a file.
        /// </summary>
        /// <param name="filePath">The path of the file.</param>
        /// <param name="versionId">The version identifier to delete.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns><c>true</c> if the version was deleted; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This operation is irreversible. The version is permanently removed from storage.
        /// </remarks>
        Task<bool> DeleteVersionAsync(
            string filePath,
            string versionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current version number for a file.
        /// </summary>
        /// <param name="filePath">The path of the file.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The current version number, or null if the file doesn't exist or versioning is disabled.</returns>
        Task<int?> GetCurrentVersionNumberAsync(
            string filePath,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a version of a file.
    /// </summary>
    public interface IFileVersion
    {
        /// <summary>
        /// Gets the version identifier (unique ID for this version).
        /// </summary>
        string VersionId { get; }

        /// <summary>
        /// Gets the version number (sequential number: 1, 2, 3, ...).
        /// </summary>
        int VersionNumber { get; }

        /// <summary>
        /// Gets the file metadata for this version.
        /// </summary>
        IFileMetadata Metadata { get; }

        /// <summary>
        /// Gets whether this version is the current (active) version.
        /// </summary>
        bool IsCurrentVersion { get; }

        /// <summary>
        /// Gets whether this version has been deleted.
        /// </summary>
        bool IsDeleted { get; }

        /// <summary>
        /// Gets when this version was created.
        /// </summary>
        DateTimeOffset CreatedAt { get; }

        /// <summary>
        /// Gets optional description or reason for this version.
        /// </summary>
        string? Description { get; }
    }
}

