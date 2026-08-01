using ModelContextProtocol.Server;

namespace Mvp24Hours.Mcp.Prompts;

[McpServerPromptType]
public static class DevKitPrompts
{
    [McpServerPrompt, Description("Workflow to scaffold a new Mvp24Hours API from architecture constraints.")]
    public static string NewMvp24HoursApi(
        [Description("Describe the application constraints")] string situation = "small CRUD API")
    {
        return $"""
            Create a new Mvp24Hours .NET 10 solution for: {situation}

            Follow this workflow using MCP tools:
            1. get_scenario_playbook — scenarioId greenfield-api
            2. resolve_architecture — pick template and reference sample
            3. get_architecture_template — read layer boundaries and doc links
            4. suggest_project_structure — generate solution tree with product name
            5. get_test_scaffold — add CustomerApiFactory and OpenApiSmokeTests
            6. get_readme_scaffold — generate sample README
            7. run_compliance_check — validate against Mvp24Hours checklist (scenarioId greenfield-api)

            Rules: use Mvp24Hours Mediator (not MediatR), net10.0, Program.cs composition,
            native OpenAPI, OpenTelemetry, TimeProvider, and verify APIs against src/.
            """;
    }

    [McpServerPrompt, Description("Add integration smoke tests following sample templates.")]
    public static string AddSmokeTests(
        [Description("Host project name")] string hostProject = "CustomerAPI.WebAPI",
        [Description("DbContext type")] string dbContext = "EFDBContext")
    {
        return $"""
            Add Mvp24Hours sample smoke tests to {hostProject}:
            1. get_test_scaffold with templateFile SAMPLE_TEST_CustomerApiFactory.cs.template
            2. get_test_scaffold with templateFile SAMPLE_TEST_OpenApiSmokeTests.cs.template
            3. Ensure host declares a partial Program class for WebApplicationFactory
            4. Use Testing environment and EF Core InMemory with DbContext={dbContext}
            5. OpenAPI test: GET /openapi/v1.json returns status < 500
            Reference: samples/src/complex-crud-ef-customer-api/CustomerAPI.Test
            """;
    }

    [McpServerPrompt, Description("Review a solution against Mvp24Hours compliance checklist.")]
    public static string ReviewMvp24HoursSolution(
        [Description("Comma-separated paths to review")] string paths = "samples/src")
    {
        return $"""
            Review Mvp24Hours solution at: {paths}

            Use MCP tools:
            1. get_scenario_playbook — scenarioId review-solution
            2. run_compliance_check on provided paths
            3. verify_doc_claim for any Mvp24Hours APIs used
            4. find_tests_for_module for modules under test
            5. get_doc compliance-checklist.md for full rule set

            Source and src/Tests/ override documentation when they conflict.
            """;
    }

    [McpServerPrompt, Description("Migrate between Mvp24Hours architecture templates.")]
    public static string MigrateArchitecture(
        [Description("Source template id")] string sourceTemplate = "simple-nlayers",
        [Description("Target template id")] string targetTemplate = "complex-nlayers",
        [Description("Product name")] string productName = "CustomerAPI",
        [Description("Comma-separated paths to migrate")] string paths = "")
    {
        return $"""
            Migrate Mvp24Hours solution from {sourceTemplate} to {targetTemplate} for {productName}.
            Paths: {(string.IsNullOrWhiteSpace(paths) ? "(entire workspace)" : paths)}

            Workflow:
            1. get_scenario_playbook — scenarioId architecture-migration
            2. plan_architecture_migration — sourceTemplateId={sourceTemplate}, targetTemplateId={targetTemplate}
            3. get_migration_playbook — use playbookId from plan if available
            4. get_sample_tree + get_sample_file — compare source and target reference samples
            5. get_di_registration_hints — from target reference sample
            6. run_compliance_check — templateId={targetTemplate}, scenarioId=architecture-migration

            Preserve business behavior. Apply layer boundaries from target template.
            Verify every Mvp24Hours API with verify_doc_claim.
            """;
    }

