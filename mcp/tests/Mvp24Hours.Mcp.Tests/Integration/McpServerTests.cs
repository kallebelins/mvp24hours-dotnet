using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Mvp24Hours.Mcp.Tests.Integration;

public class McpServerTests : McpTestFixture
{
    [Fact]
    public async Task ListTools_includes_all_devkit_tools()
    {
        var repoRoot = CreatePaths().RepoRoot;
        var projectPath = Path.GetFullPath(Path.Combine(repoRoot, "mcp", "src", "Mvp24Hours.Mcp", "Mvp24Hours.Mcp.csproj"));

        await using var client = await ConnectClientAsync(repoRoot, projectPath);
        var tools = await client.ListToolsAsync();

        var expected = new[]
        {
            "search_docs",
            "get_doc",
            "list_samples",
            "get_sample_tree",
            "get_sample_file",
            "resolve_architecture",
            "get_architecture_template",
            "list_layers",
            "suggest_project_structure",
            "get_test_scaffold",
            "get_readme_scaffold",
            "get_di_registration_hints",
            "find_source_symbol",
            "find_tests_for_module",
            "run_compliance_check",
            "verify_doc_claim"
        };

        foreach (var name in expected)
        {
            Assert.Contains(tools, t => t.Name == name);
        }
    }

    [Fact]
    public async Task GetArchitectureTemplate_tool_returns_simple_nlayers_content()
    {
        var repoRoot = CreatePaths().RepoRoot;
        var projectPath = Path.GetFullPath(Path.Combine(repoRoot, "mcp", "src", "Mvp24Hours.Mcp", "Mvp24Hours.Mcp.csproj"));

        await using var client = await ConnectClientAsync(repoRoot, projectPath);
        var result = await client.CallToolAsync(
            "get_architecture_template",
            new Dictionary<string, object?> { ["templateId"] = "simple-nlayers" });

        var text = result.Content.OfType<TextContentBlock>().First().Text ?? string.Empty;
        Assert.Contains("Simple N-Layers", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Manifest_resource_is_readable()
    {
        var repoRoot = CreatePaths().RepoRoot;
        var projectPath = Path.GetFullPath(Path.Combine(repoRoot, "mcp", "src", "Mvp24Hours.Mcp", "Mvp24Hours.Mcp.csproj"));

        await using var client = await ConnectClientAsync(repoRoot, projectPath);
        var resource = await client.ReadResourceAsync("mvp24hours://manifest");

        var text = resource.Contents.OfType<TextResourceContents>().First().Text ?? string.Empty;
        Assert.Contains("minimal-api", text, StringComparison.Ordinal);
        Assert.Contains("simple-nlayers", text, StringComparison.Ordinal);
    }

    private static async Task<McpClient> ConnectClientAsync(string repoRoot, string projectPath)
    {
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "mvp24hours-test",
            Command = "dotnet",
            Arguments =
            [
                "run",
                "--project", projectPath,
                "--configuration", "Release",
                "--no-build"
            ],
            EnvironmentVariables = new Dictionary<string, string>
            {
                ["MVP24HOURS_REPO_ROOT"] = repoRoot
            }
        });

        return await McpClient.CreateAsync(transport);
    }
}
