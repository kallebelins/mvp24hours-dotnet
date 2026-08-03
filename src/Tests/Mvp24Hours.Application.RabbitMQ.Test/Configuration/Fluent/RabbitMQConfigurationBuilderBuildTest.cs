using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Application.RabbitMQ.Test.Support.Consumers;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration.Fluent;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Filters;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga;
using Mvp24Hours.Infrastructure.RabbitMQ.Serialization;
using Mvp24Hours.Infrastructure.RabbitMQ.Transactional.Contract;

namespace Mvp24Hours.Application.RabbitMQ.Test.Configuration.Fluent;

[Trait("Category", "Unit")]
public class RabbitMQConfigurationBuilderBuildTest
{
    [Fact]
    public void Build_WithFullConfiguration_ShouldRegisterCoreServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();

        var builder = new RabbitMQConfigurationBuilder(services);
        builder
            .Host("amqp://guest:guest@localhost:5672", h => h.RetryCount(3).DispatchConsumersAsync(true))
            .AddConsumer<CustomerConsumer>(c => c.PrefetchCount = 32)
            .AddConsumer<TypedOrderConsumer, TestOrderEvent>()
            .AddConsumersFromAssemblyContaining<TypedOrderConsumer>()
            .AddRequestClient<TestOrderCommand, TestOrderResponse>(r => r.TimeoutMilliseconds = 5000)
            .UseRetry(r => r.Exponential(3, TimeSpan.FromSeconds(1)))
            .UseCircuitBreaker(cb => cb.TripThreshold(10))
            .UseInMemoryOutbox(opts => opts.BatchSize = 25)
            .AddSaga<TestOrderSagaStateMachine, TestOrderSagaData>(s => s.UseInMemory())
            .ConfigureEndpoints(e => e.SetPrefix("app"))
            .UseConsumeFilter<LoggingConsumeFilter>()
            .UsePublishFilter<LoggingPublishFilter>()
            .UseSendFilter<LoggingSendFilter>()
            .ConfigureClient(opts => opts.Exchange = "built-exchange");

        InvokeBuild(builder);

        services.Any(d => d.ServiceType == typeof(IRequestClient<TestOrderCommand, TestOrderResponse>))
            .Should().BeTrue();

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IMvpRabbitMQConnection>().Should().NotBeNull();
        provider.GetRequiredService<IMvpRabbitMQClient>().Should().NotBeNull();
        provider.GetRequiredService<RabbitMQBusConfiguration>().AutoConfigureEndpoints.Should().BeTrue();
        provider.GetRequiredService<IOptions<RabbitMQConnectionOptions>>().Value.RetryCount.Should().Be(3);
        provider.GetRequiredService<IOptions<RabbitMQClientOptions>>().Value.Exchange.Should().Be("built-exchange");
        provider.GetRequiredService<ITransactionalOutbox>().Should().NotBeNull();
        provider.GetService<IMessageConsumer<TestOrderEvent>>().Should().NotBeNull();
    }

    [Fact]
    public void Build_WithEntityFrameworkOutbox_ShouldSetOutboxFlags()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();

        var builder = new RabbitMQConfigurationBuilder(services);
        builder
            .Host("localhost", 5672)
            .UseEntityFrameworkOutbox<FakeOutboxDbContext>(opts => opts.BatchSize = 10);

        InvokeBuild(builder);

        RabbitMQBusConfiguration config = services.BuildServiceProvider().GetRequiredService<RabbitMQBusConfiguration>();
        config.UseEntityFrameworkOutbox.Should().BeTrue();
        config.OutboxDbContextType.Should().Be(typeof(FakeOutboxDbContext));
        config.OutboxOptions!.BatchSize.Should().Be(10);
    }

    [Fact]
    public void Build_WithTypedConsumeFilter_ShouldRegisterFilterServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();

        var builder = new RabbitMQConfigurationBuilder(services);
        builder
            .Host("localhost", 5672)
            .UseConsumeFilter<TypedConsumeFilter, TestOrderEvent>();

        InvokeBuild(builder);

        ServiceProvider provider = services.BuildServiceProvider();
        provider.GetServices<Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract.IConsumeFilter<TestOrderEvent>>().Should().NotBeEmpty();
    }

    private static void InvokeBuild(RabbitMQConfigurationBuilder builder)
    {
        MethodInfo? method = typeof(RabbitMQConfigurationBuilder).GetMethod(
            "Build",
            BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();
        method!.Invoke(builder, null);
    }

    private sealed class TypedOrderConsumer : IMessageConsumer<TestOrderEvent>
    {
        public Task ConsumeAsync(IConsumeContext<TestOrderEvent> context, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TestOrderSagaStateMachine : SagaStateMachine<TestOrderSagaData>
    {
        public TestOrderSagaStateMachine()
        {
            Initially(When<TestOrderCreatedEvent>().TransitionTo("Active"));
        }
    }

    private sealed class FakeOutboxDbContext;

    private sealed class TypedConsumeFilter : Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract.IConsumeFilter<TestOrderEvent>
    {
        public Task ConsumeAsync(
            Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract.IConsumeFilterContext<TestOrderEvent> context,
            Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract.ConsumeFilterDelegate<TestOrderEvent> next,
            CancellationToken cancellationToken = default)
        {
            return next(context, cancellationToken);
        }
    }
}
