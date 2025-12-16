//=====================================================================================
// Tests for MongoDbConnectionManager
//=====================================================================================
using FluentAssertions;
using Mvp24Hours.Infrastructure.Data.MongoDb.Resiliency;
using Xunit;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Resiliency;

public class MongoDbConnectionManagerTests
{
    private MongoDbResiliencyOptions CreateDefaultOptions() => new()
    {
        EnableAutoReconnect = true,
        MaxReconnectAttempts = 3,
        ReconnectDelayMilliseconds = 10, // Short delays for tests
        MaxReconnectDelayMilliseconds = 100,
        UseExponentialBackoffForReconnect = false,
        ReconnectJitterFactor = 0,
        LogConnectionEvents = false
    };

    [Fact]
    public void Should_Start_Not_Connected()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var manager = new MongoDbConnectionManager(options);

        // Act & Assert
        manager.IsConnected.Should().BeFalse();
    }

    [Fact]
    public void Should_Track_Connection_Established()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var manager = new MongoDbConnectionManager(options);

        // Act
        manager.OnConnectionEstablished();

        // Assert
        manager.IsConnected.Should().BeTrue();
        manager.LastConnectionTime.Should().NotBeNull();
    }

    [Fact]
    public void Should_Track_Connection_Lost()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var manager = new MongoDbConnectionManager(options);
        manager.OnConnectionEstablished();

        // Act
        manager.OnConnectionLost("Test disconnection");

        // Assert
        manager.IsConnected.Should().BeFalse();
        manager.LastDisconnectionTime.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Successfully_Reconnect()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var manager = new MongoDbConnectionManager(options);
        var attemptCount = 0;

        // Act
        var result = await manager.TryReconnectAsync(async ct =>
        {
            attemptCount++;
            await Task.Delay(1, ct);
            return attemptCount >= 2; // Succeed on second attempt
        });

        // Assert
        result.Should().BeTrue();
        manager.IsConnected.Should().BeTrue();
        attemptCount.Should().Be(2);
    }

    [Fact]
    public async Task Should_Fail_After_Max_Reconnect_Attempts()
    {
        // Arrange
        var options = CreateDefaultOptions();
        options.MaxReconnectAttempts = 3;
        var manager = new MongoDbConnectionManager(options);
        var attemptCount = 0;

        // Act
        var result = await manager.TryReconnectAsync(async ct =>
        {
            attemptCount++;
            await Task.Delay(1, ct);
            return false; // Always fail
        });

        // Assert
        result.Should().BeFalse();
        attemptCount.Should().Be(3);
    }

    [Fact]
    public async Task Should_Not_Reconnect_When_Disabled()
    {
        // Arrange
        var options = CreateDefaultOptions();
        options.EnableAutoReconnect = false;
        var manager = new MongoDbConnectionManager(options);
        var attemptCount = 0;

        // Act
        var result = await manager.TryReconnectAsync(async ct =>
        {
            attemptCount++;
            await Task.Delay(1, ct);
            return true;
        });

        // Assert
        result.Should().BeFalse();
        attemptCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Track_Reconnect_Attempts()
    {
        // Arrange
        var options = CreateDefaultOptions();
        options.MaxReconnectAttempts = 3;
        var manager = new MongoDbConnectionManager(options);

        // Act
        await manager.TryReconnectAsync(async ct =>
        {
            await Task.Delay(1, ct);
            return false;
        });

        // Assert
        manager.ReconnectAttempts.Should().Be(3);
    }

    [Fact]
    public void Should_Raise_ConnectionStateChanged_Event_On_Connect()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var manager = new MongoDbConnectionManager(options);
        ConnectionStateChangedEventArgs? eventArgs = null;
        manager.ConnectionStateChanged += (sender, args) => eventArgs = args;

        // Act
        manager.OnConnectionEstablished();

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.IsConnected.Should().BeTrue();
        eventArgs.PreviousState.Should().BeFalse();
    }

    [Fact]
    public void Should_Raise_ConnectionStateChanged_Event_On_Disconnect()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var manager = new MongoDbConnectionManager(options);
        manager.OnConnectionEstablished();

        ConnectionStateChangedEventArgs? eventArgs = null;
        manager.ConnectionStateChanged += (sender, args) => eventArgs = args;

        // Act
        manager.OnConnectionLost("Test reason");

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.IsConnected.Should().BeFalse();
        eventArgs.PreviousState.Should().BeTrue();
        eventArgs.Reason.Should().Be("Test reason");
    }

    [Fact]
    public async Task Should_Raise_ReconnectAttempt_Events()
    {
        // Arrange
        var options = CreateDefaultOptions();
        options.MaxReconnectAttempts = 2;
        var manager = new MongoDbConnectionManager(options);
        var attempts = new List<int>();
        manager.ReconnectAttempt += (sender, args) => attempts.Add(args.Attempt);

        // Act
        await manager.TryReconnectAsync(async ct =>
        {
            await Task.Delay(1, ct);
            return false;
        });

        // Assert
        attempts.Should().BeEquivalentTo(new[] { 1, 2 });
    }

    [Fact]
    public void Should_Not_Raise_Event_If_Already_Connected()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var manager = new MongoDbConnectionManager(options);
        manager.OnConnectionEstablished();

        var eventCount = 0;
        manager.ConnectionStateChanged += (sender, args) => eventCount++;

        // Act - Call OnConnectionEstablished again
        manager.OnConnectionEstablished();

        // Assert - Should not raise event again
        eventCount.Should().Be(0);
    }

    [Fact]
    public void Should_Not_Raise_Event_If_Already_Disconnected()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var manager = new MongoDbConnectionManager(options);

        var eventCount = 0;
        manager.ConnectionStateChanged += (sender, args) => eventCount++;

        // Act - Call OnConnectionLost when already disconnected
        manager.OnConnectionLost();

        // Assert - Should not raise event
        eventCount.Should().Be(0);
    }

    [Fact]
    public void Should_Reset_Reconnect_Attempts_On_Success()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var manager = new MongoDbConnectionManager(options);

        // Simulate some failed reconnect attempts
        _ = manager.TryReconnectAsync(async ct =>
        {
            await Task.Delay(1, ct);
            return false;
        }).Result;

        var attemptsAfterFailure = manager.ReconnectAttempts;

        // Act - Connection established
        manager.OnConnectionEstablished();

        // Assert
        attemptsAfterFailure.Should().BeGreaterThan(0);
        manager.ReconnectAttempts.Should().Be(0);
    }

    [Fact]
    public async Task Should_Respect_Cancellation_Token()
    {
        // Arrange
        var options = CreateDefaultOptions();
        options.ReconnectDelayMilliseconds = 1000;
        var manager = new MongoDbConnectionManager(options);
        using var cts = new CancellationTokenSource();

        // Act
        var task = manager.TryReconnectAsync(async ct =>
        {
            await Task.Delay(10000, ct);
            return true;
        }, cts.Token);

        // Cancel after a short delay
        await Task.Delay(50);
        cts.Cancel();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }

    [Fact]
    public void Should_Dispose_Properly()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var manager = new MongoDbConnectionManager(options);

        // Act & Assert - Should not throw
        var action = () => manager.Dispose();
        action.Should().NotThrow();
    }

    [Fact]
    public void Should_Handle_Multiple_Dispose_Calls()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var manager = new MongoDbConnectionManager(options);

        // Act & Assert - Should not throw on multiple dispose
        var action = () =>
        {
            manager.Dispose();
            manager.Dispose();
            manager.Dispose();
        };
        action.Should().NotThrow();
    }
}

