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

public sealed class ScenariosManifest
{
    public string Version { get; set; } = "1.0.0";

    public string DiscoveryPlaybook { get; set; } = string.Empty;

    public List<ScenarioDefinition> Scenarios { get; set; } = [];
}

public sealed class ScenarioDefinition
{
    public required string Id { get; set; }

    public required string Title { get; set; }

    public required string Prompt { get; set; }

    public bool DiscoveryRequired { get; set; }

    public List<string> Inputs { get; set; } = [];

    public List<ScenarioStep> Steps { get; set; } = [];
}

public sealed class ScenarioStep
{
    public int Order { get; set; }

    public required string Title { get; set; }

    public required string Tool { get; set; }

    public Dictionary<string, string> Args { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class CapabilitiesManifest
{
    public string Version { get; set; } = "1.0.0";

    public List<CapabilityDefinition> Capabilities { get; set; } = [];
}

public sealed class CapabilityDefinition
{
    public required string Id { get; set; }

    public List<string> Keywords { get; set; } = [];

    public required string DocPath { get; set; }

    public required string ReferenceSample { get; set; }

    public string? TemplateId { get; set; }

    public List<string> Symbols { get; set; } = [];

    public List<string> RelatedDocs { get; set; } = [];

    public List<string> ComplianceRules { get; set; } = [];
}

public sealed class CapabilityResolution
{
    public required string CapabilityId { get; set; }

    public required string DocPath { get; set; }

    public required string ReferenceSample { get; set; }

    public string? TemplateId { get; set; }

    public List<string> Symbols { get; set; } = [];

    public List<string> RelatedDocs { get; set; } = [];

    public List<string> ComplianceRules { get; set; } = [];

    public required string Rationale { get; set; }
}

public sealed class MigrationPlaybooksManifest
{
    public string Version { get; set; } = "1.0.0";

    public List<MigrationPlaybook> Playbooks { get; set; } = [];

    public Dictionary<string, string?> TemplatePairs { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class MigrationPlaybook
{
    public required string Id { get; set; }

    public required string Title { get; set; }

    public string? SourcePattern { get; set; }

    public string? TargetPattern { get; set; }

    public required string DocPath { get; set; }

    public string? ReferenceSample { get; set; }

    public string? SourceSample { get; set; }

    public string? TargetSample { get; set; }

    public string? TemplateId { get; set; }

    public string? SourceTemplateId { get; set; }

    public string? TargetTemplateId { get; set; }

    public List<MigrationPlaybookStep> Steps { get; set; } = [];
}

public sealed class MigrationPlaybookStep
{
    public int Order { get; set; }

    public required string Title { get; set; }

    public required string Tool { get; set; }

    public string? DocPath { get; set; }

    public string? Pattern { get; set; }

    public string? Query { get; set; }

    public string? Symbol { get; set; }

    public string? ApiName { get; set; }

    public string? SampleId { get; set; }

    public string? TemplateId { get; set; }

    public string? SourceTemplateId { get; set; }

    public string? TargetTemplateId { get; set; }

    public string? FeatureKeyword { get; set; }
}

public sealed class ArchitectureMigrationPlan
{
    public required string SourceTemplateId { get; set; }

    public required string TargetTemplateId { get; set; }

    public string? PlaybookId { get; set; }

    public required string SourceSample { get; set; }

    public required string TargetSample { get; set; }

    public List<LayerMigrationChange> LayerChanges { get; set; } = [];

    public List<string> Rationale { get; set; } = [];

    public List<MigrationPlaybookStep> Steps { get; set; } = [];
}

public sealed class LayerMigrationChange
{
    public required string ChangeType { get; set; }

    public required string LayerName { get; set; }

    public string? Pattern { get; set; }

    public List<string> Responsibilities { get; set; } = [];
}

public sealed class SamplePatternHit
{
    public required string SampleId { get; set; }

    public required string FilePath { get; set; }

    public int LineNumber { get; set; }

    public required string Line { get; set; }
}
