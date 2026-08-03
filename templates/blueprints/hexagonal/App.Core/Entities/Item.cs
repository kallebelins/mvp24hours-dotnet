using Mvp24Hours.Core.Entities;

namespace App.Core.Entities;

/// <summary>
/// Placeholder entity. Rename to your domain model and expand properties.
/// </summary>
public class Item : EntityBase<int>
{
    public DateTime Created { get; set; }
    public required string Name { get; set; }
    public string? Note { get; set; }
    public bool Active { get; set; }
}
