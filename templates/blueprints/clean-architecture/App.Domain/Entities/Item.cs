using Mvp24Hours.Core.Entities;

namespace App.Domain.Entities;

/// <summary>
/// Placeholder entity. Rename to your domain aggregate and expand properties.
/// </summary>
public class Item : EntityBase<int>
{
    public DateTime Created { get; set; }
    public required string Name { get; set; }
    public string? Note { get; set; }
    public bool Active { get; set; }
}
