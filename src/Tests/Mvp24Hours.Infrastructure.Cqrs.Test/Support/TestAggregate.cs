//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.Infrastructure.Cqrs.EventSourcing;
using CoreDomainEvent = Mvp24Hours.Core.Contract.Domain.Entity.IDomainEvent;

namespace Mvp24Hours.Infrastructure.Cqrs.Test.Support;

#region Domain Events

public record OrderCreatedEvent : DomainEventBase
{
    public Guid OrderId { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
}

public record OrderItemAddedEvent : DomainEventBase
{
    public Guid OrderId { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}

public record OrderPaidEvent : DomainEventBase
{
    public Guid OrderId { get; init; }
    public Guid PaymentId { get; init; }
    public decimal Amount { get; init; }
}

public record OrderShippedEvent : DomainEventBase
{
    public Guid OrderId { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
}

public record OrderCancelledEvent : DomainEventBase
{
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = string.Empty;
}

#endregion

#region Order Aggregate

public enum OrderStatus
{
    Created,
    Paid,
    Shipped,
    Cancelled
}

public class OrderItem
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

#pragma warning disable CS0618 // Type or member is obsolete - using IAggregate for backward compatibility with tests
public class TestOrder : AggregateRoot<Guid>, IAggregate
#pragma warning restore CS0618
{
    private readonly List<OrderItem> _items = new();

    public string CustomerEmail { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    // Required for reconstruction
    public TestOrder() { }

    // Factory method
    public static TestOrder Create(string customerEmail)
    {
        var order = new TestOrder();
        order.Raise(new OrderCreatedEvent
        {
            OrderId = Guid.NewGuid(),
            CustomerEmail = customerEmail,
            TotalAmount = 0
        });
        return order;
    }

    public void AddItem(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        if (Status != OrderStatus.Created)
            throw new InvalidOperationException("Cannot add items to a non-pending order");

        Raise(new OrderItemAddedEvent
        {
            OrderId = Id,
            ProductId = productId,
            ProductName = productName,
            Quantity = quantity,
            UnitPrice = unitPrice
        });
    }

    public void Pay(Guid paymentId)
    {
        if (Status != OrderStatus.Created)
            throw new InvalidOperationException("Order is not in a payable state");

        if (!_items.Any())
            throw new InvalidOperationException("Cannot pay for an empty order");

        Raise(new OrderPaidEvent
        {
            OrderId = Id,
            PaymentId = paymentId,
            Amount = TotalAmount
        });
    }

    public void Ship(string trackingNumber)
    {
        if (Status != OrderStatus.Paid)
            throw new InvalidOperationException("Order must be paid before shipping");

        Raise(new OrderShippedEvent
        {
            OrderId = Id,
            TrackingNumber = trackingNumber
        });
    }

    public void Cancel(string reason)
    {
        if (Status == OrderStatus.Shipped)
            throw new InvalidOperationException("Cannot cancel a shipped order");

        Raise(new OrderCancelledEvent
        {
            OrderId = Id,
            Reason = reason
        });
    }

    protected override void Apply(CoreDomainEvent @event)
    {
        switch (@event)
        {
            case OrderCreatedEvent e:
                Id = e.OrderId;
                CustomerEmail = e.CustomerEmail;
                TotalAmount = e.TotalAmount;
                Status = OrderStatus.Created;
                break;

            case OrderItemAddedEvent e:
                _items.Add(new OrderItem
                {
                    ProductId = e.ProductId,
                    ProductName = e.ProductName,
                    Quantity = e.Quantity,
                    UnitPrice = e.UnitPrice
                });
                TotalAmount += e.Quantity * e.UnitPrice;
                break;

            case OrderPaidEvent:
                Status = OrderStatus.Paid;
                break;

            case OrderShippedEvent:
                Status = OrderStatus.Shipped;
                break;

            case OrderCancelledEvent:
                Status = OrderStatus.Cancelled;
                break;
        }
    }
}

#endregion

#region Snapshot Aggregate

public class OrderSnapshot
{
    public Guid Id { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

public class TestOrderWithSnapshot : SnapshotAggregateRoot<OrderSnapshot>
{
    private readonly List<OrderItem> _items = new();

    // Local Id property since SnapshotAggregateRoot inherits from non-generic AggregateRoot
    public Guid Id { get; private set; }
    public string CustomerEmail { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public TestOrderWithSnapshot() { }

    public static TestOrderWithSnapshot Create(string customerEmail)
    {
        var order = new TestOrderWithSnapshot();
        order.Raise(new OrderCreatedEvent
        {
            OrderId = Guid.NewGuid(),
            CustomerEmail = customerEmail,
            TotalAmount = 0
        });
        return order;
    }

    public void AddItem(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        Raise(new OrderItemAddedEvent
        {
            OrderId = Id,
            ProductId = productId,
            ProductName = productName,
            Quantity = quantity,
            UnitPrice = unitPrice
        });
    }

    protected override void Apply(CoreDomainEvent @event)
    {
        switch (@event)
        {
            case OrderCreatedEvent e:
                Id = e.OrderId;
                CustomerEmail = e.CustomerEmail;
                TotalAmount = e.TotalAmount;
                Status = OrderStatus.Created;
                break;

            case OrderItemAddedEvent e:
                _items.Add(new OrderItem
                {
                    ProductId = e.ProductId,
                    ProductName = e.ProductName,
                    Quantity = e.Quantity,
                    UnitPrice = e.UnitPrice
                });
                TotalAmount += e.Quantity * e.UnitPrice;
                break;
        }
    }

    public override OrderSnapshot CreateSnapshot()
    {
        return new OrderSnapshot
        {
            Id = Id,
            CustomerEmail = CustomerEmail,
            Status = Status,
            TotalAmount = TotalAmount,
            Items = _items.ToList()
        };
    }

    public override void RestoreFromSnapshot(OrderSnapshot snapshot, long version)
    {
        Id = snapshot.Id;
        CustomerEmail = snapshot.CustomerEmail;
        Status = snapshot.Status;
        TotalAmount = snapshot.TotalAmount;
        _items.Clear();
        _items.AddRange(snapshot.Items);
        SetVersion(version);
    }
}

#endregion

