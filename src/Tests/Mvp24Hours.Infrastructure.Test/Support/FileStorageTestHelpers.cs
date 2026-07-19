//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.FileStorage.Contract;
using Mvp24Hours.Infrastructure.FileStorage.Options;
using Moq;

namespace Mvp24Hours.Infrastructure.Test.Support;

internal static class FileStorageTestHelpers
{
    public static FileStorageOptions CreateOptions(
        string? basePath = null,
        bool overwriteExistingFiles = true,
        bool createDirectoriesIfNotExists = true,
        string defaultContentType = "application/octet-stream")
    {
        return new FileStorageOptions
        {
            BasePath = basePath ?? string.Empty,
            OverwriteExistingFiles = overwriteExistingFiles,
            CreateDirectoriesIfNotExists = createDirectoriesIfNotExists,
            DefaultContentType = defaultContentType
        };
    }

    public static byte[] CreateContent(string text = "hello file storage")
    {
        return System.Text.Encoding.UTF8.GetBytes(text);
    }

    public static IFileValidator CreatePassingValidator()
    {
        var mock = new Mock<IFileValidator>();
        mock.Setup(v => v.ValidateAsync(It.IsAny<FileValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());
        return mock.Object;
    }

    public static IFileValidator CreateFailingValidator(string error = "File not allowed")
    {
        var mock = new Mock<IFileValidator>();
        mock.Setup(v => v.ValidateAsync(It.IsAny<FileValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failed(error));
        return mock.Object;
    }

    public static async IAsyncEnumerable<byte[]> CreateChunksAsync(params byte[][] chunks)
    {
        foreach (byte[] chunk in chunks)
        {
            yield return chunk;
            await Task.Yield();
        }
    }
}
