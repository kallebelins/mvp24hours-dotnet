using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Application.RabbitMQ.Test.Support.Consumers;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing.Extensions;

namespace Mvp24Hours.Application.RabbitMQ.Test.Testing;

[Trait("Category", "Unit")]
public class TestingServiceExtensionsCoverageTest
{
    [Fact]
    public void AddInMemoryRabbitMQ_ShouldRegisterBusAndClient()
    {
        var services = new ServiceCollection();

        services.AddInMemoryRabbitMQ();

        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<IInMemoryBus>().Should().NotBeNull();
        provider.GetRequiredService<IMvpRabbitMQClient>().Should().BeOfType<InMemoryBus>();
    }

    [Fact]
    public void AddRabbitMQTestHarness_ShouldRegisterHarness()
    {
        var services = new ServiceCollection();

        services.AddRabbitMQTestHarness();

        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<ITestHarness>().Should().NotBeNull();
    }

    [Fact]
    public void AddRabbitMQTestHarness_WithOptions_ShouldAutoRegisterConsumers()
    {
        var services = new ServiceCollection();

        services.AddRabbitMQTestHarness(options =>
            options.AddConsumersFromAssemblyContaining<CustomerConsumer>());

        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<ITestHarness>().Should().NotBeNull();
    }

    [Fact]
    public void ReplaceRabbitMQWithInMemory_ShouldReplaceExistingClient()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMvpRabbitMQClient>(_ => RabbitMQTestHelpers.CreateInMemoryBus());

        services.ReplaceRabbitMQWithInMemory();

        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<IMvpRabbitMQClient>().Should().BeOfType<InMemoryBus>();
    }

    [Fact]
    public void AddTestConsumer_ShouldRegisterConsumerInterfaces()
    {
        var services = new ServiceCollection();

        services.AddTestConsumer<RecordingMessageConsumer>();

        services.Should().Contain(d => d.ServiceType == typeof(RecordingMessageConsumer));
    }

    [Fact]
    public void AddTestRequestHandler_ShouldRegisterHandlerInterfaces()
    {
        var services = new ServiceCollection();

        services.AddTestRequestHandler<TestOrderRequestHandler>();

        services.Should().Contain(d => d.ServiceType == typeof(TestOrderRequestHandler));
    }

    [Fact]
    public void TestHarnessOptions_AddConsumersFromAssemblyContaining_ShouldEnableAutoRegistration()
    {
        var options = new TestHarnessOptions();

        options.AddConsumersFromAssemblyContaining<CustomerConsumer>();

        options.AutoRegisterConsumers.Should().BeTrue();
        options.ConsumerAssemblies.Should().Contain(typeof(CustomerConsumer).Assembly);
    }

    private sealed class RecordingMessageConsumer : IMessageConsumer<TestOrderEvent>
    {
        public Task ConsumeAsync(IConsumeContext<TestOrderEvent> context, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TestOrderRequestHandler : IRequestHandler<TestOrderRequest, TestOrderResponse>
    {
        public Task<TestOrderResponse> HandleAsync(IConsumeContext<TestOrderRequest> context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new TestOrderResponse { Success = true });
        }
    }

    private sealed class TestOrderRequest
    {
        public string Id { get; set; } = string.Empty;
    }
}
