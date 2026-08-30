using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Extensions;

namespace Mvp24Hours.WebAPI.Test.Extensions;

[Trait("Category", "Unit")]
public class CorrelationIdExtensionsTest
{
    private static IApplicationBuilder CreateAppBuilder()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        return new ApplicationBuilder(services.BuildServiceProvider());
    }

    [Fact]
    public void UseMvp24HoursCorrelationId_Parameterless_WithNullApp_Throws()
    {
        // Act
        Action act = () => CorrelationIdExtensions.UseMvp24HoursCorrelationId(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UseMvp24HoursCorrelationId_Parameterless_ReturnsBuilder()
    {
        // Arrange
        IApplicationBuilder app = CreateAppBuilder();

        // Act
        IApplicationBuilder result = app.UseMvp24HoursCorrelationId();

        // Assert
        result.Should().BeSameAs(app);
    }

    [Fact]
    public void UseMvp24HoursCorrelationId_WithHeaderString_WithNullApp_Throws()
    {
        // Act
        Action act = () => CorrelationIdExtensions.UseMvp24HoursCorrelationId(null!, "X-My-Correlation");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UseMvp24HoursCorrelationId_WithHeaderString_ReturnsBuilder()
    {
        // Arrange
        IApplicationBuilder app = CreateAppBuilder();

        // Act
        IApplicationBuilder result = app.UseMvp24HoursCorrelationId("X-My-Correlation");

        // Assert
        result.Should().BeSameAs(app);
    }

    [Fact]
    public void UseMvp24HoursCorrelationId_WithOptions_WithNullApp_Throws()
    {
        // Act
        Action act = () => CorrelationIdExtensions.UseMvp24HoursCorrelationId(null!, new CorrelationIdOptions());

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UseMvp24HoursCorrelationId_WithNullOptions_Throws()
    {
        // Arrange
        IApplicationBuilder app = CreateAppBuilder();

        // Act
        Action act = () => app.UseMvp24HoursCorrelationId((CorrelationIdOptions)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UseMvp24HoursCorrelationId_WithOptions_ReturnsBuilder()
    {
        // Arrange
        IApplicationBuilder app = CreateAppBuilder();
        var options = new CorrelationIdOptions { Header = "X-Custom-Correlation" };

        // Act
        IApplicationBuilder result = app.UseMvp24HoursCorrelationId(options);

        // Assert
        result.Should().BeSameAs(app);
    }
}
