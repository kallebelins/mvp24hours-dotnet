namespace Mvp24Hours.Mcp.Models;

public sealed class TemplatesManifest
{
    public string Version { get; set; } = "1.0.0";

    public List<string> TestTemplates { get; set; } = [];

    public Dictionary<string, string> LayerDocs { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string ComplianceChecklist { get; set; } = string.Empty;

    public List<ArchitectureTemplate> Templates { get; set; } = [];
}

public sealed class ArchitectureTemplate
{
    public required string Id { get; set; }

    public required string Tier { get; set; }

    public required string Shape { get; set; }

    public required string DocPath { get; set; }

    public List<LayerDefinition> Layers { get; set; } = [];

    public required string ReferenceSample { get; set; }

    public List<string> TestTemplates { get; set; } = [];

    public List<string> RelatedDocs { get; set; } = [];

    public List<string> ComplianceRules { get; set; } = [];
}

public sealed class LayerDefinition
{
    public required string Name { get; set; }

    public string? Pattern { get; set; }

    public List<string> DependsOn { get; set; } = [];

    public List<string> Responsibilities { get; set; } = [];
}

public sealed class SampleEntry
{
    public required string Id { get; set; }

    public required string Path { get; set; }

    public string Tier { get; set; } = "Unknown";

    public string Status { get; set; } = "Migrated";

    public string? ReadmePath { get; set; }
}

public sealed class DocSearchHit
{
    public required string RelativePath { get; set; }

    public required string Title { get; set; }

    public required string Snippet { get; set; }

    public int Score { get; set; }
}

public sealed class SourceSymbolHit
{
    public required string Symbol { get; set; }

    public required string FilePath { get; set; }

    public int LineNumber { get; set; }

    public required string Line { get; set; }
}

public sealed class ComplianceViolation
{
    public required string Rule { get; set; }

    public required string File { get; set; }

    public string? Detail { get; set; }
}

public sealed class ComplianceCheckResult
{
    public bool Passed { get; set; }

    public List<ComplianceViolation> Violations { get; set; } = [];

    public List<string> CheckedRules { get; set; } = [];
}

public sealed class ArchitectureResolution
{
    public required string TemplateId { get; set; }

    public required string Tier { get; set; }

    public required string DocPath { get; set; }

    public required string ReferenceSample { get; set; }

    public required string Rationale { get; set; }

    public List<string> RelatedDocs { get; set; } = [];
}
