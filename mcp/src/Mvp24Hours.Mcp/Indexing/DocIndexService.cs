using System.Text.RegularExpressions;
using Mvp24Hours.Mcp.Configuration;
using Mvp24Hours.Mcp.Models;

namespace Mvp24Hours.Mcp.Indexing;

public sealed partial class DocIndexService
{
    private readonly RepoRootResolver _paths;
    private readonly Lazy<IReadOnlyList<IndexedDoc>> _index;

    public DocIndexService(RepoRootResolver paths)
    {
        _paths = paths;
        _index = new Lazy<IReadOnlyList<IndexedDoc>>(BuildIndex);
    }

    public IReadOnlyList<DocSearchHit> Search(string query, int maxResults = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var hits = new List<DocSearchHit>();

        foreach (var doc in _index.Value)
        {
            var score = ScoreDocument(doc, terms);
            if (score <= 0)
            {
                continue;
            }

            hits.Add(new DocSearchHit
            {
                RelativePath = doc.RelativePath,
                Title = doc.Title,
                Snippet = ExtractSnippet(doc.Content, terms),
                Score = score
            });
        }

        return hits
            .OrderByDescending(h => h.Score)
            .ThenBy(h => h.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Take(maxResults)
            .ToList();
    }

    public string? GetDoc(string relativePath)
    {
        var normalized = NormalizeDocPath(relativePath);
        var fromIndex = _index.Value.FirstOrDefault(d =>
            string.Equals(d.RelativePath, normalized, StringComparison.OrdinalIgnoreCase));
        if (fromIndex is not null)
        {
            return fromIndex.Content;
        }

        var full = Path.Combine(_paths.DocsEnUsPath, normalized.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(full))
        {
            return null;
        }

        EnsureWithinDocs(full);
        return File.ReadAllText(full);
    }

    public string? GetDocByRepoPath(string repoRelativePath)
    {
        var normalized = repoRelativePath.Replace('\\', '/').TrimStart('/');
        if (normalized.StartsWith("docs/en-us/", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized["docs/en-us/".Length..];
        }

        return GetDoc(normalized);
    }

    public void Warmup() => _ = _index.Value;

    private IReadOnlyList<IndexedDoc> BuildIndex()
    {
        var docs = new List<IndexedDoc>();
        if (!Directory.Exists(_paths.DocsEnUsPath))
        {
            return docs;
        }

        foreach (var file in Directory.EnumerateFiles(_paths.DocsEnUsPath, "*.md", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(_paths.DocsEnUsPath, file).Replace('\\', '/');
            var content = File.ReadAllText(file);
            var title = ExtractTitle(content) ?? relative;
            docs.Add(new IndexedDoc(relative, title, content, GetSectionBoost(relative)));
        }

        return docs;
    }

    private static int ScoreDocument(IndexedDoc doc, string[] terms)
    {
        var score = 0;
        foreach (var term in terms)
        {
            if (doc.Title.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += 10 * doc.SectionBoost;
            }

            var count = CountOccurrences(doc.Content, term);
            score += count * doc.SectionBoost;
        }

        return score;
    }

    private static int CountOccurrences(string text, string term)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(term, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            index += term.Length;
        }

        return count;
    }

    private static int GetSectionBoost(string relativePath)
    {
        if (relativePath.StartsWith("ai-resources/", StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        if (relativePath.StartsWith("guides/architecture/", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (relativePath.Contains("testing", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        return 1;
    }

    private static string ExtractSnippet(string content, string[] terms)
    {
        var firstTerm = terms[0];
        var index = content.IndexOf(firstTerm, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return content.Length <= 200 ? content : content[..200] + "...";
        }

        var start = Math.Max(0, index - 80);
        var length = Math.Min(content.Length - start, 200);
        var snippet = content.Substring(start, length).Replace('\n', ' ').Trim();
        return snippet.Length >= length ? snippet + "..." : snippet;
    }

    private static string? ExtractTitle(string content)
    {
        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();
            var match = TitleLine().Match(trimmed);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            if (trimmed.StartsWith("---"))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                break;
            }
        }

        return null;
    }

    private static string NormalizeDocPath(string relativePath)
    {
        var path = relativePath.Replace('\\', '/').TrimStart('/');
        if (path.StartsWith("docs/en-us/", StringComparison.OrdinalIgnoreCase))
        {
            path = path["docs/en-us/".Length..];
        }

        return path;
    }

    private void EnsureWithinDocs(string fullPath)
    {
        var docsRoot = Path.GetFullPath(_paths.DocsEnUsPath);
        var normalized = Path.GetFullPath(fullPath);
        if (!normalized.StartsWith(docsRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Doc path is outside docs/en-us.");
        }
    }

    [GeneratedRegex(@"^#+\s*(.+)$")]
    private static partial Regex TitleLine();

    private sealed record IndexedDoc(string RelativePath, string Title, string Content, int SectionBoost);
}
