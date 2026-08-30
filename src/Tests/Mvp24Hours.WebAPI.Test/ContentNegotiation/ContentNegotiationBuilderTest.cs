using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.WebAPI.ContentNegotiation;
using Mvp24Hours.WebAPI.Extensions;

namespace Mvp24Hours.WebAPI.Test.ContentNegotiation;

[Trait("Category", "Unit")]
public class ContentNegotiationBuilderTest
{
    public sealed class CustomFormatter : IContentFormatter
    {
        public IReadOnlyList<string> SupportedMediaTypes { get; } = ["application/x-custom"];
        public string PrimaryMediaType => "application/x-custom";
        public bool CanWrite(Type type) => true;
        public string Serialize(object? value) => value?.ToString() ?? string.Empty;
        public Task SerializeAsync(Stream stream, object? value, System.Text.Encoding encoding, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public string GetContentType(string? charset = null) => "application/x-custom";
    }

    // ContentNegotiationBuilder's constructor and CustomFormatterTypes/CustomFormatters
    // properties are internal to Mvp24Hours.WebAPI; the only supported way to exercise the
    // builder from this test assembly is through AddMvp24HoursContentNegotiation's
    // configureBuilder callback and observing the resulting IServiceCollection, which mirrors
    // how real consumers use this builder.
    [Fact]
    public void AddFormatter_ByType_RegistersFormatterInServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        ContentNegotiationBuilder? captured = null;

        // Act
        services.AddMvp24HoursContentNegotiation(configureBuilder: builder =>
        {
            captured = builder.AddFormatter<CustomFormatter>();
        });

        // Assert
        services.Should().Contain(d => d.ServiceType == typeof(CustomFormatter));
        captured.Should().NotBeNull();
    }

    [Fact]
    public void AddFormatter_ByType_ReturnsSameBuilderForChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        ContentNegotiationBuilder? builderRef = null;
        ContentNegotiationBuilder? chainedResult = null;

        // Act
        services.AddMvp24HoursContentNegotiation(configureBuilder: builder =>
        {
            builderRef = builder;
            chainedResult = builder.AddFormatter<CustomFormatter>();
        });

        // Assert
        chainedResult.Should().BeSameAs(builderRef);
    }

    [Fact]
    public void AddFormatter_ByInstance_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var instance = new CustomFormatter();

        // Act
        Action act = () => services.AddMvp24HoursContentNegotiation(configureBuilder: builder =>
            builder.AddFormatter(instance));

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddFormatter_ByInstance_WithNull_Throws()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () => services.AddMvp24HoursContentNegotiation(configureBuilder: builder =>
            builder.AddFormatter((IContentFormatter)null!));

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddFormatter_ByFactory_RegistersFormatterInServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMvp24HoursContentNegotiation(configureBuilder: builder =>
            builder.AddFormatter(_ => new CustomFormatter()));

        // Assert
        services.Should().Contain(d => d.ServiceType == typeof(CustomFormatter));
    }

    [Fact]
    public void AddFormatter_ByFactory_WithNullFactory_Throws()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () => services.AddMvp24HoursContentNegotiation(configureBuilder: builder =>
            builder.AddFormatter<CustomFormatter>(null!));

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
