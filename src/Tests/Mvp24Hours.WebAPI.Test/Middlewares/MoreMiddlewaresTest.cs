using System.IO.Compression;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Middlewares;
using Mvp24Hours.WebAPI.Test.Support;

namespace Mvp24Hours.WebAPI.Test.Middlewares;

[Trait("Category", "Unit")]
public class MoreMiddlewaresTest
{
    // -----------------------------------------------------------------------
    // CacheControlMiddleware
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CacheControlMiddleware_Should_Bypass_WhenDisabled()
    {
        var called = false;
        var options = new CacheControlOptions { Enabled = false };
        var sut = new CacheControlMiddleware(_ => { called = true; return Task.CompletedTask; },
            Options.Create(options), NullLogger<CacheControlMiddleware>.Instance);

        await sut.InvokeAsync(WebApiTestHelpers.CreateHttpContext());

        called.Should().BeTrue();
    }

    [Fact]
    public async Task CacheControlMiddleware_Should_SetCacheControlHeader_WithPublicPolicy()
    {
        var options = new CacheControlOptions
        {
            Enabled = true,
            DefaultPolicy = new CacheControlPolicy { Public = true, MaxAge = TimeSpan.FromSeconds(60) }
        };
        var context = WebApiTestHelpers.CreateHttpContext(path: "/api/data");
        var sut = new CacheControlMiddleware(async c =>
        {
            c.Response.StatusCode = 200;
            await c.Response.WriteAsync("ok");
        }, Options.Create(options), NullLogger<CacheControlMiddleware>.Instance);

        await sut.InvokeAsync(context);

        context.Response.Headers.Should().ContainKey("Cache-Control");
        context.Response.Headers["Cache-Control"].ToString().Should().Contain("public");
        context.Response.Headers["Cache-Control"].ToString().Should().Contain("max-age=60");
    }

    [Fact]
    public async Task CacheControlMiddleware_Should_SetNoStore_WhenPolicyHasNoStore()
    {
        var options = new CacheControlOptions
        {
            Enabled = true,
            DefaultPolicy = new CacheControlPolicy { NoStore = true }
        };
        var context = WebApiTestHelpers.CreateHttpContext();
        var sut = new CacheControlMiddleware(async c =>
        {
            c.Response.StatusCode = 200;
            await c.Response.WriteAsync("data");
        }, Options.Create(options), NullLogger<CacheControlMiddleware>.Instance);

        await sut.InvokeAsync(context);

        context.Response.Headers["Cache-Control"].ToString().Should().Contain("no-store");
    }

    [Fact]
    public async Task CacheControlMiddleware_Should_Bypass_WhenPathExcluded()
    {
        var options = new CacheControlOptions
        {
            Enabled = true,
            DefaultPolicy = new CacheControlPolicy { Public = true, MaxAge = TimeSpan.FromSeconds(60) },
            ExcludedPaths = ["/health"]
        };
        var context = WebApiTestHelpers.CreateHttpContext(path: "/health");
        var sut = new CacheControlMiddleware(async c =>
        {
            c.Response.StatusCode = 200;
            await c.Response.WriteAsync("healthy");
        }, Options.Create(options), NullLogger<CacheControlMiddleware>.Instance);

        await sut.InvokeAsync(context);

        context.Response.Headers.Should().NotContainKey("Cache-Control");
    }

    [Fact]
    public async Task CacheControlMiddleware_Should_UseRoutePolicy_WhenMatches()
    {
        var options = new CacheControlOptions
        {
            Enabled = true,
            RoutePolicies = new Dictionary<string, CacheControlPolicy>
            {
                ["/api/static"] = new CacheControlPolicy { Immutable = true, Public = true }
            }
        };
        var context = WebApiTestHelpers.CreateHttpContext(path: "/api/static/logo.png");
        var sut = new CacheControlMiddleware(async c =>
        {
            c.Response.StatusCode = 200;
            await c.Response.WriteAsync("img");
        }, Options.Create(options), NullLogger<CacheControlMiddleware>.Instance);

        await sut.InvokeAsync(context);

        context.Response.Headers["Cache-Control"].ToString().Should().Contain("immutable");
    }

