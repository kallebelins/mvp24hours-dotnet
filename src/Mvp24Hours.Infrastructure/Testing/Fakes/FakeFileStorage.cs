//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
#nullable enable
using Mvp24Hours.Infrastructure.FileStorage.Contract;
using Mvp24Hours.Infrastructure.FileStorage.Providers;
using Mvp24Hours.Infrastructure.FileStorage.Results;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Testing.Fakes
{
    /// <summary>
    /// Fake file storage implementation with configurable behavior for testing.
    /// </summary>
    public class FakeFileStorage : IFakeFileStorage
    {
        private readonly Dictionary<string, FileEntry> _files = new();
        private readonly object _lock = new();

        /// <inheritdoc />
        public IReadOnlyList<string> StoredFilePaths
        {
            get
            {
                lock (_lock)
                {
                    return _files.Keys.ToList().AsReadOnly();
                }
            }
        }

        /// <inheritdoc />
        public int FileCount
        {
            get
            {
                lock (_lock)
                {
                    return _files.Count;
                }
            }
        }

        /// <inheritdoc />
        public bool ShouldUploadFail { get; set; }

        /// <inheritdoc />
        public bool ShouldDownloadFail { get; set; }

        /// <inheritdoc />
        public string FailureMessage { get; set; } = "Operation failed (simulated).";

        /// <inheritdoc />
        public TimeSpan? SimulatedDelay { get; set; }

        /// <inheritdoc />
        public Func<string, byte[], FileUploadResult>? CustomUploadResultFactory { get; set; }

        /// <inheritdoc />
        public Func<string, FileDownloadResult>? CustomDownloadResultFactory { get; set; }

        /// <inheritdoc />
        public async Task<FileUploadResult> UploadAsync(
            string filePath,
            byte[] content,
            string contentType,
            IDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (SimulatedDelay.HasValue)
            {
                await Task.Delay(SimulatedDelay.Value, cancellationToken);
            }

            if (CustomUploadResultFactory != null)
            {
                return CustomUploadResultFactory(filePath, content);
            }

            if (ShouldUploadFail)
            {
                return FileUploadResult.Failed(FailureMessage);
            }

            var normalizedPath = NormalizePath(filePath);

            lock (_lock)
            {
                var now = DateTimeOffset.UtcNow;
                _files[normalizedPath] = new FileEntry
                {
                    Content = (byte[])content.Clone(),
                    ContentType = contentType,
                    CreatedAt = now,
                    ModifiedAt = now,
                    Metadata = metadata != null ? new Dictionary<string, string>(metadata) : new()
                };
            }

            var fileMetadata = new FileMetadata(
                normalizedPath,
                Path.GetFileName(normalizedPath),
                content.Length,
                contentType,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                Guid.NewGuid().ToString("N"),
                metadata);

            return FileUploadResult.Successful(normalizedPath, fileMetadata);
        }

        /// <inheritdoc />
        public async Task<FileUploadResult> UploadFromStreamAsync(
            string filePath,
            Stream stream,
            string contentType,
            IDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken);
            return await UploadAsync(filePath, memoryStream.ToArray(), contentType, metadata, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<FileUploadResult> UploadFromChunksAsync(
            string filePath,
            IAsyncEnumerable<byte[]> chunks,
            string contentType,
            IDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            using var memoryStream = new MemoryStream();
            await foreach (var chunk in chunks.WithCancellation(cancellationToken))
            {
                if (chunk != null && chunk.Length > 0)
                {
                    await memoryStream.WriteAsync(chunk, 0, chunk.Length, cancellationToken);
                }
            }
            return await UploadAsync(filePath, memoryStream.ToArray(), contentType, metadata, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<FileDownloadResult> DownloadAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (SimulatedDelay.HasValue)
            {
                await Task.Delay(SimulatedDelay.Value, cancellationToken);
            }

            if (CustomDownloadResultFactory != null)
            {
                return CustomDownloadResultFactory(filePath);
            }

            if (ShouldDownloadFail)
            {
                return FileDownloadResult.Failed(FailureMessage);
            }

            var normalizedPath = NormalizePath(filePath);

            lock (_lock)
            {
                if (!_files.TryGetValue(normalizedPath, out var entry))
                {
                    return FileDownloadResult.NotFound(normalizedPath);
                }

                var metadata = new FileMetadata(
                    normalizedPath,
                    Path.GetFileName(normalizedPath),
                    entry.Content.Length,
                    entry.ContentType,
                    entry.CreatedAt,
                    entry.ModifiedAt,
                    Guid.NewGuid().ToString("N"),
                    entry.Metadata);

                return FileDownloadResult.Successful((byte[])entry.Content.Clone(), metadata);
            }
        }

        /// <inheritdoc />
        public async Task<FileDownloadResult> DownloadToStreamAsync(
            string filePath,
            Stream destinationStream,
            CancellationToken cancellationToken = default)
        {
            var result = await DownloadAsync(filePath, cancellationToken);
            if (result.Success && result.Content != null)
            {
                await destinationStream.WriteAsync(result.Content, 0, result.Content.Length, cancellationToken);
            }
            return result;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<byte[]> DownloadAsChunksAsync(
            string filePath,
            int chunkSize = 65536,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var result = await DownloadAsync(filePath, cancellationToken);
            if (!result.Success || result.Content == null)
            {
                yield break;
            }

            var content = result.Content;
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

        /// <inheritdoc />
        public Task<bool> ExistsAsync(string filePath, CancellationToken cancellationToken = default)
        {
            var normalizedPath = NormalizePath(filePath);
            lock (_lock)
            {
                return Task.FromResult(_files.ContainsKey(normalizedPath));
            }
        }

        /// <inheritdoc />
        public Task<bool> DeleteAsync(string filePath, CancellationToken cancellationToken = default)
        {
            var normalizedPath = NormalizePath(filePath);
            lock (_lock)
            {
                return Task.FromResult(_files.Remove(normalizedPath));
            }
        }

        /// <inheritdoc />
        public Task<IFileMetadata?> GetMetadataAsync(string filePath, CancellationToken cancellationToken = default)
        {
            var normalizedPath = NormalizePath(filePath);
            lock (_lock)
            {
                if (!_files.TryGetValue(normalizedPath, out var entry))
                {
                    return Task.FromResult<IFileMetadata?>(null);
                }

                IFileMetadata metadata = new FileMetadata(
                    normalizedPath,
                    Path.GetFileName(normalizedPath),
                    entry.Content.Length,
                    entry.ContentType,
                    entry.CreatedAt,
                    entry.ModifiedAt,
                    Guid.NewGuid().ToString("N"),
                    entry.Metadata);

                return Task.FromResult<IFileMetadata?>(metadata);
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<IFileMetadata> ListFilesAsync(
            string directoryPath = "",
            bool recursive = false,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var normalizedDir = NormalizePath(directoryPath ?? string.Empty);
            if (!string.IsNullOrEmpty(normalizedDir) && !normalizedDir.EndsWith("/"))
            {
                normalizedDir += "/";
            }

            List<KeyValuePair<string, FileEntry>> matching;
            lock (_lock)
            {
                matching = _files
                    .Where(kvp => kvp.Key.StartsWith(normalizedDir, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            foreach (var kvp in matching)
            {
                if (cancellationToken.IsCancellationRequested) yield break;

                if (!recursive)
                {
                    var relativePath = kvp.Key.Substring(normalizedDir.Length);
                    if (relativePath.Contains("/")) continue;
                }

                yield return new FileMetadata(
                    kvp.Key,
                    Path.GetFileName(kvp.Key),
                    kvp.Value.Content.Length,
                    kvp.Value.ContentType,
                    kvp.Value.CreatedAt,
                    kvp.Value.ModifiedAt,
                    Guid.NewGuid().ToString("N"),
                    kvp.Value.Metadata);
            }
        }

        /// <inheritdoc />
        public Task<bool> CopyAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default)
        {
            var normalizedSource = NormalizePath(sourcePath);
            var normalizedDest = NormalizePath(destinationPath);

            lock (_lock)
            {
                if (!_files.TryGetValue(normalizedSource, out var entry))
                {
                    return Task.FromResult(false);
                }

                _files[normalizedDest] = new FileEntry
                {
                    Content = (byte[])entry.Content.Clone(),
                    ContentType = entry.ContentType,
                    CreatedAt = DateTimeOffset.UtcNow,
                    ModifiedAt = DateTimeOffset.UtcNow,
                    Metadata = new Dictionary<string, string>(entry.Metadata)
                };

                return Task.FromResult(true);
            }
        }

        /// <inheritdoc />
        public Task<bool> MoveAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default)
        {
            var normalizedSource = NormalizePath(sourcePath);
            var normalizedDest = NormalizePath(destinationPath);

            lock (_lock)
            {
                if (!_files.TryGetValue(normalizedSource, out var entry))
                {
                    return Task.FromResult(false);
                }

                _files[normalizedDest] = entry;
                _files.Remove(normalizedSource);

                return Task.FromResult(true);
            }
        }

        /// <inheritdoc />
        public void ClearFiles()
        {
            lock (_lock)
            {
                _files.Clear();
            }
        }

        /// <inheritdoc />
        public byte[]? GetFileContent(string filePath)
        {
            var normalizedPath = NormalizePath(filePath);
            lock (_lock)
            {
                return _files.TryGetValue(normalizedPath, out var entry)
                    ? (byte[])entry.Content.Clone()
                    : null;
            }
        }

        /// <inheritdoc />
        public void SeedFile(string filePath, byte[] content, string contentType = "application/octet-stream")
        {
            var normalizedPath = NormalizePath(filePath);
            lock (_lock)
            {
                var now = DateTimeOffset.UtcNow;
                _files[normalizedPath] = new FileEntry
                {
                    Content = (byte[])content.Clone(),
                    ContentType = contentType,
                    CreatedAt = now,
                    ModifiedAt = now,
                    Metadata = new Dictionary<string, string>()
                };
            }
        }

        /// <inheritdoc />
        public bool HasFile(string filePath)
        {
            var normalizedPath = NormalizePath(filePath);
            lock (_lock)
            {
                return _files.ContainsKey(normalizedPath);
            }
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            return path.Replace('\\', '/').Trim('/');
        }

        private class FileEntry
        {
            public byte[] Content { get; set; } = Array.Empty<byte>();
            public string ContentType { get; set; } = string.Empty;
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset ModifiedAt { get; set; }
            public Dictionary<string, string> Metadata { get; set; } = new();
        }
    }
}

