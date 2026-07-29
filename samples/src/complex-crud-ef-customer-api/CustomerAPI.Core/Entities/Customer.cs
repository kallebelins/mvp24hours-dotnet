using Mvp24Hours.Core.Entities;

namespace CustomerAPI.Core.Entities;

public class Customer : EntityBase<int>
{
    public DateTime Created { get; set; }
    public required string Name { get; set; }
    public string? Note { get; set; }
    public bool Active { get; set; }

    public ICollection<Contact>? Contacts { get; set; }
}
