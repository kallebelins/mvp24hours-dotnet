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
            1. resolve_architecture — pick template and reference sample
            2. get_architecture_template — read layer boundaries and doc links
            3. suggest_project_structure — generate solution tree with product name
            4. get_test_scaffold — add CustomerApiFactory and OpenApiSmokeTests
            5. get_readme_scaffold — generate sample README
            6. run_compliance_check — validate against Mvp24Hours checklist

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
            1. run_compliance_check on provided paths
            2. verify_doc_claim for any Mvp24Hours APIs used
            3. find_tests_for_module for modules under test
            4. get_doc compliance-checklist.md for full rule set

            Source and src/Tests/ override documentation when they conflict.
            """;
    }
}
