using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Application.RabbitMQ.Test.Support.Consumers;
using Mvp24Hours.Application.RabbitMQ.Test.Support.Dto;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.RabbitMQ;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing.Helpers;

namespace Mvp24Hours.Application.RabbitMQ.Test.Integration;

[Trait("Category", "Integration")]
[Collection(RabbitMqIntegrationCollection.Name)]
public class MvpRabbitMQClientIntegrationTest(RabbitMqIntegrationFixture fixture)
{
    [DockerFact]
    public void Publish_ShouldReturnRoutingKeyAgainstRealBroker()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        IServiceProvider serviceProvider = CreateServiceProvider(includeConsumers: false);
        MvpRabbitMQClient client = serviceProvider.GetRequiredService<MvpRabbitMQClient>();

        string result = client.Publish(new CustomerEvent
        {
            Id = 42,
            Name = "Integration Customer",
            Active = true
        }, typeof(CustomerEvent).Name);

        result.Should().NotBeNullOrWhiteSpace();
        result.HasValue().Should().BeTrue();
    }

    [DockerFact]
    public void PublishAndConsume_WithRegisteredConsumer_ShouldNotThrow()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        IServiceProvider serviceProvider = CreateServiceProvider(includeConsumers: true);
        MvpRabbitMQClient client = serviceProvider.GetRequiredService<MvpRabbitMQClient>();

        client.Publish(new CustomerEvent
        {
            Id = 7,
            Name = "Consume Test",
            Active = true
        }, typeof(CustomerEvent).Name);

        Action act = () => client.Consume();

        act.Should().NotThrow();
    }

    [DockerFact]
    public void PublishBatch_ShouldReturnMessageIdsAgainstRealBroker()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        IServiceProvider serviceProvider = CreateServiceProvider(includeConsumers: false);
        MvpRabbitMQClient client = serviceProvider.GetRequiredService<MvpRabbitMQClient>();

        IEnumerable<string> ids = client.PublishBatch([
            (new CustomerEvent { Id = 1, Name = "Batch-1", Active = true }, typeof(CustomerEvent).Name),
            (new CustomerEvent { Id = 2, Name = "Batch-2", Active = true }, typeof(CustomerEvent).Name)
        ]);

        ids.Should().HaveCount(2);
        ids.Should().OnlyContain(id => !string.IsNullOrWhiteSpace(id));
    }

    [DockerFact]
    public async Task PublishAsync_ShouldReturnMessageIdAgainstRealBroker()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        IServiceProvider serviceProvider = CreateServiceProvider(includeConsumers: false);
        MvpRabbitMQClient client = serviceProvider.GetRequiredService<MvpRabbitMQClient>();

        string messageId = await client.PublishAsync(
            new CustomerEvent { Id = 99, Name = "Async Integration", Active = true },
            typeof(CustomerEvent).Name);

        messageId.Should().NotBeNullOrWhiteSpace();
    }

    [DockerFact]
    public void PublishWithTtl_ShouldReturnMessageIdAgainstRealBroker()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        IServiceProvider serviceProvider = CreateServiceProvider(includeConsumers: false);
        MvpRabbitMQClient client = serviceProvider.GetRequiredService<MvpRabbitMQClient>();

        string messageId = client.PublishWithTtl(
            new CustomerEvent { Id = 55, Name = "TTL Integration", Active = true },
            typeof(CustomerEvent).Name,
            ttlMilliseconds: 30_000);

        messageId.Should().NotBeNullOrWhiteSpace();
    }

    private IServiceProvider CreateServiceProvider(bool includeConsumers)
    {
        var services = new ServiceCollection();

        if (includeConsumers)
        {
            services.AddScoped<CustomerConsumer>();
        }

        Type[] consumers = includeConsumers
            ? [typeof(CustomerConsumer)]
            : [];

        services.AddMvp24HoursRabbitMQ(
            consumers,
            connectionOptions =>
            {
                connectionOptions.ConnectionString = fixture.ConnectionString;
                connectionOptions.DispatchConsumersAsync = true;
                connectionOptions.RetryCount = 3;
            },
            clientOptions => clientOptions.MaxRedeliveredCount = 1);

        return services.BuildServiceProvider();
    }
}

[Trait("Category", "Integration")]
[Collection(RabbitMqIntegrationCollection.Name)]
public class TestHarnessBuilderIntegrationTest(RabbitMqIntegrationFixture fixture)
{
    [DockerFact]
    public async Task Build_WithInMemoryBus_ShouldPublishAndConsume()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        ITestHarness harness = TestHarnessBuilder.Create()
            .UseInMemoryBus()
            .AddConsumer<TestOrderConsumer>()
            .Build();

        await harness.StartAsync();

        IConsumedMessage<TestOrderEvent> consumed = await harness.PublishAndWaitAsync(
            new TestOrderEvent { Name = "harness-integration" });

        consumed.IsSuccess.Should().BeTrue();
        consumed.Message.Name.Should().Be("harness-integration");
        harness.Bus.PublishedCount<TestOrderEvent>().Should().Be(1);
    }

    [DockerFact]
    public void Build_WithConsumerFromAssembly_ShouldRegisterConsumer()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        ITestHarness harness = TestHarnessBuilder.Create()
            .UseInMemoryBus()
            .AddConsumersFromAssemblyContaining<TestOrderConsumer>()
            .Build();

        harness.ServiceProvider.GetService<IMessageConsumer<TestOrderEvent>>().Should().NotBeNull();
    }
}

internal sealed class TestOrderConsumer : IMessageConsumer<TestOrderEvent>
{
    public Task ConsumeAsync(IConsumeContext<TestOrderEvent> context, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
