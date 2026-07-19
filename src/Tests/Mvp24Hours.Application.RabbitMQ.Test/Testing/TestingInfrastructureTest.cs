using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing.Helpers;

namespace Mvp24Hours.Application.RabbitMQ.Test.Testing;

public class TestingInfrastructureTest
{
    [Fact]
    public void InMemoryBus_PublishAndQuery_ShouldTrackMessages()
    {
        InMemoryBus bus = RabbitMQTestHelpers.CreateInMemoryBus();

        bus.Publish(new TestOrderEvent { Name = "one" }, "route-1");
        bus.Publish(new TestOrderEvent { Name = "two" }, "route-2");

        bus.PublishedCount<TestOrderEvent>().Should().Be(2);
        bus.WasPublished<TestOrderEvent>(m => m.Name == "two").Should().BeTrue();
    }

    [Fact]
    public async Task InMemoryBus_ConsumeAsync_WithConsumer_ShouldRecordConsumedMessage()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMessageConsumer<TestOrderEvent>, TestOrderConsumer>();
        InMemoryBus bus = new(services.BuildServiceProvider());

        ConsumeResult result = await bus.ConsumeAsync(new TestOrderEvent { Name = "consume" });

        result.IsSuccess.Should().BeTrue();
        bus.ConsumedCount<TestOrderEvent>().Should().Be(1);
    }

    [Fact]
    public async Task InMemoryBus_SimulateFailure_ShouldReturnFailureResult()
    {
        InMemoryBus bus = RabbitMQTestHelpers.CreateInMemoryBus();
        bus.SimulateFailure(new InvalidOperationException("simulated"));

        ConsumeResult result = await bus.ConsumeAsync(new TestOrderEvent());

        result.IsSuccess.Should().BeFalse();
        result.Exception.Should().NotBeNull();
    }

    [Fact]
    public void TestConsumeContextBuilder_ForTenant_ShouldSetHeader()
    {
        TestConsumeContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateTestConsumeContext(
            new TestOrderEvent(),
            b => b.ForTenant("tenant-x"));

        context.GetHeader<string>("x-tenant-id").Should().Be("tenant-x");
    }

    [Fact]
    public void ConsumedMessage_Create_ShouldExposeMetadata()
    {
        TestConsumeContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateTestConsumeContext(
            new TestOrderEvent { Name = "meta" },
            b => b.WithMessageId("msg-99"));

        IConsumedMessage<TestOrderEvent> consumed = ConsumedMessage<TestOrderEvent>.Create<IMessageConsumer<TestOrderEvent>>(
            context.Message,
            context,
            typeof(TestOrderConsumer),
            TimeSpan.FromMilliseconds(10),
            isSuccess: true);

        consumed.MessageId.Should().Be("msg-99");
        consumed.IsSuccess.Should().BeTrue();
        consumed.Message.Name.Should().Be("meta");
    }

    [Fact]
    public void TestMessageHelpers_AssertNonePublished_ShouldThrowWhenMessagesExist()
    {
        InMemoryBus bus = RabbitMQTestHelpers.CreateInMemoryBus();
        bus.Publish(new TestOrderEvent(), "route");

        Action act = () => bus.AssertNonePublished<TestOrderEvent>();

        act.Should().Throw<InvalidOperationException>();
    }

    private sealed class TestOrderConsumer : IMessageConsumer<TestOrderEvent>
    {
        public Task ConsumeAsync(IConsumeContext<TestOrderEvent> context, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
