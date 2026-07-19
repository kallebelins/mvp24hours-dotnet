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
[Trait("Category", "Unit")]
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
    }

    [Fact, Priority(1)]
    public async Task PublishAsync_ShouldExecuteAllHandlers()
    {
        // Arrange
        List<string> emailHandled = OrderCreatedEmailHandler.BeginCapture();
        List<string> auditHandled = OrderCreatedAuditHandler.BeginCapture();
        try
        {
            var notification = new OrderCreatedNotification
            {
                OrderId = 123,
                CustomerName = "John Doe",
                Amount = 99.99m
            };

            // Act
            await _mediator.PublishAsync(notification);

            // Assert
            Assert.Single(emailHandled);
            Assert.Single(auditHandled);
            Assert.Contains("123", emailHandled[0]);
            Assert.Contains("John Doe", emailHandled[0]);
            Assert.Contains("99", auditHandled[0]);
        }
        finally
        {
            OrderCreatedEmailHandler.EndCapture();
            OrderCreatedAuditHandler.EndCapture();
        }
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
        // Arrange
        List<string> emailHandled = OrderCreatedEmailHandler.BeginCapture();
        List<string> auditHandled = OrderCreatedAuditHandler.BeginCapture();
        try
        {
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
            foreach (OrderCreatedNotification notification in notifications)
            {
                await _mediator.PublishAsync(notification);
            }

            // Assert
            Assert.Equal(5, emailHandled.Count);
            Assert.Equal(5, auditHandled.Count);

            // Verify order
            for (int i = 0; i < 5; i++)
            {
                Assert.Contains($"order {i + 1}", emailHandled[i]);
            }
        }
        finally
        {
            OrderCreatedEmailHandler.EndCapture();
            OrderCreatedAuditHandler.EndCapture();
        }
    }

    [Fact, Priority(5)]
    public async Task IPublisher_ShouldWorkIndependently()
    {
        // Arrange
        List<string> emailHandled = OrderCreatedEmailHandler.BeginCapture();
        List<string> auditHandled = OrderCreatedAuditHandler.BeginCapture();
        try
        {
            IPublisher publisher = _serviceProvider.GetRequiredService<IPublisher>();
            var notification = new OrderCreatedNotification
            {
                OrderId = 999,
                CustomerName = "Publisher Test",
                Amount = 500
            };

            // Act
            await publisher.PublishAsync(notification);

            // Assert
            Assert.Single(emailHandled);
            Assert.Single(auditHandled);
        }
        finally
        {
            OrderCreatedEmailHandler.EndCapture();
            OrderCreatedAuditHandler.EndCapture();
        }
    }
}
