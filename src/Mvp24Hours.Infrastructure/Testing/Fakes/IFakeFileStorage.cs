//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
#nullable enable
using Mvp24Hours.Infrastructure.FileStorage.Contract;
using Mvp24Hours.Infrastructure.FileStorage.Results;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Testing.Fakes
{
    /// <summary>
    /// Fake file storage interface with configurable behavior for testing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface extends <see cref="IFileStorage"/> to provide additional
    /// capabilities for testing, such as:
    /// - Configuring success/failure behavior
    /// - Accessing stored files
    /// - Simulating various scenarios
    /// </para>
    /// </remarks>
    public interface IFakeFileStorage : IFileStorage
    {
        /// <summary>
        /// Gets all file paths currently stored.
        /// </summary>
        IReadOnlyList<string> StoredFilePaths { get; }

        /// <summary>
        /// Gets the count of stored files.
        /// </summary>
        int FileCount { get; }

        /// <summary>
        /// Gets or sets whether upload operations should fail.
        /// </summary>
        bool ShouldUploadFail { get; set; }

        /// <summary>
        /// Gets or sets whether download operations should fail.
        /// </summary>
        bool ShouldDownloadFail { get; set; }

        /// <summary>
        /// Gets or sets the failure message for failed operations.
        /// </summary>
        string FailureMessage { get; set; }

        /// <summary>
        /// Gets or sets the delay to simulate for operations.
        /// </summary>
        TimeSpan? SimulatedDelay { get; set; }

        /// <summary>
        /// Gets or sets a custom upload result factory.
        /// </summary>
        Func<string, byte[], FileUploadResult>? CustomUploadResultFactory { get; set; }

        /// <summary>
        /// Gets or sets a custom download result factory.
        /// </summary>
        Func<string, FileDownloadResult>? CustomDownloadResultFactory { get; set; }

        /// <summary>
        /// Clears all stored files.
        /// </summary>
        void ClearFiles();

        /// <summary>
        /// Gets the content of a file by path.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        byte[]? GetFileContent(string filePath);

        /// <summary>
        /// Seeds a file into storage for testing.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="content">The file content.</param>
        /// <param name="contentType">The content type.</param>
        void SeedFile(string filePath, byte[] content, string contentType = "application/octet-stream");

        /// <summary>
        /// Verifies that a file exists at the specified path.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        bool HasFile(string filePath);
    }
}

