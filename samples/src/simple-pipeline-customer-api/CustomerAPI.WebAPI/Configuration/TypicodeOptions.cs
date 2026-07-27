using System.ComponentModel.DataAnnotations;

namespace CustomerAPI.WebAPI.Configuration;

public sealed class TypicodeOptions
{
    public const string SectionName = "Settings";

    [Required]
    [Url]
    public string TypicodeCustomerUrl { get; set; } = string.Empty;
}
