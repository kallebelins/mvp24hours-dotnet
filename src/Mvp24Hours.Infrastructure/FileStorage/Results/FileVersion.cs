//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.FileStorage.Contract;

namespace Mvp24Hours.Infrastructure.FileStorage.Results;

/// <summary>
/// Implementation of <see cref="IFileVersion"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="FileVersion"/> class.
/// </remarks>
/// <param name="versionId">The version identifier.</param>
/// <param name="versionNumber">The version number.</param>
/// <param name="metadata">The file metadata.</param>
/// <param name="isCurrentVersion">Whether this is the current version.</param>
/// <param name="isDeleted">Whether this version is deleted.</param>
/// <param name="createdAt">When this version was created.</param>
/// <param name="description">Optional description.</param>
public class FileVersion(
    string versionId,
    int versionNumber,
    IFileMetadata metadata,
    bool isCurrentVersion,
    bool isDeleted,
    DateTimeOffset createdAt,
    string? description = null) : IFileVersion
{

    /// <inheritdoc/>
    public string VersionId { get; } = versionId ?? throw new ArgumentNullException(nameof(versionId));

    /// <inheritdoc/>
    public int VersionNumber { get; } = versionNumber;

    /// <inheritdoc/>
    public IFileMetadata Metadata { get; } = metadata ?? throw new ArgumentNullException(nameof(metadata));

    /// <inheritdoc/>
    public bool IsCurrentVersion { get; } = isCurrentVersion;

    /// <inheritdoc/>
    public bool IsDeleted { get; } = isDeleted;

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; } = createdAt;

    /// <inheritdoc/>
    public string? Description { get; } = description;
}

