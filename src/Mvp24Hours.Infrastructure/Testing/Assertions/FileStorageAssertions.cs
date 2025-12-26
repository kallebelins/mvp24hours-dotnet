//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
#nullable enable
using Mvp24Hours.Infrastructure.Testing.Fakes;
using System;
using System.Text;

namespace Mvp24Hours.Infrastructure.Testing.Assertions
{
    /// <summary>
    /// Provides assertion helpers for file storage operations in tests.
    /// </summary>
    public static class FileStorageAssertions
    {
        /// <summary>
        /// Asserts that at least one file was stored.
        /// </summary>
        public static void AssertFileStored(IFakeFileStorage fileStorage)
        {
            if (fileStorage == null) throw new ArgumentNullException(nameof(fileStorage));

            if (fileStorage.FileCount == 0)
            {
                throw new AssertionException("Expected at least one file to be stored, but none were stored.");
            }
        }

        /// <summary>
        /// Asserts that exactly the specified number of files were stored.
        /// </summary>
        public static void AssertFileCount(IFakeFileStorage fileStorage, int expectedCount)
        {
            if (fileStorage == null) throw new ArgumentNullException(nameof(fileStorage));

            if (fileStorage.FileCount != expectedCount)
            {
                throw new AssertionException(
                    $"Expected {expectedCount} file(s) to be stored, but {fileStorage.FileCount} were stored.");
            }
        }

        /// <summary>
        /// Asserts that a file exists at the specified path.
        /// </summary>
        public static void AssertFileExists(IFakeFileStorage fileStorage, string filePath)
        {
            if (fileStorage == null) throw new ArgumentNullException(nameof(fileStorage));
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));

            if (!fileStorage.HasFile(filePath))
            {
                throw new AssertionException(
                    $"Expected a file at path '{filePath}', but no such file was found.");
            }
        }

        /// <summary>
        /// Asserts that no file exists at the specified path.
        /// </summary>
        public static void AssertFileNotExists(IFakeFileStorage fileStorage, string filePath)
        {
            if (fileStorage == null) throw new ArgumentNullException(nameof(fileStorage));
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));

            if (fileStorage.HasFile(filePath))
            {
                throw new AssertionException(
                    $"Expected no file at path '{filePath}', but a file exists there.");
            }
        }

        /// <summary>
        /// Asserts that a file's content matches the expected content.
        /// </summary>
        public static void AssertFileContent(IFakeFileStorage fileStorage, string filePath, byte[] expectedContent)
        {
            if (fileStorage == null) throw new ArgumentNullException(nameof(fileStorage));
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
            if (expectedContent == null) throw new ArgumentNullException(nameof(expectedContent));

            var actualContent = fileStorage.GetFileContent(filePath);
            if (actualContent == null)
            {
                throw new AssertionException($"File at path '{filePath}' does not exist.");
            }

            if (!ByteArraysEqual(actualContent, expectedContent))
            {
                throw new AssertionException(
                    $"File content at path '{filePath}' does not match expected content. " +
                    $"Expected {expectedContent.Length} bytes, got {actualContent.Length} bytes.");
            }
        }

        /// <summary>
        /// Asserts that a file's content (as string) matches the expected content.
        /// </summary>
        public static void AssertFileContent(IFakeFileStorage fileStorage, string filePath, string expectedContent)
        {
            AssertFileContent(fileStorage, filePath, Encoding.UTF8.GetBytes(expectedContent));
        }

        /// <summary>
        /// Asserts that a file's content contains the specified text.
        /// </summary>
        public static void AssertFileContentContains(IFakeFileStorage fileStorage, string filePath, string expectedText)
        {
            if (fileStorage == null) throw new ArgumentNullException(nameof(fileStorage));
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
            if (string.IsNullOrEmpty(expectedText)) throw new ArgumentNullException(nameof(expectedText));

            var content = fileStorage.GetFileContent(filePath);
            if (content == null)
            {
                throw new AssertionException($"File at path '{filePath}' does not exist.");
            }

            var contentString = Encoding.UTF8.GetString(content);
            if (!contentString.Contains(expectedText, StringComparison.OrdinalIgnoreCase))
            {
                throw new AssertionException(
                    $"File content at path '{filePath}' does not contain '{expectedText}'.");
            }
        }

        /// <summary>
        /// Asserts that no files were stored.
        /// </summary>
        public static void AssertNoFilesStored(IFakeFileStorage fileStorage)
        {
            if (fileStorage == null) throw new ArgumentNullException(nameof(fileStorage));

            if (fileStorage.FileCount > 0)
            {
                throw new AssertionException(
                    $"Expected no files to be stored, but {fileStorage.FileCount} were stored.");
            }
        }

        /// <summary>
        /// Gets the content of a file, throwing if it doesn't exist.
        /// </summary>
        public static byte[] GetFileContent(IFakeFileStorage fileStorage, string filePath)
        {
            if (fileStorage == null) throw new ArgumentNullException(nameof(fileStorage));
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));

            var content = fileStorage.GetFileContent(filePath);
            if (content == null)
            {
                throw new AssertionException($"File at path '{filePath}' does not exist.");
            }

            return content;
        }

        /// <summary>
        /// Gets the content of a file as string, throwing if it doesn't exist.
        /// </summary>
        public static string GetFileContentAsString(IFakeFileStorage fileStorage, string filePath)
        {
            return Encoding.UTF8.GetString(GetFileContent(fileStorage, filePath));
        }

        private static bool ByteArraysEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
    }
}

