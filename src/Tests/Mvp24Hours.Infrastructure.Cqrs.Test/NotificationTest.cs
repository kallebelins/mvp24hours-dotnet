//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Infrastructure.Cqrs.Test.Support;

namespace Mvp24Hours.Infrastructure.Cqrs.Test;

/// <summary>
/// Unit tests for the notification system.
/// </summary>
[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
public class NotificationTest
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMediator _mediator;

    public NotificationTest()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(typeof(OrderCreatedNotification).Assembly);
        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();

        // Clear static handlers for fresh test state
        OrderCreatedEmailHandler.HandledNotifications.Clear();
        OrderCreatedAuditHandler.HandledNotifications.Clear();
    }

    [Fact, Priority(1)]
    public async Task PublishAsync_ShouldExecuteAllHandlers()
    {
        // Arrange
        var notification = new OrderCreatedNotification
        {
            OrderId = 123,
            CustomerName = "John Doe",
            Amount = 99.99m
        };

        // Act
        await _mediator.PublishAsync(notification);

        // Assert
        Assert.Single(OrderCreatedEmailHandler.HandledNotifications);
        Assert.Single(OrderCreatedAuditHandler.HandledNotifications);
        Assert.Contains("123", OrderCreatedEmailHandler.HandledNotifications[0]);
        Assert.Contains("John Doe", OrderCreatedEmailHandler.HandledNotifications[0]);
        Assert.Contains("99", OrderCreatedAuditHandler.HandledNotifications[0]);
    }

    [Fact, Priority(2)]
    public async Task PublishAsync_WithNoHandlers_ShouldNotThrow()
    {
        // Arrange
        var notification = new NoHandlerNotification { Message = "No one listening" };

        // Act & Assert - Should not throw
        await _mediator.PublishAsync(notification);
    }

    [Fact, Priority(3)]
    public async Task PublishAsync_WithNullNotification_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _mediator.PublishAsync<OrderCreatedNotification>(null!));
    }

    [Fact, Priority(4)]
    public async Task PublishAsync_ShouldExecuteHandlersSequentially()
    {
        // Arrange - Clear static lists
        OrderCreatedEmailHandler.HandledNotifications.Clear();
        OrderCreatedAuditHandler.HandledNotifications.Clear();

        var notifications = new List<OrderCreatedNotification>();
        for (int i = 1; i <= 5; i++)
        {
            notifications.Add(new OrderCreatedNotification
            {
                OrderId = i,
                CustomerName = $"Customer {i}",
                Amount = i * 10
            });
        }

        // Act
        foreach (var notification in notifications)
        {
            await _mediator.PublishAsync(notification);
        }

        // Assert
        Assert.Equal(5, OrderCreatedEmailHandler.HandledNotifications.Count);
        Assert.Equal(5, OrderCreatedAuditHandler.HandledNotifications.Count);

        // Verify order
        for (int i = 0; i < 5; i++)
        {
            Assert.Contains($"order {i + 1}", OrderCreatedEmailHandler.HandledNotifications[i]);
        }
    }

    [Fact, Priority(5)]
    public async Task IPublisher_ShouldWorkIndependently()
    {
        // Arrange
        OrderCreatedEmailHandler.HandledNotifications.Clear();
        OrderCreatedAuditHandler.HandledNotifications.Clear();

        var publisher = _serviceProvider.GetRequiredService<IPublisher>();
        var notification = new OrderCreatedNotification
        {
            OrderId = 999,
            CustomerName = "Publisher Test",
            Amount = 500
        };

        // Act
        await publisher.PublishAsync(notification);

        // Assert
        Assert.Single(OrderCreatedEmailHandler.HandledNotifications);
        Assert.Single(OrderCreatedAuditHandler.HandledNotifications);
    }
}

