using Mvp24Hours.Core.Entities;

namespace CustomerAPI.Entities
{
    public class Customer : EntityBase<int>
    {
        public DateTime Created { get; set; }
        public required string Name { get; set; } = string.Empty;
        public string? Note { get; set; } = string.Empty;
        public bool Active { get; set; }
    }
}
