//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Diagnostics;
using Mvp24Hours.Infrastructure.Cqrs.Test.Support;

namespace Mvp24Hours.Infrastructure.Cqrs.Test;

/// <summary>
/// Performance tests for the Mvp24Hours Mediator.
/// These tests measure execution time for common operations.
/// </summary>
/// <remarks>
/// Note: These are not full BenchmarkDotNet benchmarks but simple performance tests
/// to validate that the mediator performs within acceptable limits.
/// For detailed performance analysis, consider using BenchmarkDotNet in a separate project.
/// </remarks>
[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
public class BenchmarkTest
{
    private IServiceProvider _serviceProvider = null!;
    private IMediator _mediator = null!;

    private void SetupServices(bool withBehaviors = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
            if (withBehaviors)
            {
                options.WithDefaultBehaviors();
            }
        });
        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
    }

    [Fact, Priority(1)]
    public async Task SendAsync_SingleCommand_ShouldBeUnder5Ms()
    {
        // Arrange
        SetupServices();
        var command = new TestCommand { Name = "Performance Test", Value = 1 };

        // Warm up
        await _mediator.SendAsync(command);

        // Act
        var sw = Stopwatch.StartNew();
        await _mediator.SendAsync(command);
        sw.Stop();

        // Assert
        Assert.True(sw.ElapsedMilliseconds < 5, 
            $"Single command should complete in under 5ms, took {sw.ElapsedMilliseconds}ms");
    }

    [Fact, Priority(2)]
    public async Task SendAsync_1000Commands_ShouldBeUnder500Ms()
    {
        // Arrange
        SetupServices();
        var commands = Enumerable.Range(1, 1000)
            .Select(i => new TestCommand { Name = $"Test {i}", Value = i })
            .ToList();

        // Warm up
        await _mediator.SendAsync(commands[0]);

        // Act
        var sw = Stopwatch.StartNew();
        foreach (var command in commands)
        {
            await _mediator.SendAsync(command);
        }
        sw.Stop();

        // Assert
        var avgMs = (double)sw.ElapsedMilliseconds / 1000;
        Assert.True(sw.ElapsedMilliseconds < 500, 
            $"1000 commands should complete in under 500ms, took {sw.ElapsedMilliseconds}ms (avg: {avgMs:F3}ms)");
    }

    [Fact, Priority(3)]
    public async Task SendAsync_WithBehaviors_ShouldHaveAcceptableOverhead()
    {
        // Arrange - Without behaviors
        SetupServices(withBehaviors: false);
        var command = new TestCommand { Name = "Overhead Test", Value = 1 };

        // Warm up with multiple iterations
        for (int i = 0; i < 10; i++)
            await _mediator.SendAsync(command);

        var swWithout = Stopwatch.StartNew();
        for (int i = 0; i < 500; i++)
        {
            await _mediator.SendAsync(command);
        }
        swWithout.Stop();

        // Arrange - With behaviors (Logging, Performance, Exception)
        SetupServices(withBehaviors: true);

        // Warm up
        for (int i = 0; i < 10; i++)
            await _mediator.SendAsync(command);

        var swWith = Stopwatch.StartNew();
        for (int i = 0; i < 500; i++)
        {
            await _mediator.SendAsync(command);
        }
        swWith.Stop();

        // Assert - With behaviors should complete within acceptable time
        // We compare absolute times instead of ratios due to low baseline times
        Assert.True(swWith.ElapsedMilliseconds < 500, 
            $"500 commands with behaviors should complete in under 500ms, took {swWith.ElapsedMilliseconds}ms " +
            $"(without behaviors: {swWithout.ElapsedMilliseconds}ms)");
    }

    [Fact, Priority(4)]
    public async Task PublishAsync_SingleNotification_ShouldBeUnder5Ms()
    {
        // Arrange
        SetupServices();
        var notification = new OrderCreatedNotification
        {
            OrderId = 1,
            CustomerName = "Test",
            Amount = 100
        };

        // Warm up
        await _mediator.PublishAsync(notification);

        // Act
        var sw = Stopwatch.StartNew();
        await _mediator.PublishAsync(notification);
        sw.Stop();

        // Assert
        Assert.True(sw.ElapsedMilliseconds < 5, 
            $"Single notification should complete in under 5ms, took {sw.ElapsedMilliseconds}ms");
    }

    [Fact, Priority(5)]
    public async Task PublishAsync_1000Notifications_ShouldBeUnder500Ms()
    {
        // Arrange
        SetupServices();
        var notifications = Enumerable.Range(1, 1000)
            .Select(i => new OrderCreatedNotification
            {
                OrderId = i,
                CustomerName = $"Customer {i}",
                Amount = i * 10
            })
            .ToList();

        // Warm up
        await _mediator.PublishAsync(notifications[0]);

        // Act
        var sw = Stopwatch.StartNew();
        foreach (var notification in notifications)
        {
            await _mediator.PublishAsync(notification);
        }
        sw.Stop();

        // Assert
        var avgMs = (double)sw.ElapsedMilliseconds / 1000;
        Assert.True(sw.ElapsedMilliseconds < 500, 
            $"1000 notifications should complete in under 500ms, took {sw.ElapsedMilliseconds}ms (avg: {avgMs:F3}ms)");
    }

    [Fact, Priority(6)]
    public async Task CreateStream_100Items_ShouldBeUnder100Ms()
    {
        // Arrange
        SetupServices();
        var request = new GetItemsStreamRequest { Count = 100 };

        // Warm up
        await foreach (var _ in _mediator.CreateStream(request)) { }

        // Act
        var sw = Stopwatch.StartNew();
        var count = 0;
        await foreach (var _ in _mediator.CreateStream(request))
        {
            count++;
        }
        sw.Stop();

        // Assert
        Assert.Equal(100, count);
        Assert.True(sw.ElapsedMilliseconds < 100, 
            $"100 stream items should complete in under 100ms, took {sw.ElapsedMilliseconds}ms");
    }

    [Fact, Priority(7)]
    public async Task DomainEventDispatch_100Events_ShouldBeUnder200Ms()
    {
        // Arrange
        SetupServices();
        var dispatcher = _serviceProvider.GetRequiredService<IDomainEventDispatcher>();
        
        var aggregates = Enumerable.Range(1, 100)
            .Select(i =>
            {
                var agg = new TestAggregate { Id = i };
                agg.Register($"user{i}@test.com");
                return agg;
            })
            .ToList();

        // Warm up
        await dispatcher.DispatchEventsAsync(aggregates[0]);

        // Reset for actual test
        foreach (var agg in aggregates.Skip(1))
        {
            agg.Register($"user{agg.Id}@test.com"); // Re-add event
        }

        // Act
        var sw = Stopwatch.StartNew();
        await dispatcher.DispatchEventsAsync(aggregates.Skip(1));
        sw.Stop();

        // Assert
        Assert.True(sw.ElapsedMilliseconds < 200, 
            $"99 domain events should dispatch in under 200ms, took {sw.ElapsedMilliseconds}ms");
    }

    [Fact, Priority(8)]
    public async Task ConcurrentCommands_100Parallel_ShouldHandleCorrectly()
    {
        // Arrange
        SetupServices();
        var commands = Enumerable.Range(1, 100)
            .Select(i => new TestCommand { Name = $"Concurrent {i}", Value = i })
            .ToList();

        // Act
        var sw = Stopwatch.StartNew();
        var tasks = commands.Select(c => _mediator.SendAsync(c));
        var results = await Task.WhenAll(tasks);
        sw.Stop();

        // Assert
        Assert.Equal(100, results.Length);
        Assert.All(results, r => Assert.NotNull(r));
        Assert.True(sw.ElapsedMilliseconds < 500, 
            $"100 concurrent commands should complete in under 500ms, took {sw.ElapsedMilliseconds}ms");
    }

    [Fact, Priority(9)]
    public async Task Query_100Sequential_ShouldBeUnder100Ms()
    {
        // Arrange
        SetupServices();
        var queries = Enumerable.Range(1, 100)
            .Select(_ => new GetUserQuery { UserId = 1 })
            .ToList();

        // Warm up
        await _mediator.SendAsync(queries[0]);

        // Act
        var sw = Stopwatch.StartNew();
        foreach (var query in queries)
        {
            await _mediator.SendAsync(query);
        }
        sw.Stop();

        // Assert
        var avgMs = (double)sw.ElapsedMilliseconds / 100;
        Assert.True(sw.ElapsedMilliseconds < 100, 
            $"100 queries should complete in under 100ms, took {sw.ElapsedMilliseconds}ms (avg: {avgMs:F3}ms)");
    }

    [Fact, Priority(10)]
    public void MediatorInstantiation_ShouldBeFast()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(typeof(TestCommand).Assembly);
        var sp = services.BuildServiceProvider();

        // Act - Measure scope creation and mediator resolution
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            using var scope = sp.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        }
        sw.Stop();

        // Assert
        var avgMs = (double)sw.ElapsedMilliseconds / 1000;
        Assert.True(sw.ElapsedMilliseconds < 100, 
            $"1000 mediator resolutions should complete in under 100ms, took {sw.ElapsedMilliseconds}ms (avg: {avgMs:F3}ms)");
    }
}

