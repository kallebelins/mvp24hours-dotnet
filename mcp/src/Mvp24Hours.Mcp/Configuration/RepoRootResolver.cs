namespace Mvp24Hours.Mcp.Configuration;

public sealed class RepoRootResolver
{
    private const string EnvVarName = "MVP24HOURS_REPO_ROOT";
    private const string ManifestRelativePath = "docs/en-us/ai-resources/templates-manifest.json";
    private readonly Lazy<string> _repoRoot;

    public RepoRootResolver(McpOptions options)
    {
        _repoRoot = new Lazy<string>(() => Resolve(options.RepoRoot));
    }

    public string RepoRoot => _repoRoot.Value;

    public string DocsEnUsPath => Path.Combine(RepoRoot, "docs", "en-us");

    public string SamplesPath => Path.Combine(RepoRoot, "samples");

    public string SourcePath => Path.Combine(RepoRoot, "src");

    public string ManifestPath => Path.Combine(RepoRoot, ManifestRelativePath);

    public string ComplianceChecklistPath =>
        Path.Combine(RepoRoot, "docs", "en-us", "ai-resources", "compliance-checklist.md");

    public string LayersPath => Path.Combine(RepoRoot, "docs", "en-us", "ai-resources", "layers");

    public string ScenariosManifestPath =>
        Path.Combine(RepoRoot, "docs", "en-us", "ai-resources", "scenarios-manifest.json");

    public string CapabilitiesManifestPath =>
        Path.Combine(RepoRoot, "docs", "en-us", "ai-resources", "capabilities-manifest.json");

    public string MigrationPlaybooksPath =>
        Path.Combine(RepoRoot, "docs", "en-us", "ai-resources", "migration-playbooks.json");

    public string DiscoveryPlaybookPath =>
        Path.Combine(RepoRoot, "docs", "en-us", "ai-resources", "discovery-playbook.md");

    public string ResolveRepoRelative(string relativePath) =>
        Path.GetFullPath(Path.Combine(RepoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string Resolve(string? configuredRoot)
    {
        if (!string.IsNullOrWhiteSpace(configuredRoot))
        {
            var full = Path.GetFullPath(configuredRoot);
            ValidateRoot(full);
            return full;
        }

        var env = Environment.GetEnvironmentVariable(EnvVarName);
        if (!string.IsNullOrWhiteSpace(env))
        {
            var full = Path.GetFullPath(env);
            ValidateRoot(full);
            return full;
        }

        var current = AppContext.BaseDirectory;
        for (var i = 0; i < 12; i++)
        {
            var candidate = Path.GetFullPath(current);
            if (File.Exists(Path.Combine(candidate, ManifestRelativePath)))
            {
                return candidate;
            }

            var parent = Directory.GetParent(candidate);
            if (parent is null)
            {
                break;
            }

            current = parent.FullName;
        }

        throw new InvalidOperationException(
            $"Could not locate Mvp24Hours repository root. Set {EnvVarName} to the repo path " +
            $"(expected file: {ManifestRelativePath}).");
    }

    private static void ValidateRoot(string path)
    {
        var manifest = Path.Combine(path, ManifestRelativePath);
        if (!File.Exists(manifest))
        {
            throw new InvalidOperationException(
                $"MVP24HOURS repo root '{path}' is invalid — missing {ManifestRelativePath}.");
        }
    }
}
