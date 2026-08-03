using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Application.RabbitMQ.Test.Support.Consumers;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.RabbitMQ;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Consumers;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Deduplication;
using Mvp24Hours.Infrastructure.RabbitMQ.HealthChecks;
using Mvp24Hours.Infrastructure.RabbitMQ.Logging;
using Mvp24Hours.Infrastructure.RabbitMQ.Metrics;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga.Persistence;
using Mvp24Hours.Infrastructure.RabbitMQ.Scheduling;
using Mvp24Hours.Infrastructure.RabbitMQ.Serialization;

namespace Mvp24Hours.Application.RabbitMQ.Test.Extensions;

[Trait("Category", "Unit")]
public class RabbitMQServiceExtensionsTest
{
    [Fact]
    public void AddMvpRabbitMQ_WithNullConfigure_ShouldThrowArgumentNullException()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddMvpRabbitMQ((Action<Infrastructure.RabbitMQ.Configuration.Fluent.RabbitMQConfigurationBuilder>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddMvpRabbitMQ_ShouldRegisterIMvpRabbitMQClientAndConnection()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvpRabbitMQ(cfg => cfg.Host("localhost", 5672, h =>
            {
                h.Username("guest");
                h.Password("guest");
            }));

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IMvpRabbitMQClient>().Should().NotBeNull();
        provider.GetRequiredService<IMvpRabbitMQConnection>().Should().NotBeNull();
        provider.GetRequiredService<IMessageSerializer>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvpRabbitMQ_WithConnectionStringOverload_ShouldConfigureHost()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvpRabbitMQ("amqp://guest:guest@localhost:5672");

        ServiceProvider provider = services.BuildServiceProvider();
        RabbitMQConnectionOptions options = provider.GetRequiredService<IOptions<RabbitMQConnectionOptions>>().Value;

        options.ConnectionString.Should().Be("amqp://guest:guest@localhost:5672");
        provider.GetRequiredService<IMvpRabbitMQClient>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvpRabbitMQ_WithSagaRedis_ShouldRegisterISagaRepository()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDistributedMemoryCache();

        services.AddMvpRabbitMQ(cfg =>
        {
            cfg.Host("localhost", 5672);
            cfg.AddSaga<TestOrderSagaStateMachine, TestOrderSagaData>(s => s.UseRedis());
        });

        ServiceProvider provider = services.BuildServiceProvider();

        ISagaRepository<TestOrderSagaData> repository = provider.GetRequiredService<ISagaRepository<TestOrderSagaData>>();
        repository.Should().BeOfType<RedisSagaRepository<TestOrderSagaData>>();
    }

    [Fact]
    public void AddMvpRabbitMQ_WithSagaInMemory_ShouldRegisterInMemoryRepository()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvpRabbitMQ(cfg =>
        {
            cfg.Host("localhost", 5672);
            cfg.AddSaga<TestOrderSagaStateMachine, TestOrderSagaData>(s => s.UseInMemory());
        });

        ServiceProvider provider = services.BuildServiceProvider();

        ISagaRepository<TestOrderSagaData> repository = provider.GetRequiredService<ISagaRepository<TestOrderSagaData>>();
        repository.Should().BeOfType<InMemorySagaRepository<TestOrderSagaData>>();
    }

    [Fact]
    public void AddMvp24HoursRabbitMQHealthCheck_ShouldRegisterHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpRabbitMQ("amqp://guest:guest@localhost:5672");
        services.AddMvp24HoursRabbitMQHealthCheck("rabbitmq-test", ["messaging"]);

        ServiceProvider provider = services.BuildServiceProvider();
        HealthCheckService healthCheckService = provider.GetRequiredService<HealthCheckService>();