    [McpServerPrompt, Description("Port external codebase to Mvp24Hours via discovery (any source language).")]
    public static string PortToMvp24Hours(
        [Description("Summary of the system discovered from source code")] string situation = "CRUD API with relational database",
        [Description("Comma-separated workspace paths to source code")] string sourcePaths = "")
    {
        return $"""
            Port external codebase to Mvp24Hours .NET 10.
            Situation: {situation}
            Source paths: {(string.IsNullOrWhiteSpace(sourcePaths) ? "(user must provide)" : sourcePaths)}

            Phase A — Discovery (before MCP):
            Read source code at sourcePaths. Extract entities, endpoints, use cases, persistence, messaging, auth.

            Phase B — Mvp24Hours mapping (MCP tools):
            1. get_discovery_playbook
            2. get_scenario_playbook — scenarioId port-to-mvp24hours
            3. resolve_architecture — from discovery summary
            4. get_architecture_template + list_layers — map concepts to layers
            5. search_sample_patterns + get_sample_file — concrete reference implementations
            6. get_di_registration_hints — Program.cs wiring
            7. suggest_project_structure — target solution tree
            8. verify_doc_claim — confirm each Mvp24Hours API in src/
            9. run_compliance_check — scenarioId=port-to-mvp24hours

            Do NOT use language-specific maps. Infer structure from source code.
            Use Mvp24Hours Mediator (not MediatR), net10.0, native OpenAPI, TimeProvider.
            """;
    }

    [McpServerPrompt, Description("Add a capability or feature to an existing Mvp24Hours solution.")]
    public static string AddMvp24HoursFeature(
        [Description("Feature keyword, e.g. cqrs, rabbitmq, keycloak")] string feature = "cqrs",
        [Description("Current architecture template id")] string templateId = "complex-nlayers",
        [Description("Product name")] string productName = "CustomerAPI")
    {
        return $"""
            Add feature '{feature}' to {productName} (template: {templateId}).

            Workflow:
            1. get_scenario_playbook — scenarioId add-feature
            2. resolve_feature — featureKeyword={feature}, templateId={templateId}
            3. get_sample_tree + get_sample_file — from reference sample in resolve_feature result
            4. search_sample_patterns — find registration and handler patterns
            5. get_di_registration_hints — from reference sample
            6. verify_doc_claim — for each Mvp24Hours API used
            7. run_compliance_check — templateId={templateId}, scenarioId=add-feature

            Match existing project conventions. Do not introduce MediatR or Startup.cs.
            """;
    }

    [McpServerPrompt, Description("Migrate legacy Mvp24Hours implementations to native .NET APIs.")]
    public static string MigrateLegacyMvp24Hours(
        [Description("Comma-separated paths to review")] string paths = "samples/src",
        [Description("Focus area: telemetry, openapi, mediator, cache, resilience, or all")] string focus = "all")
    {
        return $"""
            Migrate legacy Mvp24Hours code to native .NET 9/10 APIs.
            Paths: {paths}
            Focus: {focus}

            Workflow:
            1. get_scenario_playbook — scenarioId legacy-migration
            2. get_migration_playbook — playbookId=legacy-to-native-apis
            3. get_doc — modernization/migration-guide.md
            4. search_docs — query related to focus area
            5. search_sample_patterns — find native replacement patterns in samples
            6. verify_doc_claim — confirm native APIs exist in src/
            7. run_compliance_check — scenarioId=legacy-migration

            Replace TelemetryHelper, Swashbuckle, MultiLevelCache, custom resilience with native APIs.
            """;
    }

    [McpServerPrompt, Description("Upgrade SDK and packages to .NET 10.")]
    public static string UpgradeNet10Package(
        [Description("Comma-separated paths to review")] string paths = "samples/src")
    {
        return $"""
            Upgrade Mvp24Hours solution to .NET 10.
            Paths: {paths}

            Workflow:
            1. get_scenario_playbook — scenarioId upgrade-net10
            2. get_migration_playbook — playbookId=package-9-to-10
            3. get_doc — migration.md
            4. run_compliance_check — verify net10.0, nullable, Program.cs patterns
            5. verify_doc_claim — for package APIs referenced after upgrade

            Target net10.0 with nullable reference types enabled.
            """;
    }
}
