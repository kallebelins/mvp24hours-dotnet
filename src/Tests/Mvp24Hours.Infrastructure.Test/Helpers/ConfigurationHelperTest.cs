//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using Mvp24Hours.Helpers;

namespace Mvp24Hours.Infrastructure.Test.Helpers;

[Trait("Category", "Unit")]
public class ConfigurationHelperTest
{
    private sealed class SectionSettings
    {
        public string? Name { get; set; }
    }

    private static void SetInMemoryConfiguration(Dictionary<string, string?>? additional = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["MyKey"] = "MyValue",
            ["Section:Name"] = "Test"
        };

        if (additional != null)
        {
            foreach (KeyValuePair<string, string?> pair in additional)
            {
                values[pair.Key] = pair.Value;
            }
        }

        IConfigurationRoot config = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        // intentional: covers the obsolete ConfigurationHelper until removal in v12
#pragma warning disable CS0618
        ConfigurationHelper.SetConfiguration(config);
#pragma warning restore CS0618
    }

    [Fact]
    public void SetEnvironment_AndGetEnvironment_ShouldRoundTrip()
    {
        // Arrange
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns("Development");

        // Act
        // intentional: covers the obsolete ConfigurationHelper until removal in v12
#pragma warning disable CS0618
        ConfigurationHelper.SetEnvironment(environment.Object);
        IHostEnvironment? result = ConfigurationHelper.GetEnvironment();
#pragma warning restore CS0618

        // Assert
        result.Should().BeSameAs(environment.Object);
        result!.EnvironmentName.Should().Be("Development");
    }

    [Fact]
    public void GetSettings_WithExistingKey_ShouldReturnValue()
    {
        // Arrange
        SetInMemoryConfiguration();

        // Act
        // intentional: covers the obsolete ConfigurationHelper until removal in v12
#pragma warning disable CS0618
        string? value = ConfigurationHelper.GetSettings("MyKey");
#pragma warning restore CS0618

        // Assert
        value.Should().Be("MyValue");
    }

    [Fact]
    public void GetSettingsGeneric_WithExistingSection_ShouldReturnBoundObject()
    {
        // Arrange
        SetInMemoryConfiguration();

        // Act
        // intentional: covers the obsolete ConfigurationHelper until removal in v12
#pragma warning disable CS0618
        SectionSettings? settings = ConfigurationHelper.GetSettings<SectionSettings>("Section");
#pragma warning restore CS0618

        // Assert
        settings.Should().NotBeNull();
        settings!.Name.Should().Be("Test");
    }

    [Fact]
    public void GetSection_WithExistingKey_ShouldReturnSection()
    {
        // Arrange
        SetInMemoryConfiguration();

        // Act
        // intentional: covers the obsolete ConfigurationHelper until removal in v12
#pragma warning disable CS0618
        IConfigurationSection? section = ConfigurationHelper.GetSection("Section");
#pragma warning restore CS0618

        // Assert
        section.Should().NotBeNull();
        section!.GetSection("Name").Value.Should().Be("Test");
    }

    [Fact]
    public void GetSettings_WithMissingKey_ShouldReturnNull()
    {
        // Arrange
        SetInMemoryConfiguration();

        // Act
        // intentional: covers the obsolete ConfigurationHelper until removal in v12
#pragma warning disable CS0618
        string? value = ConfigurationHelper.GetSettings("MissingKey");
#pragma warning restore CS0618

        // Assert
        value.Should().BeNull();
    }

    [Fact]
    public void GetSettingsGeneric_WithMissingSection_ShouldReturnNull()
    {
        // Arrange
        SetInMemoryConfiguration();

        // Act
        // intentional: covers the obsolete ConfigurationHelper until removal in v12
#pragma warning disable CS0618
        SectionSettings? settings = ConfigurationHelper.GetSettings<SectionSettings>("MissingSection");
#pragma warning restore CS0618

        // Assert
        settings.Should().BeNull();
    }
}
