using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;

namespace Mvp24Hours.Application.RabbitMQ.Test.Core;

public class CoreContractTest
{
    [Fact]
    public void Response_Success_ShouldSetProperties()
    {
        var response = Response<TestOrderResponse>.Success(
            new TestOrderResponse { Success = true, Message = "ok" },
            correlationId: "corr-1",
            elapsed: TimeSpan.FromMilliseconds(50));

        response.IsSuccess.Should().BeTrue();
        response.Status.Should().Be(ResponseStatus.Success);
        response.Message!.Success.Should().BeTrue();
        response.CorrelationId.Should().Be("corr-1");
        response.Elapsed.Should().Be(TimeSpan.FromMilliseconds(50));
        response.ReceivedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Response_Timeout_ShouldSetErrorMessage()
    {
        var response = Response<TestOrderResponse>.Timeout("corr-2", TimeSpan.FromSeconds(30));

        response.IsSuccess.Should().BeFalse();
        response.Status.Should().Be(ResponseStatus.Timeout);
        response.ErrorMessage.Should().Contain("timed out");
        response.CorrelationId.Should().Be("corr-2");
    }

    [Fact]
    public void Response_Failure_ShouldIncludeException()
    {
        var ex = new InvalidOperationException("failed");
        var response = Response<TestOrderResponse>.Failure("error", ex, "corr-3");

        response.IsSuccess.Should().BeFalse();
        response.Status.Should().Be(ResponseStatus.Failed);
        response.ErrorMessage.Should().Be("error");
        response.Exception.Should().BeSameAs(ex);
    }

    [Fact]
    public void Response_Cancelled_ShouldSetStatus()
    {
        var response = Response<TestOrderResponse>.Cancelled("corr-4");

        response.IsSuccess.Should().BeFalse();
        response.Status.Should().Be(ResponseStatus.Cancelled);
        response.ErrorMessage.Should().Contain("cancelled");
    }

    [Fact]
    public void ScheduleMessageOptions_ShouldHaveDefaults()
    {
        var options = new ScheduleMessageOptions();

        options.RoutingKey.Should().BeNull();
        options.Exchange.Should().BeNull();
        options.CorrelationId.Should().BeNull();
        options.Headers.Should().BeNull();
        options.Priority.Should().BeNull();
        options.TtlMilliseconds.Should().BeNull();
    }

    [Theory]
    [InlineData(ResponseStatus.Success)]
    [InlineData(ResponseStatus.Timeout)]
    [InlineData(ResponseStatus.Failed)]
    [InlineData(ResponseStatus.Cancelled)]
    public void ResponseStatus_ShouldContainAllValues(ResponseStatus status)
    {
        Enum.IsDefined(status).Should().BeTrue();
    }
}
