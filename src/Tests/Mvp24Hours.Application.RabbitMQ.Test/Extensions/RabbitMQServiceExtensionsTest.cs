using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Saga.Persistence;
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