    [Fact]
    public async Task CacheControlMiddleware_Should_SetPrivate_WhenPrivatePolicy()
    {
        var options = new CacheControlOptions
        {
            Enabled = true,
            DefaultPolicy = new CacheControlPolicy { Private = true, MaxAge = TimeSpan.FromSeconds(300) }
        };
        var context = WebApiTestHelpers.CreateHttpContext();
        var sut = new CacheControlMiddleware(async c =>
        {
            c.Response.StatusCode = 200;
            await c.Response.WriteAsync("user-data");
        }, Options.Create(options), NullLogger<CacheControlMiddleware>.Instance);

        await sut.InvokeAsync(context);

        context.Response.Headers["Cache-Control"].ToString().Should().Contain("private");
    }

    // -----------------------------------------------------------------------
    // CorsMiddleware
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CorsMiddleware_Should_SetWildcardHeaders_WhenAllowAll()
    {
        var options = new CorsOptions { AllowAll = true };
        var context = WebApiTestHelpers.CreateHttpContext();
        var sut = new CorsMiddleware(_ => Task.CompletedTask, Options.Create(options));

        await sut.Invoke(context);

        context.Response.Headers["Access-Control-Allow-Origin"].ToString().Should().Be("*");
        context.Response.Headers["Access-Control-Allow-Methods"].ToString().Should().Be("*");
        context.Response.Headers["Access-Control-Allow-Headers"].ToString().Should().Be("*");
    }

    [Fact]
    public async Task CorsMiddleware_Should_SetSpecificOrigin_WhenNotAllowAll()
    {
        var options = new CorsOptions
        {
            AllowAll = false,
            Origin = "https://example.com",
            Methods = "GET,POST",
            Headers = "Content-Type"
        };
        var context = WebApiTestHelpers.CreateHttpContext();
        var sut = new CorsMiddleware(_ => Task.CompletedTask, Options.Create(options));

        await sut.Invoke(context);

        context.Response.Headers["Access-Control-Allow-Origin"].ToString().Should().Be("https://example.com");
        context.Response.Headers["Access-Control-Allow-Methods"].ToString().Should().Be("GET,POST");
    }

