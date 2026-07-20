using Mvp24Hours.Application.Contract.Resilience;
using Mvp24Hours.Application.Logic.Resilience;
using MvpResilienceExtensions = Mvp24Hours.Application.Extensions.ResilienceServiceCollectionExtensions;

namespace Mvp24Hours.Application.Test.Extensions;

[Trait("Category", "Unit")]
public class ResilienceServiceCollectionExtensionsTest
{
    [Fact]
    public void AddMvpResilience_ShouldRegisterMapperAndLocalizer()
    {
        var services = new ServiceCollection();

        MvpResilienceExtensions.AddMvpResilience(services);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IExceptionToResultMapper>().Should().BeOfType<ExceptionToResultMapper>();
        provider.GetRequiredService<IErrorMessageLocalizer>().Should().BeOfType<DefaultErrorMessageLocalizer>();
    }

    [Fact]
    public void AddMvpResilience_WithConfigureOptions_ShouldRegisterOptions()
    {
        var services = new ServiceCollection();

        MvpResilienceExtensions.AddMvpResilience(services, options => options.IncludeExceptionDetails = true);
        ServiceProvider provider = services.BuildServiceProvider();

        ExceptionMappingOptions options = provider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<ExceptionMappingOptions>>()
            .Value;
        options.IncludeExceptionDetails.Should().BeTrue();
    }

    [Fact]
    public void AddMvpResilience_WithNullServices_ShouldThrow()
    {
        Action act = () => MvpResilienceExtensions.AddMvpResilience(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }
}
