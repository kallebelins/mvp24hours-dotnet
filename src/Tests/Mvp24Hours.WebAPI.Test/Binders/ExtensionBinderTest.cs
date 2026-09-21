using Microsoft.AspNetCore.Http;
using Mvp24Hours.WebAPI.Binders;
using Mvp24Hours.WebAPI.Test.Support;

namespace Mvp24Hours.WebAPI.Test.Binders;

[Trait("Category", "Unit")]
public class ExtensionBinderTest
{
    public sealed class SampleFilter
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    public sealed class SampleFilterBinder : ExtensionBinder<SampleFilter>
    {
    }

    [Fact]
    public async Task BindAsync_WithQueryString_ReturnsPopulatedInstance()
    {
        // Arrange
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();
        context.Request.QueryString = new QueryString("?Name=Alice&Age=30");

        // Act
        SampleFilter result = await SampleFilterBinder.BindAsync(context);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Alice");
        result.Age.Should().Be(30);
    }

    [Fact]
    public async Task BindAsync_WithoutQueryString_ReturnsNewInstance()
    {
        // Arrange
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();

        // Act
        SampleFilter result = await SampleFilterBinder.BindAsync(context);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().BeNull();
        result.Age.Should().Be(0);
    }
}
