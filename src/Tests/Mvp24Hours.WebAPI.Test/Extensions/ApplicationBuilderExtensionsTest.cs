using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Extensions;
using Mvp24Hours.WebAPI.Services;

namespace Mvp24Hours.WebAPI.Test.Extensions;

[Trait("Category", "Unit")]
public class ApplicationBuilderExtensionsTest
{
    [Fact]
    public void UseMvp24HoursFullObservability_ShouldChainAllMiddlewares()
    {
        IApplicationBuilder app = CreateAppBuilder();

        IApplicationBuilder result = app.UseMvp24HoursFullObservability();

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void UseMvp24HoursRequestObservability_ShouldChainTelemetryAndLogging()
    {
        IApplicationBuilder app = CreateAppBuilder();

        IApplicationBuilder result = app.UseMvp24HoursRequestObservability();

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void UseMvp24HoursSecurity_ShouldChainAllSecurityMiddlewares()
    {
        IApplicationBuilder app = CreateAppBuilder();

        IApplicationBuilder result = app.UseMvp24HoursSecurity();

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void UseMvp24HoursRateLimiting_Disabled_ShouldReturnSameBuilder()
    {
        IApplicationBuilder app = CreateAppBuilder();

        IApplicationBuilder result = app.UseMvp24HoursRateLimiting(enabled: false);

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void UseMvp24HoursIdempotency_Disabled_ShouldReturnSameBuilder()
    {
        IApplicationBuilder app = CreateAppBuilder();

        IApplicationBuilder result = app.UseMvp24HoursIdempotency(enabled: false);

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void UseMvp24HoursContentNegotiation_Disabled_ShouldReturnSameBuilder()
    {
        IApplicationBuilder app = CreateAppBuilder();

        IApplicationBuilder result = app.UseMvp24HoursContentNegotiation(enabled: false);

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void UseMvp24HoursExceptionHandling_ShouldReturnBuilder()
    {
        IApplicationBuilder app = CreateAppBuilder();

        IApplicationBuilder result = app.UseMvp24HoursExceptionHandling();

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void UseMvp24HoursCors_ShouldReturnBuilder()
    {
        IApplicationBuilder app = CreateAppBuilder();

        IApplicationBuilder result = app.UseMvp24HoursCors();

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void UseMvp24HoursResponseCompression_ShouldReturnBuilder()
    {
        WebApplication app = CreateWebApplication(services => services.AddResponseCompression());

        IApplicationBuilder result = app.UseMvp24HoursResponseCompression();

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void UseMvp24HoursRequestDecompression_ShouldReturnBuilder()
    {
        IApplicationBuilder app = CreateAppBuilder();

        IApplicationBuilder result = app.UseMvp24HoursRequestDecompression();

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void UseMvp24HoursResponseCaching_ShouldReturnBuilder()
    {
        WebApplication app = CreateWebApplication(services => services.AddResponseCaching());

        IApplicationBuilder result = app.UseMvp24HoursResponseCaching();

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void UseMvp24HoursETag_ShouldReturnBuilder()
    {
        IApplicationBuilder app = CreateAppBuilder(services => services.Configure<ETagOptions>(_ => { }));

        IApplicationBuilder result = app.UseMvp24HoursETag();

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void UseMvp24HoursRequestTimeout_ShouldReturnBuilder()
    {
        IApplicationBuilder app = CreateAppBuilder(services => services.Configure<RequestTimeoutOptions>(_ => { }));

        IApplicationBuilder result = app.UseMvp24HoursRequestTimeout();

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void UseMvp24HoursCacheControl_ShouldReturnBuilder()
    {
        IApplicationBuilder app = CreateAppBuilder(services => services.Configure<CacheControlOptions>(_ => { }));

        IApplicationBuilder result = app.UseMvp24HoursCacheControl();

        result.Should().BeSameAs(app);
    }

#pragma warning disable CS0618
    [Fact]
    public void UseMvp24HoursCaching_ObsoleteOverload_ShouldReturnBuilder()
    {
        WebApplication app = CreateWebApplication(services => services.AddResponseCaching());

        IApplicationBuilder result = app.UseMvp24HoursCaching("page");

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void UseMvp24HoursOutputCaching_ObsoleteOverload_ShouldReturnBuilder()
    {
        WebApplication app = CreateWebApplication(services =>
        {
            services.AddResponseCaching();
            services.AddOutputCache();
        });

        IApplicationBuilder result = app.UseMvp24HoursOutputCaching();

        result.Should().BeSameAs(app);
    }
#pragma warning restore CS0618

    [Fact]
    public void UseMvp24HoursHealthChecks_ShouldMapEndpointsOnWebApplication()
    {
        WebApplication app = CreateWebApplication(services =>
        {
            services.AddHealthChecks();
            services.Configure<Mvp24Hours.WebAPI.Configuration.HealthCheckOptions>(options =>
            {
                options.HealthPath = "/health";
                options.ReadinessPath = "/health/ready";
                options.LivenessPath = "/health/live";
            });
        });

        IApplicationBuilder result = app.UseMvp24HoursHealthChecks();

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void UseMvp24HoursSwagger_ShouldReturnBuilder()
    {
        WebApplication app = CreateWebApplication(services =>
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
        });

        IApplicationBuilder result = app.UseMvp24HoursSwagger("Test API");

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void UseMvp24HoursResponseCompression_ShouldThrow_WhenBuilderIsNull()
    {
        IApplicationBuilder? app = null;

        Action act = () => app!.UseMvp24HoursResponseCompression();

        act.Should().Throw<ArgumentNullException>();
    }

    private static IApplicationBuilder CreateAppBuilder(Action<IServiceCollection>? configure = null)
    {
        return CreateWebApplication(configure);
    }

    private static WebApplication CreateWebApplication(Action<IServiceCollection>? configure = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.Services.AddLogging();
        builder.Services.AddOptions();
        builder.Services.AddRouting();
        builder.Services.Configure<SecurityHeadersOptions>(_ => { });
        builder.Services.Configure<IpFilteringOptions>(_ => { });
        builder.Services.Configure<RequestSizeLimitOptions>(_ => { });
        builder.Services.Configure<InputSanitizationOptions>(_ => { });
        builder.Services.Configure<AntiForgeryOptions>(_ => { });
        builder.Services.Configure<RequestContextOptions>(_ => { });
        builder.Services.Configure<RequestTelemetryOptions>(_ => { });
        builder.Services.Configure<RequestLoggingOptions>(_ => { });
        builder.Services.Configure<MvpProblemDetailsOptions>(_ => { });
        builder.Services.Configure<RateLimitingOptions>(x => x.AddDefaultPolicy(10, TimeSpan.FromMinutes(1)));
        builder.Services.AddSingleton<IRequestLogger, DefaultRequestLogger>();
        builder.Services.AddSingleton<Mvp24Hours.WebAPI.RateLimiting.IRateLimitKeyGenerator, Mvp24Hours.WebAPI.RateLimiting.DefaultRateLimitKeyGenerator>();
        builder.Services.AddSingleton<Mvp24Hours.WebAPI.RateLimiting.RateLimitPartitionResolver>();
        builder.Services.AddSingleton<Mvp24Hours.WebAPI.Exceptions.IExceptionToProblemDetailsMapper, Mvp24Hours.WebAPI.Exceptions.DefaultExceptionToProblemDetailsMapper>();
        configure?.Invoke(builder.Services);
        return builder.Build();
    }
}
