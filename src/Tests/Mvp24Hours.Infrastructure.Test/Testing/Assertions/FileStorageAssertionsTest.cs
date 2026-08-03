//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Text;
using Mvp24Hours.Infrastructure.Testing.Assertions;
using Mvp24Hours.Infrastructure.Testing.Fakes;
using AssertionException = Mvp24Hours.Infrastructure.Testing.Assertions.AssertionException;

namespace Mvp24Hours.Infrastructure.Test.Testing.Assertions;

[Trait("Category", "Unit")]
public class FileStorageAssertionsTest
{
    [Fact]
    public void AssertFileStored_ShouldPassWhenFilesExist()
    {
        FakeFileStorage storage = new();
        storage.SeedFile("docs/readme.txt", Encoding.UTF8.GetBytes("hello"));

        Action act = () => FileStorageAssertions.AssertFileStored(storage);

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertFileStored_ShouldThrowWhenEmpty()
    {
        FakeFileStorage storage = new();

        Action act = () => FileStorageAssertions.AssertFileStored(storage);

        act.Should().Throw<AssertionException>().WithMessage("*at least one file*");
    }

    [Fact]
    public void AssertFileCount_ShouldPassWhenCountMatches()
    {
        FakeFileStorage storage = new();
        storage.SeedFile("a.txt", [1]);
        storage.SeedFile("b.txt", [2]);

        Action act = () => FileStorageAssertions.AssertFileCount(storage, 2);

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertFileCount_ShouldThrowWhenCountMismatch()
    {
        FakeFileStorage storage = new();
        storage.SeedFile("a.txt", [1]);

        Action act = () => FileStorageAssertions.AssertFileCount(storage, 3);

        act.Should().Throw<AssertionException>().WithMessage("*Expected 3 file*");
    }

    [Fact]
    public void AssertFileExists_ShouldPassWhenFilePresent()
    {
        FakeFileStorage storage = new();
        storage.SeedFile("data/config.json", [123]);

        Action act = () => FileStorageAssertions.AssertFileExists(storage, "data/config.json");

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertFileExists_ShouldThrowWhenFileMissing()
    {
        FakeFileStorage storage = new();

        Action act = () => FileStorageAssertions.AssertFileExists(storage, "missing.txt");

        act.Should().Throw<AssertionException>().WithMessage("*missing.txt*");
    }

    [Fact]
    public void AssertFileNotExists_ShouldPassWhenFileAbsent()
    {
        FakeFileStorage storage = new();

        Action act = () => FileStorageAssertions.AssertFileNotExists(storage, "missing.txt");

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertFileNotExists_ShouldThrowWhenFilePresent()
    {
        FakeFileStorage storage = new();
        storage.SeedFile("exists.txt", [1]);

        Action act = () => FileStorageAssertions.AssertFileNotExists(storage, "exists.txt");

        act.Should().Throw<AssertionException>().WithMessage("*no file at path 'exists.txt'*");
    }

    [Fact]
    public void AssertFileContent_WithBytes_ShouldPassWhenContentMatches()
    {
        FakeFileStorage storage = new();
        byte[] content = [10, 20, 30];
        storage.SeedFile("bin/data", content);

        Action act = () => FileStorageAssertions.AssertFileContent(storage, "bin/data", content);

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertFileContent_WithBytes_ShouldThrowWhenContentMismatch()
    {
        FakeFileStorage storage = new();
        storage.SeedFile("bin/data", [1, 2, 3]);

        Action act = () => FileStorageAssertions.AssertFileContent(storage, "bin/data", [9, 9, 9]);

        act.Should().Throw<AssertionException>().WithMessage("*does not match expected content*");
    }

    [Fact]
    public void AssertFileContent_WithString_ShouldPassWhenTextMatches()
    {
        FakeFileStorage storage = new();
        storage.SeedFile("notes.txt", Encoding.UTF8.GetBytes("hello world"));

        Action act = () => FileStorageAssertions.AssertFileContent(storage, "notes.txt", "hello world");

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertFileContent_ShouldThrowWhenFileMissing()
    {
        FakeFileStorage storage = new();

        Action act = () => FileStorageAssertions.AssertFileContent(storage, "missing.txt", "x");

        act.Should().Throw<AssertionException>().WithMessage("*does not exist*");
    }

    [Fact]
    public void AssertFileContentContains_ShouldPassWhenTextFound()
    {
        FakeFileStorage storage = new();
        storage.SeedFile("report.txt", Encoding.UTF8.GetBytes("Revenue increased by 20%"));

        Action act = () => FileStorageAssertions.AssertFileContentContains(storage, "report.txt", "Revenue");

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertFileContentContains_ShouldThrowWhenTextMissing()
    {
        FakeFileStorage storage = new();
        storage.SeedFile("report.txt", Encoding.UTF8.GetBytes("Nothing here"));

        Action act = () => FileStorageAssertions.AssertFileContentContains(storage, "report.txt", "Revenue");

        act.Should().Throw<AssertionException>().WithMessage("*does not contain 'Revenue'*");
    }

    [Fact]
    public void AssertNoFilesStored_ShouldPassWhenEmpty()
    {
        FakeFileStorage storage = new();

        Action act = () => FileStorageAssertions.AssertNoFilesStored(storage);

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertNoFilesStored_ShouldThrowWhenFilesExist()
    {
        FakeFileStorage storage = new();
        storage.SeedFile("a.txt", [1]);

        Action act = () => FileStorageAssertions.AssertNoFilesStored(storage);

        act.Should().Throw<AssertionException>().WithMessage("*Expected no files*");
    }

    [Fact]
    public void GetFileContent_ShouldReturnBytesWhenFileExists()
    {
        FakeFileStorage storage = new();
        byte[] expected = [5, 6, 7];
        storage.SeedFile("payload.bin", expected);

        byte[] actual = FileStorageAssertions.GetFileContent(storage, "payload.bin");

        actual.Should().Equal(expected);
    }

    [Fact]
    public void GetFileContentAsString_ShouldReturnDecodedText()
    {
        FakeFileStorage storage = new();
        storage.SeedFile("greeting.txt", Encoding.UTF8.GetBytes("hi"));

        string text = FileStorageAssertions.GetFileContentAsString(storage, "greeting.txt");

        text.Should().Be("hi");
    }

    [Fact]
    public void GetFileContent_ShouldThrowWhenFileMissing()
    {
        FakeFileStorage storage = new();

        Action act = () => FileStorageAssertions.GetFileContent(storage, "missing.bin");

        act.Should().Throw<AssertionException>().WithMessage("*does not exist*");
    }

    [Fact]
    public void NullArguments_ShouldThrowArgumentNullException()
    {
        FakeFileStorage storage = new();

        Action nullStorage = () => FileStorageAssertions.AssertFileStored(null!);
        Action nullPath = () => FileStorageAssertions.AssertFileExists(storage, null!);
        Action nullContent = () => FileStorageAssertions.AssertFileContent(storage, "x", (byte[])null!);
        Action nullText = () => FileStorageAssertions.AssertFileContentContains(storage, "x", null!);

        nullStorage.Should().Throw<ArgumentNullException>().WithParameterName("fileStorage");
        nullPath.Should().Throw<ArgumentNullException>().WithParameterName("filePath");
        nullContent.Should().Throw<ArgumentNullException>().WithParameterName("expectedContent");
        nullText.Should().Throw<ArgumentNullException>().WithParameterName("expectedText");
    }
}
