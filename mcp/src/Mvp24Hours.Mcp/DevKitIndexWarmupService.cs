using Mvp24Hours.Mcp.Indexing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mvp24Hours.Mcp;

public sealed class DevKitIndexWarmupService : IHostedService
{
    private readonly ManifestService _manifest;
    private readonly DocIndexService _docIndex;
    private readonly SampleCatalogService _samples;
    private readonly SourceIndexService _sourceIndex;
    private readonly ComplianceService _compliance;
    private readonly ScenariosManifestService _scenarios;
    private readonly CapabilitiesManifestService _capabilities;
    private readonly MigrationPlaybookService _migration;
    private readonly SamplePatternIndexService _samplePatterns;
    private readonly ILogger<DevKitIndexWarmupService> _logger;

    public DevKitIndexWarmupService(
        ManifestService manifest,
        DocIndexService docIndex,
        SampleCatalogService samples,
        SourceIndexService sourceIndex,
        ComplianceService compliance,
        ScenariosManifestService scenarios,
        CapabilitiesManifestService capabilities,
        MigrationPlaybookService migration,
        SamplePatternIndexService samplePatterns,
        ILogger<DevKitIndexWarmupService> logger)
    {
        _manifest = manifest;
        _docIndex = docIndex;
        _samples = samples;
        _sourceIndex = sourceIndex;
        _compliance = compliance;
        _scenarios = scenarios;
        _capabilities = capabilities;
        _migration = migration;
        _samplePatterns = samplePatterns;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Warming up Mvp24Hours DevKit indexes...");
        _manifest.Warmup();
        _docIndex.Warmup();
        _ = _samples.GetAll();
        _sourceIndex.Warmup();
        _compliance.Warmup();
        _scenarios.Warmup();
        _capabilities.Warmup();
        _migration.Warmup();
        _samplePatterns.Warmup();
        _logger.LogDebug("Mvp24Hours DevKit indexes ready.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