        healthCheckService.Should().NotBeNull();
    }

    [Fact]
    public async Task AddMvp24HoursRabbitMQHealthCheck_ShouldIncludeRabbitMQEntry()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpRabbitMQ("amqp://guest:guest@localhost:5672");
        services.AddMvp24HoursRabbitMQHealthCheck();

        ServiceProvider provider = services.BuildServiceProvider();
        HealthCheckService healthCheckService = provider.GetRequiredService<HealthCheckService>();
        HealthReport report = await healthCheckService.CheckHealthAsync();

        report.Entries.Should().ContainKey("rabbitmq");
    }

    [Fact]
    public void AddMvpRabbitMQ_WithHostPortOverload_ShouldConfigureHost()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvpRabbitMQ("localhost", 5672, cfg => cfg.AddConsumer<CustomerConsumer>());

        ServiceProvider provider = services.BuildServiceProvider();
        RabbitMQConnectionOptions options = provider.GetRequiredService<IOptions<RabbitMQConnectionOptions>>().Value;

        options.Configuration!.HostName.Should().Be("localhost");
        options.Configuration.Port.Should().Be(5672);
    }

    [Fact]
    public void AddMvp24HoursRabbitMQ_WithAssembly_ShouldRegisterClient()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvp24HoursRabbitMQ(
            typeof(CustomerConsumer).Assembly,
            opt => opt.Configuration = new RabbitMQConnection { HostName = "localhost", Port = 5672 },
            opt => opt.Exchange = "test-exchange");

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<MvpRabbitMQClient>().Should().NotBeNull();
        provider.GetRequiredService<IMvpRabbitMQConnection>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursRabbitMQ_WithConsumerTypes_ShouldRegisterClient()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursRabbitMQ(
            [typeof(CustomerConsumer)],
            opt => opt.Configuration = new RabbitMQConnection { HostName = "localhost", Port = 5672 });

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<MvpRabbitMQClient>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursRabbitMQ_WithNullFactory_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddMvp24HoursRabbitMQ<MvpRabbitMQClient>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddMvp24HoursRabbitMQMetrics_ShouldRegisterMetrics()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursRabbitMQMetrics();

        ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<IRabbitMQMetrics>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursRabbitMQStructuredLogging_ShouldRegisterLogger()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvp24HoursRabbitMQStructuredLogging();

        ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<IRabbitMQStructuredLogger>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursRabbitMQDeduplication_ShouldRegisterStore()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursRabbitMQDeduplication(expirationMinutes: 30, maxEntries: 1000);

        ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<IMessageDeduplicationStore>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursRabbitMQAdvanced_WithAllFeaturesDisabled_ShouldNotRegisterOptionalServices()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursRabbitMQAdvanced(opt =>
        {
            opt.EnableMetrics = false;
            opt.EnableStructuredLogging = false;
            opt.EnableDeduplication = false;
            opt.EnableHealthCheck = false;
            opt.EnableScheduler = false;
        });

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<IRabbitMQMetrics>().Should().BeNull();
        provider.GetService<IMessageDeduplicationStore>().Should().BeNull();
    }

    [Fact]
    public void AddMvp24HoursRabbitMQAdvanced_WithSchedulerEnabled_ShouldRegisterScheduler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMvpRabbitMQClient>(_ => Mock.Of<IMvpRabbitMQClient>());

        services.AddMvp24HoursRabbitMQAdvanced(opt =>
        {
            opt.EnableMetrics = false;
            opt.EnableStructuredLogging = false;
            opt.EnableHealthCheck = false;
            opt.EnableScheduler = true;
        });

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IMessageScheduler>().Should().NotBeNull();
        provider.GetServices<IHostedService>().Should().Contain(s => s is ScheduledMessageBackgroundService);
    }

    [Fact]
    public void AddMvp24HoursHostedService_ShouldRegisterHostedService()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursHostedService(opt =>
        {
            opt.Callback = _ => { };
            opt.Period = TimeSpan.FromSeconds(5);
        });

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetServices<IHostedService>().Should().ContainSingle(s => s is MvpRabbitMQHostedService);
    }

    [Fact]
    public void AddMvp24HoursRabbitMQScheduler_ShouldRegisterInMemoryScheduler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMvpRabbitMQClient>(_ => Mock.Of<IMvpRabbitMQClient>());

        services.AddMvp24HoursRabbitMQScheduler(opt => opt.PollingInterval = TimeSpan.FromSeconds(5));

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IScheduledMessageStore>().Should().NotBeNull();
        provider.GetRequiredService<MessageScheduler>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursRabbitMQSchedulerWithRedis_ShouldRegisterRedisStore()
    {
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();

        services.AddMvp24HoursRabbitMQSchedulerWithRedis(keyPrefix: "test:scheduled:");

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IScheduledMessageStore>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursRabbitMQBatchConsumer_ShouldRegisterBatchProcessor()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvp24HoursRabbitMQBatchConsumer<TestOrderBatchConsumer, TestOrderEvent>(opt => opt.MaxBatchSize = 10);

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IBatchConsumer<TestOrderEvent>>().Should().NotBeNull();
        provider.GetRequiredService<BatchConsumerProcessor<TestOrderEvent>>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursRabbitMQBatchConsumerWithDefinition_ShouldApplyDefinitionOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvp24HoursRabbitMQBatchConsumer<
            TestOrderBatchConsumer,
            TestOrderBatchConsumerDefinition,
            TestOrderEvent>();

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IBatchConsumerDefinition<TestOrderBatchConsumer>>().Should().NotBeNull();
        provider.GetRequiredService<BatchConsumerProcessor<TestOrderEvent>>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursRabbitMQBatchConsumersFromAssembly_ShouldDiscoverConsumers()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvp24HoursRabbitMQBatchConsumersFromAssembly(typeof(TestOrderBatchConsumer).Assembly);

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<IBatchConsumer<TestOrderEvent>>().Should().NotBeNull();
        provider.GetService<IBatchConsumerDefinition<TestOrderBatchConsumer>>().Should().NotBeNull();
    }

    [Fact]
    public void RabbitMQAdvancedOptions_ShouldHaveExpectedDefaults()
    {
        var options = new RabbitMQAdvancedOptions();

        options.EnableMetrics.Should().BeTrue();
        options.EnableStructuredLogging.Should().BeTrue();
        options.EnableDeduplication.Should().BeFalse();
        options.EnableHealthCheck.Should().BeTrue();
        options.EnableScheduler.Should().BeFalse();
        options.HealthCheckName.Should().Be("rabbitmq");
    }

    [Fact]
    public void AddMvp24HoursRabbitMQAdvanced_WithAllFeaturesEnabled_ShouldRegisterOptionalServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMvpRabbitMQClient>(_ => Mock.Of<IMvpRabbitMQClient>());

        services.AddMvp24HoursRabbitMQAdvanced(opt =>
        {
            opt.EnableMetrics = true;
            opt.EnableStructuredLogging = true;
            opt.EnableDeduplication = true;
            opt.EnableHealthCheck = true;
            opt.EnableScheduler = true;
            opt.DeduplicationExpirationMinutes = 15;
            opt.DeduplicationMaxEntries = 500;
            opt.HealthCheckTags = ["messaging"];
        });

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IRabbitMQMetrics>().Should().NotBeNull();
        provider.GetRequiredService<IRabbitMQStructuredLogger>().Should().NotBeNull();
        provider.GetRequiredService<IMessageDeduplicationStore>().Should().NotBeNull();
        provider.GetRequiredService<IMessageScheduler>().Should().NotBeNull();
        provider.GetServices<IHostedService>().Should().Contain(s => s is ScheduledMessageBackgroundService);
    }

    [Fact]
    public void AddMvp24HoursRabbitMQDeduplicationGeneric_ShouldRegisterCustomStore()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursRabbitMQDeduplication<InMemoryMessageDeduplicationStore>();

        ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<IMessageDeduplicationStore>().Should().BeOfType<InMemoryMessageDeduplicationStore>();
    }

    [Fact]
    public void AddMvp24HoursRabbitMQSchedulerGeneric_ShouldRegisterCustomStore()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMvpRabbitMQClient>(_ => Mock.Of<IMvpRabbitMQClient>());

        services.AddMvp24HoursRabbitMQScheduler<InMemoryScheduledMessageStore>(opt => opt.PollingInterval = TimeSpan.FromSeconds(2));

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IScheduledMessageStore>().Should().BeOfType<InMemoryScheduledMessageStore>();
        provider.GetRequiredService<IMessageScheduler>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursRabbitMQ_WithEmptyConsumerList_ShouldRegisterClientWithoutConsumers()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursRabbitMQ(
            [],
            opt => opt.Configuration = new RabbitMQConnection { HostName = "localhost", Port = 5672 });

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<MvpRabbitMQClient>().Should().NotBeNull();
    }

    private sealed class TestOrderSagaStateMachine : SagaStateMachine<TestOrderSagaData>
    {
        public TestOrderSagaStateMachine()
        {
            Initially(
                When<TestOrderCreatedEvent>()
                    .TransitionTo("Active"));
        }
    }
}
