using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Consumers;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Exceptions;
using Mvp24Hours.Infrastructure.RabbitMQ.Messages;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing;

namespace Mvp24Hours.Application.RabbitMQ.Test.Core;

[Trait("Category", "Unit")]
public class CoreTypesCoverageTest
{
    [Fact]
    public void FaultContext_FromConsumeContext_ShouldMapMetadata()
    {
        TestConsumeContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateTestConsumeContext(
            new TestOrderEvent { Name = "fault" },
            b => b.WithMessageId("msg-1").WithCorrelationId("corr-1").WithQueueName("orders"));

        var fault = FaultContext<TestOrderEvent>.FromConsumeContext(
            context,
            new InvalidOperationException("consume failed"));

        fault.Message.Name.Should().Be("fault");
        fault.MessageId.Should().Be("msg-1");
        fault.CorrelationId.Should().Be("corr-1");
        fault.QueueName.Should().Be("orders");
        fault.Exception.Message.Should().Be("consume failed");
    }

    [Fact]
    public void FaultContext_Constructor_WithNullMessage_ShouldThrow()
    {
        Action act = () => new FaultContext<TestOrderEvent>(
            null!,
            new Exception(),
            "msg",
            null,
            "ex",
            "rk",
            "q",
            0,
            new ServiceCollection().BuildServiceProvider());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Message_Create_ShouldPopulateMetadata()
    {
        var headers = new Dictionary<string, object> { ["x-region"] = "us" };

        var message = Message<TestOrderEvent>.Create(
            new TestOrderEvent { Name = "envelope" },
            correlationId: "corr-1",
            causationId: "cause-1",
            sourceApplication: "tests",
            headers: headers);

        message.Payload.Name.Should().Be("envelope");
        message.CorrelationId.Should().Be("corr-1");
        message.CausationId.Should().Be("cause-1");
        message.SourceApplication.Should().Be("tests");
        message.Headers.Should().ContainKey("x-region");
    }

    [Fact]
    public void RequestTimeoutException_WithFullDetails_ShouldExposeProperties()
    {
        var timeout = TimeSpan.FromSeconds(2);
        var ex = new RequestTimeoutException(typeof(TestOrderCommand), typeof(TestOrderResponse), timeout, "corr-99");

        ex.RequestType.Should().Be(typeof(TestOrderCommand));
        ex.ResponseType.Should().Be(typeof(TestOrderResponse));
        ex.Timeout.Should().Be(timeout);
        ex.CorrelationId.Should().Be("corr-99");
        ex.Message.Should().Contain(nameof(TestOrderCommand));
    }

    [Fact]
    public void RequestTimeoutException_DefaultConstructor_ShouldHaveMessage()
    {
        var ex = new RequestTimeoutException();

        ex.Message.Should().Contain("timed out");
    }

    [Fact]
    public void ConsumerDefinition_ShouldExposeConfiguredValues()
    {
        var definition = new TestConsumerDefinition();

        definition.QueueName.Should().Be("orders-queue");
        definition.Exchange.Should().Be("orders-exchange");
        definition.RoutingKey.Should().Be("orders.route");
        definition.PrefetchCount.Should().Be((ushort)20);
        definition.ConcurrentConsumers.Should().Be(2);
        definition.MaxRetryCount.Should().Be(5);
        definition.UseDeadLetterQueue.Should().BeFalse();
        definition.MessageType.Should().Be(typeof(TestOrderEvent));
    }

    private sealed class TestOrderConsumer : IMessageConsumer<TestOrderEvent>
    {
        public Task ConsumeAsync(IConsumeContext<TestOrderEvent> context, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TestConsumerDefinition : ConsumerDefinition<TestOrderConsumer>
    {
        public TestConsumerDefinition()
        {
            Queue("orders-queue");
            ExchangeName("orders-exchange");
            Route("orders.route");
            Prefetch(20);
            Concurrent(2);
            Retry(5);
            NoDeadLetter();
        }
    }
}
