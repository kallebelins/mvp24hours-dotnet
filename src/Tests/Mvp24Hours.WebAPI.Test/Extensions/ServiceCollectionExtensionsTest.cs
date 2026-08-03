using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.ContentNegotiation;
using Mvp24Hours.WebAPI.Exceptions;
using Mvp24Hours.WebAPI.Extensions;
using Mvp24Hours.WebAPI.Filters;
using Mvp24Hours.WebAPI.Http;
using Mvp24Hours.WebAPI.Idempotency;
using Mvp24Hours.WebAPI.Services;
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

        services.AddMvp24HoursApiVersioning(options => options.Strategy = ApiVersioningStrategy.Header | ApiVersioningStrategy.QueryString);

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

    [Fact]
    public void AddMvp24HoursWebJson_Should_RegisterControllersWithNewtonsoft()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursWebJson();

        services.Should().Contain(x => x.ServiceType.Name.Contains("IConfigureOptions"));
    }

    [Fact]
    public void AddMvp24HoursWebCors_Should_RegisterCorsOptions()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursWebCors(options => options.Origin = "https://app.test");

        services.BuildServiceProvider()
            .GetRequiredService<IOptions<CorsOptions>>().Value.Origin
            .Should().Be("https://app.test");
    }

    [Fact]
    public void AddMvp24HoursWebExceptions_Should_RegisterExceptionOptions()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursWebExceptions(options => options.TraceMiddleware = true);

        services.BuildServiceProvider()
            .GetRequiredService<IOptions<ExceptionOptions>>().Value.TraceMiddleware
            .Should().BeTrue();
    }

    [Fact]
    public void AddMvp24HoursCompression_Should_RegisterCompressionProviders()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursCompression(options =>
        {
            options.UseBrotli = true;
            options.UseGzip = true;
            options.EnableForHttps = true;
        });

        services.Should().Contain(x => x.ServiceType.Name.Contains("IConfigureOptions"));
    }

    [Fact]
    public void AddMvp24HoursRequestDecompression_Should_RegisterOptions()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursRequestDecompression(options => options.Enabled = true);

        services.BuildServiceProvider()
            .GetRequiredService<IOptions<RequestDecompressionOptions>>().Value.Enabled
            .Should().BeTrue();
    }

    [Fact]
    public void AddMvp24HoursResponseCaching_Should_RegisterCachingServices()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursResponseCaching(options => options.MaximumBodySize = 2048);

        services.Should().Contain(x => x.ServiceType.Name.Contains("IConfigureOptions"));
        services.BuildServiceProvider()
            .GetRequiredService<IOptions<ResponseCachingOptions>>().Value.MaximumBodySize
            .Should().Be(2048);
    }

    [Fact]
    [Obsolete]
    public void AddMvp24HoursOutputCaching_Should_RegisterOutputCache()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursOutputCaching(options => options.DefaultExpirationTimeSpan = TimeSpan.FromSeconds(30));

        services.Should().Contain(x => x.ServiceType.Name.Contains("IConfigureOptions"));
    }

    [Fact]
    public void AddMvp24HoursETag_Should_RegisterETagOptions()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursETag(options => options.Enabled = true);

        services.BuildServiceProvider()
            .GetRequiredService<IOptions<ETagOptions>>().Value.Enabled
            .Should().BeTrue();
    }

    [Fact]
    public void AddMvp24HoursRequestTimeout_Should_RegisterTimeoutOptions()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursRequestTimeout(options => options.DefaultTimeout = TimeSpan.FromSeconds(15));

        services.BuildServiceProvider()
            .GetRequiredService<IOptions<RequestTimeoutOptions>>().Value.DefaultTimeout
            .Should().Be(TimeSpan.FromSeconds(15));
    }

    [Fact]
    public void AddMvp24HoursCacheControl_Should_RegisterCacheControlOptions()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursCacheControl(options => options.Enabled = false);

        services.BuildServiceProvider()
            .GetRequiredService<IOptions<CacheControlOptions>>().Value.Enabled
            .Should().BeFalse();
    }

    [Fact]
    public void AddMvp24HoursIdempotency_Should_RegisterDefaultStoreAndGenerator()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursIdempotency(options =>
        {
            options.IntegrateWithCqrs = true;
            options.RequireIdempotencyKey = true;
        });

        services.Should().Contain(x => x.ServiceType == typeof(IIdempotencyStore));
        services.Should().Contain(x => x.ServiceType == typeof(IIdempotencyKeyGenerator));
    }

    [Fact]
    public void AddMvp24HoursExceptionMapper_Should_RegisterCustomMapper()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursExceptionMapper<FakeExceptionMapper>();

        services.Should().Contain(x =>
            x.ServiceType == typeof(IExceptionToProblemDetailsMapper) &&
            x.ImplementationType == typeof(FakeExceptionMapper));
    }

    [Fact]
    public void AddMvp24HoursModelStateValidation_Should_RegisterFilter()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursModelStateValidation();

        ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value.SuppressModelStateInvalidFilter
            .Should().BeTrue();
        provider.GetRequiredService<IOptions<MvcOptions>>().Value.Filters.Should().NotBeEmpty();
    }

    [Fact]
    public void AddMvp24HoursDistributedRateLimiting_Should_RegisterDistributedLimiter()
    {
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();

        services.AddMvp24HoursDistributedRateLimiting(options => options.InstanceName = "test:ratelimit:");

        services.Should().Contain(x => x.ServiceType == typeof(Mvp24Hours.WebAPI.RateLimiting.IDistributedRateLimiter));
    }

    [Fact]
    public void AddMvp24HoursSecurityHeaders_Should_RegisterSecurityHeadersOptions()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursSecurityHeaders(options => options.EnableXContentTypeOptions = true);

        services.BuildServiceProvider()
            .GetRequiredService<IOptions<SecurityHeadersOptions>>().Value.EnableXContentTypeOptions
            .Should().BeTrue();
    }

    [Fact]
    public void AddMvp24HoursApiKeyAuthentication_Should_RegisterOptions()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursApiKeyAuthentication(options => options.HeaderName = "X-Test-Key");

        services.BuildServiceProvider()
            .GetRequiredService<IOptions<ApiKeyAuthenticationOptions>>().Value.HeaderName
            .Should().Be("X-Test-Key");
    }

    [Fact]
    public void AddMvp24HoursIpFiltering_Should_RegisterIpFilteringOptions()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursIpFiltering(options => options.WhitelistedIps.Add("127.0.0.1"));

        services.BuildServiceProvider()
            .GetRequiredService<IOptions<IpFilteringOptions>>().Value.WhitelistedIps
            .Should().Contain("127.0.0.1");
    }

    [Fact]
    public void AddMvp24HoursAntiForgery_Should_RegisterAntiForgeryOptions()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursAntiForgery(options => options.CookieName = "mvp-csrf");

        services.BuildServiceProvider()
            .GetRequiredService<IOptions<AntiForgeryOptions>>().Value.CookieName
            .Should().Be("mvp-csrf");
    }

    [Fact]
    public void AddMvp24HoursInputSanitization_Should_RegisterSanitizationOptions()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursInputSanitization(options => options.EnableSqlInjectionDetection = true);

        services.BuildServiceProvider()
            .GetRequiredService<IOptions<InputSanitizationOptions>>().Value.EnableSqlInjectionDetection
            .Should().BeTrue();
    }

    [Fact]
    public void AddMvp24HoursRequestSizeLimit_Should_RegisterSizeLimitOptions()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursRequestSizeLimit(options => options.DefaultMaxBodySize = 4096);

        services.BuildServiceProvider()
            .GetRequiredService<IOptions<RequestSizeLimitOptions>>().Value.DefaultMaxBodySize
            .Should().Be(4096);
    }

    [Fact]
    public void AddMvp24HoursRequestTelemetry_Should_RegisterTelemetryOptions()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursRequestTelemetry(options => options.EnableTracing = true);

        services.BuildServiceProvider()
            .GetRequiredService<IOptions<RequestTelemetryOptions>>().Value.EnableTracing
            .Should().BeTrue();
    }

    [Fact]
    public void AddContentFormatter_Should_RegisterCustomFormatter()
    {
        var services = new ServiceCollection();

        services.AddContentFormatter<ProblemDetailsJsonFormatter>();

        services.Should().Contain(x =>
            x.ServiceType == typeof(IContentFormatter) &&
            x.ImplementationType == typeof(ProblemDetailsJsonFormatter));
    }

    [Fact]
    public void AddMvp24HoursContentNegotiationMvc_Should_RegisterMvcIntegration()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursContentNegotiationMvc();

        services.Should().Contain(x => x.ServiceType.Name.Contains("IConfigureOptions"));
    }
}

internal sealed class FakeIdempotencyStore : IIdempotencyStore
{
    public Task CompleteAsync(string key, int statusCode, byte[] responseBody, string contentType, string? responseHeadersJson = null, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    public Task FailAsync(string key, bool removeRecord = true, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<IdempotencyRecord?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IdempotencyRecord?>(null);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<IdempotencyLockResult> TryAcquireLockAsync(string key, string requestPath, string requestMethod, string? requestBodyHash, TimeSpan duration, string? correlationId = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(IdempotencyLockResult.Success());
    }
}

internal sealed class FakeIdempotencyKeyGenerator : IIdempotencyKeyGenerator
{
    public Task<IdempotencyKeyResult> GenerateKeyAsync(HttpContext context)
    {
        return Task.FromResult(IdempotencyKeyResult.FromHeader("fake-key"));
    }
}

internal sealed class FakeExceptionMapper : IExceptionToProblemDetailsMapper
{
    public bool CanHandle(Exception exception)
    {
        return true;
    }

    public int GetStatusCode(Exception exception)
    {
        return StatusCodes.Status400BadRequest;
    }

    public Microsoft.AspNetCore.Mvc.ProblemDetails Map(Exception exception, HttpContext context)
    {
        return new() { Title = exception.Message };
    }
}
