using System;

namespace CustomerAPI.Core.ValueObjects.Domain
{
    /// <summary>
    /// Value object representing a validated customer name.
    /// Enforces non-empty and max-length constraints at construction time.
    /// </summary>
    public sealed record CustomerName
    {
        public string Value { get; }

        public CustomerName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Customer name cannot be empty.", nameof(value));
            if (value.Trim().Length > 50)
                throw new ArgumentException("Customer name cannot exceed 50 characters.", nameof(value));

            Value = value.Trim();
        }

        public override string ToString() => Value;
    }
}
