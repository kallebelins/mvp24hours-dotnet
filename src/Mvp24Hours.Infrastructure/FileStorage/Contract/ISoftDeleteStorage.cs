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
    /// Interface for soft delete operations on files.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Soft delete marks files as deleted without permanently removing them from storage.
    /// This allows:
    /// - Recovery of accidentally deleted files
    /// - Audit trails
    /// - Compliance with data retention policies
    /// </para>
    /// <para>
    /// Soft-deleted files are typically hidden from normal operations but can be restored
    /// or permanently deleted later.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Soft delete a file
    /// await softDeleteStorage.SoftDeleteAsync("document.pdf", cancellationToken);
    /// 
    /// // List soft-deleted files
    /// var deletedFiles = await softDeleteStorage.ListDeletedFilesAsync("documents/");
    /// await foreach (var file in deletedFiles)
    /// {
    ///     Console.WriteLine($"Deleted: {file.FilePath} at {file.DeletedAt}");
    /// }
    /// 
    /// // Restore a soft-deleted file
    /// await softDeleteStorage.RestoreAsync("document.pdf", cancellationToken);
    /// 
    /// // Permanently delete a soft-deleted file
    /// await softDeleteStorage.PermanentlyDeleteAsync("document.pdf", cancellationToken);
    /// </code>
    /// </example>
    public interface ISoftDeleteStorage
    {
        /// <summary>
        /// Soft deletes a file (marks it as deleted without removing it).
        /// </summary>
        /// <param name="filePath">The path of the file to soft delete.</param>
        /// <param name="reason">Optional reason for deletion (for audit purposes).</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns><c>true</c> if the file was soft deleted; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// After soft deletion, the file is no longer accessible through normal operations
        /// but can be restored or permanently deleted later.
        /// </remarks>
        Task<bool> SoftDeleteAsync(
            string filePath,
            string? reason = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Restores a soft-deleted file, making it accessible again.
        /// </summary>
        /// <param name="filePath">The path of the file to restore.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns><c>true</c> if the file was restored; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// If a file with the same path already exists, the restore operation may fail
        /// or overwrite the existing file (provider-dependent).
        /// </remarks>
        Task<bool> RestoreAsync(
            string filePath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Permanently deletes a soft-deleted file (hard delete).
        /// </summary>
        /// <param name="filePath">The path of the file to permanently delete.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns><c>true</c> if the file was permanently deleted; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This operation is irreversible. The file is permanently removed from storage.
        /// </remarks>
        Task<bool> PermanentlyDeleteAsync(
            string filePath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all soft-deleted files in a directory.
        /// </summary>
        /// <param name="directoryPath">The directory path to search (empty for root).</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>An async enumerable of soft-deleted file metadata.</returns>
        IAsyncEnumerable<ISoftDeletedFile> ListDeletedFilesAsync(
            string directoryPath = "",
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets metadata for a soft-deleted file.
        /// </summary>
        /// <param name="filePath">The path of the soft-deleted file.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The soft-deleted file metadata, or null if the file doesn't exist or isn't deleted.</returns>
        Task<ISoftDeletedFile?> GetDeletedFileAsync(
            string filePath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Permanently deletes all soft-deleted files older than the specified retention period.
        /// </summary>
        /// <param name="retentionPeriod">Files deleted before this period will be permanently deleted.</param>
        /// <param name="directoryPath">Optional directory path to limit the cleanup scope.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The number of files permanently deleted.</returns>
        /// <remarks>
        /// This is useful for implementing data retention policies and freeing up storage space.
        /// </remarks>
        Task<int> CleanupOldDeletedFilesAsync(
            TimeSpan retentionPeriod,
            string? directoryPath = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a file is soft-deleted.
        /// </summary>
        /// <param name="filePath">The path of the file to check.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns><c>true</c> if the file is soft-deleted; otherwise, <c>false</c>.</returns>
        Task<bool> IsSoftDeletedAsync(
            string filePath,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a soft-deleted file.
    /// </summary>
    public interface ISoftDeletedFile
    {
        /// <summary>
        /// Gets the file path.
        /// </summary>
        string FilePath { get; }

        /// <summary>
        /// Gets the original file metadata before deletion.
        /// </summary>
        IFileMetadata OriginalMetadata { get; }

        /// <summary>
        /// Gets when the file was soft-deleted.
        /// </summary>
        DateTimeOffset DeletedAt { get; }

        /// <summary>
        /// Gets the reason for deletion, if provided.
        /// </summary>
        string? DeletionReason { get; }

        /// <summary>
        /// Gets who deleted the file, if available.
        /// </summary>
        string? DeletedBy { get; }
    }
}

