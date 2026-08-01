using Mvp24Hours.Mcp.Configuration;
using Mvp24Hours.Mcp.Indexing;

namespace Mvp24Hours.Mcp;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMvp24HoursDevKit(this IServiceCollection services, McpOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<RepoRootResolver>();
        services.AddSingleton<ManifestService>();
        services.AddSingleton<DocIndexService>();
        services.AddSingleton<SampleCatalogService>();
        services.AddSingleton<SourceIndexService>();
        services.AddSingleton<ComplianceService>();
        services.AddSingleton<ArchitectureResolver>();
        services.AddSingleton<ScenariosManifestService>();
        services.AddSingleton<CapabilitiesManifestService>();
        services.AddSingleton<MigrationPlaybookService>();
        services.AddSingleton<SamplePatternIndexService>();
        return services;
    }
}
