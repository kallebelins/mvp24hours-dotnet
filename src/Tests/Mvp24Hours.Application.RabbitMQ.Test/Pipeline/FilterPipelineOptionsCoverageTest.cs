using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Filters;

namespace Mvp24Hours.Application.RabbitMQ.Test.Pipeline;

[Trait("Category", "Unit")]
public class FilterPipelineOptionsCoverageTest
{
    [Fact]
    public void UseConsumeFilter_Generic_ShouldRegisterOnce()
    {
        FilterPipelineOptions options = new FilterPipelineOptions()
            .UseConsumeFilter<LoggingConsumeFilter>()
            .UseConsumeFilter<LoggingConsumeFilter>();

        options.ConsumeFilters.Should().ContainSingle(t => t == typeof(LoggingConsumeFilter));
    }

    [Fact]
    public void UseConsumeFilter_Typed_ShouldRegisterFilter()
    {
        FilterPipelineOptions options = new FilterPipelineOptions()
            .UseConsumeFilter<SequenceConsumeFilter, TestOrderEvent>();

        options.ConsumeFilters.Should().Contain(typeof(SequenceConsumeFilter));
    }

    [Fact]
    public void UseConsumeFilter_ByType_WithNull_ShouldThrow()
    {
        var options = new FilterPipelineOptions();

        Action act = () => options.UseConsumeFilter(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UseConsumeFilter_ByType_WithInvalidType_ShouldThrow()
    {
        var options = new FilterPipelineOptions();

        Action act = () => options.UseConsumeFilter(typeof(string));

        act.Should().Throw<ArgumentException>()
            .WithParameterName("filterType");
    }

    [Fact]
    public void UseConsumeFilter_ByType_WithGenericFilter_ShouldRegister()
    {
        FilterPipelineOptions options = new FilterPipelineOptions()
            .UseConsumeFilter(typeof(SequenceConsumeFilter));

        options.ConsumeFilters.Should().Contain(typeof(SequenceConsumeFilter));
    }

    [Fact]
    public void UsePublishFilter_Generic_ShouldRegisterOnce()
    {
        FilterPipelineOptions options = new FilterPipelineOptions()
            .UsePublishFilter<LoggingPublishFilter>()
            .UsePublishFilter<LoggingPublishFilter>();

        options.PublishFilters.Should().ContainSingle(t => t == typeof(LoggingPublishFilter));
    }

    [Fact]
    public void UsePublishFilter_Typed_ShouldRegisterFilter()
    {
        FilterPipelineOptions options = new FilterPipelineOptions()
            .UsePublishFilter<SequencePublishFilter, TestOrderEvent>();

        options.PublishFilters.Should().Contain(typeof(SequencePublishFilter));
    }

    [Fact]
    public void UsePublishFilter_ByType_WithNull_ShouldThrow()
    {
        var options = new FilterPipelineOptions();

        Action act = () => options.UsePublishFilter(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UsePublishFilter_ByType_WithInvalidType_ShouldThrow()
    {
        var options = new FilterPipelineOptions();

        Action act = () => options.UsePublishFilter(typeof(int));

        act.Should().Throw<ArgumentException>()
            .WithParameterName("filterType");
    }

    [Fact]
    public void UseSendFilter_Generic_ShouldRegisterOnce()
    {
        FilterPipelineOptions options = new FilterPipelineOptions()
            .UseSendFilter<LoggingSendFilter>()
            .UseSendFilter<LoggingSendFilter>();

        options.SendFilters.Should().ContainSingle(t => t == typeof(LoggingSendFilter));
    }

    [Fact]
    public void UseSendFilter_Typed_ShouldRegisterFilter()
    {
        FilterPipelineOptions options = new FilterPipelineOptions()
            .UseSendFilter<SequenceSendFilter, TestOrderEvent>();

        options.SendFilters.Should().Contain(typeof(SequenceSendFilter));
    }

    [Fact]
    public void UseSendFilter_ByType_WithNull_ShouldThrow()
    {
        var options = new FilterPipelineOptions();

        Action act = () => options.UseSendFilter(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UseSendFilter_ByType_WithInvalidType_ShouldThrow()
    {
        var options = new FilterPipelineOptions();

        Action act = () => options.UseSendFilter(typeof(DateTime));

        act.Should().Throw<ArgumentException>()
            .WithParameterName("filterType");
    }

    [Fact]
    public void UseFilter_WithMultiInterfaceFilter_ShouldRegisterInAllPipelines()
    {
        FilterPipelineOptions options = new FilterPipelineOptions()
            .UseFilter<MultiPipelineFilter>();

        options.ConsumeFilters.Should().Contain(typeof(MultiPipelineFilter));
        options.PublishFilters.Should().Contain(typeof(MultiPipelineFilter));
        options.SendFilters.Should().Contain(typeof(MultiPipelineFilter));
    }

    [Fact]
    public void RemoveFilters_ShouldRemoveRegisteredTypes()
    {
        FilterPipelineOptions options = new FilterPipelineOptions()
            .UseConsumeFilter<LoggingConsumeFilter>()
            .UsePublishFilter<LoggingPublishFilter>()
            .UseSendFilter<LoggingSendFilter>()
            .RemoveConsumeFilter<LoggingConsumeFilter>()
            .RemovePublishFilter<LoggingPublishFilter>()
            .RemoveSendFilter<LoggingSendFilter>();

        options.ConsumeFilters.Should().BeEmpty();
        options.PublishFilters.Should().BeEmpty();
        options.SendFilters.Should().BeEmpty();
    }

    [Fact]
    public void ClearFilters_ShouldRemoveAllFilters()
    {
        FilterPipelineOptions options = new FilterPipelineOptions()
            .UseConsumeFilter<LoggingConsumeFilter>()
            .UsePublishFilter<LoggingPublishFilter>()
            .UseSendFilter<LoggingSendFilter>()
            .ClearFilters();

        options.ConsumeFilters.Should().BeEmpty();
        options.PublishFilters.Should().BeEmpty();
        options.SendFilters.Should().BeEmpty();
    }

    [Fact]
    public void DefaultFlags_ShouldBeDisabled()
    {
        var options = new FilterPipelineOptions();

        options.EnableLoggingFilter.Should().BeFalse();
        options.EnableExceptionHandlingFilter.Should().BeFalse();
        options.EnableCorrelationFilter.Should().BeFalse();
        options.EnableTelemetryFilter.Should().BeFalse();
        options.EnableValidationFilter.Should().BeFalse();
    }

    private sealed class SequenceConsumeFilter : IConsumeFilter<TestOrderEvent>
    {
        public Task ConsumeAsync(IConsumeFilterContext<TestOrderEvent> context, ConsumeFilterDelegate<TestOrderEvent> next, CancellationToken cancellationToken = default)
        {
            return next(context, cancellationToken);
        }
    }

    private sealed class SequencePublishFilter : IPublishFilter<TestOrderEvent>
    {
        public Task PublishAsync(IPublishFilterContext<TestOrderEvent> context, PublishFilterDelegate<TestOrderEvent> next, CancellationToken cancellationToken = default)
        {
            return next(context, cancellationToken);
        }
    }

    private sealed class SequenceSendFilter : ISendFilter<TestOrderEvent>
    {
        public Task SendAsync(ISendFilterContext<TestOrderEvent> context, SendFilterDelegate<TestOrderEvent> next, CancellationToken cancellationToken = default)
        {
            return next(context, cancellationToken);
        }
    }

    private sealed class MultiPipelineFilter :
        IConsumeFilter<TestOrderEvent>,
        IPublishFilter<TestOrderEvent>,
        ISendFilter<TestOrderEvent>
    {
        public Task ConsumeAsync(IConsumeFilterContext<TestOrderEvent> context, ConsumeFilterDelegate<TestOrderEvent> next, CancellationToken cancellationToken = default)
        {
            return next(context, cancellationToken);
        }

        public Task PublishAsync(IPublishFilterContext<TestOrderEvent> context, PublishFilterDelegate<TestOrderEvent> next, CancellationToken cancellationToken = default)
        {
            return next(context, cancellationToken);
        }

        public Task SendAsync(ISendFilterContext<TestOrderEvent> context, SendFilterDelegate<TestOrderEvent> next, CancellationToken cancellationToken = default)
        {
            return next(context, cancellationToken);
        }
    }
}
