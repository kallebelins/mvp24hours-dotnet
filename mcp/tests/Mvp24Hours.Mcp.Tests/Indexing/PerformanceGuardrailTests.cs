using System.Diagnostics;
using Mvp24Hours.Mcp.Indexing;

namespace Mvp24Hours.Mcp.Tests.Indexing;

public class PerformanceGuardrailTests : McpTestFixture
{
    private const int FindSymbolTimeoutMs = 5_000;
    private const int CheckPathsTimeoutMs = 10_000;

    [Fact]
    public void FindSymbol_second_call_completes_within_timeout()
    {
        var source = new SourceIndexService(CreatePaths());

        _ = source.FindSymbol("IMediator", 1);
        var sw = Stopwatch.StartNew();
        var hits = source.FindSymbol("IMediator");
        sw.Stop();

        Assert.NotEmpty(hits);
        Assert.True(sw.ElapsedMilliseconds < FindSymbolTimeoutMs,
            $"FindSymbol took {sw.ElapsedMilliseconds}ms (limit {FindSymbolTimeoutMs}ms).");
    }

    [Fact]
    public void CheckPaths_on_sample_completes_within_timeout()
    {
        var paths = CreatePaths();
        var manifest = new ManifestService(paths);
        var compliance = new ComplianceService(paths, manifest);

        _ = compliance.CheckPaths(["docs/en-us/ai-resources/compliance-checklist.md"]);
        var sw = Stopwatch.StartNew();
        var result = compliance.CheckPaths(["samples/src/minimal-crud-ef-customer-api/CustomerAPI"]);
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < CheckPathsTimeoutMs,
            $"CheckPaths took {sw.ElapsedMilliseconds}ms (limit {CheckPathsTimeoutMs}ms).");
        Assert.NotNull(result);
    }
}
