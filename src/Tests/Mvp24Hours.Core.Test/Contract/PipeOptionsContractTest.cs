//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;

namespace Mvp24Hours.Core.Test.Contract;

/// <summary>
/// Unit tests for Pipe resilience options, validation results and dead-letter contracts.
/// </summary>
[Trait("Category", "Unit")]
public class PipeOptionsContractTest
{
    #region BulkheadOptions

    [Fact]
    public void BulkheadOptions_Default_HasExpectedValues()
    {
        BulkheadOptions options = BulkheadOptions.Default;

        options.Key.Should().Be("default");
        options.MaxConcurrency.Should().Be(10);
        options.QueueLimit.Should().Be(20);
        options.QueueTimeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void BulkheadOptions_Presets_ConfigureAsDocumented()
    {
        BulkheadOptions.Narrow.MaxConcurrency.Should().Be(2);
        BulkheadOptions.Narrow.QueueLimit.Should().Be(5);
        BulkheadOptions.Wide.MaxConcurrency.Should().Be(50);
        BulkheadOptions.Wide.QueueLimit.Should().Be(100);
        BulkheadOptions.NoQueue.QueueLimit.Should().Be(0);
        BulkheadOptions.NoQueue.QueueTimeout.Should().BeNull();
    }

    [Fact]
    public void BulkheadOptions_Callbacks_CanBeAssigned()
    {
        int queued = 0;
        bool rejected = false;
        TimeSpan? wait = null;
        var options = new BulkheadOptions
        {
            OnQueued = pos => queued = pos,
            OnRejected = () => rejected = true,
            OnDequeued = t => wait = t
        };

        options.OnQueued!(3);
        options.OnRejected!();
        options.OnDequeued!(TimeSpan.FromMilliseconds(15));

        queued.Should().Be(3);
        rejected.Should().BeTrue();
        wait.Should().Be(TimeSpan.FromMilliseconds(15));
    }

    [Fact]
    public void PipelineBulkheadRejectedException_StoresKeyAndReason()
    {
        var ex = new PipelineBulkheadRejectedException("api", BulkheadRejectionReason.QueueFull);
        var withInner = new PipelineBulkheadRejectedException(
            "api", BulkheadRejectionReason.QueueTimeout, new InvalidOperationException("inner"));

        ex.BulkheadKey.Should().Be("api");
        ex.Reason.Should().Be(BulkheadRejectionReason.QueueFull);
        ex.Message.Should().Contain("QueueFull");
        withInner.InnerException.Should().BeOfType<InvalidOperationException>();
        Enum.GetValues<BulkheadRejectionReason>().Should().HaveCount(3);
    }

    #endregion

    #region CircuitBreakerOptions

    [Fact]
    public void CircuitBreakerOptions_Default_HasExpectedValues()
    {
        CircuitBreakerOptions options = CircuitBreakerOptions.Default;

        options.Key.Should().Be("default");
        options.FailureThreshold.Should().Be(5);
        options.OpenDuration.Should().Be(TimeSpan.FromSeconds(30));
        options.SuccessThreshold.Should().Be(2);
        options.SamplingDuration.Should().Be(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void CircuitBreakerOptions_Presets_ConfigureAsDocumented()
    {
        CircuitBreakerOptions.Sensitive.FailureThreshold.Should().Be(2);
        CircuitBreakerOptions.Sensitive.SuccessThreshold.Should().Be(1);
        CircuitBreakerOptions.Tolerant.FailureThreshold.Should().Be(10);
        CircuitBreakerOptions.Tolerant.OpenDuration.Should().Be(TimeSpan.FromSeconds(15));
    }

    [Fact]
    public void CircuitBreakerOptions_ShouldCountAsFailure_UsesPredicateAndExceptionTypes()
    {
        var allFailures = new CircuitBreakerOptions();
        allFailures.ShouldCountAsFailure(new Exception()).Should().BeTrue();

        var typed = new CircuitBreakerOptions
        {
            BreakOnExceptions = [typeof(TimeoutException)]
        };
        typed.ShouldCountAsFailure(new TimeoutException()).Should().BeTrue();
        typed.ShouldCountAsFailure(new InvalidOperationException()).Should().BeFalse();

        var withPredicate = new CircuitBreakerOptions
        {
            BreakOnExceptions = [typeof(TimeoutException)],
            ShouldCountAsFailurePredicate = ex => ex is ArgumentException
        };
        withPredicate.ShouldCountAsFailure(new ArgumentException()).Should().BeTrue();
        withPredicate.ShouldCountAsFailure(new TimeoutException()).Should().BeFalse();
    }

    [Fact]
    public void PipelineCircuitBreakerOpenException_StoresRetryAfter()
    {
        DateTimeOffset retry = DateTimeOffset.UtcNow.AddMinutes(1);
        var ex = new PipelineCircuitBreakerOpenException("cb-1", retry);
        var withInner = new PipelineCircuitBreakerOpenException(
            "cb-1", retry, new Exception("inner"));

        ex.CircuitBreakerKey.Should().Be("cb-1");
        ex.RetryAfter.Should().Be(retry);
        withInner.InnerException!.Message.Should().Be("inner");
        Enum.GetValues<PipelineCircuitState>().Should().HaveCount(3);
    }

    #endregion

    #region RetryOptions

    [Fact]
    public void RetryOptions_Presets_ConfigureAsDocumented()
    {
        RetryOptions.Default.MaxRetryAttempts.Should().Be(3);
        RetryOptions.NoRetry.MaxRetryAttempts.Should().Be(0);
        RetryOptions.Aggressive.MaxRetryAttempts.Should().Be(5);
        RetryOptions.Aggressive.InitialRetryDelay.Should().Be(TimeSpan.FromMilliseconds(100));
        RetryOptions.Conservative.MaxRetryAttempts.Should().Be(2);
        RetryOptions.Conservative.InitialRetryDelay.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void RetryOptions_CalculateDelay_AppliesBackoffAndCap()
    {
        var options = new RetryOptions
        {
            InitialRetryDelay = TimeSpan.FromMilliseconds(100),
            BackoffMultiplier = 2,
            MaxRetryDelay = TimeSpan.FromMilliseconds(250),
            UseJitter = false
        };

        options.CalculateDelay(1).Should().Be(TimeSpan.FromMilliseconds(100));
        options.CalculateDelay(2).Should().Be(TimeSpan.FromMilliseconds(200));
        options.CalculateDelay(3).Should().Be(TimeSpan.FromMilliseconds(250));
    }

    [Fact]
    public void RetryOptions_CalculateDelay_WithJitter_StaysNearBaseDelay()
    {
        var options = new RetryOptions
        {
            InitialRetryDelay = TimeSpan.FromMilliseconds(1000),
            BackoffMultiplier = 1,
            MaxRetryDelay = TimeSpan.FromSeconds(10),
            UseJitter = true,
            JitterFactor = 0.25
        };

        TimeSpan delay = options.CalculateDelay(1);

        delay.TotalMilliseconds.Should().BeInRange(750, 1250);
    }

    [Fact]
    public void RetryOptions_ShouldRetry_RespectsAttemptsExceptionsAndPredicate()
    {
        var options = new RetryOptions { MaxRetryAttempts = 2 };
        options.ShouldRetry(new Exception(), 0).Should().BeTrue();
        options.ShouldRetry(new Exception(), 2).Should().BeFalse();

        options.RetryableExceptions = [typeof(TimeoutException)];
        options.ShouldRetry(new TimeoutException(), 0).Should().BeTrue();
        options.ShouldRetry(new InvalidOperationException(), 0).Should().BeFalse();

        options.ShouldRetryPredicate = _ => true;
        options.ShouldRetry(new InvalidOperationException(), 0).Should().BeTrue();
    }

    #endregion

    #region FallbackOptions

    [Fact]
    public void FallbackOptions_Default_FallsBackOnAllExceptions()
    {
        FallbackOptions options = FallbackOptions.Default;

        options.FallbackOnFaulty.Should().BeTrue();
        options.ShouldFallback(new Exception()).Should().BeTrue();
    }

    [Fact]
    public void FallbackOptions_ShouldFallback_UsesExceptionTypesAndPredicate()
    {
        var options = new FallbackOptions
        {
            FallbackOnExceptions = [typeof(HttpRequestException)]
        };
        options.ShouldFallback(new HttpRequestException()).Should().BeTrue();
        options.ShouldFallback(new InvalidOperationException()).Should().BeFalse();

        options.ShouldFallbackPredicate = ex => ex is ArgumentNullException;
        options.ShouldFallback(new ArgumentNullException()).Should().BeTrue();
        options.ShouldFallback(new HttpRequestException()).Should().BeFalse();
    }

    #endregion

    #region PipelineValidation

    [Fact]
    public void PipelineValidationResult_SuccessAndFailure_BehaveAsExpected()
    {
        var success = PipelineValidationResult.Success();
        success.IsValid.Should().BeTrue();
        success.Errors.Should().BeEmpty();
        success.Invoking(r => r.ThrowIfInvalid()).Should().NotThrow();

        var error = new PipelineValidationError("E1", "bad", "OpA", 2);
        var failure = PipelineValidationResult.Failure(error);

        failure.IsValid.Should().BeFalse();
        failure.Errors.Should().ContainSingle()
            .Which.Should().Be(error);
        failure.Invoking(r => r.ThrowIfInvalid())
            .Should().Throw<PipelineValidationException>()
            .Which.Errors.Should().ContainSingle(e => e.Code == "E1" && e.OperationIndex == 2);
    }

    [Fact]
    public void PipelineValidationException_Message_IncludesErrorCount()
    {
        var errors = new List<PipelineValidationError>
        {
            new("A", "one"),
            new("B", "two", "Step", 1)
        };

        var ex = new PipelineValidationException(errors);

        ex.Errors.Should().HaveCount(2);
        ex.Message.Should().Contain("2 error(s)");
    }

    #endregion

    #region DeadLetter

    [Fact]
    public void DeadLetterOperation_DefaultValues_AreCorrect()
    {
        var deadLetter = new DeadLetterOperation
        {
            OperationName = "SendEmail",
            Reason = DeadLetterReason.MaxRetriesExceeded,
            RetryAttempts = 3,
            CorrelationId = "corr-1",
            ErrorMessage = "failed"
        };

        deadLetter.Id.Should().NotBe(Guid.Empty);
        deadLetter.FailedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        deadLetter.Metadata.Should().BeEmpty();
        deadLetter.IsAcknowledged.Should().BeFalse();
        deadLetter.OperationName.Should().Be("SendEmail");
        deadLetter.Reason.Should().Be(DeadLetterReason.MaxRetriesExceeded);
    }

    [Fact]
    public void DeadLetterReason_ContainsExpectedMembers()
    {
        Enum.GetValues<DeadLetterReason>().Should().Contain([
            DeadLetterReason.MaxRetriesExceeded,
            DeadLetterReason.NonRetryableException,
            DeadLetterReason.CircuitBreakerOpen,
            DeadLetterReason.BulkheadRejected,
            DeadLetterReason.FallbackFailed,
            DeadLetterReason.Timeout,
            DeadLetterReason.ManualIntervention,
            DeadLetterReason.Unknown
        ]);
    }

    [Fact]
    public void DeadLetterOperation_AcknowledgeMetadata_CanBeSet()
    {
        var deadLetter = new DeadLetterOperation
        {
            IsAcknowledged = true,
            AcknowledgedAt = DateTimeOffset.UtcNow,
            AcknowledgedBy = "admin",
            ReprocessCount = 2,
            LastReprocessAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            Metadata = { ["source"] = "pipeline" }
        };

        deadLetter.IsAcknowledged.Should().BeTrue();
        deadLetter.AcknowledgedBy.Should().Be("admin");
        deadLetter.ReprocessCount.Should().Be(2);
        deadLetter.Metadata["source"].Should().Be("pipeline");
    }

    [Fact]
    public void IDeadLetterStore_DeclaresPersistenceMethods()
    {
        string[] methodNames = [.. typeof(IDeadLetterStore).GetMethods().Select(m => m.Name)];

        methodNames.Should().Contain([
            "StoreAsync",
            "GetByIdAsync",
            "GetAllAsync",
            "GetCountAsync",
            "AcknowledgeAsync",
            "MarkReprocessedAsync",
            "DeleteAsync",
            "PurgeAcknowledgedAsync",
            "GetForReprocessingAsync"
        ]);
    }

    #endregion
}
