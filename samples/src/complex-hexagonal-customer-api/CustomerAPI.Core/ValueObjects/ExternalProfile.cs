namespace CustomerAPI.Core.ValueObjects
{
    /// <summary>
    /// Value object representing a user profile retrieved from an external source (e.g. Typicode JSONPlaceholder).
    /// Kept in Core so both Application and Infrastructure can reference it without circular dependencies.
    /// </summary>
    public sealed class ExternalProfile
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Phone { get; init; } = string.Empty;
        public string Website { get; init; } = string.Empty;
    }
}
