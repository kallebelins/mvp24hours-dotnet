//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentAssertions;
using Mvp24Hours.Infrastructure.CronJob.Observability;
using System;

namespace Mvp24Hours.Infrastructure.CronJob.Test;

/// <summary>
/// Tests for CronJobMetricsService functionality.
/// </summary>
public class CronJobMetricsTest
{
    #region RecordJobStarted Tests

    [Fact]
    public void RecordJobStarted_ShouldCreateJobState()
    {
        // Arrange
        var metricsService = new CronJobMetricsService();

        // Act
        metricsService.RecordJobStarted("TestJob", "* * * * *");

        // Assert
        var state = metricsService.GetJobState("TestJob");
        state.Should().NotBeNull();
        state!.JobName.Should().Be("TestJob");
        state.CronExpression.Should().Be("* * * * *");
        state.IsRunning.Should().BeTrue();
        state.StartTime.Should().NotBeNull();
    }

    [Fact]
    public void RecordJobStarted_ShouldHandleNullCronExpression()
    {
        // Arrange
        var metricsService = new CronJobMetricsService();

        // Act
        metricsService.RecordJobStarted("OneTimeJob", null);

        // Assert
        var state = metricsService.GetJobState("OneTimeJob");
        state.Should().NotBeNull();
        state!.CronExpression.Should().BeNull();
    }

    #endregion

    #region RecordExecution Tests

    [Fact]
    public void RecordExecution_ShouldUpdateSuccessMetrics()
    {
        // Arrange
        var metricsService = new CronJobMetricsService();
        metricsService.RecordJobStarted("TestJob", "* * * * *");

        // Act
        metricsService.RecordExecution("TestJob", 150.5, success: true, 1);

        // Assert
        var state = metricsService.GetJobState("TestJob");
        state!.TotalExecutions.Should().Be(1);
        state.TotalFailures.Should().Be(0);
        state.LastExecutionSuccess.Should().BeTrue();
        state.LastExecutionDurationMs.Should().Be(150.5);
        state.SuccessRate.Should().Be(100);
    }

    [Fact]
    public void RecordExecution_ShouldUpdateFailureMetrics()
    {
        // Arrange
        var metricsService = new CronJobMetricsService();
        metricsService.RecordJobStarted("TestJob", "* * * * *");

        // Act
        metricsService.RecordExecution("TestJob", 100, success: false, 1);

        // Assert
        var state = metricsService.GetJobState("TestJob");
        state!.TotalExecutions.Should().Be(1);
        state.TotalFailures.Should().Be(1);
        state.LastExecutionSuccess.Should().BeFalse();
        state.SuccessRate.Should().Be(0);
    }

    [Fact]
    public void RecordExecution_ShouldCalculateSuccessRate()
    {
        // Arrange
        var metricsService = new CronJobMetricsService();
        metricsService.RecordJobStarted("TestJob", "* * * * *");

        // Act - 7 successes, 3 failures = 70% success rate
        for (int i = 0; i < 7; i++)
        {
            metricsService.RecordExecution("TestJob", 100, success: true, i + 1);
        }
        for (int i = 0; i < 3; i++)
        {
            metricsService.RecordExecution("TestJob", 100, success: false, 8 + i);
        }

        // Assert
        var state = metricsService.GetJobState("TestJob");
        state!.TotalExecutions.Should().Be(10);
        state.TotalFailures.Should().Be(3);
        state.SuccessRate.Should().Be(70);
    }

    #endregion

    #region RecordFailure Tests

    [Fact]
    public void RecordFailure_ShouldStoreExceptionInfo()
    {
        // Arrange
        var metricsService = new CronJobMetricsService();
        metricsService.RecordJobStarted("TestJob", "* * * * *");
        var exception = new InvalidOperationException("Test error message");

        // Act
        metricsService.RecordFailure("TestJob", exception, 250, 1);

        // Assert
        var state = metricsService.GetJobState("TestJob");
        state!.LastErrorMessage.Should().Be("Test error message");
        state.LastErrorType.Should().Be("InvalidOperationException");
    }

    #endregion

    #region RecordJobStopped Tests

