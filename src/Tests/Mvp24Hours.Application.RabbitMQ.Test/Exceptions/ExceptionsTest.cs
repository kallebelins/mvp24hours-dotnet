using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Exceptions;

namespace Mvp24Hours.Application.RabbitMQ.Test.Exceptions;

public class ExceptionsTest
{
    [Fact]
    public void RequestTimeoutException_DefaultConstructor_ShouldHaveDefaultMessage()
    {
        var ex = new RequestTimeoutException();

        ex.Message.Should().Contain("timed out");
        ex.CorrelationId.Should().BeNull();
        ex.RequestType.Should().BeNull();
        ex.ResponseType.Should().BeNull();
    }

    [Fact]
    public void RequestTimeoutException_MessageConstructor_ShouldSetMessage()
    {
        var ex = new RequestTimeoutException("Custom timeout error");

        ex.Message.Should().Be("Custom timeout error");
    }

    [Fact]
    public void RequestTimeoutException_MessageAndInnerException_ShouldSetBoth()
    {
        var inner = new Exception("inner");
        var ex = new RequestTimeoutException("timeout", inner);

        ex.Message.Should().Be("timeout");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void RequestTimeoutException_FullConstructor_ShouldSetAllProperties()
    {
        var timeout = TimeSpan.FromSeconds(30);
        var ex = new RequestTimeoutException(
            typeof(TestOrderCommand),
            typeof(TestOrderResponse),
            timeout,
            correlationId: "corr-abc");

        ex.RequestType.Should().Be(typeof(TestOrderCommand));
        ex.ResponseType.Should().Be(typeof(TestOrderResponse));
        ex.Timeout.Should().Be(timeout);
        ex.CorrelationId.Should().Be("corr-abc");
        ex.Message.Should().Contain(nameof(TestOrderCommand));
        ex.Message.Should().Contain(nameof(TestOrderResponse));
        ex.Message.Should().Contain("30000");
    }

    [Fact]
    public void RequestTimeoutException_FullConstructor_WithoutCorrelationId_ShouldHaveNullCorrelationId()
    {
        var ex = new RequestTimeoutException(
            typeof(TestOrderCommand),
            typeof(TestOrderResponse),
            TimeSpan.FromSeconds(10));

        ex.CorrelationId.Should().BeNull();
    }

    [Fact]
    public void RequestTimeoutException_ShouldBeTimeoutException()
    {
        var ex = new RequestTimeoutException();

        ex.Should().BeAssignableTo<TimeoutException>();
    }

    [Fact]
    public void RequestTimeoutException_ShouldBeException()
    {
        var ex = new RequestTimeoutException();

        ex.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void RequestTimeoutException_CanBeCaught_AsTimeoutException()
    {
        Exception? caught = null;
        try
        {
            throw new RequestTimeoutException("test timeout");
        }
        catch (TimeoutException ex)
        {
            caught = ex;
        }

        caught.Should().NotBeNull();
        caught!.Message.Should().Be("test timeout");
    }

    [Fact]
    public void RequestTimeoutException_Timeout_Default_ShouldBeZero()
    {
        var ex = new RequestTimeoutException("message");

        ex.Timeout.Should().Be(TimeSpan.Zero);
    }
}
