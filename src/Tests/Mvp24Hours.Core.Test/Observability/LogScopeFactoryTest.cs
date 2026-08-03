using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Mvp24Hours.Core.Observability;

namespace Mvp24Hours.Core.Test.Observability;

[Trait("Category", "Unit")]
public class LogScopeFactoryTest
{
    [Fact]
    public void BeginHttpScope_WithTraceId_ShouldIncludeMethodPathAndTraceId()
    {
        LoggerScopeCapture capture = CreateLoggerCapture();

        using (LogScopeFactory.BeginHttpScope(capture.Logger, "GET", "/api/orders", "trace-123"))
        {
            capture.Scope.Should().NotBeNull();
        }

        capture.Scope![SemanticTags.HttpRequestMethod].Should().Be("GET");
        capture.Scope[SemanticTags.UrlPath].Should().Be("/api/orders");
        capture.Scope["TraceId"].Should().Be("trace-123");
    }

    [Fact]
    public void BeginHttpScope_WithoutTraceId_ShouldUseCurrentActivityTraceId()
    {
        using ActivityListener listener = CreateListener();
        using var source = new ActivitySource(nameof(LogScopeFactoryTest));
        using Activity activity = source.StartActivity("http")!;
        LoggerScopeCapture capture = CreateLoggerCapture();

        using (LogScopeFactory.BeginHttpScope(capture.Logger, "POST", "/api/items"))
        {
            capture.Scope.Should().NotBeNull();
            capture.Scope!["TraceId"].Should().Be(activity.TraceId.ToString());
        }
    }

    [Fact]
    public void BeginDbScope_WithTable_ShouldIncludeDbTags()
    {
        LoggerScopeCapture capture = CreateLoggerCapture();

        using (LogScopeFactory.BeginDbScope(capture.Logger, "postgresql", "SELECT", "orders"))
        {
            capture.Scope.Should().NotBeNull();
        }

        capture.Scope![SemanticTags.DbSystem].Should().Be("postgresql");
        capture.Scope[SemanticTags.DbOperation].Should().Be("SELECT");
        capture.Scope[SemanticTags.DbSqlTable].Should().Be("orders");
    }

    [Fact]
    public void BeginMessagingScope_WithMessageId_ShouldIncludeMessagingTags()
    {
        using ActivityListener listener = CreateListener();
        using var source = new ActivitySource(nameof(LogScopeFactoryTest));
        using Activity activity = source.StartActivity("messaging")!;
        activity.SetBaggage("correlation.id", "corr-42");
        LoggerScopeCapture capture = CreateLoggerCapture();

        using (LogScopeFactory.BeginMessagingScope(capture.Logger, "rabbitmq", "orders.created", "msg-99"))
        {
            capture.Scope.Should().NotBeNull();
        }

        capture.Scope![SemanticTags.MessagingSystem].Should().Be("rabbitmq");
        capture.Scope[SemanticTags.MessagingDestinationName].Should().Be("orders.created");
        capture.Scope[SemanticTags.MessagingMessageId].Should().Be("msg-99");
        capture.Scope[SemanticTags.CorrelationId].Should().Be("corr-42");
    }

    [Fact]
    public void BeginMediatorScope_ShouldIncludeRequestMetadata()
    {
        using ActivityListener listener = CreateListener();
        using var source = new ActivitySource(nameof(LogScopeFactoryTest));
        using Activity activity = source.StartActivity("mediator")!;
        LoggerScopeCapture capture = CreateLoggerCapture();

        using (LogScopeFactory.BeginMediatorScope(capture.Logger, "CreateOrderCommand", "Command"))
        {
            capture.Scope.Should().NotBeNull();
        }

        capture.Scope![SemanticTags.MediatorRequestName].Should().Be("CreateOrderCommand");
        capture.Scope[SemanticTags.MediatorRequestType].Should().Be("Command");
        capture.Scope["SpanId"].Should().Be(activity.SpanId.ToString());
    }

