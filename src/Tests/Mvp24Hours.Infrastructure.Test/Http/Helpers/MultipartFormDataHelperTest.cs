using System.Text;
using Mvp24Hours.Infrastructure.Http.Helpers;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.Http.Helpers;

[Trait("Category", "Unit")]
public class MultipartFormDataHelperTest
{
    [Fact]
    public void Constructor_Parameterless_ShouldGenerateBoundaryAutomatically()
    {
        var helper = new MultipartFormDataHelper();

        MultipartFormDataContent content = helper.Build();

        content.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithCustomBoundary_ShouldUseIt()
    {
        var helper = new MultipartFormDataHelper("my-custom-boundary");

        MultipartFormDataContent content = helper.Build();

        content.Headers.ContentType!.Parameters.Should().Contain(p => p.Name == "boundary" && p.Value!.Contains("my-custom-boundary"));
    }

    [Fact]
    public void Constructor_WithNullBoundary_ShouldThrow()
    {
        Action act = () => _ = new MultipartFormDataHelper((string)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("boundary");
    }

    [Fact]
    public async Task AddField_WithNullOrWhitespaceName_ShouldThrow()
    {
        var helper = new MultipartFormDataHelper();

        Action act = () => helper.AddField(" ", "value");

        act.Should().Throw<ArgumentException>().WithParameterName("name");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task AddField_WithValidNameAndValue_ShouldAddStringContent()
    {
        var helper = new MultipartFormDataHelper();

        MultipartFormDataHelper result = helper.AddField("title", "hello world");

        result.Should().BeSameAs(helper);
        MultipartFormDataContent content = helper.Build();
        string body = await content.ReadAsStringAsync();
        body.Should().Contain("hello world");
        body.Should().Contain("name=title");
    }

    [Fact]
    public async Task AddField_WithNullValue_ShouldAddEmptyStringContent()
    {
        var helper = new MultipartFormDataHelper();

        helper.AddField("title", null!);

        MultipartFormDataContent content = helper.Build();
        string body = await content.ReadAsStringAsync();
        body.Should().Contain("name=title");
    }

    [Fact]
    public void AddFile_Stream_WithNullOrWhitespaceName_ShouldThrow()
    {
        var helper = new MultipartFormDataHelper();
        using var stream = new MemoryStream();

        Action act = () => helper.AddFile(" ", stream, "file.txt");

        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void AddFile_Stream_WithNullStream_ShouldThrow()
    {
        var helper = new MultipartFormDataHelper();

        Action act = () => helper.AddFile("file", (Stream)null!, "file.txt");

        act.Should().Throw<ArgumentNullException>().WithParameterName("stream");
    }

    [Fact]
    public void AddFile_Stream_WithNullOrWhitespaceFileName_ShouldThrow()
    {
        var helper = new MultipartFormDataHelper();
        using var stream = new MemoryStream();

        Action act = () => helper.AddFile("file", stream, " ");

        act.Should().Throw<ArgumentException>().WithParameterName("fileName");
    }

    [Fact]
    public async Task AddFile_Stream_WithContentType_ShouldSetContentTypeHeader()
    {
        var helper = new MultipartFormDataHelper();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("payload"));

        helper.AddFile("file", stream, "file.bin", "application/custom");

        MultipartFormDataContent content = helper.Build();
        HttpContent part = content.Single();
        part.Headers.ContentType!.MediaType.Should().Be("application/custom");
        await Task.CompletedTask;
    }

    [Fact]
    public void AddFile_ByteArray_WithNullBytes_ShouldThrow()
    {
        var helper = new MultipartFormDataHelper();

        Action act = () => helper.AddFile("file", (byte[])null!, "file.bin");

        act.Should().Throw<ArgumentNullException>().WithParameterName("bytes");
    }

    [Fact]
    public async Task AddFile_ByteArray_ShouldAddContentMatchingBytes()
    {
        var helper = new MultipartFormDataHelper();
        byte[] payload = Encoding.UTF8.GetBytes("byte-content");

        helper.AddFile("file", payload, "file.bin");

        MultipartFormDataContent content = helper.Build();
        byte[] read = await content.Single().ReadAsByteArrayAsync();
        read.Should().Equal(payload);
    }

    [Fact]
    public async Task AddFileAsync_WithNullOrWhitespaceName_ShouldThrow()
    {
        var helper = new MultipartFormDataHelper();

        Func<Task> act = () => helper.AddFileAsync(" ", CreateChunks("a"), "file.bin");

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public async Task AddFileAsync_WithNullStream_ShouldThrow()
    {
        var helper = new MultipartFormDataHelper();

        Func<Task> act = () => helper.AddFileAsync("file", null!, "file.bin");

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("stream");
    }

    [Fact]
    public async Task AddFileAsync_WithNullOrWhitespaceFileName_ShouldThrow()
    {
        var helper = new MultipartFormDataHelper();

        Func<Task> act = () => helper.AddFileAsync("file", CreateChunks("a"), " ");

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("fileName");
    }

    [Fact]
    public async Task AddFileAsync_ShouldAccumulateChunksIntoContent()
    {
        var helper = new MultipartFormDataHelper();

        MultipartFormDataHelper result = await helper.AddFileAsync("file", CreateChunks("chunk1", "chunk2"), "file.bin", "text/plain");

        result.Should().BeSameAs(helper);
        MultipartFormDataContent content = helper.Build();
        HttpContent part = content.Single();
        string body = await part.ReadAsStringAsync();
        body.Should().Be("chunk1chunk2");
        part.Headers.ContentType!.MediaType.Should().Be("text/plain");
    }

    [Fact]
    public void AddFileFromPath_WithNullOrWhitespacePath_ShouldThrow()
    {
        var helper = new MultipartFormDataHelper();

        Action act = () => helper.AddFileFromPath("file", " ");

        act.Should().Throw<ArgumentException>().WithParameterName("filePath");
    }

    [Fact]
    public void AddFileFromPath_WithMissingFile_ShouldThrowFileNotFoundException()
    {
        var helper = new MultipartFormDataHelper();

        Action act = () => helper.AddFileFromPath("file", @"C:\this\path\does\not\exist.txt");

        act.Should().Throw<FileNotFoundException>();
    }

    [Theory]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("photo.jpeg", "image/jpeg")]
    [InlineData("image.png", "image/png")]
    [InlineData("anim.gif", "image/gif")]
    [InlineData("doc.pdf", "application/pdf")]
    [InlineData("notes.txt", "text/plain")]
    [InlineData("data.json", "application/json")]
    [InlineData("data.xml", "application/xml")]
    [InlineData("archive.zip", "application/zip")]
    [InlineData("table.csv", "text/csv")]
    [InlineData("unknown.bin", "application/octet-stream")]
    public async Task AddFileFromPath_ShouldInferContentTypeFromExtension(string fileName, string expectedContentType)
    {
        using var tempDir = new HelpersTestHelpers.TempDirectory();
        string filePath = Path.Combine(tempDir.Path, fileName);
        await File.WriteAllTextAsync(filePath, "content");
        var helper = new MultipartFormDataHelper();

        helper.AddFileFromPath("file", filePath);

        MultipartFormDataContent content = helper.Build();
        HttpContent part = content.Single();
        part.Headers.ContentType!.MediaType.Should().Be(expectedContentType);
    }

    [Fact]
    public async Task AddFileFromPath_WithExplicitContentType_ShouldOverrideInference()
    {
        using var tempDir = new HelpersTestHelpers.TempDirectory();
        string filePath = Path.Combine(tempDir.Path, "photo.jpg");
        await File.WriteAllTextAsync(filePath, "content");
        var helper = new MultipartFormDataHelper();

        helper.AddFileFromPath("file", filePath, "application/custom");

        MultipartFormDataContent content = helper.Build();
        content.Single().Headers.ContentType!.MediaType.Should().Be("application/custom");
    }

    [Fact]
    public void AddFields_WithNullDictionary_ShouldThrow()
    {
        var helper = new MultipartFormDataHelper();

        Action act = () => helper.AddFields(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("fields");
    }

    [Fact]
    public async Task AddFields_ShouldAddEachEntryAsAField()
    {
        var helper = new MultipartFormDataHelper();
        var fields = new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" };

        MultipartFormDataHelper result = helper.AddFields(fields);

        result.Should().BeSameAs(helper);
        MultipartFormDataContent content = helper.Build();
        content.Should().HaveCount(2);
        string body = await content.ReadAsStringAsync();
        body.Should().Contain("name=a").And.Contain("name=b").And.Contain("1").And.Contain("2");
    }

    [Fact]
    public void Build_ShouldReturnSameContentAcrossCalls()
    {
        var helper = new MultipartFormDataHelper();

        MultipartFormDataContent first = helper.Build();
        MultipartFormDataContent second = helper.Build();

        first.Should().BeSameAs(second);
    }

    [Fact]
    public void CreateMultipartFormData_Parameterless_ShouldReturnUsableHelper()
    {
        MultipartFormDataHelper helper = MultipartFormDataExtensions.CreateMultipartFormData();

        helper.Should().NotBeNull();
        helper.Build().Should().NotBeNull();
    }

    [Fact]
    public void CreateMultipartFormData_WithBoundary_ShouldApplyIt()
    {
        MultipartFormDataHelper helper = MultipartFormDataExtensions.CreateMultipartFormData("extension-boundary");

        MultipartFormDataContent content = helper.Build();
        content.Headers.ContentType!.Parameters.Should().Contain(p => p.Name == "boundary" && p.Value!.Contains("extension-boundary"));
    }

    private static async IAsyncEnumerable<byte[]> CreateChunks(params string[] chunks)
    {
        foreach (string chunk in chunks)
        {
            yield return Encoding.UTF8.GetBytes(chunk);
            await Task.Yield();
        }
    }
}
