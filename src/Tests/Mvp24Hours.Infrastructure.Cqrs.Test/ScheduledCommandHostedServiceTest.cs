using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Infrastructure.Cqrs.Scheduling;

namespace Mvp24Hours.Infrastructure.Cqrs.Test;

[Trait("Category", "Unit")]
public class ScheduledCommandHostedServiceTest
{
    [Fact]
    public void Constructor_WithNullScopeFactory_ShouldThrow()
    {
        static void act()
        {
            new ScheduledCommandHostedService(null!, NullLogger<ScheduledCommandHostedService>.Instance);
        }

        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrow()
    {
        var scopeFactory = new Mock<IServiceScopeFactory>();

        void act()
        {
            new ScheduledCommandHostedService(scopeFactory.Object, null!);
        }

        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public async Task ProcessCommandAsync_ShouldExecuteReadyCommand()
    {
        var store = new InMemoryScheduledCommandStore();
        var senderMock = new Mock<ISender>();
        senderMock
            .Setup(s => s.SendAsync(It.IsAny<HostedServiceTestCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var entry = new ScheduledCommandEntry
        {
            CommandType = $"{typeof(HostedServiceTestCommand).FullName}, {typeof(HostedServiceTestCommand).Assembly.GetName().Name}",
            CommandPayload = JsonSerializer.Serialize(new HostedServiceTestCommand { Value = "execute-me" }, JsonOptions()),
            ScheduledAt = DateTime.UtcNow.AddMinutes(-1),
            Status = ScheduledCommandStatus.Pending
        };
        await store.SaveAsync(entry);

        ScheduledCommandHostedService service = CreateService(store, senderMock.Object);
        await InvokeProcessScheduledCommandsAsync(service);

        ScheduledCommandEntry? updated = await store.GetByIdAsync(entry.Id);
        Assert.NotNull(updated);
        Assert.Equal(ScheduledCommandStatus.Completed, updated.Status);
        Assert.NotNull(updated.CompletedAt);
        senderMock.Verify(s => s.SendAsync(It.IsAny<HostedServiceTestCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessCommandAsync_WithExpiredEntry_ShouldMarkExpiredViaMarkExpiredFlow()
    {
        var store = new InMemoryScheduledCommandStore();
        var senderMock = new Mock<ISender>();

        var entry = new ScheduledCommandEntry
        {
            CommandType = $"{typeof(HostedServiceTestCommand).FullName}, {typeof(HostedServiceTestCommand).Assembly.GetName().Name}",
            CommandPayload = JsonSerializer.Serialize(new HostedServiceTestCommand()),
            ScheduledAt = DateTime.UtcNow.AddMinutes(-10),
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            Status = ScheduledCommandStatus.Pending
        };
        await store.SaveAsync(entry);

        ScheduledCommandHostedService service = CreateService(store, senderMock.Object);
        await InvokeMarkExpiredCommandsAsync(service);

        ScheduledCommandEntry? updated = await store.GetByIdAsync(entry.Id);
        Assert.Equal(ScheduledCommandStatus.Expired, updated!.Status);
        senderMock.Verify(s => s.SendAsync(It.IsAny<HostedServiceTestCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessCommandAsync_WhenExecutionFails_ShouldScheduleRetry()
    {
        var store = new InMemoryScheduledCommandStore();
        var senderMock = new Mock<ISender>();
        senderMock
            .Setup(s => s.SendAsync(It.IsAny<HostedServiceTestCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("handler failed"));

        var entry = new ScheduledCommandEntry
        {
            CommandType = $"{typeof(HostedServiceTestCommand).FullName}, {typeof(HostedServiceTestCommand).Assembly.GetName().Name}",
            CommandPayload = JsonSerializer.Serialize(new HostedServiceTestCommand()),
            ScheduledAt = DateTime.UtcNow.AddMinutes(-1),
            Status = ScheduledCommandStatus.Pending,
            MaxRetries = 3
        };
        await store.SaveAsync(entry);

        FakeTimeProvider timeProvider = new(DateTimeOffset.UtcNow);
        ScheduledCommandHostedService service = CreateService(store, senderMock.Object, timeProvider);
        await InvokeProcessScheduledCommandsAsync(service);

        ScheduledCommandEntry? updated = await store.GetByIdAsync(entry.Id);
        Assert.Equal(ScheduledCommandStatus.Failed, updated!.Status);
        Assert.Equal(1, updated.RetryCount);
        Assert.NotNull(updated.NextRetryAt);
        Assert.Contains("handler failed", updated.ErrorMessage);
    }

    [Fact]
    public async Task ProcessCommandAsync_WhenMaxRetriesReached_ShouldMarkFailedWithoutRetry()
    {
        var store = new InMemoryScheduledCommandStore();
        var senderMock = new Mock<ISender>();
        senderMock
            .Setup(s => s.SendAsync(It.IsAny<HostedServiceTestCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("final failure"));

        var entry = new ScheduledCommandEntry
        {
            CommandType = $"{typeof(HostedServiceTestCommand).FullName}, {typeof(HostedServiceTestCommand).Assembly.GetName().Name}",
            CommandPayload = JsonSerializer.Serialize(new HostedServiceTestCommand()),
            ScheduledAt = DateTime.UtcNow.AddMinutes(-1),
            Status = ScheduledCommandStatus.Pending,
            RetryCount = 2,
            MaxRetries = 3
        };
        await store.SaveAsync(entry);

        ScheduledCommandHostedService service = CreateService(store, senderMock.Object);
        await InvokeProcessScheduledCommandsAsync(service);

        ScheduledCommandEntry? updated = await store.GetByIdAsync(entry.Id);
        Assert.Equal(ScheduledCommandStatus.Failed, updated!.Status);
        Assert.Equal(3, updated.RetryCount);
        Assert.Null(updated.NextRetryAt);
    }

    [Fact]
    public async Task ProcessRetryCommandsAsync_ShouldProcessReadyRetries()
    {
        var store = new InMemoryScheduledCommandStore();
        var senderMock = new Mock<ISender>();
        senderMock
            .Setup(s => s.SendAsync(It.IsAny<HostedServiceTestCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var entry = new ScheduledCommandEntry
        {
            CommandType = $"{typeof(HostedServiceTestCommand).FullName}, {typeof(HostedServiceTestCommand).Assembly.GetName().Name}",
            CommandPayload = JsonSerializer.Serialize(new HostedServiceTestCommand()),
            ScheduledAt = DateTime.UtcNow.AddMinutes(-10),
            Status = ScheduledCommandStatus.Failed,
            RetryCount = 1,
            MaxRetries = 3,
            NextRetryAt = DateTime.UtcNow.AddMinutes(-1)
        };
        await store.SaveAsync(entry);

        ScheduledCommandHostedService service = CreateService(store, senderMock.Object);
        await InvokeProcessRetryCommandsAsync(service);

        ScheduledCommandEntry? updated = await store.GetByIdAsync(entry.Id);
        Assert.Equal(ScheduledCommandStatus.Completed, updated!.Status);
    }

    [Fact]
    public async Task MarkExpiredCommandsAsync_ShouldDelegateToStore()
    {
        var store = new InMemoryScheduledCommandStore();
        await store.SaveAsync(new ScheduledCommandEntry
        {
            CommandType = "T",
            CommandPayload = "{}",
            ScheduledAt = DateTime.UtcNow.AddMinutes(-10),
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            Status = ScheduledCommandStatus.Pending
        });

        ScheduledCommandHostedService service = CreateService(store, new Mock<ISender>().Object);
        await InvokeMarkExpiredCommandsAsync(service);

        IReadOnlyList<ScheduledCommandEntry> expired = await store.GetByStatusAsync(ScheduledCommandStatus.Expired);
        Assert.Single(expired);
    }

    [Fact]
    public async Task PurgeOldCommandsAsync_ShouldPurgeCompletedEntries()
    {
        var store = new InMemoryScheduledCommandStore();
        await store.SaveAsync(new ScheduledCommandEntry
        {
            CommandType = "T",
            CommandPayload = "{}",
            ScheduledAt = DateTime.UtcNow.AddDays(-10),
            CompletedAt = DateTime.UtcNow.AddDays(-8),
            Status = ScheduledCommandStatus.Completed
        });

        FakeTimeProvider timeProvider = new(DateTimeOffset.UtcNow);
        ScheduledCommandHostedService service = CreateService(
            store,
            new Mock<ISender>().Object,
            timeProvider,
            new ScheduledCommandOptions { PurgeCompletedAfter = TimeSpan.FromDays(1) });

        await InvokePurgeOldCommandsAsync(service);

        IReadOnlyList<ScheduledCommandEntry> completed = await store.GetByStatusAsync(ScheduledCommandStatus.Completed);
        Assert.Empty(completed);
    }

    [Fact]
    public void CalculateNextRetryTime_ShouldUseExponentialBackoff()
    {
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        ScheduledCommandHostedService service = CreateService(
            new InMemoryScheduledCommandStore(),
            new Mock<ISender>().Object,
            timeProvider,
            new ScheduledCommandOptions
            {
                RetryBaseDelay = TimeSpan.FromSeconds(5),
                RetryMaxDelay = TimeSpan.FromHours(1)
            });

        DateTime firstRetry = InvokeCalculateNextRetryTime(service, 1);
        DateTime secondRetry = InvokeCalculateNextRetryTime(service, 2);

        Assert.Equal(timeProvider.GetUtcNow().UtcDateTime.AddSeconds(5), firstRetry);
        Assert.Equal(timeProvider.GetUtcNow().UtcDateTime.AddSeconds(25), secondRetry);
    }

    private static ScheduledCommandHostedService CreateService(
        IScheduledCommandStore store,
        ISender sender,
        TimeProvider? timeProvider = null,
        ScheduledCommandOptions? options = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(store);
        services.AddSingleton(sender);
        ServiceProvider provider = services.BuildServiceProvider();

        IServiceScopeFactory scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        return new ScheduledCommandHostedService(
            scopeFactory,
            NullLogger<ScheduledCommandHostedService>.Instance,
            options ?? new ScheduledCommandOptions { BatchSize = 10, PollingInterval = TimeSpan.FromSeconds(1) },
            timeProvider);
    }

    private static JsonSerializerOptions JsonOptions()
    {
        return new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    private static async Task InvokeProcessScheduledCommandsAsync(ScheduledCommandHostedService service)
    {
        MethodInfo? method = typeof(ScheduledCommandHostedService).GetMethod(
            "ProcessScheduledCommandsAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        await (Task)method!.Invoke(service, [CancellationToken.None])!;
    }

    private static async Task InvokeProcessRetryCommandsAsync(ScheduledCommandHostedService service)
    {
        MethodInfo? method = typeof(ScheduledCommandHostedService).GetMethod(
            "ProcessRetryCommandsAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        await (Task)method!.Invoke(service, [CancellationToken.None])!;
    }

    private static async Task InvokeMarkExpiredCommandsAsync(ScheduledCommandHostedService service)
    {
        MethodInfo? method = typeof(ScheduledCommandHostedService).GetMethod(
            "MarkExpiredCommandsAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        await (Task)method!.Invoke(service, [CancellationToken.None])!;
    }

    private static async Task InvokePurgeOldCommandsAsync(ScheduledCommandHostedService service)
    {
        MethodInfo? method = typeof(ScheduledCommandHostedService).GetMethod(
            "PurgeOldCommandsAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        await (Task)method!.Invoke(service, [CancellationToken.None])!;
    }

    private static DateTime InvokeCalculateNextRetryTime(ScheduledCommandHostedService service, int retryCount)
    {
        MethodInfo? method = typeof(ScheduledCommandHostedService).GetMethod(
            "CalculateNextRetryTime",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        return (DateTime)method!.Invoke(service, [retryCount])!;
    }

    public class HostedServiceTestCommand : IScheduledCommand<bool>
    {
        public string Value { get; set; } = string.Empty;
    }
}

internal sealed class FakeTimeProvider(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _utcNow = start;

    public override DateTimeOffset GetUtcNow()
    {
        return _utcNow;
    }

    public void Advance(TimeSpan delta)
    {
        _utcNow = _utcNow.Add(delta);
    }
}
