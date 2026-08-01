using System.Text.RegularExpressions;
using Mvp24Hours.Mcp.Configuration;
using Mvp24Hours.Mcp.Models;

namespace Mvp24Hours.Mcp.Indexing;

public sealed partial class SamplePatternIndexService
{
    private readonly RepoRootResolver _paths;
    private readonly SampleCatalogService _samples;
    private readonly Lazy<IReadOnlyList<(string SampleId, string FilePath, int LineNumber, string Line)>> _index;

    public SamplePatternIndexService(RepoRootResolver paths, SampleCatalogService samples)
    {
        _paths = paths;
        _samples = samples;
        _index = new Lazy<IReadOnlyList<(string, string, int, string)>>(BuildIndex);
    }

    public IReadOnlyList<SamplePatternHit> Search(string pattern, int maxResults = 20)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return [];
        }

        var comparison = StringComparison.OrdinalIgnoreCase;
        return _index.Value
            .Where(entry => entry.Line.Contains(pattern, comparison))
            .Take(maxResults)
            .Select(entry => new SamplePatternHit
            {
                SampleId = entry.SampleId,
                FilePath = entry.FilePath,
                LineNumber = entry.LineNumber,
                Line = entry.Line.Trim()
            })
            .ToList();
    }

    public void Warmup() => _ = _index.Value;

    private IReadOnlyList<(string SampleId, string FilePath, int LineNumber, string Line)> BuildIndex()
    {
        var entries = new List<(string, string, int, string)>();
        var samplesRoot = Path.Combine(_paths.SamplesPath, "src");

        if (!Directory.Exists(samplesRoot))
        {
            return entries;
        }

        foreach (var sample in _samples.GetAll())
        {
            var sampleRoot = _paths.ResolveRepoRelative(sample.Path);
            if (!Directory.Exists(sampleRoot))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(sampleRoot, "*.cs", SearchOption.AllDirectories)
                         .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                                     !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
            {
                var relative = Path.GetRelativePath(sampleRoot, file).Replace('\\', '/');
                var lines = File.ReadAllLines(file);
                for (var i = 0; i < lines.Length; i++)
                {
                    if (PatternLine().IsMatch(lines[i]))
                    {
                        entries.Add((sample.Id, relative, i + 1, lines[i]));
                    }
                }
            }
        }

        return entries;
    }

    [GeneratedRegex(@"\b(AddMvp|IMediator|DbContext|MapOpenApi|AddOpenApi|WebApplicationFactory|HybridCache|AddHealthChecks|IPipeline|ISaga|IOutbox|IInbox|AddMvpRabbitMQ|AddMvpKeycloak|AddMvpCronJob|Repository|UnitOfWork)\w*", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex PatternLine();
}
