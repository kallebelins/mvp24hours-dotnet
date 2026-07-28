using System;

namespace CustomerAPI.Core.ValueObjects.Domain
{
    /// <summary>
    /// Value object representing a validated contact description (phone number, e-mail address, etc.).
    /// Enforces non-empty and max-length constraints at construction time.
    /// </summary>
    public sealed record ContactDescription
    {
        public string Value { get; }

        public ContactDescription(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Contact description cannot be empty.", nameof(value));
            if (value.Trim().Length > 255)
                throw new ArgumentException("Contact description cannot exceed 255 characters.", nameof(value));

            Value = value.Trim();
        }

        public override string ToString() => Value;
    }
}