    [Fact]
    public void RecordJobStopped_ShouldUpdateJobState()
    {
        // Arrange
        var metricsService = new CronJobMetricsService();
        metricsService.RecordJobStarted("TestJob", "* * * * *");
        metricsService.RecordExecution("TestJob", 100, success: true, 1);

        // Act
        metricsService.RecordJobStopped("TestJob", totalExecutions: 10);

        // Assert
        var state = metricsService.GetJobState("TestJob");
        state!.IsRunning.Should().BeFalse();
        state.StopTime.Should().NotBeNull();
    }

    #endregion

    #region RecordSkippedExecution Tests

    [Fact]
    public void RecordSkippedExecution_ShouldUpdateSkippedCount()
    {
        // Arrange
        var metricsService = new CronJobMetricsService();
        metricsService.RecordJobStarted("TestJob", "* * * * *");

        // Act
        metricsService.RecordSkippedExecution("TestJob", "overlapping");

        // Assert
        var state = metricsService.GetJobState("TestJob");
        state!.TotalSkipped.Should().Be(1);
        state.LastSkipReason.Should().Be("overlapping");
    }

    [Fact]
    public void RecordSkippedExecution_ShouldTrackDifferentReasons()
    {
        // Arrange
        var metricsService = new CronJobMetricsService();
        metricsService.RecordJobStarted("TestJob", "* * * * *");

        // Act
        metricsService.RecordSkippedExecution("TestJob", "overlapping");
        metricsService.RecordSkippedExecution("TestJob", "circuit_breaker_open");

        // Assert
        var state = metricsService.GetJobState("TestJob");
        state!.TotalSkipped.Should().Be(2);
        state.LastSkipReason.Should().Be("circuit_breaker_open");
    }

    #endregion

    #region RecordRetryAttempt Tests

    [Fact]
    public void RecordRetryAttempt_ShouldUpdateRetryCount()
    {
        // Arrange
        var metricsService = new CronJobMetricsService();
        metricsService.RecordJobStarted("TestJob", "* * * * *");

        // Act
        metricsService.RecordRetryAttempt("TestJob", attemptNumber: 1, maxAttempts: 3, delayMs: 100);
        metricsService.RecordRetryAttempt("TestJob", attemptNumber: 2, maxAttempts: 3, delayMs: 200);

        // Assert
        var state = metricsService.GetJobState("TestJob");
        state!.TotalRetries.Should().Be(2);
    }

    #endregion

    #region RecordCircuitBreakerStateChange Tests

    [Fact]
    public void RecordCircuitBreakerStateChange_ShouldUpdateState()
    {
        // Arrange
        var metricsService = new CronJobMetricsService();
        metricsService.RecordJobStarted("TestJob", "* * * * *");

        // Act
        metricsService.RecordCircuitBreakerStateChange("TestJob", "Closed", "Open");

        // Assert
        var state = metricsService.GetJobState("TestJob");
        state!.CircuitBreakerState.Should().Be("Open");
    }

    #endregion

    #region RecordNextScheduledExecution Tests

    [Fact]
    public void RecordNextScheduledExecution_ShouldUpdateSchedule()
    {
        // Arrange
        var metricsService = new CronJobMetricsService();
        metricsService.RecordJobStarted("TestJob", "* * * * *");
        var nextExecution = DateTimeOffset.UtcNow.AddMinutes(5);

        // Act
        metricsService.RecordNextScheduledExecution("TestJob", nextExecution);

        // Assert
        var state = metricsService.GetJobState("TestJob");
        state!.NextScheduledExecution.Should().Be(nextExecution);
    }

    #endregion

    #region IncrementActiveJob/DecrementActiveJob Tests

    [Fact]
    public void IncrementActiveJob_ShouldSetActive()
    {
        // Arrange
        var metricsService = new CronJobMetricsService();
        metricsService.RecordJobStarted("TestJob", "* * * * *");

        // Act
        metricsService.IncrementActiveJob("TestJob");

        // Assert
        var state = metricsService.GetJobState("TestJob");
        state!.IsActive.Should().BeTrue();
    }

    [Fact]
    public void DecrementActiveJob_ShouldSetInactive()
    {
        // Arrange
        var metricsService = new CronJobMetricsService();
        metricsService.RecordJobStarted("TestJob", "* * * * *");
        metricsService.IncrementActiveJob("TestJob");

        // Act
        metricsService.DecrementActiveJob("TestJob");

        // Assert
        var state = metricsService.GetJobState("TestJob");
        state!.IsActive.Should().BeFalse();
    }

