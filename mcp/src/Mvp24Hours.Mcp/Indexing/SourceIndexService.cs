using System.Text.RegularExpressions;
using Mvp24Hours.Mcp.Configuration;
using Mvp24Hours.Mcp.Models;

namespace Mvp24Hours.Mcp.Indexing;

public sealed partial class SourceIndexService
{
    private readonly RepoRootResolver _paths;

    public SourceIndexService(RepoRootResolver paths)
    {
        _paths = paths;
    }

    public IReadOnlyList<SourceSymbolHit> FindSymbol(string symbol, int maxResults = 30)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return [];
        }

        var hits = new List<SourceSymbolHit>();
        var srcRoot = _paths.SourcePath;
        if (!Directory.Exists(srcRoot))
        {
            return hits;
        }

        foreach (var file in Directory.EnumerateFiles(srcRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
                file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                if (!lines[i].Contains(symbol, StringComparison.Ordinal))
                {
                    continue;
                }

                hits.Add(new SourceSymbolHit
                {
                    Symbol = symbol,
                    FilePath = Path.GetRelativePath(_paths.RepoRoot, file).Replace('\\', '/'),
                    LineNumber = i + 1,
                    Line = lines[i].Trim()
                });

                if (hits.Count >= maxResults)
                {
                    return hits;
                }
            }
        }

        return hits;
    }

    public IReadOnlyList<string> FindTestsForModule(string moduleName, int maxResults = 20)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
        {
            return [];
        }

        var testsRoot = Path.Combine(_paths.SourcePath, "Tests");
        if (!Directory.Exists(testsRoot))
        {
            return [];
        }

        var normalized = moduleName.Replace('/', Path.DirectorySeparatorChar);
        var results = new List<string>();

        foreach (var dir in Directory.EnumerateDirectories(testsRoot, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(_paths.RepoRoot, dir).Replace('\\', '/');
            if (relative.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileName(dir).Contains(moduleName, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(relative);
            }

            if (results.Count >= maxResults)
            {
                break;
            }
        }

        foreach (var file in Directory.EnumerateFiles(testsRoot, "*.csproj", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(_paths.RepoRoot, file).Replace('\\', '/');
            if (relative.Contains(normalized, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(relative);
            }
        }

        return results.Distinct(StringComparer.OrdinalIgnoreCase).Take(maxResults).ToList();
    }

    public bool SymbolExists(string symbol) => FindSymbol(symbol, 1).Count > 0;

    public bool VerifyDocClaim(string apiName)
    {
        if (string.IsNullOrWhiteSpace(apiName))
        {
            return false;
        }

        return SymbolExists(apiName) || FindSymbol(apiName, 1).Count > 0;
    }

    [GeneratedRegex(@"\b(class|interface|record|struct)\s+(\w+)", RegexOptions.Compiled)]
    private static partial Regex TypeDeclaration();
}
