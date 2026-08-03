namespace App.Core.ValueObjects.Domain;

/// <summary>
/// Value object representing a validated item name.
/// </summary>
public sealed record ItemName
{
    public string Value { get; }

    public ItemName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Item name cannot be empty.", nameof(value));
        if (value.Trim().Length > 100)
            throw new ArgumentException("Item name cannot exceed 100 characters.", nameof(value));

        Value = value.Trim();
    }

    public override string ToString() => Value;
}