    [Fact]
    public void BeginPipelineScope_ShouldIncludePipelineMetadata()
    {
        LoggerScopeCapture capture = CreateLoggerCapture();

        using (LogScopeFactory.BeginPipelineScope(capture.Logger, "OrderPipeline", "Validate", 2))
        {
            capture.Scope.Should().NotBeNull();
        }

        capture.Scope![SemanticTags.PipelineName].Should().Be("OrderPipeline");
        capture.Scope[SemanticTags.PipelineOperationName].Should().Be("Validate");
        capture.Scope[SemanticTags.PipelineOperationIndex].Should().Be(2);
    }

    [Fact]
    public void BeginCacheScope_ShouldIncludeCacheMetadata()
    {
        LoggerScopeCapture capture = CreateLoggerCapture();

        using (LogScopeFactory.BeginCacheScope(capture.Logger, "redis", "get", "orders:42"))
        {
            capture.Scope.Should().NotBeNull();
        }

        capture.Scope![SemanticTags.CacheSystem].Should().Be("redis");
        capture.Scope[SemanticTags.CacheOperation].Should().Be("get");
        capture.Scope[SemanticTags.CacheKey].Should().Be("orders:42");
    }

    [Fact]
    public void BeginJobScope_ShouldIncludeJobMetadata()
    {
        LoggerScopeCapture capture = CreateLoggerCapture();

        using (LogScopeFactory.BeginJobScope(capture.Logger, "job-1", "SendEmailJob", 3))
        {
            capture.Scope.Should().NotBeNull();
        }

        capture.Scope![SemanticTags.JobId].Should().Be("job-1");
        capture.Scope[SemanticTags.JobType].Should().Be("SendEmailJob");
        capture.Scope[SemanticTags.JobAttempt].Should().Be(3);
    }

    [Fact]
    public void BeginErrorScope_WithCodeAndCategory_ShouldIncludeErrorMetadata()
    {
        LoggerScopeCapture capture = CreateLoggerCapture();
        var exception = new InvalidOperationException("failed");

        using (LogScopeFactory.BeginErrorScope(capture.Logger, exception, "ERR001", "Validation"))
        {
            capture.Scope.Should().NotBeNull();
        }

        capture.Scope![SemanticTags.ErrorType].Should().Be(typeof(InvalidOperationException).FullName);
        capture.Scope[SemanticTags.ErrorMessage].Should().Be("failed");
        capture.Scope[SemanticTags.ErrorCode].Should().Be("ERR001");
        capture.Scope[SemanticTags.ErrorCategory].Should().Be("Validation");
    }

    [Fact]
    public void LogMessagePatterns_ShouldExposeModuleBaseEventIds()
    {
        LogMessagePatterns.CoreBaseEventId.Should().Be(1000);
        LogMessagePatterns.PipeBaseEventId.Should().Be(2000);
        LogMessagePatterns.CqrsBaseEventId.Should().Be(3000);
        LogMessagePatterns.DataBaseEventId.Should().Be(4000);
        LogMessagePatterns.RabbitMQBaseEventId.Should().Be(5000);
        LogMessagePatterns.WebAPIBaseEventId.Should().Be(6000);
        LogMessagePatterns.CachingBaseEventId.Should().Be(7000);
        LogMessagePatterns.CronJobBaseEventId.Should().Be(8000);
        LogMessagePatterns.InfrastructureBaseEventId.Should().Be(9000);
    }

    private static LoggerScopeCapture CreateLoggerCapture()
    {
        return new LoggerScopeCapture();
    }

    private sealed class LoggerScopeCapture
    {
        public Dictionary<string, object>? Scope { get; private set; }

        public ILogger Logger { get; }

        public LoggerScopeCapture()
        {
            var logger = new Mock<ILogger>();
            logger
                .Setup(l => l.BeginScope(It.IsAny<object>()))
                .Callback<object>(state => Scope = state.Should().BeOfType<Dictionary<string, object>>().Subject)
                .Returns(Mock.Of<IDisposable>());
            Logger = logger.Object;
        }
    }

    private static ActivityListener CreateListener()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }
}
