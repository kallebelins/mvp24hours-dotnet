using System.Text.RegularExpressions;
using Mvp24Hours.Mcp.Configuration;
using Mvp24Hours.Mcp.Models;

namespace Mvp24Hours.Mcp.Indexing;

public sealed partial class SourceIndexService
{
    private readonly RepoRootResolver _paths;
    private readonly Lazy<SourceSymbolIndex> _index;

    public SourceIndexService(RepoRootResolver paths)
    {
        _paths = paths;
        _index = new Lazy<SourceSymbolIndex>(BuildIndex);
    }

    public IReadOnlyList<SourceSymbolHit> FindSymbol(string symbol, int maxResults = 30)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return [];
        }

        var hits = new List<SourceSymbolHit>();
        foreach (var file in _index.Value.Files)
        {
            for (var i = 0; i < file.Lines.Length; i++)
            {
                if (!file.Lines[i].Contains(symbol, StringComparison.Ordinal))
                {
                    continue;
                }

                hits.Add(new SourceSymbolHit
                {
                    Symbol = symbol,
                    FilePath = file.RelativePath,
                    LineNumber = i + 1,
                    Line = file.Lines[i].Trim()
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

    public bool SymbolExists(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return false;
        }

        var index = _index.Value;
        if (index.TypeHits.ContainsKey(symbol))
        {
            return true;
        }

        return FindSymbol(symbol, 1).Count > 0;
    }

    public bool VerifyDocClaim(string apiName) => SymbolExists(apiName);

    public void Warmup() => _ = _index.Value;

    private SourceSymbolIndex BuildIndex()
    {
        var files = new List<IndexedSourceFile>();
        var typeHits = new Dictionary<string, List<SourceSymbolHit>>(StringComparer.Ordinal);

        var srcRoot = _paths.SourcePath;
        if (!Directory.Exists(srcRoot))
        {
            return new SourceSymbolIndex(files, typeHits);
        }

        foreach (var file in Directory.EnumerateFiles(srcRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
                file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(_paths.RepoRoot, file).Replace('\\', '/');
            var lines = File.ReadAllLines(file);
            files.Add(new IndexedSourceFile(relativePath, lines));

            for (var i = 0; i < lines.Length; i++)
            {
                var typeMatch = TypeDeclaration().Match(lines[i]);
                if (!typeMatch.Success)
                {
                    continue;
                }

                var typeName = typeMatch.Groups[2].Value;
                if (!typeHits.TryGetValue(typeName, out var hits))
                {
                    hits = [];
                    typeHits[typeName] = hits;
                }

                hits.Add(new SourceSymbolHit
                {
                    Symbol = typeName,
                    FilePath = relativePath,
                    LineNumber = i + 1,
                    Line = lines[i].Trim()
                });
            }
        }

        return new SourceSymbolIndex(files, typeHits);
    }

    [GeneratedRegex(@"\b(class|interface|record|struct)\s+(\w+)", RegexOptions.Compiled)]
    private static partial Regex TypeDeclaration();

    private sealed record IndexedSourceFile(string RelativePath, string[] Lines);

    private sealed record SourceSymbolIndex(
        IReadOnlyList<IndexedSourceFile> Files,
        Dictionary<string, List<SourceSymbolHit>> TypeHits);
}
