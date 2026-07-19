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

        ConfigurationHelper.SetConfiguration(config);
    }

    [Fact]
    public void SetEnvironment_AndGetEnvironment_ShouldRoundTrip()
    {
        // Arrange
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns("Development");

        // Act
        ConfigurationHelper.SetEnvironment(environment.Object);
        IHostEnvironment? result = ConfigurationHelper.GetEnvironment();

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
        string? value = ConfigurationHelper.GetSettings("MyKey");

        // Assert
        value.Should().Be("MyValue");
    }

    [Fact]
    public void GetSettingsGeneric_WithExistingSection_ShouldReturnBoundObject()
    {
        // Arrange
        SetInMemoryConfiguration();

        // Act
        SectionSettings? settings = ConfigurationHelper.GetSettings<SectionSettings>("Section");

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
        IConfigurationSection? section = ConfigurationHelper.GetSection("Section");

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
        string? value = ConfigurationHelper.GetSettings("MissingKey");

        // Assert
        value.Should().BeNull();
    }

    [Fact]
    public void GetSettingsGeneric_WithMissingSection_ShouldReturnNull()
    {
        // Arrange
        SetInMemoryConfiguration();

        // Act
        SectionSettings? settings = ConfigurationHelper.GetSettings<SectionSettings>("MissingSection");

        // Assert
        settings.Should().BeNull();
    }
}
