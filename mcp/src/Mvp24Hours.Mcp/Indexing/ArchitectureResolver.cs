using Mvp24Hours.Mcp.Indexing;
using Mvp24Hours.Mcp.Models;

namespace Mvp24Hours.Mcp.Indexing;

public sealed class ArchitectureResolver
{
    private readonly ManifestService _manifest;

    public ArchitectureResolver(ManifestService manifest)
    {
        _manifest = manifest;
    }

    public ArchitectureResolution Resolve(string situation, string? teamSize = null, bool messaging = false, bool cqrs = false)
    {
        var text = situation.ToLowerInvariant();

        if (text.Contains("microservice") || text.Contains("independent deploy") || text.Contains("aspire"))
        {
            return ResolveTemplate("microservices", "Independent deployment or multi-service orchestration.");
        }

        if (cqrs || text.Contains("cqrs") || text.Contains("command") && text.Contains("query"))
        {
            return ResolveTemplate("cqrs", "Different read/write models or mediator pipeline behaviors.");
        }

        if (text.Contains("ddd") || text.Contains("aggregate") || text.Contains("bounded context"))
        {
            return ResolveTemplate("ddd", "Rich domain language and invariants.");
        }

        if (text.Contains("clean arch") || text.Contains("inward dependency"))
        {
            return ResolveTemplate("clean-architecture", "Strict framework independence and inward dependency rule.");
        }

        if (text.Contains("hexagonal") || text.Contains("port") || text.Contains("adapter"))
        {
            return ResolveTemplate("hexagonal", "Replaceable external adapters and explicit ports.");
        }

        if (messaging || text.Contains("rabbit") || text.Contains("event-driven") || text.Contains("outbox"))
        {
            return ResolveTemplate("event-driven", "Asynchronous integration with eventual consistency.");
        }

        if (text.Contains("modular monolith") || text.Contains("enterprise") || text.Contains("dto"))
        {
            return ResolveTemplate("complex-nlayers", "Modular monolith with DTO boundaries and application layer.");
        }

        if (text.Contains("small") || text.Contains("minimal") || text.Contains("single host"))
        {
            return ResolveTemplate("minimal-api", "Small cohesive HTTP service with minimal ceremony.");
        }

        if (text.Contains("crud") || text.Contains("conventional") || text.Contains("simple"))
        {
            return ResolveTemplate("simple-nlayers", "Conventional business application with clear layer separation.");
        }

        if (!string.IsNullOrWhiteSpace(teamSize) && int.TryParse(teamSize, out var size) && size > 1)
        {
            return ResolveTemplate("complex-nlayers", "Multiple contributors benefit from stronger module boundaries.");
        }

        return ResolveTemplate("simple-nlayers", "Default starting point for conventional applications.");
    }

    private ArchitectureResolution ResolveTemplate(string id, string rationale)
    {
        var template = _manifest.GetTemplate(id)
            ?? throw new KeyNotFoundException($"Template '{id}' not found in manifest.");

        return new ArchitectureResolution
        {
            TemplateId = template.Id,
            Tier = template.Tier,
            DocPath = template.DocPath,
            ReferenceSample = template.ReferenceSample,
            Rationale = rationale,
            RelatedDocs = template.RelatedDocs
        };
    }
}
