using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.ContentNegotiation;
using Mvp24Hours.WebAPI.Exceptions;
using Mvp24Hours.WebAPI.Extensions;
using Mvp24Hours.WebAPI.Filters;
using Mvp24Hours.WebAPI.Http;
using Mvp24Hours.WebAPI.Idempotency;
using Mvp24Hours.WebAPI.RateLimiting;
using Mvp24Hours.WebAPI.Services;
using Mvp24Hours.WebAPI.Test.Support;
using HealthCheckOptions = Mvp24Hours.WebAPI.Configuration.HealthCheckOptions;
using SecurityHeadersOptions = Mvp24Hours.WebAPI.Configuration.SecurityHeadersOptions;

namespace Mvp24Hours.WebAPI.Test.Extensions;

[Trait("Category", "Unit")]
public class ServiceCollectionExtensionsTest
{
    [Fact]
    public void AddMvp24HoursWebEssential_Should_RegisterHttpContextAccessor()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvp24HoursWebEssential();

        services.Should().Contain(x => x.ServiceType == typeof(IHttpContextAccessor));
    }

    [Fact]
    public void AddMvp24HoursModelBinders_Should_RegisterModelBinderProvider()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursModelBinders();

        ServiceProvider provider = services.BuildServiceProvider();
        IOptions<MvcOptions> mvcOptions = provider.GetRequiredService<IOptions<MvcOptions>>();

        mvcOptions.Value.ModelBinderProviders.Should().NotBeEmpty();
    }

    [Fact]
    public void AddMvp24HoursWebGzip_Should_RegisterResponseCompression()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursWebGzip(enableForHttps: true);

        services.Should().Contain(x => x.ServiceType.Name.Contains("IConfigureOptions"));
    }

    [Fact]
    public void AddMvp24HoursIdempotencyInMemory_Should_RegisterInMemoryStore()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursIdempotencyInMemory(TimeSpan.FromHours(2));

        ServiceProvider provider = services.BuildServiceProvider();
        IIdempotencyStore store = provider.GetRequiredService<IIdempotencyStore>();

        store.Should().BeOfType<InMemoryIdempotencyStore>();
        provider.GetRequiredService<IOptions<IdempotencyOptions>>().Value.CacheDuration.Should().Be(TimeSpan.FromHours(2));
    }

    [Fact]
    public void AddMvp24HoursIdempotencyStore_Should_RegisterCustomStore()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursIdempotencyStore<FakeIdempotencyStore>();

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IIdempotencyStore>().Should().BeOfType<FakeIdempotencyStore>();
        provider.GetRequiredService<IOptions<IdempotencyOptions>>().Value.StorageType.Should().Be(IdempotencyStorageType.Custom);
    }

    [Fact]
    public void AddMvp24HoursIdempotencyKeyGenerator_Should_RegisterCustomGenerator()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursIdempotencyKeyGenerator<FakeIdempotencyKeyGenerator>();

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IIdempotencyKeyGenerator>().Should().BeOfType<FakeIdempotencyKeyGenerator>();
    }

    [Fact]
    public void AddMvp24HoursContentNegotiationJson_Should_SetJsonDefault()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursContentNegotiationJson(enableXml: false);

        ServiceProvider provider = services.BuildServiceProvider();
        ContentNegotiationOptions options = provider.GetRequiredService<IOptions<ContentNegotiationOptions>>().Value;

        options.DefaultMediaType.Should().Be("application/json");
        options.SupportedMediaTypes.Should().NotContain(m => m.MediaType.Contains("xml", StringComparison.OrdinalIgnoreCase));
        provider.GetRequiredService<IContentFormatterRegistry>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursContentNegotiationXml_Should_SetXmlDefault()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursContentNegotiationXml(enableJson: false);

        ServiceProvider provider = services.BuildServiceProvider();
        ContentNegotiationOptions options = provider.GetRequiredService<IOptions<ContentNegotiationOptions>>().Value;

        options.DefaultMediaType.Should().Be("application/xml");
        options.SupportedMediaTypes.Should().NotContain(m => m.MediaType.Contains("json", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AddMvp24HoursContentNegotiation_Should_RegisterNegotiatorAndRegistry()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursContentNegotiation(
            options => options.AddVaryHeader = true,
            builder => builder.AddFormatter(new ProblemDetailsJsonFormatter()));

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IContentFormatterRegistry>().Should().NotBeNull();
        provider.GetRequiredService<AcceptHeaderNegotiator>().Should().NotBeNull();
        provider.GetRequiredService<IOptions<ContentNegotiationOptions>>().Value.AddVaryHeader.Should().BeTrue();
    }

    [Fact]
    public void AddMvp24HoursRequestContext_Should_RegisterCorrelationServices()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursRequestContext(options => options.CorrelationIdHeader = "X-Trace-Id");

        services.Should().Contain(x => x.ServiceType == typeof(IHttpContextAccessor));
        services.Should().Contain(x => x.ServiceType == typeof(ICorrelationContextProvider));
        services.Should().Contain(x => x.ServiceType == typeof(CorrelationIdHandler));
        services.BuildServiceProvider()
            .GetRequiredService<IOptions<RequestContextOptions>>().Value.CorrelationIdHeader
            .Should().Be("X-Trace-Id");
    }

    [Fact]
    public void AddMvp24HoursProblemDetails_Should_RegisterMappersAndFilters()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursProblemDetails(options => options.ProblemTypeBaseUri = "https://errors.test");

        services.Should().Contain(x => x.ServiceType == typeof(DefaultExceptionToProblemDetailsMapper));
        services.Should().Contain(x => x.ServiceType == typeof(ValidationProblemDetailsMapper));
        services.Should().Contain(x => x.ServiceType == typeof(IExceptionToProblemDetailsMapper));
        services.Should().Contain(x => x.ServiceType == typeof(ModelStateValidationFilter));
        services.Should().Contain(x => x.ServiceType == typeof(ProblemDetailsResultFilter));
    }

    [Fact]
    public void AddMvp24HoursProblemDetailsAll_Should_RegisterModelStateValidation()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursProblemDetailsAll(options => options.FallbackDetail = "fallback");

        services.Should().Contain(x => x.ServiceType == typeof(IExceptionToProblemDetailsMapper));
        services.BuildServiceProvider()
            .GetRequiredService<IOptions<MvpProblemDetailsOptions>>().Value.FallbackDetail
            .Should().Be("fallback");
    }

    [Fact]
    public void AddMvp24HoursApiVersioning_Should_RegisterVersioningServices()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursApiVersioning(options =>
        {
            options.Strategy = ApiVersioningStrategy.Header | ApiVersioningStrategy.QueryString;
        });

        services.Should().Contain(x => x.ServiceType.Name.Contains("IApiVersionDescriptionProvider"));
    }

    [Fact]
    public void AddMvp24HoursRateLimiting_Should_RegisterKeyGeneratorAndResolver()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursRateLimiting(options => options.AddDefaultPolicy(50, TimeSpan.FromMinutes(1)));

        services.Should().Contain(x => x.ServiceType == typeof(Mvp24Hours.WebAPI.RateLimiting.IRateLimitKeyGenerator));
        services.Should().Contain(x => x.ServiceType == typeof(Mvp24Hours.WebAPI.RateLimiting.RateLimitPartitionResolver));
    }

    [Fact]
    public void AddMvp24HoursHealthChecks_Should_ReturnHealthChecksBuilder()
    {
        var services = new ServiceCollection();

        IHealthChecksBuilder builder = services.AddMvp24HoursHealthChecks(options =>
        {
            options.HealthPath = "/healthz";
            options.EnableDetailedResponses = true;
        });

        builder.Should().NotBeNull();
        services.BuildServiceProvider()
            .GetRequiredService<IOptions<HealthCheckOptions>>().Value.HealthPath
            .Should().Be("/healthz");
    }

    [Fact]
    public void AddMvp24HoursRequestLogging_Should_RegisterRequestLogger()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursRequestLogging(options => options.LogSlowRequests = true);

        services.Should().Contain(x => x.ServiceType == typeof(IRequestLogger));
    }

    [Fact]
    public void AddMvp24HoursRequestObservability_Should_RegisterLoggingAndTelemetry()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursRequestObservability(
            logging => logging.LoggingLevel = RequestLoggingLevel.Detailed,
            telemetry => telemetry.EnableMetrics = true);

        services.Should().Contain(x => x.ServiceType == typeof(IRequestLogger));
        services.BuildServiceProvider()
            .GetRequiredService<IOptions<RequestTelemetryOptions>>().Value.EnableMetrics
            .Should().BeTrue();
    }

    [Fact]
    public void AddMvp24HoursSecurity_Should_RegisterSecurityOptions()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursSecurity(
            securityHeaders => securityHeaders.EnableHsts = true,
            sizeLimit => sizeLimit.DefaultMaxBodySize = 2048,
            sanitization => sanitization.EnableXssSanitization = true,
            antiForgery => antiForgery.Enabled = true);

        services.BuildServiceProvider()
            .GetRequiredService<IOptions<SecurityHeadersOptions>>().Value.EnableHsts
            .Should().BeTrue();
    }

    [Fact]
    public void AddMvp24HoursIdempotencyDistributed_Should_RegisterDistributedStore()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursIdempotencyDistributed(TimeSpan.FromHours(6));

        services.Should().Contain(x =>
            x.ServiceType == typeof(IIdempotencyStore) &&
            x.ImplementationType == typeof(DistributedCacheIdempotencyStore));
    }

    [Fact]
    public void AddMvp24HoursStrictContentNegotiation_Should_SetStrictOptions()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursStrictContentNegotiation();

        ContentNegotiationOptions options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<ContentNegotiationOptions>>().Value;

        options.Return406WhenNoMatch.Should().BeTrue();
        options.UseRfc7807ContentTypeForProblemDetails.Should().BeTrue();
    }
}

internal sealed class FakeIdempotencyStore : IIdempotencyStore
{
    public Task CompleteAsync(string key, int statusCode, byte[] responseBody, string contentType, string? responseHeadersJson = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task FailAsync(string key, bool removeRecord = true, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<IdempotencyRecord?> GetAsync(string key, CancellationToken cancellationToken = default)
        => Task.FromResult<IdempotencyRecord?>(null);

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<IdempotencyLockResult> TryAcquireLockAsync(string key, string requestPath, string requestMethod, string? requestBodyHash, TimeSpan duration, string? correlationId = null, CancellationToken cancellationToken = default)
        => Task.FromResult(IdempotencyLockResult.Success());
}

internal sealed class FakeIdempotencyKeyGenerator : IIdempotencyKeyGenerator
{
    public Task<IdempotencyKeyResult> GenerateKeyAsync(HttpContext context)
        => Task.FromResult(IdempotencyKeyResult.FromHeader("fake-key"));
}
