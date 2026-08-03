using System.Net;
using Microsoft.AspNetCore.Builder;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Extensions;
using Mvp24Hours.WebAPI.Test.Support;

namespace Mvp24Hours.WebAPI.Test.Integration;

[Trait("Category", "Unit")]
public class MiddlewarePipelineFactoryTest
{
    [Fact]
    public async Task SecurityHeadersPipeline_Should_AddHeaders_OnHttpsRequest()
    {
        await using WebApiTestApplicationFactory factory = new WebApiTestApplicationFactory()
            .ConfigureServices(services => services.AddMvp24HoursSecurityHeaders(options =>
            {
                options.EnableHsts = true;
                options.HstsPreload = true;
                options.EnableContentSecurityPolicy = true;
                options.ContentSecurityPolicy = "default-src 'self'";
                options.EnableXFrameOptions = true;
                options.XFrameOptions = XFrameOptionsValue.SameOrigin;
                options.EnableXXssProtection = true;
                options.XXssProtection = XssProtectionMode.Block;
                options.EnableReferrerPolicy = true;
                options.ReferrerPolicy = ReferrerPolicyValue.StrictOriginWhenCrossOrigin;
                options.EnablePermissionsPolicy = true;
                options.EnableCacheControlForSensitiveEndpoints = true;
                options.SensitivePaths = ["/api/admin/**"];
                options.CustomHeaders = new Dictionary<string, string> { ["X-Custom"] = "mvp24" };
            }))
            .ConfigurePipeline(app => app.UseMvp24HoursSecurityHeaders())
            .ConfigureEndpoints(endpoints =>
            {
                endpoints.MapGet("/api/admin/users", () => "admin");
                endpoints.MapGet("/health", () => "ok");
            });

        using HttpClient client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        using HttpResponseMessage response = await client.GetAsync("/api/admin/users");
        string body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Be("admin");
        response.Headers.Should().ContainKey("Content-Security-Policy");
        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.Should().ContainKey("Referrer-Policy");
        response.Headers.Should().ContainKey("X-Custom");
        response.Headers.Should().ContainKey("Cache-Control");
    }

    [Fact]
    public async Task RequestTelemetryPipeline_Should_ProcessAuthenticatedRequest()
    {
        await using WebApiTestApplicationFactory factory = new WebApiTestApplicationFactory()
            .ConfigureServices(services => services.AddMvp24HoursRequestTelemetry(options =>
            {
                options.EnableTracing = true;
                options.EnableMetrics = true;
                options.EnrichWithUser = true;
                options.EnrichWithTenant = true;
                options.EnrichWithHeaders = true;
                options.CustomTags = new Dictionary<string, string> { ["env"] = "test" };
                options.DurationBuckets = [100, 500, 1000];
            }))
            .ConfigurePipeline(app => app.UseMvp24HoursRequestTelemetry())
            .ConfigureEndpoints(endpoints => endpoints.MapGet("/api/profile", () => "profile"));

        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Correlation-Id", "corr-telemetry-1");
        client.DefaultRequestHeaders.Add("X-Causation-Id", "cause-1");
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "tenant-42");
        client.DefaultRequestHeaders.Add("Accept", "application/json");

        using HttpResponseMessage response = await client.GetAsync("/api/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey("X-Correlation-ID");
    }

    [Fact]
    public async Task IpFilteringPipeline_Should_BlockNonWhitelistedClient()
    {
        await using WebApiTestApplicationFactory factory = new WebApiTestApplicationFactory()
            .ConfigureServices(services => services.AddMvp24HoursIpFiltering(options =>
            {
                options.Enabled = true;
                options.Mode = IpFilteringMode.Whitelist;
                options.WhitelistedIps = ["127.0.0.1"];
                options.UseForwardedHeaders = true;
            }))
            .ConfigurePipeline(app => app.UseMvp24HoursIpFiltering())
            .ConfigureEndpoints(endpoints => endpoints.MapGet("/api/secure", () => "secure"));

        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.10");

        using HttpResponseMessage response = await client.GetAsync("/api/secure");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task IpFilteringPipeline_Should_Bypass_WhenPathExcluded()
    {
        await using WebApiTestApplicationFactory factory = new WebApiTestApplicationFactory()
            .ConfigureServices(services => services.AddMvp24HoursIpFiltering(options =>
            {
                options.Enabled = true;
                options.Mode = IpFilteringMode.Whitelist;
                options.WhitelistedIps = ["203.0.113.10"];
                options.ExcludedPaths = ["/api/open"];
            }))
            .ConfigurePipeline(app => app.UseMvp24HoursIpFiltering())
            .ConfigureEndpoints(endpoints => endpoints.MapGet("/api/open", () => "open"));

        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/open");
        string body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Be("open");
    }

    [Fact]
    public async Task RequestTelemetryPipeline_Should_SkipExcludedPath()
    {
        int counter = 0;

        await using WebApiTestApplicationFactory factory = new WebApiTestApplicationFactory()
            .ConfigureServices(services => services.AddMvp24HoursRequestTelemetry(options =>
            {
                options.EnableTracing = false;
                options.EnableMetrics = false;
                options.ExcludedPaths = ["/metrics"];
            }))
            .ConfigurePipeline(app => app.UseMvp24HoursRequestTelemetry())
            .ConfigureEndpoints(endpoints => endpoints.MapGet("/metrics", () => $"count:{++counter}"));

        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage first = await client.GetAsync("/metrics");
        using HttpResponseMessage second = await client.GetAsync("/metrics");

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        counter.Should().Be(2);
    }
}
