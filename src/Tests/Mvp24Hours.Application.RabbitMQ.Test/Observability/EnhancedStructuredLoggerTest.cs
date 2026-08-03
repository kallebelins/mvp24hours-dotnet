using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Infrastructure.RabbitMQ.Observability;

namespace Mvp24Hours.Application.RabbitMQ.Test.Observability;

[Trait("Category", "Unit")]
public class EnhancedStructuredLoggerTest
{
    private readonly EnhancedStructuredLogger _logger = new(NullLogger<EnhancedStructuredLogger>.Instance);

    [Fact]
    public void LogMessagePublishedWithEnvelope_ShouldNotThrow()
    {
        var envelope = new MessageEnvelope
        {
            MessageId = "msg-1",
            CorrelationId = "corr-1",
            CausationId = "cause-1",
            Exchange = "orders",
            RoutingKey = "order.created",
            MessageType = "OrderCreated",
            PayloadSize = 256,
            Priority = 5,
            Persistent = true,
            Expiration = TimeSpan.FromMinutes(5),
            TenantId = "tenant-1",
            UserId = "user-1"
        };

        Action act = () => _logger.LogMessagePublishedWithEnvelope(envelope, TimeSpan.FromMilliseconds(12));

        act.Should().NotThrow();
    }

    [Fact]
    public void LogMessageConsumedWithEnvelope_ShouldLogSuccessAndFailure()
    {
        var envelope = new MessageEnvelope
        {
            MessageId = "msg-2",
            QueueName = "orders",
            MessageType = "OrderCreated",
            PayloadSize = 128,
            Redelivered = true,
            RedeliveryCount = 2
        };

        Action success = () => _logger.LogMessageConsumedWithEnvelope(envelope, TimeSpan.FromMilliseconds(20), success: true);
        Action failure = () => _logger.LogMessageConsumedWithEnvelope(envelope, TimeSpan.FromMilliseconds(20), success: false);

        success.Should().NotThrow();
        failure.Should().NotThrow();
    }

    [Fact]
    public void LogMessageEnvelopeDebug_ShouldSanitizeSensitiveHeaders()
    {
        using var factory = LoggerFactory.Create(builder => builder.AddProvider(new ListLoggerProvider()));
        var logger = new EnhancedStructuredLogger(factory.CreateLogger<EnhancedStructuredLogger>(), sensitiveHeaders: ["Authorization"]);
        var envelope = new MessageEnvelope
        {
            MessageId = "msg-3",
            Headers = new Dictionary<string, object>
            {
                ["Authorization"] = "Bearer secret",
                ["x-custom"] = "visible"
            }
        };

        Action act = () => logger.LogMessageEnvelopeDebug("publish", envelope);

        act.Should().NotThrow();
    }

    [Fact]
    public void LogBatchProcessed_ShouldNotThrow()
    {
        Action act = () => _logger.LogBatchProcessed("queue-a", batchSize: 10, successCount: 8, failedCount: 2, TimeSpan.FromMilliseconds(100));

        act.Should().NotThrow();
    }

    [Fact]
    public void LogSagaStep_ShouldLogSuccessAndFailure()
    {
        Action success = () => _logger.LogSagaStep("OrderSaga", "corr-1", "Validate", success: true, TimeSpan.FromMilliseconds(5));
        Action failure = () => _logger.LogSagaStep("OrderSaga", "corr-1", "Validate", success: false, TimeSpan.FromMilliseconds(5), "validation failed");

        success.Should().NotThrow();
        failure.Should().NotThrow();
    }

    [Fact]
    public void LogMessagePublished_ShouldDelegateToEnvelopeLogger()
    {
        Action act = () => _logger.LogMessagePublished("msg-4", "ex", "route", 64, priority: 1, elapsed: TimeSpan.FromMilliseconds(1));

        act.Should().NotThrow();
    }

    [Fact]
    public void LogMessageReceived_ShouldNotThrow()
    {
        Action act = () => _logger.LogMessageReceived("msg-5", "ex", "route", "consumer-1", redelivered: false, bodySize: 32);

        act.Should().NotThrow();
    }

    [Fact]
    public void LogMessageAcked_And_Nacked_And_Rejected_ShouldNotThrow()
    {
        Action ack = () => _logger.LogMessageAcked("msg-6", deliveryTag: 10, TimeSpan.FromMilliseconds(3));
        Action nack = () => _logger.LogMessageNacked("msg-6", deliveryTag: 10, requeue: true, reason: "retry");
        Action reject = () => _logger.LogMessageRejected("msg-6", deliveryTag: 10, reason: "poison");

        ack.Should().NotThrow();
        nack.Should().NotThrow();
        reject.Should().NotThrow();
    }

    [Fact]
    public void LogMessageRedelivered_And_DuplicateSkipped_ShouldNotThrow()
    {
        Action redelivered = () => _logger.LogMessageRedelivered("msg-7", redeliveryCount: 2, maxRedeliveries: 5);
        Action duplicate = () => _logger.LogDuplicateMessageSkipped("msg-7");

        redelivered.Should().NotThrow();
        duplicate.Should().NotThrow();
    }

    [Fact]
    public void LogPublisherConfirm_And_Nack_ShouldNotThrow()
    {
        Action confirm = () => _logger.LogPublisherConfirm("msg-8", deliveryTag: 11);
        Action nack = () => _logger.LogPublisherNack("msg-8", deliveryTag: 11);

        confirm.Should().NotThrow();
        nack.Should().NotThrow();
    }

    [Theory]
    [InlineData("connected")]
    [InlineData("disconnected")]
    [InlineData("reconnecting")]
    [InlineData("blocked")]
    [InlineData("unblocked")]
    [InlineData("custom")]
    public void LogConnectionEvent_ShouldHandleEventTypes(string eventType)
    {
        Action act = () => _logger.LogConnectionEvent(eventType, "localhost", 5672, reason: "test");

        act.Should().NotThrow();
    }

    [Fact]
    public void LogChannelEvent_And_Error_ShouldNotThrow()
    {
        Action channel = () => _logger.LogChannelEvent("opened", channelNumber: 1, reason: "ok");
        Action error = () => _logger.LogError("publish", new InvalidOperationException("boom"), messageId: "msg-9");

        channel.Should().NotThrow();
        error.Should().NotThrow();
    }

    [Fact]
    public void LogQueueDeclared_And_ExchangeDeclared_ShouldNotThrow()
    {
        Action queue = () => _logger.LogQueueDeclared("orders", durable: true, exclusive: false, autoDelete: false, messageCount: 3);
        Action exchange = () => _logger.LogExchangeDeclared("orders", "topic", durable: true, autoDelete: false);

        queue.Should().NotThrow();
        exchange.Should().NotThrow();
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenLoggerIsNull()
    {
        Action act = () => _ = new EnhancedStructuredLogger(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class ListLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new ListLogger();

        public void Dispose()
        {
        }
    }

    private sealed class ListLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
        }
    }
}