    #endregion

    #region GetAllJobStates Tests

    [Fact]
    public void GetAllJobStates_ShouldReturnAllJobs()
    {
        // Arrange
        var metricsService = new CronJobMetricsService();
        metricsService.RecordJobStarted("Job1", "* * * * *");
        metricsService.RecordJobStarted("Job2", "*/5 * * * *");
        metricsService.RecordJobStarted("Job3", "0 * * * *");

        // Act
        var states = metricsService.GetAllJobStates();

        // Assert
        states.Should().HaveCount(3);
        states.Should().ContainKey("Job1");
        states.Should().ContainKey("Job2");
        states.Should().ContainKey("Job3");
    }

    [Fact]
    public void GetAllJobStates_ShouldReturnEmptyDict_WhenNoJobs()
    {
        // Arrange
        var metricsService = new CronJobMetricsService();

        // Act
        var states = metricsService.GetAllJobStates();

        // Assert
        states.Should().BeEmpty();
    }

    #endregion

    #region CronJobState Tests

    [Fact]
    public void CronJobState_SuccessRate_ShouldBe100_WhenNoExecutions()
    {
        // Arrange
        var state = new CronJobState();

        // Assert
        state.SuccessRate.Should().Be(100);
    }

    [Fact]
    public void CronJobState_TimeSinceLastExecution_ShouldBeNull_WhenNoExecution()
    {
        // Arrange
        var state = new CronJobState();

        // Assert
        state.TimeSinceLastExecution.Should().BeNull();
    }

    [Fact]
    public void CronJobState_TimeSinceLastExecution_ShouldReturnTimeSpan()
    {
        // Arrange
        var state = new CronJobState
        {
            LastExecutionTime = DateTimeOffset.UtcNow.AddMinutes(-5)
        };

        // Assert
        state.TimeSinceLastExecution.Should().NotBeNull();
        state.TimeSinceLastExecution!.Value.TotalMinutes.Should().BeApproximately(5, 0.5);
    }

    #endregion

    #region RecordLastExecution Tests

    [Fact]
    public void RecordLastExecution_ShouldUpdateLastExecutionTime()
    {
        // Arrange
        var metricsService = new CronJobMetricsService();
        metricsService.RecordJobStarted("TestJob", "* * * * *");
        var lastExecution = DateTimeOffset.UtcNow;

        // Act
        metricsService.RecordLastExecution("TestJob", lastExecution);

        // Assert
        var state = metricsService.GetJobState("TestJob");
        state!.LastExecutionTime.Should().Be(lastExecution);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GetJobState_ShouldReturnNull_ForUnknownJob()
    {
        // Arrange
        var metricsService = new CronJobMetricsService();

        // Act
        var state = metricsService.GetJobState("UnknownJob");

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void RecordExecution_ShouldCreateState_IfNotExists()
    {
        // Arrange
        var metricsService = new CronJobMetricsService();

        // Act
        metricsService.RecordExecution("NewJob", 100, success: true, 1);

        // Assert
        var state = metricsService.GetJobState("NewJob");
        state.Should().NotBeNull();
        state!.TotalExecutions.Should().Be(1);
    }

    [Fact]
    public void MultipleJobs_ShouldHaveIndependentStates()
    {
        // Arrange
        var metricsService = new CronJobMetricsService();
        metricsService.RecordJobStarted("Job1", "* * * * *");
        metricsService.RecordJobStarted("Job2", "*/5 * * * *");

        // Act
        metricsService.RecordExecution("Job1", 100, success: true, 1);
        metricsService.RecordExecution("Job1", 100, success: true, 2);
        metricsService.RecordExecution("Job2", 200, success: false, 1);

        // Assert
        var state1 = metricsService.GetJobState("Job1");
        var state2 = metricsService.GetJobState("Job2");

        state1!.TotalExecutions.Should().Be(2);
        state1.TotalFailures.Should().Be(0);
        state1.SuccessRate.Should().Be(100);

        state2!.TotalExecutions.Should().Be(1);
        state2.TotalFailures.Should().Be(1);
        state2.SuccessRate.Should().Be(0);
    }

    #endregion
}

