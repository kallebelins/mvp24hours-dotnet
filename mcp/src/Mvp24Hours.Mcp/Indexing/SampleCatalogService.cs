using Mvp24Hours.Mcp.Configuration;
using Mvp24Hours.Mcp.Models;

namespace Mvp24Hours.Mcp.Indexing;

public sealed class SampleCatalogService
{
    private readonly RepoRootResolver _paths;
    private readonly ManifestService _manifest;
    private readonly Lazy<IReadOnlyList<SampleEntry>> _samples;

    public SampleCatalogService(RepoRootResolver paths, ManifestService manifest)
    {
        _paths = paths;
        _manifest = manifest;
        _samples = new Lazy<IReadOnlyList<SampleEntry>>(BuildCatalog);
    }

    public IReadOnlyList<SampleEntry> GetAll() => _samples.Value;

    public SampleEntry? GetById(string id) =>
        _samples.Value.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<SampleEntry> Filter(string? tier = null, string? shape = null, string? technology = null)
    {
        var query = _manifest.GetAllTemplates().AsEnumerable();

        if (!string.IsNullOrWhiteSpace(tier))
        {
            query = query.Where(t => t.Tier.Contains(tier, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(shape))
        {
            query = query.Where(t => t.Shape.Contains(shape, StringComparison.OrdinalIgnoreCase));
        }

        var templateSamples = query.Select(t => t.ReferenceSample).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var results = _samples.Value.AsEnumerable();

        if (templateSamples.Count > 0 && (tier is not null || shape is not null))
        {
            results = results.Where(s => templateSamples.Any(ts =>
                s.Path.StartsWith(ts, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(technology))
        {
            results = results.Where(s =>
                s.Id.Contains(technology, StringComparison.OrdinalIgnoreCase) ||
                s.Path.Contains(technology, StringComparison.OrdinalIgnoreCase));
        }

        return results.OrderBy(s => s.Id, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public string BuildTree(string sampleId, int maxDepth = 4)
    {
        var sample = GetById(sampleId)
            ?? throw new KeyNotFoundException($"Sample '{sampleId}' not found.");

        var root = _paths.ResolveRepoRelative(sample.Path);
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException($"Sample directory not found: {sample.Path}");
        }

        return BuildTreeLines(root, string.Empty, 0, maxDepth);
    }

    public string? ReadSampleFile(string sampleId, string relativeFilePath, int maxBytes)
    {
        var sample = GetById(sampleId)
            ?? throw new KeyNotFoundException($"Sample '{sampleId}' not found.");

        var full = Path.GetFullPath(Path.Combine(
            _paths.ResolveRepoRelative(sample.Path),
            relativeFilePath.Replace('/', Path.DirectorySeparatorChar)));

        var sampleRoot = Path.GetFullPath(_paths.ResolveRepoRelative(sample.Path));
        if (!full.StartsWith(sampleRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("File path escapes sample directory.");
        }

        if (!File.Exists(full))
        {
            return null;
        }

        var info = new FileInfo(full);
        if (info.Length > maxBytes)
        {
            return $"[Truncated — file size {info.Length} bytes exceeds limit {maxBytes}. Path: {relativeFilePath}]";
        }

        return File.ReadAllText(full);
    }

    public string? FindReadmePath(string sampleId)
    {
        var sample = GetById(sampleId);
        return sample?.ReadmePath;
    }

    private IReadOnlyList<SampleEntry> BuildCatalog()
    {
        var samplesRoot = Path.Combine(_paths.SamplesPath, "src");
        if (!Directory.Exists(samplesRoot))
        {
            return [];
        }

        var entries = new List<SampleEntry>();
        foreach (var dir in Directory.EnumerateDirectories(samplesRoot))
        {
            var id = Path.GetFileName(dir);
            var readme = FindReadme(dir);
            var tier = InferTier(id);

            entries.Add(new SampleEntry
            {
                Id = id,
                Path = Path.GetRelativePath(_paths.RepoRoot, dir).Replace('\\', '/'),
                Tier = tier,
                ReadmePath = readme is null
                    ? null
                    : Path.GetRelativePath(_paths.RepoRoot, readme).Replace('\\', '/')
            });
        }

        return entries;
    }

    private static string? FindReadme(string sampleDir)
    {
        var candidates = new[]
        {
            Path.Combine(sampleDir, "README.md"),
            Directory.EnumerateFiles(sampleDir, "README.md", SearchOption.AllDirectories).FirstOrDefault()
        };

        return candidates.FirstOrDefault(c => c is not null && File.Exists(c));
    }

    private static string InferTier(string sampleId)
    {
        if (sampleId.StartsWith("minimal-", StringComparison.OrdinalIgnoreCase))
        {
            return "Minimal";
        }

        if (sampleId.StartsWith("simple-", StringComparison.OrdinalIgnoreCase))
        {
            return "Simple";
        }

        if (sampleId.StartsWith("microservices-", StringComparison.OrdinalIgnoreCase))
        {
            return "Blueprint";
        }

        if (sampleId.StartsWith("complex-", StringComparison.OrdinalIgnoreCase))
        {
            if (sampleId.Contains("cqrs", StringComparison.OrdinalIgnoreCase) ||
                sampleId.Contains("ddd", StringComparison.OrdinalIgnoreCase) ||
                sampleId.Contains("clean-architecture", StringComparison.OrdinalIgnoreCase) ||
                sampleId.Contains("hexagonal", StringComparison.OrdinalIgnoreCase) ||
                sampleId.Contains("event-driven", StringComparison.OrdinalIgnoreCase))
            {
                return "Blueprint";
            }

            if (sampleId.Contains("keycloak", StringComparison.OrdinalIgnoreCase) ||
                sampleId.Contains("saga", StringComparison.OrdinalIgnoreCase) ||
                sampleId.Contains("event-sourcing", StringComparison.OrdinalIgnoreCase) ||
                sampleId.Contains("observability", StringComparison.OrdinalIgnoreCase) ||
                sampleId.Contains("cronjob", StringComparison.OrdinalIgnoreCase) ||
                sampleId.Contains("hybridcache", StringComparison.OrdinalIgnoreCase))
            {
                return "Capability";
            }

            return "Complex";
        }

        return "Unknown";
    }

    private static string BuildTreeLines(string dir, string prefix, int depth, int maxDepth)
    {
        if (depth > maxDepth)
        {
            return prefix + "... (max depth reached)\n";
        }

        var sb = new System.Text.StringBuilder();
        var entries = Directory.EnumerateFileSystemEntries(dir)
            .Where(e => !Path.GetFileName(e).Equals("bin", StringComparison.OrdinalIgnoreCase) &&
                        !Path.GetFileName(e).Equals("obj", StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e, StringComparer.OrdinalIgnoreCase)
            .ToList();

        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var name = Path.GetFileName(entry);
            var isLast = i == entries.Count - 1;
            var connector = isLast ? "└── " : "├── ";
            sb.Append(prefix).Append(connector).AppendLine(name);

            if (Directory.Exists(entry) && depth < maxDepth)
            {
                var childPrefix = prefix + (isLast ? "    " : "│   ");
                sb.Append(BuildTreeLines(entry, childPrefix, depth + 1, maxDepth));
            }
        }

        return sb.ToString();
    }
}
