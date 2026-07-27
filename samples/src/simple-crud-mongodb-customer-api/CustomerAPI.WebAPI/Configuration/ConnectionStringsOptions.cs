using System.ComponentModel.DataAnnotations;

namespace CustomerAPI.WebAPI.Configuration;

public sealed class ConnectionStringsOptions
{
    public const string SectionName = "ConnectionStrings";

    [Required]
    public string MongoDbContext { get; set; } = string.Empty;

}