    [Fact]
    public async Task CorsMiddleware_Should_Return200_ForOptionsRequest()
    {
        var options = new CorsOptions { AllowAll = true, AllowRequestOptions = true };
        var context = WebApiTestHelpers.CreateHttpContext(method: "OPTIONS");
        var sut = new CorsMiddleware(_ => Task.CompletedTask, Options.Create(options));

        await sut.Invoke(context);

        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task CorsMiddleware_Should_SetCredentials_WhenProvided()
    {
        var options = new CorsOptions { AllowAll = true, Credentials = "true" };
        var context = WebApiTestHelpers.CreateHttpContext();
        var sut = new CorsMiddleware(_ => Task.CompletedTask, Options.Create(options));

        await sut.Invoke(context);

        context.Response.Headers["Access-Control-Allow-Credentials"].ToString().Should().Be("true");
    }

    // -----------------------------------------------------------------------
    // InputSanitizationMiddleware
    // -----------------------------------------------------------------------

    [Fact]
    public async Task InputSanitizationMiddleware_Should_Bypass_WhenDisabled()
    {
        var called = false;
        var options = new InputSanitizationOptions { Enabled = false };
        var sut = new InputSanitizationMiddleware(_ => { called = true; return Task.CompletedTask; },
            Options.Create(options), NullLogger<InputSanitizationMiddleware>.Instance);

        await sut.InvokeAsync(WebApiTestHelpers.CreateHttpContext());

        called.Should().BeTrue();
    }

    [Fact]
    public async Task InputSanitizationMiddleware_Should_Return400_OnXssInQueryString()
    {
        var options = new InputSanitizationOptions
        {
            Enabled = true,
            Mode = SanitizationMode.Validate,
            SanitizeQueryStrings = true,
            EnableXssSanitization = true
        };
        var context = WebApiTestHelpers.CreateHttpContext(path: "/api/search");
        context.Request.QueryString = new QueryString("?q=<script>alert(1)</script>");
        var sut = new InputSanitizationMiddleware(_ => Task.CompletedTask,
            Options.Create(options), NullLogger<InputSanitizationMiddleware>.Instance);

        await sut.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task InputSanitizationMiddleware_Should_Allow_CleanInput()
    {
        var called = false;
        var options = new InputSanitizationOptions
        {
            Enabled = true,
            SanitizeQueryStrings = true,
            EnableXssSanitization = true
        };
        var context = WebApiTestHelpers.CreateHttpContext(path: "/api/search");
        context.Request.QueryString = new QueryString("?q=hello+world");
        var sut = new InputSanitizationMiddleware(_ => { called = true; return Task.CompletedTask; },
            Options.Create(options), NullLogger<InputSanitizationMiddleware>.Instance);

        await sut.InvokeAsync(context);

        called.Should().BeTrue();
    }

    [Fact]
    public async Task InputSanitizationMiddleware_Should_LogOnly_WhenModeIsLogOnly()
    {
        var called = false;
        var options = new InputSanitizationOptions
        {
            Enabled = true,
            Mode = SanitizationMode.LogOnly,
            SanitizeQueryStrings = true,
            EnableXssSanitization = true
        };
        var context = WebApiTestHelpers.CreateHttpContext(path: "/api/search");
        context.Request.QueryString = new QueryString("?q=<script>xss</script>");
        var sut = new InputSanitizationMiddleware(_ => { called = true; return Task.CompletedTask; },
            Options.Create(options), NullLogger<InputSanitizationMiddleware>.Instance);

        await sut.InvokeAsync(context);

        called.Should().BeTrue();
    }

    [Fact]
    public async Task InputSanitizationMiddleware_Should_Bypass_ForExcludedPath()
    {
        var called = false;
        var options = new InputSanitizationOptions
        {
            Enabled = true,
            Mode = SanitizationMode.Validate,
            SanitizeQueryStrings = true,
            EnableXssSanitization = true,
            ExcludedPaths = ["/swagger/**"]
        };
        var context = WebApiTestHelpers.CreateHttpContext(path: "/swagger/index.html");
        context.Request.QueryString = new QueryString("?q=<script>xss</script>");
        var sut = new InputSanitizationMiddleware(_ => { called = true; return Task.CompletedTask; },
            Options.Create(options), NullLogger<InputSanitizationMiddleware>.Instance);

        await sut.InvokeAsync(context);

        called.Should().BeTrue();
    }

    // -----------------------------------------------------------------------
    // RequestDecompressionMiddleware
    // -----------------------------------------------------------------------

    [Fact]
    public async Task RequestDecompressionMiddleware_Should_Bypass_WhenDisabled()
    {
        var called = false;
        var options = new RequestDecompressionOptions { Enabled = false };
        var sut = new RequestDecompressionMiddleware(_ => { called = true; return Task.CompletedTask; },
            Options.Create(options), NullLogger<RequestDecompressionMiddleware>.Instance);

        await sut.InvokeAsync(WebApiTestHelpers.CreateHttpContext());

        called.Should().BeTrue();
    }

    [Fact]
    public async Task RequestDecompressionMiddleware_Should_PassThrough_WhenNoContentEncoding()
    {
        var called = false;
        var options = new RequestDecompressionOptions { Enabled = true };
        var sut = new RequestDecompressionMiddleware(_ => { called = true; return Task.CompletedTask; },
            Options.Create(options), NullLogger<RequestDecompressionMiddleware>.Instance);

        var context = WebApiTestHelpers.CreateHttpContext(method: "POST", body: "{\"key\":\"value\"}");

        await sut.InvokeAsync(context);

        called.Should().BeTrue();
    }

    [Fact]
    public async Task RequestDecompressionMiddleware_Should_PassThrough_WhenUnsupportedEncoding()
    {
        var called = false;
        var options = new RequestDecompressionOptions
        {
            Enabled = true,
            SupportedEncodings = ["gzip", "deflate", "br"]
        };
        var sut = new RequestDecompressionMiddleware(_ => { called = true; return Task.CompletedTask; },
            Options.Create(options), NullLogger<RequestDecompressionMiddleware>.Instance);

        var context = WebApiTestHelpers.CreateHttpContext(method: "POST");
        context.Request.Headers.ContentEncoding = "zstd";

        await sut.InvokeAsync(context);

        called.Should().BeTrue();
    }

    [Fact]
    public async Task RequestDecompressionMiddleware_Should_DecompressGzipBody()
    {
        var options = new RequestDecompressionOptions
        {
            Enabled = true,
            SupportedEncodings = ["gzip"]
        };
        string originalContent = "{\"name\":\"test\"}";
        byte[] compressed = CompressGzip(originalContent);

        var context = WebApiTestHelpers.CreateHttpContext();
        context.Request.Method = "POST";
        context.Request.Body = new MemoryStream(compressed);
        context.Request.ContentLength = compressed.Length;
        context.Request.Headers.ContentEncoding = "gzip";

        string? capturedBody = null;
        var sut = new RequestDecompressionMiddleware(async c =>
        {
            using var reader = new StreamReader(c.Request.Body);
            capturedBody = await reader.ReadToEndAsync();
        }, Options.Create(options), NullLogger<RequestDecompressionMiddleware>.Instance);

        await sut.InvokeAsync(context);

        capturedBody.Should().Be(originalContent);
    }

    [Fact]
    public async Task RequestDecompressionMiddleware_Should_Bypass_ForExcludedPath()
    {
        var called = false;
        var options = new RequestDecompressionOptions
        {
            Enabled = true,
            ExcludedPaths = ["/health"]
        };
        var sut = new RequestDecompressionMiddleware(_ => { called = true; return Task.CompletedTask; },
            Options.Create(options), NullLogger<RequestDecompressionMiddleware>.Instance);

        var context = WebApiTestHelpers.CreateHttpContext(path: "/health");
        context.Request.Headers.ContentEncoding = "gzip";

        await sut.InvokeAsync(context);

        called.Should().BeTrue();
    }

    // -----------------------------------------------------------------------
    // CachingMiddleware
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CachingMiddleware_Should_Bypass_WhenDisabled()
    {
        var called = false;
        var options = new ResponseCachingOptions { Enabled = false };
        var sut = new CachingMiddleware(_ => { called = true; return Task.CompletedTask; },
            Options.Create(options), NullLogger<CachingMiddleware>.Instance);

        await sut.InvokeAsync(WebApiTestHelpers.CreateHttpContext());

        called.Should().BeTrue();
    }

    [Fact]
    public async Task CachingMiddleware_Should_CallNext_WhenEnabled()
    {
        var called = false;
        var options = new ResponseCachingOptions { Enabled = true };
        var sut = new CachingMiddleware(_ => { called = true; return Task.CompletedTask; },
            Options.Create(options), NullLogger<CachingMiddleware>.Instance);

        await sut.InvokeAsync(WebApiTestHelpers.CreateHttpContext());

        called.Should().BeTrue();
    }

    [Fact]
    public async Task CachingMiddleware_Should_ApplyCacheProfile_WhenProfileExists()
    {
        var options = new ResponseCachingOptions
        {
            Enabled = true,
            DefaultProfile = "default",
            Profiles = new Dictionary<string, CacheProfile>
            {
                ["default"] = new CacheProfile { Duration = 60, Location = ResponseCacheLocation.Any }
            }
        };
        var context = WebApiTestHelpers.CreateHttpContext();
        var sut = new CachingMiddleware(_ => Task.CompletedTask,
            Options.Create(options), NullLogger<CachingMiddleware>.Instance);

        await sut.InvokeAsync(context);

        context.Response.GetTypedHeaders().CacheControl.Should().NotBeNull();
    }

    [Fact]
    public async Task CachingMiddleware_Should_Bypass_ForExcludedPath()
    {
        var called = false;
        var options = new ResponseCachingOptions
        {
            Enabled = true,
            ExcludedPaths = ["/health"]
        };
        var sut = new CachingMiddleware(_ => { called = true; return Task.CompletedTask; },
            Options.Create(options), NullLogger<CachingMiddleware>.Instance);

        var context = WebApiTestHelpers.CreateHttpContext(path: "/health");
        await sut.InvokeAsync(context);

        called.Should().BeTrue();
    }

    // -----------------------------------------------------------------------
    // RequestTimeoutMiddleware
    // -----------------------------------------------------------------------

    [Fact]
    public async Task RequestTimeoutMiddleware_Should_Bypass_WhenDisabled()
    {
        var called = false;
        var options = new RequestTimeoutOptions { Enabled = false };
        var sut = new RequestTimeoutMiddleware(_ => { called = true; return Task.CompletedTask; },
            Options.Create(options), NullLogger<RequestTimeoutMiddleware>.Instance);

        await sut.InvokeAsync(WebApiTestHelpers.CreateHttpContext());

        called.Should().BeTrue();
    }

    [Fact]
    public async Task RequestTimeoutMiddleware_Should_Bypass_ForExcludedPath()
    {
        var called = false;
        var options = new RequestTimeoutOptions
        {
            Enabled = true,
            DefaultTimeout = TimeSpan.FromSeconds(1),
            ExcludedPaths = ["/health"]
        };
        var sut = new RequestTimeoutMiddleware(_ => { called = true; return Task.CompletedTask; },
            Options.Create(options), NullLogger<RequestTimeoutMiddleware>.Instance);

        var context = WebApiTestHelpers.CreateHttpContext(path: "/health");
        await sut.InvokeAsync(context);

        called.Should().BeTrue();
    }

    [Fact]
    public async Task RequestTimeoutMiddleware_Should_CompleteNormally_WhenRequestIsFast()
    {
        var called = false;
        var options = new RequestTimeoutOptions
        {
            Enabled = true,
            DefaultTimeout = TimeSpan.FromSeconds(30)
        };
        var sut = new RequestTimeoutMiddleware(_ => { called = true; return Task.CompletedTask; },
            Options.Create(options), NullLogger<RequestTimeoutMiddleware>.Instance);

        await sut.InvokeAsync(WebApiTestHelpers.CreateHttpContext());

        called.Should().BeTrue();
    }

    [Fact]
    public async Task RequestTimeoutMiddleware_Should_Return408_WhenTimeout()
    {
        var options = new RequestTimeoutOptions
        {
            Enabled = true,
            DefaultTimeout = TimeSpan.FromMilliseconds(1)
        };
        var context = WebApiTestHelpers.CreateHttpContext();
        var sut = new RequestTimeoutMiddleware(async c =>
        {
            await Task.Delay(200, c.RequestAborted);
        }, Options.Create(options), NullLogger<RequestTimeoutMiddleware>.Instance);

        await sut.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(408);
    }

    [Fact]
    public async Task RequestTimeoutMiddleware_Should_UseEndpointTimeout_WhenPathMatches()
    {
        var called = false;
        var options = new RequestTimeoutOptions
        {
            Enabled = true,
            DefaultTimeout = TimeSpan.FromSeconds(30),
            EndpointTimeouts = new Dictionary<string, TimeSpan>
            {
                ["/api/slow"] = TimeSpan.FromSeconds(60)
            }
        };
        var sut = new RequestTimeoutMiddleware(_ => { called = true; return Task.CompletedTask; },
            Options.Create(options), NullLogger<RequestTimeoutMiddleware>.Instance);

        var context = WebApiTestHelpers.CreateHttpContext(path: "/api/slow/operation");
        await sut.InvokeAsync(context);

        called.Should().BeTrue();
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static byte[] CompressGzip(string content)
    {
        using var ms = new MemoryStream();
        using (var gz = new GZipStream(ms, CompressionMode.Compress, leaveOpen: true))
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content);
            gz.Write(bytes, 0, bytes.Length);
        }
        return ms.ToArray();
    }
}
