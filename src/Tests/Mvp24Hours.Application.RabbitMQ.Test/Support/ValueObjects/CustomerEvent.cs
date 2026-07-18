namespace Mvp24Hours.Application.RabbitMQ.Test.Support.Dto
{
    public class CustomerEvent : MessageBusEvent
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Active { get; set; }
    }
}
