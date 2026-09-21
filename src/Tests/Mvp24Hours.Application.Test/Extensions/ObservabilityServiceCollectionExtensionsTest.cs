//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Contract.Observability;
using Mvp24Hours.Application.Extensions;
using Mvp24Hours.Application.Logic.Observability;

namespace Mvp24Hours.Application.Test.Extensions;

[Trait("Category", "Unit")]
public class ObservabilityServiceCollectionExtensionsTest
{
    private sealed class CustomAuditStore : IApplicationAuditStore
    {
        public Task SaveAsync(ApplicationAuditEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IList<ApplicationAuditEntry>> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
            => Task.FromResult<IList<ApplicationAuditEntry>>([]);
        public Task<IList<ApplicationAuditEntry>> GetByUserIdAsync(string userId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
            => Task.FromResult<IList<ApplicationAuditEntry>>([]);
        public Task<IList<ApplicationAuditEntry>> GetByEntityAsync(string entityType, string entityId, CancellationToken cancellationToken = default)
            => Task.FromResult<IList<ApplicationAuditEntry>>([]);
    }

    [Fact]
    public void AddMvp24HoursApplicationObservability_Parameterless_ShouldRegisterDefaults()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursApplicationObservability();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICorrelationIdAccessor>().Should().NotBeNull();
        provider.GetRequiredService<IOperationMetrics>().Should().BeOfType<ApplicationOperationMetrics>();
        provider.GetRequiredService<IApplicationAuditStore>().Should().BeOfType<InMemoryApplicationAuditStore>();
    }

    [Fact]
    public void AddMvp24HoursApplicationObservability_WithOptionsInstance_WhenNull_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddMvp24HoursApplicationObservability((ApplicationObservabilityOptions)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void AddMvp24HoursApplicationObservability_WithMetricsDisabled_ShouldRegisterNullMetrics()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursApplicationObservability(options => options.EnableMetrics = false);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IOperationMetrics>().Should().BeSameAs(NullOperationMetrics.Instance);
    }

    [Fact]
    public void AddMvp24HoursApplicationObservability_WithAuditTrailDisabled_ShouldNotRegisterAuditStore()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursApplicationObservability(options => options.EnableAuditTrail = false);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<IApplicationAuditStore>().Should().BeNull();
    }

    [Fact]
    public void AddMvp24HoursApplicationObservability_WithConfigureAction_ShouldApplyOptions()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursApplicationObservability(options =>
        {
            options.EnableMetrics = false;
            options.EnableAuditTrail = false;
        });
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IOperationMetrics>().Should().BeSameAs(NullOperationMetrics.Instance);
        provider.GetService<IApplicationAuditStore>().Should().BeNull();
    }

    [Fact]
    public void AddCorrelationId_ShouldRegisterAllCorrelationInterfacesToSameInstance()
    {
        var services = new ServiceCollection();

        services.AddCorrelationId();
        ServiceProvider provider = services.BuildServiceProvider();

        var context = provider.GetRequiredService<ICorrelationIdContext>();
        var accessor = provider.GetRequiredService<ICorrelationIdAccessor>();
        var setter = provider.GetRequiredService<ICorrelationIdSetter>();

        context.Should().BeSameAs(accessor);
        context.Should().BeSameAs(setter);
    }

    [Fact]
    public void AddApplicationMetrics_WithoutOptions_ShouldRegisterDefaultOptions()
    {
        var services = new ServiceCollection();

        services.AddApplicationMetrics();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IOperationMetrics>().Should().BeOfType<ApplicationOperationMetrics>();
        provider.GetRequiredService<OperationMetricsOptions>().Should().NotBeNull();
    }

    [Fact]
    public void AddApplicationMetrics_WithOptions_ShouldRegisterProvidedOptions()
    {
        var services = new ServiceCollection();
        var options = new OperationMetricsOptions { Enabled = false };

        services.AddApplicationMetrics(options);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<OperationMetricsOptions>().Should().BeSameAs(options);
    }

    [Fact]
    public void AddInMemoryAuditStore_WithDefaults_ShouldRegisterInMemoryStore()
    {
        var services = new ServiceCollection();

        services.AddInMemoryAuditStore();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IApplicationAuditStore>().Should().BeOfType<InMemoryApplicationAuditStore>();
    }

    [Fact]
    public void AddAuditStore_Generic_ShouldRegisterCustomImplementation()
    {
        var services = new ServiceCollection();

        services.AddAuditStore<CustomAuditStore>();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IApplicationAuditStore>().Should().BeOfType<CustomAuditStore>();
    }

    [Fact]
    public void AddAuditStore_WithFactory_ShouldRegisterCustomImplementation()
    {
        var services = new ServiceCollection();
        var instance = new CustomAuditStore();

        services.AddAuditStore(_ => instance);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IApplicationAuditStore>().Should().BeSameAs(instance);
    }
}
