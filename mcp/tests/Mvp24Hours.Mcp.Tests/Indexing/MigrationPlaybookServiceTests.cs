using Mvp24Hours.Mcp.Indexing;

namespace Mvp24Hours.Mcp.Tests.Indexing;

public class MigrationPlaybookServiceTests : McpTestFixture
{
    [Fact]
    public void PlanMigration_simple_to_complex_returns_layer_changes()
    {
        var paths = CreatePaths();
        var templates = new ManifestService(paths);
        var service = new MigrationPlaybookService(paths, templates);
        service.Warmup();

        var plan = service.PlanMigration("simple-nlayers", "complex-nlayers");
        Assert.NotNull(plan);
        Assert.Equal("simple-to-complex-nlayers", plan!.PlaybookId);
        Assert.Contains(plan.LayerChanges, c => c.ChangeType == "Add" && c.LayerName == "Application");
    }

    [Fact]
    public void GetPlaybook_returns_legacy_migration()
    {
        var paths = CreatePaths();
        var templates = new ManifestService(paths);
        var service = new MigrationPlaybookService(paths, templates);

        var playbook = service.GetPlaybook("legacy-to-native-apis");
        Assert.NotNull(playbook);
        Assert.NotEmpty(playbook!.Steps);
    }
}
