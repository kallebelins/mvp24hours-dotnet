//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Mvp24Hours.Infrastructure.HealthChecks;
using Mvp24Hours.Infrastructure.Http.Contract;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.HealthChecks;

[Trait("Category", "Unit")]
public class HttpClientHealthCheckTest
{
    [Fact]
    public void Constructor_WithNullHttpClient_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new HttpClientHealthCheck<HealthCheckTestApi>(
            null!,
            new HttpClientHealthCheckOptions(),
            HealthChecksTestHelpers.CreateLogger<HttpClientHealthCheck<HealthCheckTestApi>>());

        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new HttpClientHealthCheck<HealthCheckTestApi>(
            HealthChecksTestHelpers.CreateTypedHttpClientMock<HealthCheckTestApi>().Object,
            new HttpClientHealthCheckOptions(),
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldUseDefaults()
    {
        var check = new HttpClientHealthCheck<HealthCheckTestApi>(
            HealthChecksTestHelpers.CreateTypedHttpClientMock<HealthCheckTestApi>().Object,
            null,
            HealthChecksTestHelpers.CreateLogger<HttpClientHealthCheck<HealthCheckTestApi>>());

        check.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckHealthAsync_WithSuccessfulGet_ShouldReturnHealthy()
    {
        Mock<ITypedHttpClient<HealthCheckTestApi>> mock = HealthChecksTestHelpers.CreateTypedHttpClientMock<HealthCheckTestApi>();
        var check = CreateCheck(mock.Object, new HttpClientHealthCheckOptions
        {
            HealthEndpoint = "/health",
            DegradedThresholdMs = 10_000,
            FailureThresholdMs = 30_000
        });

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data["statusCode"].Should().Be(200);
        result.Data["healthEndpoint"].ToString().Should().Contain("/health");
        result.Data["baseAddress"].ToString().Should().Contain("api.example.com");
    }

    [Fact]
    public async Task CheckHealthAsync_WithHeadRequest_ShouldUseSendAsync()
    {
        Mock<ITypedHttpClient<HealthCheckTestApi>> mock = HealthChecksTestHelpers.CreateTypedHttpClientMock<HealthCheckTestApi>();
        var check = CreateCheck(mock.Object, new HttpClientHealthCheckOptions
        {
            UseHeadRequest = true,
            HealthEndpoint = "/health",
            DegradedThresholdMs = 10_000,
            FailureThresholdMs = 30_000
        });

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        mock.Verify(c => c.SendAsync(
            It.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Head),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckHealthAsync_WithUnexpectedStatusCode_ShouldReturnUnhealthy()
    {
        Mock<ITypedHttpClient<HealthCheckTestApi>> mock = HealthChecksTestHelpers.CreateTypedHttpClientMock<HealthCheckTestApi>(
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var check = CreateCheck(mock.Object, new HttpClientHealthCheckOptions
        {
            ExpectedStatusCode = HttpStatusCode.OK,
            DegradedThresholdMs = 10_000,
            FailureThresholdMs = 30_000
        });

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("unexpected status code");
        result.Data["statusCode"].Should().Be(503);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenResponseExceedsFailureThreshold_ShouldReturnUnhealthy()
    {
        Mock<ITypedHttpClient<HealthCheckTestApi>> mock = HealthChecksTestHelpers.CreateTypedHttpClientMock<HealthCheckTestApi>();
        var check = CreateCheck(mock.Object, new HttpClientHealthCheckOptions
        {
            DegradedThresholdMs = 0,
            FailureThresholdMs = 0
        });

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("exceeded threshold");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenResponseExceedsDegradedThreshold_ShouldReturnDegraded()
    {
        Mock<ITypedHttpClient<HealthCheckTestApi>> mock = HealthChecksTestHelpers.CreateTypedHttpClientMock<HealthCheckTestApi>();
        var check = CreateCheck(mock.Object, new HttpClientHealthCheckOptions
        {
            DegradedThresholdMs = 0,
            FailureThresholdMs = 30_000
        });

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("is slow");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenContentValidationFails_ShouldReturnDegraded()
    {
        Mock<ITypedHttpClient<HealthCheckTestApi>> mock = HealthChecksTestHelpers.CreateTypedHttpClientMock<HealthCheckTestApi>(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not-matching")
            });
        var check = CreateCheck(mock.Object, new HttpClientHealthCheckOptions
        {
            ValidateResponseContent = true,
            ExpectedResponseContent = "Healthy",
            DegradedThresholdMs = 10_000,
            FailureThresholdMs = 30_000
        });

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("content validation failed");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenContentValidationPasses_ShouldReturnHealthy()
    {
        Mock<ITypedHttpClient<HealthCheckTestApi>> mock = HealthChecksTestHelpers.CreateTypedHttpClientMock<HealthCheckTestApi>(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Service is Healthy")
            });
        var check = CreateCheck(mock.Object, new HttpClientHealthCheckOptions
        {
            ValidateResponseContent = true,
            ExpectedResponseContent = "healthy",
            DegradedThresholdMs = 10_000,
            FailureThresholdMs = 30_000
        });

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHttpRequestException_ShouldReturnUnhealthy()
    {
        Mock<ITypedHttpClient<HealthCheckTestApi>> mock = HealthChecksTestHelpers.CreateTypedHttpClientMock<HealthCheckTestApi>(
            sendException: new HttpRequestException("connection refused"));
        var check = CreateCheck(mock.Object, new HttpClientHealthCheckOptions
        {
            UseHeadRequest = true,
            DegradedThresholdMs = 10_000,
            FailureThresholdMs = 30_000
        });

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Exception.Should().BeOfType<HttpRequestException>();
        result.Description.Should().Contain("connection refused");
    }

    [Fact]
    public async Task CheckHealthAsync_WithAbsoluteHealthEndpoint_ShouldUseAsIs()
    {
        Mock<ITypedHttpClient<HealthCheckTestApi>> mock = HealthChecksTestHelpers.CreateTypedHttpClientMock<HealthCheckTestApi>();
        var check = CreateCheck(mock.Object, new HttpClientHealthCheckOptions
        {
            HealthEndpoint = "https://status.example.com/ready",
            DegradedThresholdMs = 10_000,
            FailureThresholdMs = 30_000
        });

        HealthCheckResult result = await check.CheckHealthAsync(HealthChecksTestHelpers.CreateContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data["healthEndpoint"].Should().Be("https://status.example.com/ready");
    }

    private static HttpClientHealthCheck<HealthCheckTestApi> CreateCheck(
        ITypedHttpClient<HealthCheckTestApi> httpClient,
        HttpClientHealthCheckOptions? options = null)
    {
        return new HttpClientHealthCheck<HealthCheckTestApi>(
            httpClient,
            options,
            HealthChecksTestHelpers.CreateLogger<HttpClientHealthCheck<HealthCheckTestApi>>());
    }
}
