using Mvp24Hours.Core.Entities;

namespace CustomerAPI.Domain.Entities;

public class Customer : EntityBase<int>
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool Active { get; set; }
    public DateTime Created { get; set; }
}
