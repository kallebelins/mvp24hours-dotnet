//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Bson;
using MongoDB.Driver.GridFS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.GridFS
{
    /// <summary>
    /// Interface for MongoDB GridFS operations to store and retrieve large files.
    /// </summary>
    /// <remarks>
    /// <para>
    /// GridFS is a specification for storing and retrieving files that exceed
    /// the BSON document size limit of 16MB. This service provides:
    /// <list type="bullet">
    ///   <item>Upload files from streams, byte arrays, or file paths</item>
    ///   <item>Download files to streams, byte arrays, or file paths</item>
    ///   <item>File metadata management</item>
    ///   <item>File search and listing</item>
    ///   <item>Chunked file operations</item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IMongoDbGridFsService
    {
        /// <summary>
        /// Gets the underlying GridFS bucket.
        /// </summary>
        IGridFSBucket Bucket { get; }

        /// <summary>
        /// Uploads a file from a stream.
        /// </summary>
        /// <param name="filename">The filename to store.</param>
        /// <param name="stream">The source stream.</param>
        /// <param name="options">Optional upload options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The ObjectId of the uploaded file.</returns>
        Task<ObjectId> UploadAsync(
            string filename,
            Stream stream,
            GridFSUploadOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads a file from a byte array.
        /// </summary>
        /// <param name="filename">The filename to store.</param>
        /// <param name="bytes">The file content as bytes.</param>
        /// <param name="options">Optional upload options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The ObjectId of the uploaded file.</returns>
        Task<ObjectId> UploadAsync(
            string filename,
            byte[] bytes,
            GridFSUploadOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads a file from a local file path.
        /// </summary>
        /// <param name="filename">The filename to store in GridFS.</param>
        /// <param name="filePath">The local file path.</param>
        /// <param name="options">Optional upload options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The ObjectId of the uploaded file.</returns>
        Task<ObjectId> UploadFromFileAsync(
            string filename,
            string filePath,
            GridFSUploadOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads a file to a stream.
        /// </summary>
        /// <param name="id">The ObjectId of the file.</param>
        /// <param name="destination">The destination stream.</param>
        /// <param name="options">Optional download options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DownloadAsync(
            ObjectId id,
            Stream destination,
            GridFSDownloadOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads a file by filename to a stream.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="destination">The destination stream.</param>
        /// <param name="options">Optional download options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DownloadByNameAsync(
            string filename,
            Stream destination,
            GridFSDownloadByNameOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads a file as a byte array.
        /// </summary>
        /// <param name="id">The ObjectId of the file.</param>
        /// <param name="options">Optional download options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The file content as bytes.</returns>
        Task<byte[]> DownloadAsBytesAsync(
            ObjectId id,
            GridFSDownloadOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads a file to a local file path.
        /// </summary>
        /// <param name="id">The ObjectId of the file.</param>
        /// <param name="filePath">The local file path to save to.</param>
        /// <param name="options">Optional download options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DownloadToFileAsync(
            ObjectId id,
            string filePath,
            GridFSDownloadOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Opens a download stream for reading a file.
        /// </summary>
        /// <param name="id">The ObjectId of the file.</param>
        /// <param name="options">Optional download options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A stream for reading the file.</returns>
        Task<Stream> OpenDownloadStreamAsync(
            ObjectId id,
            GridFSDownloadOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets file information by ObjectId.
        /// </summary>
        /// <param name="id">The ObjectId of the file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The file information, or null if not found.</returns>
        Task<GridFSFileInfo> GetFileInfoAsync(
            ObjectId id,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets file information by filename.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The file information, or null if not found.</returns>
        Task<GridFSFileInfo> GetFileInfoByNameAsync(
            string filename,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all files matching a filter.
        /// </summary>
        /// <param name="filter">Optional filter document.</param>
        /// <param name="skip">Number of files to skip.</param>
        /// <param name="limit">Maximum number of files to return.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of file information.</returns>
        Task<IList<GridFSFileInfo>> ListFilesAsync(
            BsonDocument filter = null,
            int? skip = null,
            int? limit = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches files by filename pattern.
        /// </summary>
        /// <param name="filenamePattern">The filename pattern (regex).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of matching file information.</returns>
        Task<IList<GridFSFileInfo>> SearchByFilenameAsync(
            string filenamePattern,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a file by ObjectId.
        /// </summary>
        /// <param name="id">The ObjectId of the file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DeleteAsync(ObjectId id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Renames a file.
        /// </summary>
        /// <param name="id">The ObjectId of the file.</param>
        /// <param name="newFilename">The new filename.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RenameAsync(ObjectId id, string newFilename, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a file exists by ObjectId.
        /// </summary>
        /// <param name="id">The ObjectId of the file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the file exists, otherwise false.</returns>
        Task<bool> ExistsAsync(ObjectId id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a file exists by filename.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the file exists, otherwise false.</returns>
        Task<bool> ExistsByNameAsync(string filename, CancellationToken cancellationToken = default);

        /// <summary>
        /// Drops the entire GridFS bucket (all files and chunks).
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DropBucketAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the total size of all files in the bucket.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Total size in bytes.</returns>
        Task<long> GetTotalSizeAsync(CancellationToken cancellationToken = default);
    }
}

