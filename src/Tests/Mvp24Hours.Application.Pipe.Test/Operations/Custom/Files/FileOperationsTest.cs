//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Pipe.Test.Support;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Operations.Custom.Files;

namespace Mvp24Hours.Application.Pipe.Test.Operations.Custom.Files;

[Trait("Category", "Unit")]
public class FileOperationsTest : IDisposable
{
    private sealed class SampleDto
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "Mvp24HoursPipeFileOpsTest_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    #region [ FileTokenWriteOperation / FileTokenReadOperation (sync) ]

    [Fact]
    public void FileTokenWriteOperation_ThenFileTokenReadOperation_ShouldRoundTrip()
    {
        var message = new Infrastructure.Pipe.PipelineMessage("shared-token-1");
        message.AddContent(new SampleDto { Name = "Alice", Value = 42 });
        var writeOp = new FileTokenWriteOperation<SampleDto>(_tempDir);

        writeOp.Execute(message);

        var readMessage = new Infrastructure.Pipe.PipelineMessage("shared-token-1");
        var readOp = new FileTokenReadOperation<SampleDto>(_tempDir);

        readOp.Execute(readMessage);

        SampleDto? result = readMessage.GetContent<SampleDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Alice");
        result.Value.Should().Be(42);
    }

    [Fact]
    public void FileTokenWriteOperation_WithEmptyFilePath_DoesNotWriteFile()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        message.AddContent(new SampleDto { Name = "Bob" });
        var writeOp = new FileTokenWriteOperation<SampleDto>(string.Empty);

        writeOp.Execute(message);

        Directory.Exists(_tempDir).Should().BeFalse();
    }

    [Fact]
    public void FileTokenWriteOperation_WithoutDtoInMessage_DoesNotThrow()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        var writeOp = new FileTokenWriteOperation<SampleDto>(_tempDir);

        Action act = () => writeOp.Execute(message);

        act.Should().NotThrow();
    }

    [Fact]
    public void FileTokenReadOperation_WithEmptyFilePath_DoesNotAddContent()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        var readOp = new FileTokenReadOperation<SampleDto>(string.Empty);

        readOp.Execute(message);

        message.GetContent<SampleDto>().Should().BeNull();
    }

    [Fact]
    public void FileTokenReadOperation_WhenFileDoesNotExist_DoesNotAddContent()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        var readOp = new FileTokenReadOperation<SampleDto>(_tempDir);

        readOp.Execute(message);

        message.GetContent<SampleDto>().Should().BeNull();
    }

    [Fact]
    public void FileTokenWriteOperation_IsRequired_ReturnsTrue()
    {
        var writeOp = new FileTokenWriteOperation<SampleDto>(_tempDir);

        writeOp.IsRequired.Should().BeTrue();
        writeOp.FilePath.Should().Be(_tempDir);
    }

    #endregion

    #region [ FileTokenWriteOperationAsync / FileTokenReadOperationAsync ]

    [Fact]
    public async Task FileTokenWriteOperationAsync_ThenFileTokenReadOperationAsync_ShouldRoundTrip()
    {
        var message = new Infrastructure.Pipe.PipelineMessage("shared-token-2");
        message.AddContent(new SampleDto { Name = "Carol", Value = 7 });
        var writeOp = new FileTokenWriteOperationAsync<SampleDto>(_tempDir);

        await writeOp.ExecuteAsync(message);

        var readMessage = new Infrastructure.Pipe.PipelineMessage("shared-token-2");
        var readOp = new FileTokenReadOperationAsync<SampleDto>(_tempDir);

        await readOp.ExecuteAsync(readMessage);

        SampleDto? result = readMessage.GetContent<SampleDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Carol");
        result.Value.Should().Be(7);
    }

    [Fact]
    public async Task FileTokenWriteOperationAsync_WithEmptyFilePath_DoesNotWriteFile()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        message.AddContent(new SampleDto { Name = "Dave" });
        var writeOp = new FileTokenWriteOperationAsync<SampleDto>(string.Empty);

        await writeOp.ExecuteAsync(message);

        Directory.Exists(_tempDir).Should().BeFalse();
    }

    [Fact]
    public async Task FileTokenReadOperationAsync_WhenFileDoesNotExist_DoesNotAddContent()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        var readOp = new FileTokenReadOperationAsync<SampleDto>(_tempDir);

        await readOp.ExecuteAsync(message);

        message.GetContent<SampleDto>().Should().BeNull();
    }

    #endregion

    #region [ FileLogWriteOperation / FileLogWriteOperationAsync ]

    [Fact]
    public void FileLogWriteOperation_WithValidPath_CreatesLogFile()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage("key", "value");
        var writeOp = new FileLogWriteOperation(_tempDir);

        writeOp.Execute(message);

        string dayFolder = Path.Combine(_tempDir, $"{DateTime.Today:yyyy_MM_dd}");
        Directory.Exists(dayFolder).Should().BeTrue();
        Directory.GetFiles(dayFolder, "message_*.log").Should().NotBeEmpty();
    }

    [Fact]
    public void FileLogWriteOperation_WithEmptyFilePath_DoesNotCreateFile()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        var writeOp = new FileLogWriteOperation(string.Empty);

        writeOp.Execute(message);

        Directory.Exists(_tempDir).Should().BeFalse();
    }

    [Fact]
    public async Task FileLogWriteOperationAsync_WithValidPath_CreatesLogFile()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage("key", "value");
        var writeOp = new FileLogWriteOperationAsync(_tempDir);

        await writeOp.ExecuteAsync(message);

        string dayFolder = Path.Combine(_tempDir, $"{DateTime.Today:yyyy_MM_dd}");
        Directory.Exists(dayFolder).Should().BeTrue();
        Directory.GetFiles(dayFolder, "message_*.log").Should().NotBeEmpty();
    }

    [Fact]
    public async Task FileLogWriteOperationAsync_WithEmptyFilePath_DoesNotCreateFile()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        var writeOp = new FileLogWriteOperationAsync(string.Empty);

        await writeOp.ExecuteAsync(message);

        Directory.Exists(_tempDir).Should().BeFalse();
    }

    #endregion
}
