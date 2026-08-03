namespace App.Core.ValueObjects.Items;

public class ItemResult
{
    public int Id { get; set; }
    public DateTime Created { get; set; }
    public required string Name { get; set; }
    public string? Note { get; set; }
    public bool Active { get; set; }
}
