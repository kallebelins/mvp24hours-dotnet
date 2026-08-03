using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing;
using Mvp24Hours.Infrastructure.RabbitMQ.Transactional;
using Mvp24Hours.Infrastructure.RabbitMQ.Transactional.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Transactional.Extensions;

namespace Mvp24Hours.Application.RabbitMQ.Test.Transactional;

[Trait("Category", "Unit")]
public class TransactionalExtensionsCoverageTest
{
    [Fact]
    public void AddTransactionalMessaging_ShouldRegisterCoreServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMvpRabbitMQClient>(RabbitMQTestHelpers.CreateInMemoryBus());
        services.AddTransactionalMessaging();

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ITransactionalOutbox>().Should().BeOfType<InMemoryTransactionalOutbox>();
        provider.GetRequiredService<ITransactionalBus>().Should().BeOfType<TransactionalBus>();
        provider.GetRequiredService<ITransactionalConsumeContextFactory>().Should().BeOfType<TransactionalConsumeContextFactory>();
        provider.GetServices<IHostedService>().OfType<OutboxPublisher>().Should().ContainSingle();
    }

    [Fact]
    public void AddTransactionalMessaging_WithCustomOutboxType_ShouldRegisterOutbox()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTransactionalMessaging<CustomTransactionalOutbox>();

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ITransactionalOutbox>().Should().BeOfType<CustomTransactionalOutbox>();
    }

    [Fact]
    public void AddTransactionalMessaging_WithFactory_ShouldRegisterOutbox()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTransactionalMessaging(_ => new CustomTransactionalOutbox());

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ITransactionalOutbox>().Should().BeOfType<CustomTransactionalOutbox>();
    }

    [Fact]
    public void AddInMemoryOutbox_WithConfigure_ShouldApplyOptions()
    {
        var services = new ServiceCollection();
        services.AddInMemoryOutbox(o => o.MaxRetryCount = 7);

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<InMemoryTransactionalOutboxOptions>().MaxRetryCount.Should().Be(7);
        provider.GetRequiredService<ITransactionalOutbox>().Should().BeOfType<InMemoryTransactionalOutbox>();
    }

    [Fact]
    public void ConfigureOutboxPublisher_ShouldRegisterOptions()
    {
        var services = new ServiceCollection();
        services.ConfigureOutboxPublisher(o => o.BatchSize = 25);

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<OutboxPublisherOptions>().BatchSize.Should().Be(25);
    }

    [Fact]
    public void TransactionalConsumeContextFactory_Create_ShouldWrapContext()
    {
        var outbox = new InMemoryTransactionalOutbox();
        var bus = new TransactionalBus(outbox, RabbitMQTestHelpers.CreateNullLogger<TransactionalBus>());
        var factory = new TransactionalConsumeContextFactory(bus);
        TestConsumeContext<TestOrderEvent> inner = RabbitMQTestHelpers.CreateTestConsumeContext(new TestOrderEvent());

        ITransactionalConsumeContext<TestOrderEvent> context = factory.Create(inner);

        context.Message.Should().BeSameAs(inner.Message);
        context.TransactionalBus.Should().BeSameAs(bus);
    }

    private sealed class CustomTransactionalOutbox : ITransactionalOutbox
    {
        public Task AddAsync(TransactionalOutboxMessage message, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IEnumerable<TransactionalOutboxMessage> messages, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<TransactionalOutboxMessage>> GetPendingAsync(int batchSize = 100, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TransactionalOutboxMessage>>([]);
        }

        public Task MarkAsPublishedAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }

        public Task<IReadOnlyList<TransactionalOutboxMessage>> GetDeadLettersAsync(int batchSize = 100, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TransactionalOutboxMessage>>([]);
        }

        public Task<int> CleanupAsync(DateTime olderThan, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }
    }
}
