using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga.Extensions;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga.Persistence;

namespace Mvp24Hours.Application.RabbitMQ.Test.Extensions;

[Trait("Category", "Unit")]
public class SagaServiceExtensionsCoverageTest
{
    [Fact]
    public void AddSagaInMemory_ShouldRegisterRepository()
    {
        var services = new ServiceCollection();
        services.AddSagaInMemory<TestOrderSagaData>();

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ISagaRepository<TestOrderSagaData>>()
            .Should().BeOfType<InMemorySagaRepository<TestOrderSagaData>>();
    }

    [Fact]
    public void AddSagaRedis_ShouldRegisterRepositoryAndOptions()
    {
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        services.AddSagaRedis<TestOrderSagaData>(o => o.DefaultExpiration = TimeSpan.FromHours(12));

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ISagaRepository<TestOrderSagaData>>()
            .Should().BeOfType<RedisSagaRepository<TestOrderSagaData>>();
        provider.GetRequiredService<RedisSagaRepositoryOptions>().DefaultExpiration.Should().Be(TimeSpan.FromHours(12));
    }

    [Fact]
    public void AddSagaConsumer_ShouldRegisterProcessorAndAdapter()
    {
        var services = new ServiceCollection();
        services.AddSagaConsumer<TestOrderSagaData, TestOrderCreatedEvent, TestSagaConsumer>();

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<TestSagaConsumer>().Should().NotBeNull();
        provider.GetService<SagaMessageConsumerAdapter<TestOrderSagaData, TestOrderCreatedEvent, TestSagaConsumer>>().Should().NotBeNull();
        provider.GetService<SagaConsumerProcessor<TestOrderSagaData, TestOrderCreatedEvent, TestSagaConsumer>>().Should().NotBeNull();
    }

    [Fact]
    public void AddSagaStateMachineConsumer_ShouldRegisterConsumer()
    {
        var services = new ServiceCollection();
        services.AddSagaStateMachine<TestOrderSagaData, TestOrderStateMachine>();
        services.AddSagaStateMachineConsumer<TestOrderSagaData, TestOrderCreatedEvent, TestOrderStateMachine>();

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<SagaStateMachineConsumer<TestOrderSagaData, TestOrderCreatedEvent, TestOrderStateMachine>>()
            .Should().NotBeNull();
    }

    [Fact]
    public void AddSaga_WithInMemoryPersistence_ShouldRegisterStateMachineAndRepository()
    {
        var services = new ServiceCollection();
        services.AddSaga<TestOrderSagaData, TestOrderStateMachine>(o => o.PersistenceType = SagaPersistenceType.InMemory);

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<TestOrderStateMachine>().Should().NotBeNull();
        provider.GetRequiredService<ISagaRepository<TestOrderSagaData>>()
            .Should().BeOfType<InMemorySagaRepository<TestOrderSagaData>>();
    }

    [Fact]
    public void SagaOptions_Defaults_ShouldHaveExpectedValues()
    {
        var options = new SagaOptions<TestOrderSagaData>();

        options.PersistenceType.Should().Be(SagaPersistenceType.InMemory);
        options.EnableTimeouts.Should().BeTrue();
        options.DefaultExpiration.Should().Be(TimeSpan.FromHours(24));
        options.CompletedExpiration.Should().Be(TimeSpan.FromHours(1));
    }

    private sealed class TestSagaConsumer : ISagaConsumer<TestOrderSagaData, TestOrderCreatedEvent>
    {
        public Guid GetCorrelationId(TestOrderCreatedEvent message)
        {
            return message.CorrelationId;
        }

        public Task ConsumeAsync(
            ISagaConsumeContext<TestOrderSagaData, TestOrderCreatedEvent> context,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TestOrderStateMachine : SagaStateMachine<TestOrderSagaData>
    {
        public TestOrderStateMachine()
        {
            Initially(When<TestOrderCreatedEvent>().TransitionTo("AwaitingPayment"));
        }
    }
}
