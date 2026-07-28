using System.ComponentModel.DataAnnotations;

namespace CustomerAPI.Infrastructure.Configuration
{
    /// <summary>
    /// Validated options for the Typicode JSONPlaceholder outbound HTTP adapter.
    /// </summary>
    public sealed class TypicodeOptions
    {
        public const string SectionName = "Typicode";

        [Required]
        [Url]
        public string UsersUrl { get; set; } = string.Empty;
    }
}
