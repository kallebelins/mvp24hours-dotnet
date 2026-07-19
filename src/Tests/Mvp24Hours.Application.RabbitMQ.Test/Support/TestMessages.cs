namespace Mvp24Hours.Application.RabbitMQ.Test.Support;

public class TestOrderEvent
{
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "order";
}

public class TestOrderCreatedEvent
{
    public Guid CorrelationId { get; set; }
    public string OrderId { get; set; } = string.Empty;
}

public class TestPaymentCompletedEvent
{
    public Guid CorrelationId { get; set; }
}

public class TestOrderCommand
{
    public string Action { get; set; } = string.Empty;
}

public class TestOrderResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class TestOrderSagaData
{
    public string OrderId { get; set; } = string.Empty;
    public bool Paid { get; set; }
}

public interface ITestOrderEvent { }

public class TestOrderCreatedEventMessage : ITestOrderEvent
{
    public string OrderId { get; set; } = string.Empty;
}

public class TestOrderCommandMessage : ITestOrderEvent
{
    public string Command { get; set; } = string.Empty;
}
