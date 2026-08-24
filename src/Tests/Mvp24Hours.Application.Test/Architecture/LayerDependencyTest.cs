using System.Xml.Linq;

namespace Mvp24Hours.Application.Test.Architecture;

/// <summary>
/// Architectural guard tests ensuring the Application layer does not reference
/// Infrastructure projects directly, preserving Clean Architecture dependency rules.
/// </summary>
[Trait("Category", "Architecture")]
public class LayerDependencyTest
{
    private static readonly string ApplicationCsprojPath = GetApplicationCsprojPath();

    [Fact]
    public void Application_ShouldNotReference_InfrastructureDataEFCore()
    {
        XDocument csproj = XDocument.Load(ApplicationCsprojPath);

        bool hasEfCoreProjectRef = csproj.Descendants("ProjectReference")
            .Any(pr => pr.Attribute("Include")?.Value
                .Contains("Infrastructure.Data.EFCore", StringComparison.OrdinalIgnoreCase) == true);

        hasEfCoreProjectRef.Should().BeFalse(
            "Application layer must not reference Infrastructure.Data.EFCore directly. " +
            "Use IBulkOperationsAsync<T> or IUnitOfWorkAsync abstractions from Core instead.");
    }

    [Fact]
    public void Application_ShouldNotReference_InfrastructureBase()
    {
        XDocument csproj = XDocument.Load(ApplicationCsprojPath);

        bool hasInfraProjectRef = csproj.Descendants("ProjectReference")
            .Any(pr =>
            {
                string? include = pr.Attribute("Include")?.Value;
                if (include == null) return false;
                // Match "Mvp24Hours.Infrastructure.csproj" but NOT "Mvp24Hours.Infrastructure.Data.*" etc.
                string fileName = Path.GetFileName(include);
                return fileName.Equals("Mvp24Hours.Infrastructure.csproj", StringComparison.OrdinalIgnoreCase);
            });

        hasInfraProjectRef.Should().BeFalse(
            "Application layer must not reference the base Infrastructure project directly. " +
            "Extract needed contracts to Core or compose at the host level.");
    }

    [Fact]
    public void Application_ShouldNotHavePackageReference_MicrosoftEntityFrameworkCore()
    {
        XDocument csproj = XDocument.Load(ApplicationCsprojPath);

        bool hasEfCorePackage = csproj.Descendants("PackageReference")
            .Any(pr => pr.Attribute("Include")?.Value
                .Equals("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase) == true);

        hasEfCorePackage.Should().BeFalse(
            "Application layer must not depend on Microsoft.EntityFrameworkCore NuGet package. " +
            "EF Core concerns belong in the Infrastructure.Data.EFCore project.");
    }

    private static string GetApplicationCsprojPath()
    {
        // Walk up from the test assembly output directory to find the solution root
        string? directory = AppContext.BaseDirectory;
        while (directory != null)
        {
            string candidate = Path.Combine(directory, "src", "Mvp24Hours.Application", "Mvp24Hours.Application.csproj");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new FileNotFoundException(
            "Could not locate Mvp24Hours.Application.csproj. " +
            "Ensure the test is run from within the repository.");
    }
}
