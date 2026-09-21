using Mvp24Hours.Mcp.Indexing;

namespace Mvp24Hours.Mcp.Tests.Indexing;

public class ComplianceServiceTests : McpTestFixture
{
    [Fact]
    public void CheckPaths_detects_mediatr_violation_in_temp_file()
    {
        var paths = CreatePaths();
        var manifest = new ManifestService(paths);
        var compliance = new ComplianceService(paths, manifest);

        var tempDir = Path.Combine(paths.RepoRoot, "mcp", "tests", ".compliance-temp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var appDir = Path.Combine(tempDir, "Product.Application");
        Directory.CreateDirectory(appDir);

        var csFile = Path.Combine(appDir, "Handlers.cs");
        File.WriteAllText(csFile, "using MediatR; public class Handler { }");

        var csproj = Path.Combine(appDir, "Product.Application.csproj");
        File.WriteAllText(csproj, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);

        try
        {
            var relDir = Path.GetRelativePath(paths.RepoRoot, tempDir).Replace('\\', '/');
            var result = compliance.CheckPaths([relDir]);

            Assert.False(result.Passed);
            Assert.Contains(result.Violations, v =>
                v.Rule.Contains("MediatR", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void CheckPaths_passes_for_valid_net10_csproj()
    {
        var paths = CreatePaths();
        var manifest = new ManifestService(paths);
        var compliance = new ComplianceService(paths, manifest);

        var samplePath = "samples/src/minimal-crud-ef-customer-api/CustomerAPI";
        var result = compliance.CheckPaths([samplePath]);

        Assert.DoesNotContain(result.Violations, v =>
            v.Rule.Contains("MediatR", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CheckPaths_does_not_flag_missing_nullable_when_directory_build_props_enables_it()
    {
        var paths = CreatePaths();
        var manifest = new ManifestService(paths);
        var compliance = new ComplianceService(paths, manifest);

        var tempDir = Path.Combine(paths.RepoRoot, "mcp", "tests", ".compliance-temp-" + Guid.NewGuid().ToString("N"));
        var projectDir = Path.Combine(tempDir, "Product.Core");
        Directory.CreateDirectory(projectDir);

        // Directory.Build.props lives one level above the .csproj, like src/Directory.Build.props does.
        File.WriteAllText(Path.Combine(tempDir, "Directory.Build.props"), """
            <Project>
              <PropertyGroup>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);

        File.WriteAllText(Path.Combine(projectDir, "Product.Core.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        try
        {
            var relDir = Path.GetRelativePath(paths.RepoRoot, tempDir).Replace('\\', '/');
            var result = compliance.CheckPaths([relDir]);

            Assert.DoesNotContain(result.Violations, v =>
                v.Rule.Contains("Nullable", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void CheckPaths_flags_missing_nullable_when_no_directory_build_props_enables_it()
    {
        var paths = CreatePaths();
        var manifest = new ManifestService(paths);
        var compliance = new ComplianceService(paths, manifest);

        var tempDir = Path.Combine(paths.RepoRoot, "mcp", "tests", ".compliance-temp-" + Guid.NewGuid().ToString("N"));
        var projectDir = Path.Combine(tempDir, "Product.Core");
        Directory.CreateDirectory(projectDir);

        File.WriteAllText(Path.Combine(projectDir, "Product.Core.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        try
        {
            var relDir = Path.GetRelativePath(paths.RepoRoot, tempDir).Replace('\\', '/');
            var result = compliance.CheckPaths([relDir]);

            Assert.Contains(result.Violations, v =>
                v.Rule.Contains("Nullable", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
