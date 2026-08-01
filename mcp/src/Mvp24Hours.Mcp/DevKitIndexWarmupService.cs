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
    private readonly ILogger<DevKitIndexWarmupService> _logger;

    public DevKitIndexWarmupService(
        ManifestService manifest,
        DocIndexService docIndex,
        SampleCatalogService samples,
        SourceIndexService sourceIndex,
        ComplianceService compliance,
        ILogger<DevKitIndexWarmupService> logger)
    {
        _manifest = manifest;
        _docIndex = docIndex;
        _samples = samples;
        _sourceIndex = sourceIndex;
        _compliance = compliance;
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
        _logger.LogDebug("Mvp24Hours DevKit indexes ready.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
