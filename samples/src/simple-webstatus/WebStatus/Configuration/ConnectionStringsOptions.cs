using System.ComponentModel.DataAnnotations;

namespace WebStatus.Configuration;

public sealed class ConnectionStringsOptions
{
    public const string SectionName = "ConnectionStrings";

    [Required]
    public string SqlServer { get; set; } = string.Empty;

    [Required]
    public string PostgreSql { get; set; } = string.Empty;

    [Required]
    public string MySql { get; set; } = string.Empty;

    [Required]
    public string Redis { get; set; } = string.Empty;

    [Required]
    public string MongoDb { get; set; } = string.Empty;

    [Required]
    public string RabbitMQ { get; set; } = string.Empty;
}

public sealed class HealthCatalogOptions
{
    public const string SectionName = "HealthCatalog";

    public string MongoDatabaseName { get; set; } = "mvp24hours";
}
