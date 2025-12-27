//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.GridFS
{
    /// <summary>
    /// Service for MongoDB GridFS operations to store and retrieve large files.
    /// </summary>
    /// <remarks>
    /// <para>
    /// GridFS stores files in two collections:
    /// <list type="bullet">
    ///   <item><c>fs.files</c> - stores file metadata</item>
    ///   <item><c>fs.chunks</c> - stores file data in chunks</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Upload a file
    /// var fileId = await gridFsService.UploadFromFileAsync("document.pdf", "/path/to/document.pdf");
    /// 
    /// // Download to stream
    /// using var stream = new MemoryStream();
    /// await gridFsService.DownloadAsync(fileId, stream);
    /// 
    /// // List files
    /// var files = await gridFsService.ListFilesAsync();
    /// foreach (var file in files)
    /// {
    ///     Console.WriteLine($"{file.Filename}: {file.Length} bytes");
    /// }
    /// </code>
    /// </example>
    public class MongoDbGridFsService : IMongoDbGridFsService
    {
        private readonly IGridFSBucket _bucket;
        private readonly ILogger<MongoDbGridFsService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbGridFsService"/> class.
        /// </summary>
        /// <param name="database">The MongoDB database.</param>
        /// <param name="options">Optional GridFS bucket options.</param>
        /// <param name="logger">Optional logger.</param>
        public MongoDbGridFsService(
            IMongoDatabase database,
            GridFSBucketOptions options = null,
            ILogger<MongoDbGridFsService> logger = null)
        {
            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }

            _bucket = new GridFSBucket(database, options ?? new GridFSBucketOptions
            {
                BucketName = "fs",
                ChunkSizeBytes = 255 * 1024, // 255 KB default chunk size
                WriteConcern = WriteConcern.WMajority,
                ReadPreference = ReadPreference.Primary
            });

            _logger = logger;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbGridFsService"/> class with an existing bucket.
        /// </summary>
        /// <param name="bucket">The GridFS bucket.</param>
        /// <param name="logger">Optional logger.</param>
        public MongoDbGridFsService(IGridFSBucket bucket, ILogger<MongoDbGridFsService> logger = null)
        {
            _bucket = bucket ?? throw new ArgumentNullException(nameof(bucket));
            _logger = logger;
        }

        /// <inheritdoc/>
        public IGridFSBucket Bucket => _bucket;

        /// <inheritdoc/>
        public async Task<ObjectId> UploadAsync(
            string filename,
            Stream stream,
            GridFSUploadOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentException("Filename cannot be empty.", nameof(filename));
            }

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var id = await _bucket.UploadFromStreamAsync(filename, stream, options, cancellationToken);

            _logger?.LogDebug("File '{Filename}' uploaded to GridFS with ID: {FileId}", filename, id);

            return id;
        }

        /// <inheritdoc/>
        public async Task<ObjectId> UploadAsync(
            string filename,
            byte[] bytes,
            GridFSUploadOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (bytes == null || bytes.Length == 0)
            {
                throw new ArgumentException("Bytes cannot be null or empty.", nameof(bytes));
            }

            var id = await _bucket.UploadFromBytesAsync(filename, bytes, options, cancellationToken);

            _logger?.LogDebug("File '{Filename}' ({Size} bytes) uploaded to GridFS with ID: {FileId}",
                filename, bytes.Length, id);

            return id;
        }

        /// <inheritdoc/>
        public async Task<ObjectId> UploadFromFileAsync(
            string filename,
            string filePath,
            GridFSUploadOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found.", filePath);
            }

            await using var stream = File.OpenRead(filePath);
            return await UploadAsync(filename ?? Path.GetFileName(filePath), stream, options, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task DownloadAsync(
            ObjectId id,
            Stream destination,
            GridFSDownloadOptions options = null,
            CancellationToken cancellationToken = default)
        {
            await _bucket.DownloadToStreamAsync(id, destination, options, cancellationToken);

            _logger?.LogDebug("File with ID '{FileId}' downloaded from GridFS.", id);
        }

        /// <inheritdoc/>
        public async Task DownloadByNameAsync(
            string filename,
            Stream destination,
            GridFSDownloadByNameOptions options = null,
            CancellationToken cancellationToken = default)
        {
            await _bucket.DownloadToStreamByNameAsync(filename, destination, options, cancellationToken);

            _logger?.LogDebug("File '{Filename}' downloaded from GridFS.", filename);
        }

        /// <inheritdoc/>
        public async Task<byte[]> DownloadAsBytesAsync(
            ObjectId id,
            GridFSDownloadOptions options = null,
            CancellationToken cancellationToken = default)
        {
            var bytes = await _bucket.DownloadAsBytesAsync(id, options, cancellationToken);

            return bytes;
        }

        /// <inheritdoc/>
        public async Task DownloadToFileAsync(
            ObjectId id,
            string filePath,
            GridFSDownloadOptions options = null,
            CancellationToken cancellationToken = default)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var stream = File.Create(filePath);
            await DownloadAsync(id, stream, options, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Stream> OpenDownloadStreamAsync(
            ObjectId id,
            GridFSDownloadOptions options = null,
            CancellationToken cancellationToken = default)
        {
            return await _bucket.OpenDownloadStreamAsync(id, options, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<GridFSFileInfo> GetFileInfoAsync(ObjectId id, CancellationToken cancellationToken = default)
        {
            var filter = Builders<GridFSFileInfo>.Filter.Eq(x => x.Id, id);
            var cursor = await _bucket.FindAsync(filter, cancellationToken: cancellationToken);
            return await cursor.FirstOrDefaultAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<GridFSFileInfo> GetFileInfoByNameAsync(string filename, CancellationToken cancellationToken = default)
        {
            var filter = Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, filename);
            var options = new GridFSFindOptions { Sort = Builders<GridFSFileInfo>.Sort.Descending(x => x.UploadDateTime), Limit = 1 };
            var cursor = await _bucket.FindAsync(filter, options, cancellationToken);
            return await cursor.FirstOrDefaultAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IList<GridFSFileInfo>> ListFilesAsync(
            BsonDocument filter = null,
            int? skip = null,
            int? limit = null,
            CancellationToken cancellationToken = default)
        {
            var findOptions = new GridFSFindOptions
            {
                Skip = skip,
                Limit = limit,
                Sort = Builders<GridFSFileInfo>.Sort.Descending(x => x.UploadDateTime)
            };

            var filterDefinition = filter != null
                ? new BsonDocumentFilterDefinition<GridFSFileInfo>(filter)
                : FilterDefinition<GridFSFileInfo>.Empty;

            var cursor = await _bucket.FindAsync(filterDefinition, findOptions, cancellationToken);
            return await cursor.ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IList<GridFSFileInfo>> SearchByFilenameAsync(
            string filenamePattern,
            CancellationToken cancellationToken = default)
        {
            var filter = Builders<GridFSFileInfo>.Filter.Regex(x => x.Filename, new BsonRegularExpression(filenamePattern, "i"));
            var cursor = await _bucket.FindAsync(filter, cancellationToken: cancellationToken);
            return await cursor.ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(ObjectId id, CancellationToken cancellationToken = default)
        {
            await _bucket.DeleteAsync(id, cancellationToken);

            _logger?.LogDebug("File with ID '{FileId}' deleted from GridFS.", id);
        }

        /// <inheritdoc/>
        public async Task RenameAsync(ObjectId id, string newFilename, CancellationToken cancellationToken = default)
        {
            await _bucket.RenameAsync(id, newFilename, cancellationToken);

            _logger?.LogDebug("File with ID '{FileId}' renamed to '{NewFilename}'.", id, newFilename);
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(ObjectId id, CancellationToken cancellationToken = default)
        {
            var fileInfo = await GetFileInfoAsync(id, cancellationToken);
            return fileInfo != null;
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsByNameAsync(string filename, CancellationToken cancellationToken = default)
        {
            var fileInfo = await GetFileInfoByNameAsync(filename, cancellationToken);
            return fileInfo != null;
        }

        /// <inheritdoc/>
        public async Task DropBucketAsync(CancellationToken cancellationToken = default)
        {
            await _bucket.DropAsync(cancellationToken);

            _logger?.LogWarning("GridFS bucket dropped.");
        }

        /// <inheritdoc/>
        public async Task<long> GetTotalSizeAsync(CancellationToken cancellationToken = default)
        {
            var files = await ListFilesAsync(cancellationToken: cancellationToken);
            long totalSize = 0;
            foreach (var file in files)
            {
                totalSize += file.Length;
            }
            return totalSize;
        }
    }
}

