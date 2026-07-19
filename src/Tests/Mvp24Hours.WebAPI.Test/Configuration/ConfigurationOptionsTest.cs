using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.WebAPI.Configuration;

namespace Mvp24Hours.WebAPI.Test.Configuration;

[Trait("Category", "Unit")]
public class ConfigurationOptionsTest
{
    [Fact]
    public void ExceptionOptions_Should_DefaultToDefaultMapper()
    {
        var sut = new ExceptionOptions();

        sut.StatusCodeHandle.Should().NotBeNull();
        sut.StatusCodeHandle(new InvalidOperationException()).Should().Be(409);
    }

    [Fact]
    public void CorrelationIdOptions_Should_HaveExpectedDefaults()
    {
        var sut = new CorrelationIdOptions();

        sut.Header.Should().Be("X-Correlation-ID");
        sut.IncludeInResponse.Should().BeTrue();
    }

    [Fact]
    public void SecurityHeadersOptions_Should_EnableSecurityFeaturesByDefault()
    {
        var sut = new SecurityHeadersOptions();

        sut.EnableHsts.Should().BeTrue();
        sut.EnableContentSecurityPolicy.Should().BeTrue();
        sut.EnableXFrameOptions.Should().BeTrue();
    }

    [Fact]
    public void ETagOptions_Should_DefaultToContentHash()
    {
        var sut = new ETagOptions();

        sut.Enabled.Should().BeTrue();
        sut.Algorithm.Should().Be(ETagAlgorithm.ContentHash);
    }

    [Fact]
    public void RateLimitingOptions_Should_AddDefaultPolicy()
    {
        var sut = new RateLimitingOptions();

        sut.AddDefaultPolicy(50, TimeSpan.FromMinutes(2));

        sut.Policies.Should().ContainKey(sut.DefaultPolicyName);
        sut.Policies[sut.DefaultPolicyName].PermitLimit.Should().Be(50);
    }

    [Fact]
    public void RateLimitingOptions_Should_MapEndpointPolicy()
    {
        var sut = new RateLimitingOptions();

        sut.MapEndpointToPolicy("/api/*", "default");

        sut.EndpointPolicies["/api/*"].Should().Be("default");
    }

    [Fact]
    public void ApiVersioningOptions_Should_DefaultToV1()
    {
        var sut = new ApiVersioningOptions();

        sut.DefaultApiVersion.Should().BeEquivalentTo(new ApiVersion(1, 0));
        sut.AssumeDefaultVersionWhenUnspecified.Should().BeTrue();
    }

    [Fact]
    public void HealthCheckOptions_Should_ExposeDefaultRoutes()
    {
        var sut = new HealthCheckOptions();

        sut.HealthPath.Should().Be("/health");
        sut.ReadinessPath.Should().Be("/health/ready");
        sut.LivenessPath.Should().Be("/health/live");
    }

    [Fact]
    public void ProblemDetailsOptions_Should_DefaultToRfc7807()
    {
        var sut = new ProblemDetailsOptions();

        sut.UseRfc7807ContentType.Should().BeTrue();
        sut.FallbackStatusCode.Should().Be(500);
    }

    [Fact]
    public void MvpProblemDetailsOptions_Should_DefaultToRfc7807()
    {
        var sut = new MvpProblemDetailsOptions();

        sut.UseRfc7807ContentType.Should().BeTrue();
        sut.CorrelationIdHeaderName.Should().Be("X-Correlation-ID");
    }

    [Fact]
    public void IdempotencyOptions_Should_DefaultToDistributedAndHeaderOrBody()
    {
        var sut = new IdempotencyOptions();

        sut.StorageType.Should().Be(IdempotencyStorageType.DistributedCache);
        sut.KeySource.Should().Be(IdempotencyKeySource.HeaderOrRequestBody);
        sut.IdempotentMethods.Should().Contain("POST");
    }

    [Fact]
    public void IdempotencyOptions_Should_AddCustomRules()
    {
        var sut = new IdempotencyOptions();

        sut.RequireIdempotencyForPath("/api/orders/*");
        sut.ExcludePath("/healthz");
        sut.AddNonCacheableStatusCode(418);

        sut.RequiredPaths.Should().Contain("/api/orders/*");
        sut.ExcludedPaths.Should().Contain("/healthz");
        sut.NonCacheableStatusCodes.Should().Contain(418);
    }
}
