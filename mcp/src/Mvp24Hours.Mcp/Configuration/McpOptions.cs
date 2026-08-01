namespace Mvp24Hours.Mcp.Configuration;

public sealed class McpOptions
{
    public const string SectionName = "Mvp24HoursMcp";

    public string? RepoRoot { get; set; }

    public int MaxFileBytes { get; set; } = 512_000;

    public int MaxSearchResults { get; set; } = 20;

    public int HttpPort { get; set; } = 5199;
}
