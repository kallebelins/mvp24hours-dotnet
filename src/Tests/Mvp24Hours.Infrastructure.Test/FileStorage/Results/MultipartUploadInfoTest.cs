//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.FileStorage.Results;

namespace Mvp24Hours.Infrastructure.Test.FileStorage.Results;

[Trait("Category", "Unit")]
public class MultipartUploadInfoTest
{
    [Fact]
    public void Constructor_WithValidValues_ShouldSetAllProperties()
    {
        Dictionary<int, string> partUrls = new()
        {
            [1] = "https://storage/part-1",
            [2] = "https://storage/part-2"
        };

        MultipartUploadInfo info = new("upload-abc", partUrls, partSize: 5242880, totalParts: 2);

        info.UploadId.Should().Be("upload-abc");
        info.PartUrls.Should().BeSameAs(partUrls);
        info.PartSize.Should().Be(5242880);
        info.TotalParts.Should().Be(2);
    }

    [Fact]
    public void Constructor_WithNullPartUrls_ShouldUseEmptyDictionary()
    {
        MultipartUploadInfo info = new("upload-abc", null!, partSize: 1024, totalParts: 0);

        info.PartUrls.Should().NotBeNull().And.BeEmpty();
    }
}
