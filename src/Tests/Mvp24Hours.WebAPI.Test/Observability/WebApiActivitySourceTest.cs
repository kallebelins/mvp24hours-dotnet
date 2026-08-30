using System.Diagnostics;
using Mvp24Hours.WebAPI.Observability;

namespace Mvp24Hours.WebAPI.Test.Observability;

[Trait("Category", "Unit")]
public class WebApiActivitySourceTest
{
    [Fact]
    public void StartHttpRequestActivity_WithoutListener_ReturnsNull()
    {
        // Arrange - no ActivityListener is registered for this source in this test.
        // Act
        Activity? activity = WebApiActivitySource.StartHttpRequestActivity("GET", "/api/orders");

        // Assert
        activity.Should().BeNull();
    }

    [Fact]
    public void StartHttpRequestActivity_WithListener_SetsMethodAndPathTags()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == WebApiActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        Activity? activity = WebApiActivitySource.StartHttpRequestActivity("POST", "/api/customers", correlationId: "corr-1");

        // Assert
        activity.Should().NotBeNull();
        activity!.GetTagItem(WebApiActivitySource.TagNames.HttpMethod).Should().Be("POST");
        activity.GetTagItem(WebApiActivitySource.TagNames.HttpPath).Should().Be("/api/customers");
        activity.GetTagItem(WebApiActivitySource.TagNames.CorrelationId).Should().Be("corr-1");
        activity.Dispose();
    }

    [Fact]
    public void StartHttpRequestActivity_WithoutCorrelationId_DoesNotSetCorrelationTag()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == WebApiActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        Activity? activity = WebApiActivitySource.StartHttpRequestActivity("GET", "/api/orders");

        // Assert
        activity.Should().NotBeNull();
        activity!.GetTagItem(WebApiActivitySource.TagNames.CorrelationId).Should().BeNull();
        activity.Dispose();
    }

    [Fact]
    public void SetSuccess_WithNullActivity_DoesNotThrow()
    {
        // Act
        Action act = () => WebApiActivitySource.SetSuccess(null, 200);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void SetSuccess_WithActivity_SetsStatusCodeAndOkStatus()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == WebApiActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        Activity? activity = WebApiActivitySource.Source.StartActivity("test-activity");

        // Act
        WebApiActivitySource.SetSuccess(activity, 200);

        // Assert
        activity.Should().NotBeNull();
        activity!.GetTagItem(WebApiActivitySource.TagNames.HttpStatusCode).Should().Be(200);
        activity.GetTagItem(WebApiActivitySource.TagNames.IsSuccess).Should().Be(true);
        activity.Status.Should().Be(ActivityStatusCode.Ok);
        activity.Dispose();
    }

    [Fact]
    public void SetError_WithNullActivity_DoesNotThrow()
    {
        // Act
        Action act = () => WebApiActivitySource.SetError(null, new InvalidOperationException("boom"), 500);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void SetError_WithActivity_SetsErrorTagsAndExceptionEvent()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == WebApiActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        Activity? activity = WebApiActivitySource.Source.StartActivity("test-error-activity");
        var exception = new InvalidOperationException("boom");

        // Act
        WebApiActivitySource.SetError(activity, exception, 500);

        // Assert
        activity.Should().NotBeNull();
        activity!.GetTagItem(WebApiActivitySource.TagNames.HttpStatusCode).Should().Be(500);
        activity.GetTagItem(WebApiActivitySource.TagNames.IsSuccess).Should().Be(false);
        activity.GetTagItem(WebApiActivitySource.TagNames.ErrorType).Should().Be(typeof(InvalidOperationException).FullName);
        activity.GetTagItem(WebApiActivitySource.TagNames.ErrorMessage).Should().Be("boom");
        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.Events.Should().ContainSingle(e => e.Name == "exception");
        activity.Dispose();
    }

    [Fact]
    public void RecordRequest_WithMinimalArguments_DoesNotThrow()
    {
        // Act
        Action act = () => WebApiActivitySource.RecordRequest("GET", "/api/health", 200, 12.5);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordRequest_WithAllOptionalArguments_DoesNotThrow()
    {
        // Act
        Action act = () => WebApiActivitySource.RecordRequest(
            method: "POST",
            path: "/api/orders",
            statusCode: 500,
            durationMs: 250.0,
            requestSizeBytes: 128,
            responseSizeBytes: 256,
            isError: true,
            isSlow: true);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void IncrementAndDecrementInProgress_DoNotThrow()
    {
        // Act
        Action act = () =>
        {
            WebApiActivitySource.IncrementInProgress();
            WebApiActivitySource.IncrementInProgress();
            WebApiActivitySource.DecrementInProgress();
            WebApiActivitySource.DecrementInProgress();
        };

        // Assert
        act.Should().NotThrow();
    }
}
