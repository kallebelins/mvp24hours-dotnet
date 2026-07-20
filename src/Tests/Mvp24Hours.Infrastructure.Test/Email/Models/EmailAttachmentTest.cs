//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Email.Models;

namespace Mvp24Hours.Infrastructure.Test.Email.Models;

[Trait("Category", "Unit")]
public class EmailAttachmentTest
{
    [Fact]
    public void Constructor_WithByteArray_ShouldSetProperties()
    {
        byte[] content = "hello"u8.ToArray();
        var attachment = new EmailAttachment("file.txt", content, "text/plain");

        attachment.FileName.Should().Be("file.txt");
        attachment.ContentType.Should().Be("text/plain");
        attachment.Content.Should().BeEquivalentTo(content);
        attachment.ContentLength.Should().Be(content.Length);
        attachment.IsInline.Should().BeFalse();
        attachment.ContentId.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidFileName_ShouldThrowArgumentNullException(string? fileName)
    {
        Action act = () => _ = new EmailAttachment(fileName!, "data"u8.ToArray(), "text/plain");

        act.Should().Throw<ArgumentNullException>().WithParameterName("fileName");
    }

    [Fact]
    public void Constructor_WithNullContent_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new EmailAttachment("file.txt", null!, "text/plain");

        act.Should().Throw<ArgumentNullException>().WithParameterName("content");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidContentType_ShouldThrowArgumentNullException(string? contentType)
    {
        Action act = () => _ = new EmailAttachment("file.txt", "data"u8.ToArray(), contentType!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("contentType");
    }

    [Fact]
    public void Constructor_WithStream_ShouldSetContentLengthWhenSeekable()
    {
        byte[] content = "stream-data"u8.ToArray();
        using var stream = new MemoryStream(content);

        var attachment = new EmailAttachment("file.bin", stream, "application/octet-stream");

        attachment.ContentLength.Should().Be(content.Length);
        attachment.GetContentStream().Should().NotBeNull();
        using Stream contentStream = attachment.GetContentStream()!;
        using var reader = new StreamReader(contentStream);
        reader.ReadToEnd().Should().Be("stream-data");
    }

    [Fact]
    public void Constructor_WithNullStream_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new EmailAttachment("file.bin", (Stream)null!, "application/octet-stream");

        act.Should().Throw<ArgumentNullException>().WithParameterName("contentStream");
    }

    [Fact]
    public void InlineProperties_ShouldRoundTrip()
    {
        var attachment = new EmailAttachment("logo.png", "png"u8.ToArray(), "image/png")
        {
            ContentId = "logo",
            IsInline = true
        };

        attachment.ContentId.Should().Be("logo");
        attachment.IsInline.Should().BeTrue();
    }
}
