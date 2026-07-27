using System.ComponentModel.DataAnnotations;

namespace CustomerAPI.WebAPI.Configuration;

public sealed class ConnectionStringsOptions
{
    public const string SectionName = "ConnectionStrings";

    [Required]
    public string EFDBContext { get; set; } = string.Empty;

}
