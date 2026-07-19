using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy;
using Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Filters;

namespace Mvp24Hours.Application.RabbitMQ.Test.MultiTenancy;

public class MultiTenancyTest
{
    [Fact]
    public async Task InMemoryTenantRabbitMQResolver_ResolveAsync_ShouldReturnDefaultConfiguration()
    {
        var resolver = new InMemoryTenantRabbitMQResolver(Options.Create(new TenantRabbitMQOptions()));

        TenantRabbitMQConfiguration? config = await resolver.ResolveAsync("tenant-a");

        config.Should().NotBeNull();
        config!.TenantId.Should().Be("tenant-a");
        config.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task InMemoryTenantRabbitMQResolver_AddOrUpdate_ShouldOverrideStaticConfig()
    {
        var resolver = new InMemoryTenantRabbitMQResolver(Options.Create(new TenantRabbitMQOptions()));
        resolver.AddOrUpdate(new TenantRabbitMQConfiguration
        {
            TenantId = "tenant-b",
            VirtualHost = "/custom",
            QueuePrefix = "custom-"
        });

        TenantRabbitMQConfiguration? config = await resolver.ResolveAsync("tenant-b");

        config!.VirtualHost.Should().Be("/custom");
        config.QueuePrefix.Should().Be("custom-");
    }

    [Fact]
    public async Task InMemoryTenantRabbitMQResolver_WithEmptyTenantId_ShouldReturnNull()
    {
        var resolver = new InMemoryTenantRabbitMQResolver(Options.Create(new TenantRabbitMQOptions()));

        TenantRabbitMQConfiguration? config = await resolver.ResolveAsync(string.Empty);

        config.Should().BeNull();
    }

    [Fact]
    public void TenantRabbitMQOptions_GetVirtualHost_ShouldApplyTemplate()
    {
        var options = new TenantRabbitMQOptions
        {
            VirtualHostTemplate = "/tenants/{tenantId}"
        };

        options.GetVirtualHost("acme").Should().Be("/tenants/acme");
        options.GetQueuePrefix("acme").Should().Contain("acme");
    }

    [Fact]
    public async Task TenantConsumeFilter_WithoutTenantAndRejectEnabled_ShouldSendToDeadLetter()
    {
        var options = Options.Create(new TenantRabbitMQOptions { RejectMessagesWithoutTenant = true });
        var filter = new TenantConsumeFilter(options);
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(new TestOrderEvent());
        bool nextCalled = false;

        await filter.ConsumeAsync(context, async (_, _) =>
        {
            nextCalled = true;
            await Task.CompletedTask;
        });

        nextCalled.Should().BeFalse();
        context.ShouldSendToDeadLetter.Should().BeTrue();
    }

    [Fact]
    public async Task TenantConsumeFilter_WithTenantHeader_ShouldSetCurrentContext()
    {
        var options = Options.Create(new TenantRabbitMQOptions { RejectMessagesWithoutTenant = false });
        var filter = new TenantConsumeFilter(options);
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(
            new TestOrderEvent(),
            b => b.ForTenant("tenant-42"));

        await filter.ConsumeAsync(context, async (_, _) =>
        {
            TenantConsumeFilter.Current!.TenantId.Should().Be("tenant-42");
            await Task.CompletedTask;
        });
    }

    [Fact]
    public async Task TenantPublishFilter_WithAutoPropagateDisabled_ShouldNotAddHeaders()
    {
        var services = new ServiceCollection();
        IServiceProvider provider = services.BuildServiceProvider();
        var options = Options.Create(new TenantRabbitMQOptions { AutoPropagateTenantHeaders = false });
        var filter = new TenantPublishFilter(options, provider);
        PublishFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreatePublishFilterContext(new TestOrderEvent(), serviceProvider: provider);

        await filter.PublishAsync(context, async (_, _) => await Task.CompletedTask);

        context.Headers.Should().NotContainKey(options.Value.TenantIdHeader);
    }
}
