//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Email.Models;

namespace Mvp24Hours.Infrastructure.Test.Email.Models;

[Trait("Category", "Unit")]
public class EmbeddedImageTest
{
    [Fact]
    public void ParameterlessConstructor_ShouldUseDefaults()
    {
        var image = new EmbeddedImage();

        image.ContentId.Should().BeEmpty();
        image.ContentBytes.Should().NotBeNull().And.BeEmpty();
        image.ContentType.Should().Be("image/png");
        image.FileName.Should().BeNull();
        image.ContentLength.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithParameters_ShouldSetProperties()
    {
        byte[] content = [0x89, 0x50, 0x4E, 0x47];
        var image = new EmbeddedImage("logo", content, "image/png");

        image.ContentId.Should().Be("logo");
        image.ContentBytes.Should().BeEquivalentTo(content);
        image.ContentType.Should().Be("image/png");
        image.ContentLength.Should().Be(content.Length);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidContentId_ShouldThrowArgumentException(string? contentId)
    {
        Action act = () => _ = new EmbeddedImage(contentId!, [1, 2, 3], "image/png");

        act.Should().Throw<ArgumentException>().WithParameterName("contentId");
    }

    [Fact]
    public void Constructor_WithNullContentBytes_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new EmbeddedImage("logo", null!, "image/png");

        act.Should().Throw<ArgumentNullException>().WithParameterName("contentBytes");
    }

    [Fact]
    public void Constructor_WithNullContentType_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new EmbeddedImage("logo", [1, 2, 3], null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("contentType");
    }

    [Fact]
    public void Properties_ShouldRoundTripValues()
    {
        var image = new EmbeddedImage
        {
            ContentId = "banner",
            ContentBytes = [10, 20, 30],
            ContentType = "image/jpeg",
            FileName = "banner.jpg"
        };

        image.ContentId.Should().Be("banner");
        image.ContentBytes.Should().Equal(10, 20, 30);
        image.ContentType.Should().Be("image/jpeg");
        image.FileName.Should().Be("banner.jpg");
        image.ContentLength.Should().Be(3);
    }
}
