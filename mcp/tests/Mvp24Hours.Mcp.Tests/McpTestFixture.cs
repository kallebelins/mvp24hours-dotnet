using Mvp24Hours.Mcp.Configuration;
using Mvp24Hours.Mcp.Indexing;

namespace Mvp24Hours.Mcp.Tests;

public abstract class McpTestFixture
{
    protected static RepoRootResolver CreatePaths()
    {
        var repoRoot = Environment.GetEnvironmentVariable("MVP24HOURS_REPO_ROOT")
            ?? FindRepoRoot();

        var options = new McpOptions { RepoRoot = repoRoot };
        return new RepoRootResolver(options);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var manifest = Path.Combine(dir.FullName, "docs", "en-us", "ai-resources", "templates-manifest.json");
            if (File.Exists(manifest))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not find repo root for tests.");
    }
}
