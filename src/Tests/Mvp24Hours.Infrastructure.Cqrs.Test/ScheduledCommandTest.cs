//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.Infrastructure.Cqrs.Scheduling;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Mvp24Hours.Infrastructure.Cqrs.Test
{
    #region [ Test Commands ]

    public class TestScheduledCommand : IScheduledCommand
    {
        public string Data { get; set; } = string.Empty;
    }

    public class TestScheduledCommandWithResponse : IScheduledCommand<string>
    {
        public string Data { get; set; } = string.Empty;
    }

    #endregion

    public class ScheduledCommandTest
    {
        #region [ InMemoryScheduledCommandStore Tests ]

        [Fact]
        public async Task Store_SaveAsync_SavesEntry()
        {
            // Arrange
            var store = new InMemoryScheduledCommandStore();
            var entry = new ScheduledCommandEntry
            {
                CommandType = "TestCommand",
                CommandPayload = "{}",
                ScheduledAt = DateTime.UtcNow
            };

            // Act
            await store.SaveAsync(entry);
            var retrieved = await store.GetByIdAsync(entry.Id);

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal(entry.Id, retrieved.Id);
            Assert.Equal("TestCommand", retrieved.CommandType);
        }

        [Fact]
        public async Task Store_GetByIdAsync_ReturnsNullForNonExistent()
        {
            // Arrange
            var store = new InMemoryScheduledCommandStore();

            // Act
            var result = await store.GetByIdAsync("non-existent-id");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Store_GetReadyForExecutionAsync_ReturnsReadyCommands()
        {
            // Arrange
            var store = new InMemoryScheduledCommandStore();

            // Ready command (past scheduled time)
            await store.SaveAsync(new ScheduledCommandEntry
            {
                CommandType = "ReadyCommand",
                CommandPayload = "{}",
                ScheduledAt = DateTime.UtcNow.AddMinutes(-5),
                Status = ScheduledCommandStatus.Pending
            });

            // Not ready command (future scheduled time)
            await store.SaveAsync(new ScheduledCommandEntry
            {
                CommandType = "FutureCommand",
                CommandPayload = "{}",
                ScheduledAt = DateTime.UtcNow.AddHours(1),
                Status = ScheduledCommandStatus.Pending
            });

            // Act
            var ready = await store.GetReadyForExecutionAsync();

            // Assert
            Assert.Single(ready);
            Assert.Equal("ReadyCommand", ready[0].CommandType);
        }

        [Fact]
        public async Task Store_GetReadyForExecutionAsync_RespectsExpiration()
        {
            // Arrange
            var store = new InMemoryScheduledCommandStore();

            // Expired command
            await store.SaveAsync(new ScheduledCommandEntry
            {
                CommandType = "ExpiredCommand",
                CommandPayload = "{}",
                ScheduledAt = DateTime.UtcNow.AddMinutes(-10),
                ExpiresAt = DateTime.UtcNow.AddMinutes(-5),
                Status = ScheduledCommandStatus.Pending
            });

            // Valid command
            await store.SaveAsync(new ScheduledCommandEntry
            {
                CommandType = "ValidCommand",
                CommandPayload = "{}",
                ScheduledAt = DateTime.UtcNow.AddMinutes(-5),
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                Status = ScheduledCommandStatus.Pending
            });

            // Act
            var ready = await store.GetReadyForExecutionAsync();

            // Assert
            Assert.Single(ready);
            Assert.Equal("ValidCommand", ready[0].CommandType);
        }

        [Fact]
        public async Task Store_GetReadyForExecutionAsync_RespectsPriority()
        {
            // Arrange
            var store = new InMemoryScheduledCommandStore();

            await store.SaveAsync(new ScheduledCommandEntry
            {
                CommandType = "LowPriority",
                CommandPayload = "{}",
                ScheduledAt = DateTime.UtcNow.AddMinutes(-5),
                Priority = 100,
                Status = ScheduledCommandStatus.Pending
            });

            await store.SaveAsync(new ScheduledCommandEntry
            {
                CommandType = "HighPriority",
                CommandPayload = "{}",
                ScheduledAt = DateTime.UtcNow.AddMinutes(-5),
                Priority = 10,
                Status = ScheduledCommandStatus.Pending
            });

            // Act
            var ready = await store.GetReadyForExecutionAsync();

            // Assert
            Assert.Equal(2, ready.Count);
            Assert.Equal("HighPriority", ready[0].CommandType);
            Assert.Equal("LowPriority", ready[1].CommandType);
        }

        [Fact]
        public async Task Store_UpdateAsync_UpdatesEntry()
        {
            // Arrange
            var store = new InMemoryScheduledCommandStore();
            var entry = new ScheduledCommandEntry
            {
                CommandType = "TestCommand",
                CommandPayload = "{}",
                ScheduledAt = DateTime.UtcNow,
                Status = ScheduledCommandStatus.Pending
            };
            await store.SaveAsync(entry);

            // Act
            entry.Status = ScheduledCommandStatus.Completed;
            await store.UpdateAsync(entry);
            var retrieved = await store.GetByIdAsync(entry.Id);

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal(ScheduledCommandStatus.Completed, retrieved.Status);
        }

        [Fact]
        public async Task Store_DeleteAsync_DeletesEntry()
        {
            // Arrange
            var store = new InMemoryScheduledCommandStore();
            var entry = new ScheduledCommandEntry
            {
                CommandType = "TestCommand",
                CommandPayload = "{}",
                ScheduledAt = DateTime.UtcNow
            };
            await store.SaveAsync(entry);

            // Act
            var deleted = await store.DeleteAsync(entry.Id);
            var retrieved = await store.GetByIdAsync(entry.Id);

            // Assert
            Assert.True(deleted);
            Assert.Null(retrieved);
        }

        [Fact]
        public async Task Store_PurgeCompletedAsync_RemovesOldCompletedEntries()
        {
            // Arrange
            var store = new InMemoryScheduledCommandStore();

            // Old completed
            await store.SaveAsync(new ScheduledCommandEntry
            {
                CommandType = "OldCompleted",
                CommandPayload = "{}",
                ScheduledAt = DateTime.UtcNow.AddDays(-10),
                CompletedAt = DateTime.UtcNow.AddDays(-8),
                Status = ScheduledCommandStatus.Completed
            });

            // Recent completed
            await store.SaveAsync(new ScheduledCommandEntry
            {
                CommandType = "RecentCompleted",
                CommandPayload = "{}",
                ScheduledAt = DateTime.UtcNow.AddHours(-1),
                CompletedAt = DateTime.UtcNow.AddMinutes(-30),
                Status = ScheduledCommandStatus.Completed
            });

            // Act
            var purged = await store.PurgeCompletedAsync(DateTime.UtcNow.AddDays(-1));
            var byStatus = await store.GetByStatusAsync(ScheduledCommandStatus.Completed);

            // Assert
            Assert.Equal(1, purged);
            Assert.Single(byStatus);
            Assert.Equal("RecentCompleted", byStatus[0].CommandType);
        }

        [Fact]
        public async Task Store_GetByStatusAsync_ReturnsCorrectEntries()
        {
            // Arrange
            var store = new InMemoryScheduledCommandStore();

            await store.SaveAsync(new ScheduledCommandEntry
            {
                CommandType = "Pending1",
                CommandPayload = "{}",
                ScheduledAt = DateTime.UtcNow,
                Status = ScheduledCommandStatus.Pending
            });

            await store.SaveAsync(new ScheduledCommandEntry
            {
                CommandType = "Completed1",
                CommandPayload = "{}",
                ScheduledAt = DateTime.UtcNow,
                Status = ScheduledCommandStatus.Completed
            });

            await store.SaveAsync(new ScheduledCommandEntry
            {
                CommandType = "Failed1",
                CommandPayload = "{}",
                ScheduledAt = DateTime.UtcNow,
                Status = ScheduledCommandStatus.Failed
            });

            // Act
            var pending = await store.GetByStatusAsync(ScheduledCommandStatus.Pending);
            var completed = await store.GetByStatusAsync(ScheduledCommandStatus.Completed);
            var failed = await store.GetByStatusAsync(ScheduledCommandStatus.Failed);

            // Assert
            Assert.Single(pending);
            Assert.Single(completed);
            Assert.Single(failed);
        }

        [Fact]
        public async Task Store_GetByCorrelationIdAsync_ReturnsCorrectEntries()
        {
            // Arrange
            var store = new InMemoryScheduledCommandStore();
            var correlationId = Guid.NewGuid().ToString();

            await store.SaveAsync(new ScheduledCommandEntry
            {
                CommandType = "Correlated1",
                CommandPayload = "{}",
                ScheduledAt = DateTime.UtcNow,
                CorrelationId = correlationId
            });

            await store.SaveAsync(new ScheduledCommandEntry
            {
                CommandType = "Correlated2",
                CommandPayload = "{}",
                ScheduledAt = DateTime.UtcNow,
                CorrelationId = correlationId
            });

            await store.SaveAsync(new ScheduledCommandEntry
            {
                CommandType = "Other",
                CommandPayload = "{}",
                ScheduledAt = DateTime.UtcNow,
                CorrelationId = Guid.NewGuid().ToString()
            });

            // Act
            var correlated = await store.GetByCorrelationIdAsync(correlationId);

            // Assert
            Assert.Equal(2, correlated.Count);
        }

        [Fact]
        public async Task Store_MarkExpiredAsync_MarksExpiredEntries()
        {
            // Arrange
            var store = new InMemoryScheduledCommandStore();

            await store.SaveAsync(new ScheduledCommandEntry
            {
                CommandType = "Expired",
                CommandPayload = "{}",
                ScheduledAt = DateTime.UtcNow.AddMinutes(-10),
                ExpiresAt = DateTime.UtcNow.AddMinutes(-5),
                Status = ScheduledCommandStatus.Pending
            });

            await store.SaveAsync(new ScheduledCommandEntry
            {
                CommandType = "Valid",
                CommandPayload = "{}",
                ScheduledAt = DateTime.UtcNow.AddMinutes(-5),
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                Status = ScheduledCommandStatus.Pending
            });

            // Act
            var marked = await store.MarkExpiredAsync();
            var expired = await store.GetByStatusAsync(ScheduledCommandStatus.Expired);

            // Assert
            Assert.Equal(1, marked);
            Assert.Single(expired);
            Assert.Equal("Expired", expired[0].CommandType);
        }

        [Fact]
        public async Task Store_GetStatisticsAsync_ReturnsCorrectCounts()
        {
            // Arrange
            var store = new InMemoryScheduledCommandStore();

            await store.SaveAsync(new ScheduledCommandEntry { Status = ScheduledCommandStatus.Pending, ScheduledAt = DateTime.UtcNow, CommandType = "T", CommandPayload = "{}" });
            await store.SaveAsync(new ScheduledCommandEntry { Status = ScheduledCommandStatus.Pending, ScheduledAt = DateTime.UtcNow, CommandType = "T", CommandPayload = "{}" });
            await store.SaveAsync(new ScheduledCommandEntry { Status = ScheduledCommandStatus.Processing, ScheduledAt = DateTime.UtcNow, CommandType = "T", CommandPayload = "{}" });
            await store.SaveAsync(new ScheduledCommandEntry { Status = ScheduledCommandStatus.Completed, ScheduledAt = DateTime.UtcNow, CommandType = "T", CommandPayload = "{}" });
            await store.SaveAsync(new ScheduledCommandEntry { Status = ScheduledCommandStatus.Failed, ScheduledAt = DateTime.UtcNow, CommandType = "T", CommandPayload = "{}" });

            // Act
            var stats = await store.GetStatisticsAsync();

            // Assert
            Assert.Equal(2, stats.PendingCount);
            Assert.Equal(1, stats.ProcessingCount);
            Assert.Equal(1, stats.CompletedCount);
            Assert.Equal(1, stats.FailedCount);
            Assert.Equal(5, stats.TotalCount);
        }

        [Fact]
        public async Task Store_GetReadyForRetryAsync_ReturnsRetryableCommands()
        {
            // Arrange
            var store = new InMemoryScheduledCommandStore();

            // Ready for retry
            await store.SaveAsync(new ScheduledCommandEntry
            {
                CommandType = "RetryReady",
                CommandPayload = "{}",
                ScheduledAt = DateTime.UtcNow.AddMinutes(-10),
                Status = ScheduledCommandStatus.Failed,
                RetryCount = 1,
                MaxRetries = 3,
                NextRetryAt = DateTime.UtcNow.AddMinutes(-5)
            });

            // Not ready for retry (future NextRetryAt)
            await store.SaveAsync(new ScheduledCommandEntry
            {
                CommandType = "RetryNotReady",
                CommandPayload = "{}",
                ScheduledAt = DateTime.UtcNow.AddMinutes(-10),
                Status = ScheduledCommandStatus.Failed,
                RetryCount = 1,
                MaxRetries = 3,
                NextRetryAt = DateTime.UtcNow.AddHours(1)
            });

            // Max retries reached
            await store.SaveAsync(new ScheduledCommandEntry
            {
                CommandType = "MaxRetriesReached",
                CommandPayload = "{}",
                ScheduledAt = DateTime.UtcNow.AddMinutes(-10),
                Status = ScheduledCommandStatus.Failed,
                RetryCount = 3,
                MaxRetries = 3,
                NextRetryAt = DateTime.UtcNow.AddMinutes(-5)
            });

            // Act
            var ready = await store.GetReadyForRetryAsync();

            // Assert
            Assert.Single(ready);
            Assert.Equal("RetryReady", ready[0].CommandType);
        }

        #endregion

        #region [ CommandScheduler Tests ]

        [Fact]
        public async Task Scheduler_ScheduleAsync_CreatesEntry()
        {
            // Arrange
            var store = new InMemoryScheduledCommandStore();
            var loggerMock = new Mock<ILogger<CommandScheduler>>();
            var scheduler = new CommandScheduler(store, loggerMock.Object);

            var command = new TestScheduledCommand { Data = "Test" };

            // Act
            var id = await scheduler.ScheduleAsync(command);

            // Assert
            Assert.NotEmpty(id);
            var entry = await store.GetByIdAsync(id);
            Assert.NotNull(entry);
            Assert.Contains("TestScheduledCommand", entry.CommandType);
            Assert.Contains("Test", entry.CommandPayload);
        }

        [Fact]
        public async Task Scheduler_ScheduleAsync_WithDelay_SetsCorrectTime()
        {
            // Arrange
            var store = new InMemoryScheduledCommandStore();
            var loggerMock = new Mock<ILogger<CommandScheduler>>();
            var scheduler = new CommandScheduler(store, loggerMock.Object);

            var command = new TestScheduledCommand { Data = "Test" };
            var delay = TimeSpan.FromMinutes(30);
            var beforeSchedule = DateTime.UtcNow;

            // Act
            var id = await scheduler.ScheduleAsync(command, delay);

            // Assert
            var entry = await store.GetByIdAsync(id);
            Assert.NotNull(entry);
            Assert.True(entry.ScheduledAt >= beforeSchedule.Add(delay).AddSeconds(-1));
            Assert.True(entry.ScheduledAt <= DateTime.UtcNow.Add(delay).AddSeconds(1));
        }

        [Fact]
        public async Task Scheduler_ScheduleAsync_WithOptions_AppliesAllOptions()
        {
            // Arrange
            var store = new InMemoryScheduledCommandStore();
            var loggerMock = new Mock<ILogger<CommandScheduler>>();
            var scheduler = new CommandScheduler(store, loggerMock.Object);

            var command = new TestScheduledCommand { Data = "Test" };
            var correlationId = Guid.NewGuid().ToString();
            var expiresAt = DateTime.UtcNow.AddHours(2);

            var options = ScheduleOptions.After(TimeSpan.FromMinutes(10))
                .WithMaxRetries(5)
                .WithExpiration(expiresAt)
                .WithPriority(10)
                .WithCorrelationId(correlationId);

            // Act
            var id = await scheduler.ScheduleAsync(command, options);

            // Assert
            var entry = await store.GetByIdAsync(id);
            Assert.NotNull(entry);
            Assert.Equal(5, entry.MaxRetries);
            Assert.Equal(expiresAt, entry.ExpiresAt);
            Assert.Equal(10, entry.Priority);
            Assert.Equal(correlationId, entry.CorrelationId);
        }

        [Fact]
        public async Task Scheduler_CancelAsync_CancelsPendingCommand()
        {
            // Arrange
            var store = new InMemoryScheduledCommandStore();
            var loggerMock = new Mock<ILogger<CommandScheduler>>();
            var scheduler = new CommandScheduler(store, loggerMock.Object);

            var command = new TestScheduledCommand { Data = "Test" };
            var id = await scheduler.ScheduleAsync(command);

            // Act
            var cancelled = await scheduler.CancelAsync(id);

            // Assert
            Assert.True(cancelled);
            var entry = await store.GetByIdAsync(id);
            Assert.NotNull(entry);
            Assert.Equal(ScheduledCommandStatus.Cancelled, entry.Status);
        }

        [Fact]
        public async Task Scheduler_CancelAsync_FailsForNonPendingCommand()
        {
            // Arrange
            var store = new InMemoryScheduledCommandStore();
            var loggerMock = new Mock<ILogger<CommandScheduler>>();
            var scheduler = new CommandScheduler(store, loggerMock.Object);

            var entry = new ScheduledCommandEntry
            {
                CommandType = "Test",
                CommandPayload = "{}",
                ScheduledAt = DateTime.UtcNow,
                Status = ScheduledCommandStatus.Completed
            };
            await store.SaveAsync(entry);

            // Act
            var cancelled = await scheduler.CancelAsync(entry.Id);

            // Assert
            Assert.False(cancelled);
        }

        [Fact]
        public async Task Scheduler_RescheduleAsync_UpdatesScheduledTime()
        {
            // Arrange
            var store = new InMemoryScheduledCommandStore();
            var loggerMock = new Mock<ILogger<CommandScheduler>>();
            var scheduler = new CommandScheduler(store, loggerMock.Object);

            var command = new TestScheduledCommand { Data = "Test" };
            var id = await scheduler.ScheduleAsync(command);
            var newTime = DateTime.UtcNow.AddHours(5);

            // Act
            var rescheduled = await scheduler.RescheduleAsync(id, newTime);

            // Assert
            Assert.True(rescheduled);
            var entry = await store.GetByIdAsync(id);
            Assert.NotNull(entry);
            Assert.Equal(newTime, entry.ScheduledAt);
        }

        [Fact]
        public async Task Scheduler_GetStatusAsync_ReturnsCorrectEntry()
        {
            // Arrange
            var store = new InMemoryScheduledCommandStore();
            var loggerMock = new Mock<ILogger<CommandScheduler>>();
            var scheduler = new CommandScheduler(store, loggerMock.Object);

            var command = new TestScheduledCommand { Data = "Test" };
            var id = await scheduler.ScheduleAsync(command);

            // Act
            var entry = await scheduler.GetStatusAsync(id);

            // Assert
            Assert.NotNull(entry);
            Assert.Equal(id, entry.Id);
            Assert.Equal(ScheduledCommandStatus.Pending, entry.Status);
        }

        [Fact]
        public async Task Scheduler_RetryAsync_ResetsFailedCommand()
        {
            // Arrange
            var store = new InMemoryScheduledCommandStore();
            var loggerMock = new Mock<ILogger<CommandScheduler>>();
            var scheduler = new CommandScheduler(store, loggerMock.Object);

            var entry = new ScheduledCommandEntry
            {
                CommandType = "Test",
                CommandPayload = "{}",
                ScheduledAt = DateTime.UtcNow.AddMinutes(-10),
                Status = ScheduledCommandStatus.Failed,
                RetryCount = 1,
                MaxRetries = 3,
                NextRetryAt = DateTime.UtcNow.AddHours(1)
            };
            await store.SaveAsync(entry);

            // Act
            var retried = await scheduler.RetryAsync(entry.Id);

            // Assert
            Assert.True(retried);
            var updated = await store.GetByIdAsync(entry.Id);
            Assert.NotNull(updated);
            Assert.Equal(ScheduledCommandStatus.Pending, updated.Status);
            Assert.Null(updated.NextRetryAt);
        }

        [Fact]
        public async Task Scheduler_RetryAsync_FailsForMaxRetriesReached()
        {
            // Arrange
            var store = new InMemoryScheduledCommandStore();
            var loggerMock = new Mock<ILogger<CommandScheduler>>();
            var scheduler = new CommandScheduler(store, loggerMock.Object);

            var entry = new ScheduledCommandEntry
            {
                CommandType = "Test",
                CommandPayload = "{}",
                ScheduledAt = DateTime.UtcNow.AddMinutes(-10),
                Status = ScheduledCommandStatus.Failed,
                RetryCount = 3,
                MaxRetries = 3
            };
            await store.SaveAsync(entry);

            // Act
            var retried = await scheduler.RetryAsync(entry.Id);

            // Assert
            Assert.False(retried);
        }

        #endregion

        #region [ ScheduledCommandEntry Tests ]

        [Fact]
        public void ScheduledCommandEntry_CanRetry_ReturnsTrueWhenCanRetry()
        {
            // Arrange
            var entry = new ScheduledCommandEntry
            {
                Status = ScheduledCommandStatus.Failed,
                RetryCount = 1,
                MaxRetries = 3
            };

            // Assert
            Assert.True(entry.CanRetry);
        }

        [Fact]
        public void ScheduledCommandEntry_CanRetry_ReturnsFalseWhenMaxRetriesReached()
        {
            // Arrange
            var entry = new ScheduledCommandEntry
            {
                Status = ScheduledCommandStatus.Failed,
                RetryCount = 3,
                MaxRetries = 3
            };

            // Assert
            Assert.False(entry.CanRetry);
        }

        [Fact]
        public void ScheduledCommandEntry_CanRetry_ReturnsFalseWhenNotFailed()
        {
            // Arrange
            var entry = new ScheduledCommandEntry
            {
                Status = ScheduledCommandStatus.Pending,
                RetryCount = 0,
                MaxRetries = 3
            };

            // Assert
            Assert.False(entry.CanRetry);
        }

        [Fact]
        public void ScheduledCommandEntry_IsExpired_ReturnsTrueWhenExpired()
        {
            // Arrange
            var entry = new ScheduledCommandEntry
            {
                ExpiresAt = DateTime.UtcNow.AddMinutes(-5)
            };

            // Assert
            Assert.True(entry.IsExpired);
        }

        [Fact]
        public void ScheduledCommandEntry_IsExpired_ReturnsFalseWhenNotExpired()
        {
            // Arrange
            var entry = new ScheduledCommandEntry
            {
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            // Assert
            Assert.False(entry.IsExpired);
        }

        [Fact]
        public void ScheduledCommandEntry_IsExpired_ReturnsFalseWhenNoExpiration()
        {
            // Arrange
            var entry = new ScheduledCommandEntry
            {
                ExpiresAt = null
            };

            // Assert
            Assert.False(entry.IsExpired);
        }

        [Fact]
        public void ScheduledCommandEntry_IsReady_ReturnsTrueWhenReady()
        {
            // Arrange
            var entry = new ScheduledCommandEntry
            {
                Status = ScheduledCommandStatus.Pending,
                ScheduledAt = DateTime.UtcNow.AddMinutes(-5),
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            // Assert
            Assert.True(entry.IsReady);
        }

        [Fact]
        public void ScheduledCommandEntry_IsReady_ReturnsFalseWhenNotScheduledYet()
        {
            // Arrange
            var entry = new ScheduledCommandEntry
            {
                Status = ScheduledCommandStatus.Pending,
                ScheduledAt = DateTime.UtcNow.AddHours(1),
                ExpiresAt = DateTime.UtcNow.AddHours(2)
            };

            // Assert
            Assert.False(entry.IsReady);
        }

        [Fact]
        public void ScheduledCommandEntry_IsReady_ReturnsFalseWhenExpired()
        {
            // Arrange
            var entry = new ScheduledCommandEntry
            {
                Status = ScheduledCommandStatus.Pending,
                ScheduledAt = DateTime.UtcNow.AddMinutes(-10),
                ExpiresAt = DateTime.UtcNow.AddMinutes(-5)
            };

            // Assert
            Assert.False(entry.IsReady);
        }

        #endregion

        #region [ ScheduleOptions Tests ]

        [Fact]
        public void ScheduleOptions_Now_CreatesOptionsWithCurrentTime()
        {
            // Act
            var before = DateTime.UtcNow;
            var options = ScheduleOptions.Now();
            var after = DateTime.UtcNow;

            // Assert
            Assert.True(options.ScheduledAt >= before);
            Assert.True(options.ScheduledAt <= after);
        }

        [Fact]
        public void ScheduleOptions_After_CreatesOptionsWithDelay()
        {
            // Arrange
            var delay = TimeSpan.FromMinutes(30);

            // Act
            var before = DateTime.UtcNow;
            var options = ScheduleOptions.After(delay);
            var after = DateTime.UtcNow;

            // Assert
            Assert.True(options.ScheduledAt >= before.Add(delay).AddSeconds(-1));
            Assert.True(options.ScheduledAt <= after.Add(delay).AddSeconds(1));
        }

        [Fact]
        public void ScheduleOptions_At_CreatesOptionsWithSpecificTime()
        {
            // Arrange
            var scheduledAt = DateTime.UtcNow.AddHours(5);

            // Act
            var options = ScheduleOptions.At(scheduledAt);

            // Assert
            Assert.Equal(scheduledAt, options.ScheduledAt);
        }

        [Fact]
        public void ScheduleOptions_FluentMethods_Work()
        {
            // Arrange
            var correlationId = Guid.NewGuid().ToString();
            var expiresAt = DateTime.UtcNow.AddHours(2);

            // Act
            var options = ScheduleOptions.Now()
                .WithMaxRetries(5)
                .WithExpiration(expiresAt)
                .WithPriority(10)
                .WithCorrelationId(correlationId);

            // Assert
            Assert.Equal(5, options.MaxRetries);
            Assert.Equal(expiresAt, options.ExpiresAt);
            Assert.Equal(10, options.Priority);
            Assert.Equal(correlationId, options.CorrelationId);
        }

        [Fact]
        public void ScheduleOptions_WithTtl_SetsExpiration()
        {
            // Arrange
            var ttl = TimeSpan.FromHours(2);

            // Act
            var before = DateTime.UtcNow;
            var options = ScheduleOptions.Now().WithTtl(ttl);
            var after = DateTime.UtcNow;

            // Assert
            Assert.NotNull(options.ExpiresAt);
            Assert.True(options.ExpiresAt >= before.Add(ttl).AddSeconds(-1));
            Assert.True(options.ExpiresAt <= after.Add(ttl).AddSeconds(1));
        }

        #endregion

        #region [ DI Integration Tests ]

        [Fact]
        public void SchedulingExtensions_AddMvpScheduledCommands_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddMvpScheduledCommands(options =>
            {
                options.PollingInterval = TimeSpan.FromSeconds(10);
                options.Enabled = false; // Disable hosted service for test
            });

            var provider = services.BuildServiceProvider();

            // Assert
            var store = provider.GetService<IScheduledCommandStore>();
            var scheduler = provider.GetService<ICommandScheduler>();
            var options = provider.GetService<ScheduledCommandOptions>();

            Assert.NotNull(store);
            Assert.IsType<InMemoryScheduledCommandStore>(store);
            Assert.NotNull(scheduler);
            Assert.NotNull(options);
            Assert.Equal(TimeSpan.FromSeconds(10), options.PollingInterval);
        }

        [Fact]
        public void SchedulingExtensions_AddMvpCommandSchedulerOnly_RegistersOnlyScheduler()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddMvpCommandSchedulerOnly();

            var provider = services.BuildServiceProvider();

            // Assert
            var store = provider.GetService<IScheduledCommandStore>();
            var scheduler = provider.GetService<ICommandScheduler>();

            Assert.NotNull(store);
            Assert.NotNull(scheduler);
        }

        #endregion
    }
}

