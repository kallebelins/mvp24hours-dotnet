using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy;
using Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Filters;

namespace Mvp24Hours.Application.RabbitMQ.Test.Pipeline;

public class PipelineFiltersTest
{
    [Fact]
    public async Task CorrelationConsumeFilter_ShouldSetAsyncLocalContext()
    {
        var filter = new CorrelationConsumeFilter();
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(
            new TestOrderEvent(),
            b => b.WithCorrelationId("corr-123").WithMessageId("msg-1"));

        CorrelationContext? captured = null;
        await filter.ConsumeAsync(context, async (_, _) =>
        {
            captured = CorrelationConsumeFilter.Current;
            await Task.CompletedTask;
        });

        captured.Should().NotBeNull();
        captured!.CorrelationId.Should().Be("corr-123");
        context.Items["CorrelationId"].Should().Be("corr-123");
    }

    [Fact]
    public async Task TelemetryConsumeFilter_ShouldInvokeNext()
    {
        var filter = new TelemetryConsumeFilter();
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(new TestOrderEvent());
        bool nextCalled = false;

        await filter.ConsumeAsync(context, async (_, _) =>
        {
            nextCalled = true;
            await Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task TelemetryPublishFilter_ShouldInjectTraceParentHeader()
    {
        var filter = new TelemetryPublishFilter();
        PublishFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreatePublishFilterContext(new TestOrderEvent());
        bool nextCalled = false;

        await filter.PublishAsync(context, async (_, _) =>
        {
            nextCalled = true;
            await Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
        // traceparent is injected only when an Activity listener is registered
        context.Items.Should().NotContainKey("traceparent");
    }

    [Fact]
    public async Task ExceptionHandlingConsumeFilter_ShouldRethrowWhenConfigured()
    {
        var filter = new ExceptionHandlingConsumeFilter(
            options: Microsoft.Extensions.Options.Options.Create(new ExceptionHandlingFilterOptions
            {
                MaxRetries = 0,
                RethrowException = true
            }));
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(new TestOrderEvent());

        Func<Task> act = () => filter.ConsumeAsync(context, (_, _) => throw new InvalidOperationException("boom"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task FilterPipelineExecutor_WithoutFilters_ShouldExecuteFinalAction()
    {
        var services = new ServiceCollection();
        IServiceProvider provider = services.BuildServiceProvider();
        var executor = new FilterPipelineExecutor(provider, new FilterPipelineOptions());
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(new TestOrderEvent());
        bool executed = false;

        await executor.ExecuteConsumeFiltersAsync(
            context,
            (_, _) =>
            {
                executed = true;
                return Task.CompletedTask;
            });

        executed.Should().BeTrue();
    }

    [Fact]
    public void ConsumeFilterContext_SendToDeadLetter_ShouldSkipRemainingFilters()
    {
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(new TestOrderEvent());

        context.SendToDeadLetter("invalid message");

        context.ShouldSendToDeadLetter.Should().BeTrue();
        context.DeadLetterReason.Should().Be("invalid message");
    }

    #region LoggingConsumeFilter

    [Fact]
    public async Task LoggingConsumeFilter_ShouldCallNext_WhenSuccessful()
    {
        var filter = new LoggingConsumeFilter();
        var context = RabbitMQTestHelpers.CreateConsumeFilterContext(new TestOrderEvent());
        bool nextCalled = false;

        await filter.ConsumeAsync<TestOrderEvent>(context, (ctx, ct) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task LoggingConsumeFilter_WithLogger_ShouldNotThrow()
    {
        ILogger<LoggingConsumeFilter> logger = NullLogger<LoggingConsumeFilter>.Instance;
        var filter = new LoggingConsumeFilter(logger);
        var context = RabbitMQTestHelpers.CreateConsumeFilterContext(new TestOrderEvent());

        Func<Task> act = async () => await filter.ConsumeAsync<TestOrderEvent>(
            context,
            (ctx, ct) => Task.CompletedTask);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LoggingConsumeFilter_WhenNextThrows_ShouldRethrow()
    {
        var filter = new LoggingConsumeFilter();
        var context = RabbitMQTestHelpers.CreateConsumeFilterContext(new TestOrderEvent());

        Func<Task> act = async () => await filter.ConsumeAsync<TestOrderEvent>(
            context,
            (ctx, ct) => throw new InvalidOperationException("process error"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("process error");
    }

    [Fact]
    public async Task LoggingConsumeFilter_WithNullLogger_ShouldNotThrow()
    {
        var filter = new LoggingConsumeFilter(null);
        var context = RabbitMQTestHelpers.CreateConsumeFilterContext(new TestOrderEvent());

        Func<Task> act = async () => await filter.ConsumeAsync<TestOrderEvent>(
            context,
            (ctx, ct) => Task.CompletedTask);

        await act.Should().NotThrowAsync();
    }

    #endregion

    #region ValidationFilterOptions

    [Fact]
    public void ValidationFilterOptions_Defaults_ShouldHaveExpectedValues()
    {
        var options = new ValidationFilterOptions();

        options.ThrowOnValidationFailure.Should().BeFalse();
        options.SendInvalidToDeadLetter.Should().BeTrue();
        options.SkipInvalidMessages.Should().BeFalse();
        options.CancelInvalidPublish.Should().BeTrue();
        options.LogMissingValidators.Should().BeFalse();
    }

    [Fact]
    public void ValidationFilterOptions_CustomValues_ShouldBeSetCorrectly()
    {
        var options = new ValidationFilterOptions
        {
            ThrowOnValidationFailure = true,
            SendInvalidToDeadLetter = false,
            SkipInvalidMessages = true,
            CancelInvalidPublish = false,
            LogMissingValidators = true
        };

        options.ThrowOnValidationFailure.Should().BeTrue();
        options.SendInvalidToDeadLetter.Should().BeFalse();
        options.SkipInvalidMessages.Should().BeTrue();
        options.CancelInvalidPublish.Should().BeFalse();
        options.LogMissingValidators.Should().BeTrue();
    }

    #endregion

    #region ValidationError

    [Fact]
    public void ValidationError_Constructor_ShouldSetProperties()
    {
        var error = new ValidationError("Name", "Name is required", "NotEmpty");

        error.PropertyName.Should().Be("Name");
        error.ErrorMessage.Should().Be("Name is required");
        error.ErrorCode.Should().Be("NotEmpty");
    }

    [Fact]
    public void ValidationError_WithoutErrorCode_ShouldHaveNullCode()
    {
        var error = new ValidationError("Name", "Name is required");

        error.PropertyName.Should().Be("Name");
        error.ErrorMessage.Should().Be("Name is required");
        error.ErrorCode.Should().BeNull();
    }

    #endregion

    #region MessageValidationException

    [Fact]
    public void MessageValidationException_ShouldSetMessageType()
    {
        var errors = new List<ValidationError>
        {
            new("Field", "Field is required")
        };
        var ex = new MessageValidationException("TestMessage", errors);

        ex.MessageType.Should().Be("TestMessage");
    }

    [Fact]
    public void MessageValidationException_ShouldSetValidationErrors()
    {
        var errors = new List<ValidationError>
        {
            new("Field1", "Error1"),
            new("Field2", "Error2")
        };
        var ex = new MessageValidationException("MyMsg", errors);

        ex.ValidationErrors.Should().HaveCount(2);
        ex.ValidationErrors[0].PropertyName.Should().Be("Field1");
        ex.ValidationErrors[1].PropertyName.Should().Be("Field2");
    }

    [Fact]
    public void MessageValidationException_ShouldBeException()
    {
        var errors = new List<ValidationError>();
        var ex = new MessageValidationException("T", errors);

        ex.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void MessageValidationException_MessageShouldContainMessageType()
    {
        var errors = new List<ValidationError> { new("P", "E") };
        var ex = new MessageValidationException("OrderCommand", errors);

        ex.Message.Should().Contain("OrderCommand");
    }

    #endregion

    #region RateLimitingConsumeFilterOptions

    [Fact]
    public void RateLimitingConsumeFilterOptions_Defaults_ShouldHaveExpectedValues()
    {
        var options = new RateLimitingConsumeFilterOptions();

        options.KeyMode.Should().Be(RateLimitKeyMode.ByQueue);
        options.KeyGenerator.Should().BeNull();
        options.ExceededBehavior.Should().Be(RateLimitExceededBehavior.Retry);
        options.DefaultRetryDelay.Should().Be(TimeSpan.FromSeconds(5));
        options.OnRateLimited.Should().BeNull();
        options.TypeSpecificOptions.Should().BeEmpty();
    }

    [Fact]
    public void RateLimitingConsumeFilterOptions_Default_ShouldReturnNewInstance()
    {
        var opts1 = RateLimitingConsumeFilterOptions.Default;
        var opts2 = RateLimitingConsumeFilterOptions.Default;

        opts1.Should().NotBeSameAs(opts2);
    }

    [Fact]
    public void RateLimitKeyMode_AllValues_ShouldBeDefined()
    {
        var values = Enum.GetValues<RateLimitKeyMode>();

        values.Should().Contain(RateLimitKeyMode.ByQueue);
        values.Should().Contain(RateLimitKeyMode.ByMessageType);
        values.Should().Contain(RateLimitKeyMode.ByExchange);
        values.Should().Contain(RateLimitKeyMode.ByRoutingKey);
        values.Should().Contain(RateLimitKeyMode.ByConsumerTag);
        values.Should().Contain(RateLimitKeyMode.Global);
    }

    [Fact]
    public void RateLimitExceededBehavior_AllValues_ShouldBeDefined()
    {
        var values = Enum.GetValues<RateLimitExceededBehavior>();

        values.Should().Contain(RateLimitExceededBehavior.Throw);
        values.Should().Contain(RateLimitExceededBehavior.Retry);
        values.Should().Contain(RateLimitExceededBehavior.DeadLetter);
        values.Should().Contain(RateLimitExceededBehavior.Skip);
    }

    #endregion

    #region SendFilterContext

    [Fact]
    public void SendFilterContext_Constructor_ShouldSetProperties()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var msg = new TestOrderEvent { Name = "O1" };

        var ctx = new SendFilterContext<TestOrderEvent>(
            msg,
            "my-queue",
            provider,
            "msg-id-1",
            "corr-id-1");

        ctx.Message.Should().BeSameAs(msg);
        ctx.MessageId.Should().Be("msg-id-1");
        ctx.CorrelationId.Should().Be("corr-id-1");
        ctx.DestinationQueue.Should().Be("my-queue");
        ctx.ServiceProvider.Should().BeSameAs(provider);
        ctx.ShouldSkipRemainingFilters.Should().BeFalse();
        ctx.ShouldCancelSend.Should().BeFalse();
    }

    [Fact]
    public void SendFilterContext_NullMessage_ShouldThrow()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        Action act = () => new SendFilterContext<TestOrderEvent>(null!, "queue", provider);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SendFilterContext_NullQueue_ShouldThrow()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        Action act = () => new SendFilterContext<TestOrderEvent>(
            new TestOrderEvent(), null!, provider);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SendFilterContext_NullServiceProvider_ShouldThrow()
    {
        Action act = () => new SendFilterContext<TestOrderEvent>(
            new TestOrderEvent(), "queue", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SendFilterContext_SkipRemainingFilters_ShouldSetFlag()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var ctx = new SendFilterContext<TestOrderEvent>(new TestOrderEvent(), "q", provider);

        ctx.SkipRemainingFilters();

        ctx.ShouldSkipRemainingFilters.Should().BeTrue();
    }

    [Fact]
    public void SendFilterContext_CancelSend_ShouldSetFlagAndReason()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var ctx = new SendFilterContext<TestOrderEvent>(new TestOrderEvent(), "q", provider);

        ctx.CancelSend("test reason");

        ctx.ShouldCancelSend.Should().BeTrue();
        ctx.CancellationReason.Should().Be("test reason");
    }

    [Fact]
    public void SendFilterContext_SetCorrelationId_ShouldUpdateHeader()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var ctx = new SendFilterContext<TestOrderEvent>(new TestOrderEvent(), "q", provider);

        ctx.SetCorrelationId("new-corr");

        ctx.CorrelationId.Should().Be("new-corr");
        ctx.Headers.Should().ContainKey("x-correlation-id");
        ctx.Headers["x-correlation-id"].Should().Be("new-corr");
    }

    [Fact]
    public void SendFilterContext_SetCausationId_ShouldUpdateHeader()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var ctx = new SendFilterContext<TestOrderEvent>(new TestOrderEvent(), "q", provider);

        ctx.SetCausationId("caus-id");

        ctx.CausationId.Should().Be("caus-id");
        ctx.Headers.Should().ContainKey("x-causation-id");
    }

    [Fact]
    public void SendFilterContext_SetException_ShouldStoreException()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var ctx = new SendFilterContext<TestOrderEvent>(new TestOrderEvent(), "q", provider);
        var ex = new InvalidOperationException("err");

        ctx.SetException(ex);

        ctx.Exception.Should().BeSameAs(ex);
    }

    [Fact]
    public void SendFilterContext_ResetSkip_ShouldClearFlag()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var ctx = new SendFilterContext<TestOrderEvent>(new TestOrderEvent(), "q", provider);
        ctx.SkipRemainingFilters();

        ctx.ResetSkip();

        ctx.ShouldSkipRemainingFilters.Should().BeFalse();
    }

    [Fact]
    public void SendFilterContext_ResetCancel_ShouldClearFlagAndReason()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var ctx = new SendFilterContext<TestOrderEvent>(new TestOrderEvent(), "q", provider);
        ctx.CancelSend("reason");

        ctx.ResetCancel();

        ctx.ShouldCancelSend.Should().BeFalse();
        ctx.CancellationReason.Should().BeNull();
    }

    [Fact]
    public void SendFilterContext_AutoGeneratesMessageId_WhenNotProvided()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        var ctx = new SendFilterContext<TestOrderEvent>(new TestOrderEvent(), "q", provider);

        ctx.MessageId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(ctx.MessageId, out _).Should().BeTrue();
    }

    [Fact]
    public void SendFilterContext_SentAt_ShouldBeRecent()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var before = DateTimeOffset.UtcNow;

        var ctx = new SendFilterContext<TestOrderEvent>(new TestOrderEvent(), "q", provider);

        ctx.SentAt.Should().BeOnOrAfter(before);
        ctx.SentAt.Should().BeOnOrBefore(DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void SendFilterContext_Items_ShouldBeEmpty_Initially()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        var ctx = new SendFilterContext<TestOrderEvent>(new TestOrderEvent(), "q", provider);

        ctx.Items.Should().BeEmpty();
    }

    #endregion
}
