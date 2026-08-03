using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Filters;

namespace Mvp24Hours.Application.RabbitMQ.Test.Pipeline;

[Trait("Category", "Unit")]
public class FilterPipelineExtensionsCoverageTest
{
    [Fact]
    public void AddMvp24HoursRabbitMQFilters_ShouldRegisterOptionsAndExecutor()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursRabbitMQFilters(options =>
        {
            options.EnableLoggingFilter = true;
            options.EnableCorrelationFilter = true;
        });

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<FilterPipelineOptions>().EnableLoggingFilter.Should().BeTrue();
        provider.GetRequiredService<IFilterPipelineExecutor>().Should().NotBeNull();
        provider.GetServices<IConsumeFilter>().Should().ContainSingle(f => f is CorrelationConsumeFilter);
    }

    [Fact]
    public void AddMvp24HoursRabbitMQFiltersWithDefaults_ShouldEnableAllDefaultFilters()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursRabbitMQFiltersWithDefaults();

        ServiceProvider provider = services.BuildServiceProvider();
        FilterPipelineOptions options = provider.GetRequiredService<FilterPipelineOptions>();

        options.EnableLoggingFilter.Should().BeTrue();
        options.EnableExceptionHandlingFilter.Should().BeTrue();
        options.EnableCorrelationFilter.Should().BeTrue();
        options.EnableTelemetryFilter.Should().BeTrue();
        options.EnableValidationFilter.Should().BeTrue();
    }

    [Fact]
    public void AddConsumeFilter_ShouldRegisterTypedAndUntypedDescriptors()
    {
        var services = new ServiceCollection();
        services.AddConsumeFilter<LoggingConsumeFilter>();

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetServices<IConsumeFilter>().Should().ContainSingle(f => f is LoggingConsumeFilter);
        provider.GetRequiredService<LoggingConsumeFilter>().Should().NotBeNull();
    }

    [Fact]
    public void AddConsumeFilter_Typed_ShouldRegisterMessageSpecificFilter()
    {
        var services = new ServiceCollection();
        services.AddConsumeFilter<StubTypedConsumeFilter, TestOrderEvent>();

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IConsumeFilter<TestOrderEvent>>().Should().BeOfType<StubTypedConsumeFilter>();
    }

    [Fact]
    public void AddPublishFilter_ShouldRegisterFilters()
    {
        var services = new ServiceCollection();
        services.AddPublishFilter<LoggingPublishFilter>();
        services.AddPublishFilter<StubTypedPublishFilter, TestOrderEvent>();

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetServices<IPublishFilter>().Should().ContainSingle(f => f is LoggingPublishFilter);
        provider.GetRequiredService<IPublishFilter<TestOrderEvent>>().Should().BeOfType<StubTypedPublishFilter>();
    }

    [Fact]
    public void AddSendFilter_ShouldRegisterFilters()
    {
        var services = new ServiceCollection();
        services.AddSendFilter<LoggingSendFilter>();
        services.AddSendFilter<StubTypedSendFilter, TestOrderEvent>();

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetServices<ISendFilter>().Should().ContainSingle(f => f is LoggingSendFilter);
        provider.GetRequiredService<ISendFilter<TestOrderEvent>>().Should().BeOfType<StubTypedSendFilter>();
    }

    [Fact]
    public void AddRabbitMQLoggingFilters_ShouldRegisterAllLoggingFilters()
    {
        var services = new ServiceCollection();
        services.AddRabbitMQLoggingFilters();

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetServices<IConsumeFilter>().Should().ContainSingle(f => f is LoggingConsumeFilter);
        provider.GetServices<IPublishFilter>().Should().ContainSingle(f => f is LoggingPublishFilter);
        provider.GetServices<ISendFilter>().Should().ContainSingle(f => f is LoggingSendFilter);
    }

    [Fact]
    public void AddRabbitMQExceptionHandlingFilter_WithConfigure_ShouldRegisterFilter()
    {
        var services = new ServiceCollection();
        services.AddRabbitMQExceptionHandlingFilter(options => options.MaxRetries = 5);

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetServices<IConsumeFilter>().Should().ContainSingle(f => f is ExceptionHandlingConsumeFilter);
    }

    [Fact]
    public void AddRabbitMQCorrelationFilters_ShouldRegisterAllCorrelationFilters()
    {
        var services = new ServiceCollection();
        services.AddRabbitMQCorrelationFilters();

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetServices<IConsumeFilter>().Should().ContainSingle(f => f is CorrelationConsumeFilter);
        provider.GetServices<IPublishFilter>().Should().ContainSingle(f => f is CorrelationPublishFilter);
        provider.GetServices<ISendFilter>().Should().ContainSingle(f => f is CorrelationSendFilter);
    }

    [Fact]
    public void AddRabbitMQTelemetryFilters_ShouldRegisterAllTelemetryFilters()
    {
        var services = new ServiceCollection();
        services.AddRabbitMQTelemetryFilters();

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetServices<IConsumeFilter>().Should().ContainSingle(f => f is TelemetryConsumeFilter);
        provider.GetServices<IPublishFilter>().Should().ContainSingle(f => f is TelemetryPublishFilter);
        provider.GetServices<ISendFilter>().Should().ContainSingle(f => f is TelemetrySendFilter);
    }

    [Fact]
    public void AddRabbitMQValidationFilters_WithConfigure_ShouldRegisterValidationFilters()
    {
        var services = new ServiceCollection();
        services.AddRabbitMQValidationFilters(options => options.ThrowOnValidationFailure = true);

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetServices<IConsumeFilter>().Should().ContainSingle(f => f is ValidationConsumeFilter);
        provider.GetServices<IPublishFilter>().Should().ContainSingle(f => f is ValidationPublishFilter);
    }

    private sealed class StubTypedConsumeFilter : IConsumeFilter<TestOrderEvent>
    {
        public Task ConsumeAsync(IConsumeFilterContext<TestOrderEvent> context, ConsumeFilterDelegate<TestOrderEvent> next, CancellationToken cancellationToken = default)
        {
            return next(context, cancellationToken);
        }
    }

    private sealed class StubTypedPublishFilter : IPublishFilter<TestOrderEvent>
    {
        public Task PublishAsync(IPublishFilterContext<TestOrderEvent> context, PublishFilterDelegate<TestOrderEvent> next, CancellationToken cancellationToken = default)
        {
            return next(context, cancellationToken);
        }
    }

    private sealed class StubTypedSendFilter : ISendFilter<TestOrderEvent>
    {
        public Task SendAsync(ISendFilterContext<TestOrderEvent> context, SendFilterDelegate<TestOrderEvent> next, CancellationToken cancellationToken = default)
        {
            return next(context, cancellationToken);
        }
    }
}
