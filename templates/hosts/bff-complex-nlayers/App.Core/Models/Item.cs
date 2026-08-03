namespace App.Core.Models;

/// <summary>
/// Simple domain model (not an EF entity). Rename and expand for your BFF aggregate.
/// </summary>
public class Item
{
    public int Id { get; set; }
    public DateTime Created { get; set; }
    public required string Name { get; set; }
    public string? Note { get; set; }
    public bool Active { get; set; }
}
