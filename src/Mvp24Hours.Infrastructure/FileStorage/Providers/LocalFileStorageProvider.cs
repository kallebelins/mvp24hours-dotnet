//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.FileStorage.Contract;
using Mvp24Hours.Infrastructure.FileStorage.Options;
using Mvp24Hours.Infrastructure.FileStorage.Results;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.FileStorage.Providers
{
    /// <summary>
    /// Local filesystem file storage provider.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provider stores files on the local filesystem. It's suitable for single-server
    /// deployments, development environments, and scenarios where shared storage is not required.
    /// </para>
    /// <para>
    /// <strong>Features:</strong>
    /// - Automatic directory creation
    /// - Thread-safe file operations
    /// - Support for streaming uploads/downloads
    /// - Full metadata support (creation time, modification time, etc.)
    /// </para>
    /// <para>
    /// <strong>Path Handling:</strong>
    /// File paths are relative to the base path specified in options. The provider automatically
    /// creates necessary directory structures when uploading files.
    /// </para>
    /// </remarks>
    public class LocalFileStorageProvider : IFileStorage
    {
        private readonly FileStorageOptions _options;
        private readonly IFileValidator? _validator;
        private readonly string _baseDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalFileStorageProvider"/> class.
        /// </summary>
        /// <param name="options">The file storage options.</param>
        /// <param name="validator">Optional file validator.</param>
        /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
        /// <exception cref="ArgumentException">Thrown when base path is invalid.</exception>
        public LocalFileStorageProvider(
            FileStorageOptions options,
            IFileValidator? validator = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _validator = validator;

            // Determine base directory
            if (!string.IsNullOrWhiteSpace(options.BasePath))
            {
                _baseDirectory = Path.IsPathRooted(options.BasePath)
                    ? options.BasePath
                    : Path.Combine(Directory.GetCurrentDirectory(), options.BasePath);
            }
            else
            {
                _baseDirectory = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            }

            // Create base directory if it doesn't exist
            if (_options.CreateDirectoriesIfNotExists && !Directory.Exists(_baseDirectory))
            {
                Directory.CreateDirectory(_baseDirectory);
            }
        }

        /// <inheritdoc/>
        public async Task<FileUploadResult> UploadAsync(
            string filePath,
            byte[] content,
            string contentType,
            IDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return FileUploadResult.Failed("File path cannot be null or empty.");
            }

            if (content == null)
            {
                return FileUploadResult.Failed("File content cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = _options.DefaultContentType;
            }

            var fullPath = GetFullPath(filePath);

            // Validate file
            if (_validator != null)
            {
                var validationContext = new FileValidationContext(
                    Path.GetFileName(filePath),
                    content.Length,
                    contentType,
                    Path.GetExtension(filePath),
                    new MemoryStream(content));

                var validationResult = await _validator.ValidateAsync(validationContext, cancellationToken);
                if (!validationResult.IsValid)
                {
                    return FileUploadResult.Failed($"File validation failed: {string.Join("; ", validationResult.Errors)}");
                }
            }

            try
            {
                // Create directory if needed
                var directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory) && _options.CreateDirectoriesIfNotExists && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Check if file exists and overwrite policy
                if (File.Exists(fullPath) && !_options.OverwriteExistingFiles)
                {
                    return FileUploadResult.Failed($"File already exists: {filePath}");
                }

                // Write file
                await File.WriteAllBytesAsync(fullPath, content, cancellationToken);

                // Get file info
                var fileInfo = new FileInfo(fullPath);
                var fileMetadata = new FileMetadata(
                    filePath,
                    Path.GetFileName(filePath),
                    fileInfo.Length,
                    contentType,
                    fileInfo.CreationTimeUtc,
                    fileInfo.LastWriteTimeUtc,
                    fileInfo.LastWriteTimeUtc.Ticks.ToString(),
                    metadata);

                return FileUploadResult.Successful(filePath, fileMetadata);
            }
            catch (Exception ex)
            {
                return FileUploadResult.Failed($"Failed to upload file: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<FileUploadResult> UploadFromStreamAsync(
            string filePath,
            Stream stream,
            string contentType,
            IDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return FileUploadResult.Failed("File path cannot be null or empty.");
            }

            if (stream == null)
            {
                return FileUploadResult.Failed("Stream cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = _options.DefaultContentType;
            }

            var fullPath = GetFullPath(filePath);

            try
            {
                // Create directory if needed
                var directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory) && _options.CreateDirectoriesIfNotExists && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Check if file exists and overwrite policy
                if (File.Exists(fullPath) && !_options.OverwriteExistingFiles)
                {
                    return FileUploadResult.Failed($"File already exists: {filePath}");
                }

                // Write file from stream
                using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
                await stream.CopyToAsync(fileStream, cancellationToken);
                await fileStream.FlushAsync(cancellationToken);

                // Get file info
                var fileInfo = new FileInfo(fullPath);
                var fileMetadata = new FileMetadata(
                    filePath,
                    Path.GetFileName(filePath),
                    fileInfo.Length,
                    contentType,
                    fileInfo.CreationTimeUtc,
                    fileInfo.LastWriteTimeUtc,
                    fileInfo.LastWriteTimeUtc.Ticks.ToString(),
                    metadata);

                return FileUploadResult.Successful(filePath, fileMetadata);
            }
            catch (Exception ex)
            {
                return FileUploadResult.Failed($"Failed to upload file from stream: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<FileUploadResult> UploadFromChunksAsync(
            string filePath,
            IAsyncEnumerable<byte[]> chunks,
            string contentType,
            IDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return FileUploadResult.Failed("File path cannot be null or empty.");
            }

            if (chunks == null)
            {
                return FileUploadResult.Failed("Chunks cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = _options.DefaultContentType;
            }

            var fullPath = GetFullPath(filePath);

            try
            {
                // Create directory if needed
                var directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory) && _options.CreateDirectoriesIfNotExists && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Check if file exists and overwrite policy
                if (File.Exists(fullPath) && !_options.OverwriteExistingFiles)
                {
                    return FileUploadResult.Failed($"File already exists: {filePath}");
                }

                // Write file from chunks
                using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
                await foreach (var chunk in chunks.WithCancellation(cancellationToken))
                {
                    if (chunk != null && chunk.Length > 0)
                    {
                        await fileStream.WriteAsync(chunk, 0, chunk.Length, cancellationToken);
                    }
                }
                await fileStream.FlushAsync(cancellationToken);

                // Get file info
                var fileInfo = new FileInfo(fullPath);
                var fileMetadata = new FileMetadata(
                    filePath,
                    Path.GetFileName(filePath),
                    fileInfo.Length,
                    contentType,
                    fileInfo.CreationTimeUtc,
                    fileInfo.LastWriteTimeUtc,
                    fileInfo.LastWriteTimeUtc.Ticks.ToString(),
                    metadata);

                return FileUploadResult.Successful(filePath, fileMetadata);
            }
            catch (Exception ex)
            {
                return FileUploadResult.Failed($"Failed to upload file from chunks: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public Task<FileDownloadResult> DownloadAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Task.FromResult(FileDownloadResult.Failed("File path cannot be null or empty."));
            }

            var fullPath = GetFullPath(filePath);

            if (!File.Exists(fullPath))
            {
                return Task.FromResult(FileDownloadResult.NotFound(filePath));
            }

            try
            {
                var content = File.ReadAllBytes(fullPath);
                var fileInfo = new FileInfo(fullPath);

                var metadata = new FileMetadata(
                    filePath,
                    Path.GetFileName(filePath),
                    fileInfo.Length,
                    GetContentType(fullPath),
                    fileInfo.CreationTimeUtc,
                    fileInfo.LastWriteTimeUtc,
                    fileInfo.LastWriteTimeUtc.Ticks.ToString(),
                    null);

                return Task.FromResult(FileDownloadResult.Successful(content, metadata));
            }
            catch (Exception ex)
            {
                return Task.FromResult(FileDownloadResult.Failed($"Failed to download file: {ex.Message}", ex));
            }
        }

        /// <inheritdoc/>
        public Task<FileDownloadResult> DownloadToStreamAsync(
            string filePath,
            Stream destinationStream,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Task.FromResult(FileDownloadResult.Failed("File path cannot be null or empty."));
            }

            if (destinationStream == null)
            {
                return Task.FromResult(FileDownloadResult.Failed("Destination stream cannot be null."));
            }

            var fullPath = GetFullPath(filePath);

            if (!File.Exists(fullPath))
            {
                return Task.FromResult(FileDownloadResult.NotFound(filePath));
            }

            return Task.Run(async () =>
            {
                try
                {
                    using var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                    await fileStream.CopyToAsync(destinationStream, cancellationToken);
                    await destinationStream.FlushAsync(cancellationToken);

                    var fileInfo = new FileInfo(fullPath);
                    var metadata = new FileMetadata(
                        filePath,
                        Path.GetFileName(filePath),
                        fileInfo.Length,
                        GetContentType(fullPath),
                        fileInfo.CreationTimeUtc,
                        fileInfo.LastWriteTimeUtc,
                        fileInfo.LastWriteTimeUtc.Ticks.ToString(),
                        null);

                    return FileDownloadResult.Successful(Array.Empty<byte>(), metadata);
                }
                catch (Exception ex)
                {
                    return FileDownloadResult.Failed($"Failed to download file to stream: {ex.Message}", ex);
                }
            }, cancellationToken);
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<byte[]> DownloadAsChunksAsync(
            string filePath,
            int chunkSize = 65536,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                yield break;
            }

            var fullPath = GetFullPath(filePath);

            if (!File.Exists(fullPath))
            {
                yield break;
            }

            using var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            var buffer = new byte[chunkSize];
            int bytesRead;

            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, chunkSize, cancellationToken)) > 0)
            {
                if (bytesRead < chunkSize)
                {
                    var chunk = new byte[bytesRead];
                    Array.Copy(buffer, chunk, bytesRead);
                    yield return chunk;
                }
                else
                {
                    yield return buffer;
                }
            }
        }

        /// <inheritdoc/>
        public Task<bool> ExistsAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Task.FromResult(false);
            }

            var fullPath = GetFullPath(filePath);
            return Task.FromResult(File.Exists(fullPath));
        }

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Task.FromResult(false);
            }

            var fullPath = GetFullPath(filePath);

            if (!File.Exists(fullPath))
            {
                return Task.FromResult(false);
            }

            try
            {
                File.Delete(fullPath);
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        /// <inheritdoc/>
        public Task<IFileMetadata?> GetMetadataAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Task.FromResult<IFileMetadata?>(null);
            }

            var fullPath = GetFullPath(filePath);

            if (!File.Exists(fullPath))
            {
                return Task.FromResult<IFileMetadata?>(null);
            }

            try
            {
                var fileInfo = new FileInfo(fullPath);
                var metadata = new FileMetadata(
                    filePath,
                    Path.GetFileName(filePath),
                    fileInfo.Length,
                    GetContentType(fullPath),
                    fileInfo.CreationTimeUtc,
                    fileInfo.LastWriteTimeUtc,
                    fileInfo.LastWriteTimeUtc.Ticks.ToString(),
                    null);

                return Task.FromResult<IFileMetadata?>(metadata);
            }
            catch
            {
                return Task.FromResult<IFileMetadata?>(null);
            }
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<IFileMetadata> ListFilesAsync(
            string directoryPath = "",
            bool recursive = false,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var fullPath = GetFullPath(directoryPath ?? string.Empty);

            if (!Directory.Exists(fullPath))
            {
                yield break;
            }

            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.GetFiles(fullPath, "*", searchOption);

            foreach (var file in files)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                FileMetadata? metadata = null;
                try
                {
                    var relativePath = Path.GetRelativePath(_baseDirectory, file).Replace('\\', '/');
                    var fileInfo = new FileInfo(file);

                    metadata = new FileMetadata(
                        relativePath,
                        fileInfo.Name,
                        fileInfo.Length,
                        GetContentType(file),
                        fileInfo.CreationTimeUtc,
                        fileInfo.LastWriteTimeUtc,
                        fileInfo.LastWriteTimeUtc.Ticks.ToString(),
                        null);
                }
                catch
                {
                    // Skip files that can't be accessed
                    continue;
                }

                if (metadata != null)
                {
                    yield return metadata;
                }
            }
        }

        /// <inheritdoc/>
        public Task<bool> CopyAsync(
            string sourcePath,
            string destinationPath,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(destinationPath))
            {
                return Task.FromResult(false);
            }

            var sourceFullPath = GetFullPath(sourcePath);
            var destinationFullPath = GetFullPath(destinationPath);

            if (!File.Exists(sourceFullPath))
            {
                return Task.FromResult(false);
            }

            try
            {
                var directory = Path.GetDirectoryName(destinationFullPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.Copy(sourceFullPath, destinationFullPath, _options.OverwriteExistingFiles);
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        /// <inheritdoc/>
        public Task<bool> MoveAsync(
            string sourcePath,
            string destinationPath,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(destinationPath))
            {
                return Task.FromResult(false);
            }

            var sourceFullPath = GetFullPath(sourcePath);
            var destinationFullPath = GetFullPath(destinationPath);

            if (!File.Exists(sourceFullPath))
            {
                return Task.FromResult(false);
            }

            try
            {
                var directory = Path.GetDirectoryName(destinationFullPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.Move(sourceFullPath, destinationFullPath, _options.OverwriteExistingFiles);
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        private string GetFullPath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return _baseDirectory;
            }

            // Normalize path separators
            filePath = filePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

            // Remove leading directory separators
            filePath = filePath.TrimStart(Path.DirectorySeparatorChar);

            // Combine with base directory
            var fullPath = Path.Combine(_baseDirectory, filePath);

            // Ensure path is within base directory (security check)
            var baseDirFull = Path.GetFullPath(_baseDirectory);
            var fullPathNormalized = Path.GetFullPath(fullPath);

            if (!fullPathNormalized.StartsWith(baseDirFull, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException($"Access denied: Path is outside base directory.");
            }

            return fullPathNormalized;
        }

        private string? GetContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".txt" => "text/plain",
                ".json" => "application/json",
                ".xml" => "application/xml",
                ".zip" => "application/zip",
                _ => _options.DefaultContentType
            };
        }
    }
}

