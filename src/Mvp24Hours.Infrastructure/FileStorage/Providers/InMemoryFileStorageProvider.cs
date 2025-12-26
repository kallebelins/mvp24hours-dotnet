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
    /// In-memory file storage provider for testing and development purposes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provider stores files in memory using a dictionary. It's useful for unit tests,
    /// integration tests, and development scenarios where persistent storage is not required.
    /// </para>
    /// <para>
    /// <strong>Limitations:</strong>
    /// - Files are lost when the application restarts
    /// - Memory usage grows with stored files
    /// - Not suitable for production use
    /// </para>
    /// <para>
    /// <strong>Thread Safety:</strong>
    /// This implementation is thread-safe and can be used concurrently from multiple threads.
    /// </para>
    /// </remarks>
    public class InMemoryFileStorageProvider : IFileStorage
    {
        private readonly FileStorageOptions _options;
        private readonly IFileValidator? _validator;
        private readonly Dictionary<string, FileEntry> _files = new();
        private readonly object _lock = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryFileStorageProvider"/> class.
        /// </summary>
        /// <param name="options">The file storage options.</param>
        /// <param name="validator">Optional file validator.</param>
        /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
        public InMemoryFileStorageProvider(
            FileStorageOptions options,
            IFileValidator? validator = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _validator = validator;
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

            filePath = NormalizePath(filePath);

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

            // Check if file exists and overwrite policy
            lock (_lock)
            {
                if (_files.ContainsKey(filePath) && !_options.OverwriteExistingFiles)
                {
                    return FileUploadResult.Failed($"File already exists: {filePath}");
                }

                var now = DateTimeOffset.UtcNow;
                var fileEntry = new FileEntry
                {
                    Content = (byte[])content.Clone(),
                    ContentType = contentType,
                    CreatedAt = _files.ContainsKey(filePath) ? _files[filePath].CreatedAt : now,
                    ModifiedAt = now,
                    ETag = Guid.NewGuid().ToString("N"),
                    Metadata = metadata != null ? new Dictionary<string, string>(metadata) : new Dictionary<string, string>()
                };

                _files[filePath] = fileEntry;
            }

            var fileMetadata = new FileMetadata(
                filePath,
                Path.GetFileName(filePath),
                content.Length,
                contentType,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                Guid.NewGuid().ToString("N"),
                metadata);

            return FileUploadResult.Successful(filePath, fileMetadata);
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

            filePath = NormalizePath(filePath);

            byte[] content;
            try
            {
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream, cancellationToken);
                content = memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                return FileUploadResult.Failed($"Failed to read stream: {ex.Message}", ex);
            }

            return await UploadAsync(filePath, content, contentType, metadata, cancellationToken);
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

            filePath = NormalizePath(filePath);

            using var memoryStream = new MemoryStream();
            try
            {
                await foreach (var chunk in chunks.WithCancellation(cancellationToken))
                {
                    if (chunk != null && chunk.Length > 0)
                    {
                        await memoryStream.WriteAsync(chunk, 0, chunk.Length, cancellationToken);
                    }
                }

                var content = memoryStream.ToArray();
                return await UploadAsync(filePath, content, contentType, metadata, cancellationToken);
            }
            catch (Exception ex)
            {
                return FileUploadResult.Failed($"Failed to upload from chunks: {ex.Message}", ex);
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

            filePath = NormalizePath(filePath);

            lock (_lock)
            {
                if (!_files.TryGetValue(filePath, out var fileEntry))
                {
                    return Task.FromResult(FileDownloadResult.NotFound(filePath));
                }

                var metadata = new FileMetadata(
                    filePath,
                    Path.GetFileName(filePath),
                    fileEntry.Content.Length,
                    fileEntry.ContentType,
                    fileEntry.CreatedAt,
                    fileEntry.ModifiedAt,
                    fileEntry.ETag,
                    fileEntry.Metadata);

                var content = (byte[])fileEntry.Content.Clone();
                return Task.FromResult(FileDownloadResult.Successful(content, metadata));
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

            filePath = NormalizePath(filePath);

            lock (_lock)
            {
                if (!_files.TryGetValue(filePath, out var fileEntry))
                {
                    return Task.FromResult(FileDownloadResult.NotFound(filePath));
                }

                return Task.Run(async () =>
                {
                    try
                    {
                        await destinationStream.WriteAsync(fileEntry.Content, 0, fileEntry.Content.Length, cancellationToken);
                        await destinationStream.FlushAsync(cancellationToken);

                        var metadata = new FileMetadata(
                            filePath,
                            Path.GetFileName(filePath),
                            fileEntry.Content.Length,
                            fileEntry.ContentType,
                            fileEntry.CreatedAt,
                            fileEntry.ModifiedAt,
                            fileEntry.ETag,
                            fileEntry.Metadata);

                        return FileDownloadResult.Successful(Array.Empty<byte>(), metadata);
                    }
                    catch (Exception ex)
                    {
                        return FileDownloadResult.Failed($"Failed to write to stream: {ex.Message}", ex);
                    }
                }, cancellationToken);
            }
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

            filePath = NormalizePath(filePath);

            FileEntry? fileEntry;
            lock (_lock)
            {
                if (!_files.TryGetValue(filePath, out fileEntry))
                {
                    yield break;
                }
            }

            var content = fileEntry.Content;
            var offset = 0;

            while (offset < content.Length && !cancellationToken.IsCancellationRequested)
            {
                var remaining = content.Length - offset;
                var currentChunkSize = Math.Min(chunkSize, remaining);
                var chunk = new byte[currentChunkSize];
                Array.Copy(content, offset, chunk, 0, currentChunkSize);
                offset += currentChunkSize;
                yield return chunk;
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

            filePath = NormalizePath(filePath);

            lock (_lock)
            {
                return Task.FromResult(_files.ContainsKey(filePath));
            }
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

            filePath = NormalizePath(filePath);

            lock (_lock)
            {
                return Task.FromResult(_files.Remove(filePath));
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

            filePath = NormalizePath(filePath);

            lock (_lock)
            {
                if (!_files.TryGetValue(filePath, out var fileEntry))
                {
                    return Task.FromResult<IFileMetadata?>(null);
                }

                var metadata = new FileMetadata(
                    filePath,
                    Path.GetFileName(filePath),
                    fileEntry.Content.Length,
                    fileEntry.ContentType,
                    fileEntry.CreatedAt,
                    fileEntry.ModifiedAt,
                    fileEntry.ETag,
                    fileEntry.Metadata);

                return Task.FromResult<IFileMetadata?>(metadata);
            }
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<IFileMetadata> ListFilesAsync(
            string directoryPath = "",
            bool recursive = false,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            directoryPath = NormalizePath(directoryPath ?? string.Empty);
            if (!string.IsNullOrEmpty(directoryPath) && !directoryPath.EndsWith("/"))
            {
                directoryPath += "/";
            }

            List<KeyValuePair<string, FileEntry>> matchingFiles;

            lock (_lock)
            {
                matchingFiles = _files
                    .Where(kvp => kvp.Key.StartsWith(directoryPath, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            foreach (var kvp in matchingFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                var filePath = kvp.Key;
                var fileEntry = kvp.Value;

                // Check if recursive or direct child
                if (!recursive)
                {
                    var relativePath = filePath.Substring(directoryPath.Length);
                    if (relativePath.Contains("/"))
                    {
                        continue; // Skip files in subdirectories
                    }
                }

                var metadata = new FileMetadata(
                    filePath,
                    Path.GetFileName(filePath),
                    fileEntry.Content.Length,
                    fileEntry.ContentType,
                    fileEntry.CreatedAt,
                    fileEntry.ModifiedAt,
                    fileEntry.ETag,
                    fileEntry.Metadata);

                yield return metadata;
            }
        }

        /// <inheritdoc/>
        public Task<bool> CopyAsync(
            string sourcePath,
            string destinationPath,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return Task.FromResult(false);
            }

            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                return Task.FromResult(false);
            }

            sourcePath = NormalizePath(sourcePath);
            destinationPath = NormalizePath(destinationPath);

            lock (_lock)
            {
                if (!_files.TryGetValue(sourcePath, out var sourceEntry))
                {
                    return Task.FromResult(false);
                }

                var destinationEntry = new FileEntry
                {
                    Content = (byte[])sourceEntry.Content.Clone(),
                    ContentType = sourceEntry.ContentType,
                    CreatedAt = DateTimeOffset.UtcNow,
                    ModifiedAt = DateTimeOffset.UtcNow,
                    ETag = Guid.NewGuid().ToString("N"),
                    Metadata = new Dictionary<string, string>(sourceEntry.Metadata)
                };

                _files[destinationPath] = destinationEntry;
                return Task.FromResult(true);
            }
        }

        /// <inheritdoc/>
        public Task<bool> MoveAsync(
            string sourcePath,
            string destinationPath,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return Task.FromResult(false);
            }

            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                return Task.FromResult(false);
            }

            sourcePath = NormalizePath(sourcePath);
            destinationPath = NormalizePath(destinationPath);

            lock (_lock)
            {
                if (!_files.TryGetValue(sourcePath, out var sourceEntry))
                {
                    return Task.FromResult(false);
                }

                sourceEntry.ModifiedAt = DateTimeOffset.UtcNow;
                sourceEntry.ETag = Guid.NewGuid().ToString("N");
                _files[destinationPath] = sourceEntry;
                _files.Remove(sourcePath);

                return Task.FromResult(true);
            }
        }

        private string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            // Normalize path separators
            path = path.Replace('\\', '/');

            // Remove leading/trailing slashes (except for root)
            path = path.Trim('/');

            // Apply base path
            if (!string.IsNullOrEmpty(_options.BasePath))
            {
                var basePath = _options.BasePath.Trim('/');
                if (!string.IsNullOrEmpty(basePath))
                {
                    path = $"{basePath}/{path}";
                }
            }

            return path;
        }

        private class FileEntry
        {
            public byte[] Content { get; set; } = Array.Empty<byte>();
            public string ContentType { get; set; } = string.Empty;
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset ModifiedAt { get; set; }
            public string ETag { get; set; } = string.Empty;
            public Dictionary<string, string> Metadata { get; set; } = new();
        }
    }
}

