//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.FileStorage.Contract;

namespace Mvp24Hours.Infrastructure.FileStorage.Results;

/// <summary>
/// Implementation of <see cref="ISoftDeletedFile"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SoftDeletedFile"/> class.
/// </remarks>
/// <param name="filePath">The file path.</param>
/// <param name="originalMetadata">The original file metadata.</param>
/// <param name="deletedAt">When the file was deleted.</param>
/// <param name="deletionReason">Optional reason for deletion.</param>
/// <param name="deletedBy">Optional identifier of who deleted the file.</param>
public class SoftDeletedFile(
    string filePath,
    IFileMetadata originalMetadata,
    DateTimeOffset deletedAt,
    string? deletionReason = null,
    string? deletedBy = null) : ISoftDeletedFile
{

    /// <inheritdoc/>
    public string FilePath { get; } = filePath ?? throw new ArgumentNullException(nameof(filePath));

    /// <inheritdoc/>
    public IFileMetadata OriginalMetadata { get; } = originalMetadata ?? throw new ArgumentNullException(nameof(originalMetadata));

    /// <inheritdoc/>
    public DateTimeOffset DeletedAt { get; } = deletedAt;

    /// <inheritdoc/>
    public string? DeletionReason { get; } = deletionReason;

    /// <inheritdoc/>
    public string? DeletedBy { get; } = deletedBy;
}

