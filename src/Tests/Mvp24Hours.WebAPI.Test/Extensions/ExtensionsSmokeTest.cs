using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Extensions;
using Mvp24Hours.WebAPI.Services;

namespace Mvp24Hours.WebAPI.Test.Extensions;

[Trait("Category", "Unit")]
public class ExtensionsSmokeTest
{
    [Fact]
    public void ServiceCollectionExtensions_Should_RegisterProblemDetailsServices()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursProblemDetails();

        services.Should().Contain(x => x.ServiceType.Name.Contains("IExceptionToProblemDetailsMapper"));
    }

    [Fact]
    public void ServiceCollectionExtensions_Should_RegisterRequestLogging()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursRequestLogging();

        services.Should().Contain(x => x.ServiceType == typeof(IRequestLogger));
    }

    [Fact]
    public void ServiceCollectionExtensions_Should_RegisterRateLimiting()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursRateLimiting(x => x.AddDefaultPolicy(10, TimeSpan.FromSeconds(30)));

        services.Should().Contain(x => x.ServiceType.Name.Contains("IRateLimitKeyGenerator"));
    }

    [Fact]
    public void ServiceCollectionExtensions_Should_RegisterContentNegotiation()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursContentNegotiation();

        services.Should().Contain(x => x.ServiceType.Name.Contains("IContentFormatterRegistry"));
    }

    [Fact]
    public void ServiceCollectionExtensions_Should_RegisterIdempotency()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursIdempotency();

        services.Should().Contain(x => x.ServiceType.Name.Contains("IIdempotencyStore"));
    }

    [Fact]
    public void ApplicationBuilderExtensions_Should_ReturnBuilderForSecurityPipeline()
    {
        var app = CreateAppBuilder();

        IApplicationBuilder result = app
            .UseMvp24HoursSecurityHeaders()
            .UseMvp24HoursIpFiltering()
            .UseMvp24HoursRequestSizeLimit()
            .UseMvp24HoursInputSanitization()
            .UseMvp24HoursAntiForgery();

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void ApplicationBuilderExtensions_Should_ReturnBuilderForObservabilityPipeline()
    {
        var app = CreateAppBuilder();

        IApplicationBuilder result = app
            .UseMvp24HoursRequestContext()
            .UseMvp24HoursRequestTelemetry()
            .UseMvp24HoursRequestLogging();

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void ApplicationBuilderExtensions_Should_AddProblemDetailsAndRateLimiting()
    {
        var app = CreateAppBuilder();

        IApplicationBuilder result = app
            .UseMvp24HoursProblemDetails()
            .UseMvp24HoursRateLimiting();

        result.Should().BeSameAs(app);
    }

    private static IApplicationBuilder CreateAppBuilder()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddRouting();
        services.Configure<SecurityHeadersOptions>(_ => { });
        services.Configure<IpFilteringOptions>(_ => { });
        services.Configure<RequestSizeLimitOptions>(_ => { });
        services.Configure<InputSanitizationOptions>(_ => { });
        services.Configure<AntiForgeryOptions>(_ => { });
        services.Configure<RequestContextOptions>(_ => { });
        services.Configure<RequestTelemetryOptions>(_ => { });
        services.Configure<RequestLoggingOptions>(_ => { });
        services.Configure<MvpProblemDetailsOptions>(_ => { });
        services.Configure<RateLimitingOptions>(x => x.AddDefaultPolicy(10, TimeSpan.FromMinutes(1)));
        services.AddSingleton<IRequestLogger, DefaultRequestLogger>();
        services.AddSingleton<Mvp24Hours.WebAPI.RateLimiting.IRateLimitKeyGenerator, Mvp24Hours.WebAPI.RateLimiting.DefaultRateLimitKeyGenerator>();
        services.AddSingleton<Mvp24Hours.WebAPI.RateLimiting.RateLimitPartitionResolver>();
        services.AddSingleton<Mvp24Hours.WebAPI.Exceptions.IExceptionToProblemDetailsMapper, Mvp24Hours.WebAPI.Exceptions.DefaultExceptionToProblemDetailsMapper>();

        return new ApplicationBuilder(services.BuildServiceProvider());
    }
}
