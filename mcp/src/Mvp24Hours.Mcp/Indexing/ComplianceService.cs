using System.Text.RegularExpressions;
using Mvp24Hours.Mcp.Configuration;
using Mvp24Hours.Mcp.Models;

namespace Mvp24Hours.Mcp.Indexing;

public sealed partial class ComplianceService
{
    private readonly RepoRootResolver _paths;
    private readonly ManifestService _manifest;
    private readonly Lazy<IReadOnlyList<string>> _checklistRules;

    public ComplianceService(RepoRootResolver paths, ManifestService manifest)
    {
        _paths = paths;
        _manifest = manifest;
        _checklistRules = new Lazy<IReadOnlyList<string>>(LoadChecklistRules);
    }

    public ComplianceCheckResult CheckPaths(IEnumerable<string> repoRelativePaths, string? templateId = null)
    {
        var result = new ComplianceCheckResult { Passed = true };
        var rules = _checklistRules.Value.ToList();

        if (!string.IsNullOrWhiteSpace(templateId))
        {
            var template = _manifest.GetTemplate(templateId);
            if (template is not null)
            {
                rules.AddRange(template.ComplianceRules);
            }
        }

        result.CheckedRules = rules.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        foreach (var relative in repoRelativePaths)
        {
            var full = _paths.ResolveRepoRelative(relative);
            if (File.Exists(full))
            {
                CheckFile(full, relative, rules, result);
            }
            else if (Directory.Exists(full))
            {
                foreach (var file in Directory.EnumerateFiles(full, "*.*", SearchOption.AllDirectories)
                             .Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                                         f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)))
                {
                    var rel = Path.GetRelativePath(_paths.RepoRoot, file).Replace('\\', '/');
                    CheckFile(file, rel, rules, result);
                }
            }
        }

        result.Passed = result.Violations.Count == 0;
        return result;
    }

    private void CheckFile(string fullPath, string relativePath, List<string> rules, ComplianceCheckResult result)
    {
        if (!fullPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) &&
            !fullPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var content = File.ReadAllText(fullPath);

        if (content.Contains("Startup.cs", StringComparison.Ordinal) &&
            relativePath.EndsWith("Startup.cs", StringComparison.OrdinalIgnoreCase))
        {
            AddViolation(result, "No legacy Startup.cs", relativePath, "Legacy Startup.cs detected.");
        }

        if (content.Contains("MediatR", StringComparison.Ordinal))
        {
            AddViolation(result, "Use Mvp24Hours Mediator not MediatR", relativePath, "MediatR reference found.");
        }

        if (content.Contains("BuildServiceProvider()", StringComparison.Ordinal))
        {
            AddViolation(result, "Do not call BuildServiceProvider during registration", relativePath);
        }

        if (content.Contains("new HttpClient()", StringComparison.Ordinal))
        {
            AddViolation(result, "Do not instantiate HttpClient directly", relativePath);
        }

        if (fullPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) &&
            !content.Contains("<Nullable>enable</Nullable>", StringComparison.Ordinal) &&
            !content.Contains("<Nullable>enable</Nullable>", StringComparison.OrdinalIgnoreCase))
        {
            AddViolation(result, "Nullable reference types enabled", relativePath, "Missing <Nullable>enable</Nullable>.");
        }

        if (relativePath.Contains(".Application.", StringComparison.OrdinalIgnoreCase) ||
            relativePath.Contains("/Application/", StringComparison.OrdinalIgnoreCase))
        {
            if (ProjectReferencePattern().IsMatch(content) &&
                (content.Contains(".Infrastructure", StringComparison.Ordinal) ||
                 content.Contains(".WebAPI", StringComparison.Ordinal)))
            {
                AddViolation(result, "Application must not reference Infrastructure or WebAPI", relativePath);
            }
        }
    }

    public void Warmup() => _ = _checklistRules.Value;

    private IReadOnlyList<string> LoadChecklistRules()
    {
        var path = _paths.ComplianceChecklistPath;
        if (!File.Exists(path))
        {
            return [];
        }

        var lines = File.ReadAllLines(path);
        return lines
            .Where(l => l.TrimStart().StartsWith("- [ ]", StringComparison.Ordinal))
            .Select(l => l[(l.IndexOf(']') + 1)..].Trim())
            .ToList();
    }

    private static void AddViolation(
        ComplianceCheckResult result,
        string rule,
        string file,
        string? detail = null)
    {
        result.Violations.Add(new ComplianceViolation
        {
            Rule = rule,
            File = file,
            Detail = detail
        });
    }

    [GeneratedRegex(@"<ProjectReference\s+Include=""([^""]+)""", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex ProjectReferencePattern();
}
