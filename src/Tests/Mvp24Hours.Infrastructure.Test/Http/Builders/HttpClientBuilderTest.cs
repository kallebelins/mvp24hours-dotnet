using System.Security.Cryptography.X509Certificates;
using Mvp24Hours.Infrastructure.Http.Builders;
using Mvp24Hours.Infrastructure.Http.Options;

namespace Mvp24Hours.Infrastructure.Test.Http.Builders;

[Trait("Category", "Unit")]
public class HttpClientBuilderTest
{
    [Fact]
    public void Create_ShouldReturnEmptyOptions()
    {
        HttpClientOptions options = HttpClientBuilder.Create().Build();

        options.Name.Should().BeEmpty();
        options.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        options.EnableDecompression.Should().BeTrue();
    }

    [Fact]
    public void Create_WithName_ShouldSetName()
    {
        HttpClientOptions options = HttpClientBuilder.Create("PartnerApi").Build();

        options.Name.Should().Be("PartnerApi");
    }

    [Fact]
    public void WithName_Null_ShouldThrow()
    {
        Action act = () => HttpClientBuilder.Create().WithName(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithBaseAddress_StringAndUri_ShouldConfigureAddress()
    {
        HttpClientOptions options = HttpClientBuilder.Create()
            .WithBaseAddress("https://api.example.com/")
            .Build();

        options.BaseAddress.Should().Be(new Uri("https://api.example.com/"));

        HttpClientOptions uriOptions = HttpClientBuilder.Create()
            .WithBaseAddress(new Uri("https://other.example.com/"))
            .Build();

        uriOptions.BaseAddress!.Host.Should().Be("other.example.com");
    }

    [Fact]
    public void WithTimeout_ShouldConfigureTimeout()
    {
        HttpClientOptions options = HttpClientBuilder.Create()
            .WithTimeout(TimeSpan.FromMinutes(2))
            .WithTimeout(45)
            .Build();

        options.Timeout.Should().Be(TimeSpan.FromSeconds(45));
    }

    [Fact]
    public void WithHeadersAndAuth_ShouldConfigureDefaults()
    {
        HttpClientOptions options = HttpClientBuilder.Create()
            .WithDefaultHeader("X-Custom", "value")
            .WithDefaultHeaders(new Dictionary<string, string> { ["X-Second"] = "2" })
            .WithAcceptHeader("application/json")
            .WithUserAgent("Mvp24Hours.Test")
            .WithBearerToken("token-123")
            .WithApiKey("secret-key", "X-Api-Key")
            .Build();

        options.DefaultHeaders["X-Custom"].Should().Be("value");
        options.DefaultHeaders["X-Second"].Should().Be("2");
        options.AcceptHeader.Should().Be("application/json");
        options.UserAgent.Should().Be("Mvp24Hours.Test");
        options.DefaultHeaders["Authorization"].Should().Be("Bearer token-123");
        options.DefaultHeaders["X-Api-Key"].Should().Be("secret-key");

        HttpClientOptions basic = HttpClientBuilder.Create()
            .WithBasicAuth("user", "pass")
            .Build();

        basic.DefaultHeaders["Authorization"].Should().StartWith("Basic ");
    }

    [Fact]
    public void CertificateConfiguration_ShouldSetCertificateOptions()
    {
        HttpClientOptions file = HttpClientBuilder.Create()
            .WithCertificateFromFile("cert.pfx", "pwd")
            .Build();
        HttpClientOptions base64 = HttpClientBuilder.Create()
            .WithCertificateFromBase64("base64-cert", "pwd2")
            .Build();
        HttpClientOptions thumbprint = HttpClientBuilder.Create()
            .WithCertificateFromStore("THUMB", StoreLocation.LocalMachine, StoreName.Root)
            .Build();
        HttpClientOptions subject = HttpClientBuilder.Create()
            .WithCertificateFromStoreBySubject("CN=Test", StoreLocation.CurrentUser, StoreName.My)
            .DisableServerCertificateValidation()
            .Build();

        file.Certificate!.FilePath.Should().Be("cert.pfx");
        base64.Certificate!.Base64Certificate.Should().Be("base64-cert");
        thumbprint.Certificate!.Thumbprint.Should().Be("THUMB");
        subject.Certificate!.SubjectName.Should().Be("CN=Test");
        subject.ValidateServerCertificate.Should().BeFalse();
    }

    [Fact]
    public void RetryPolicyConfiguration_ShouldConfigureOptions()
    {
        HttpClientOptions retry = HttpClientBuilder.Create()
            .WithRetry(maxRetries: 5, initialDelay: TimeSpan.FromMilliseconds(250))
            .Build();
        HttpClientOptions exponential = HttpClientBuilder.Create()
            .WithExponentialRetry(4, TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(1))
            .Build();
        HttpClientOptions configured = HttpClientBuilder.Create()
            .WithRetry(r => r.Enabled = true)
            .WithoutRetry()
            .Build();

        retry.RetryPolicy!.Enabled.Should().BeTrue();
        retry.RetryPolicy.MaxRetries.Should().Be(5);
        exponential.RetryPolicy!.BackoffType.Should().Be(BackoffType.Exponential);
        configured.RetryPolicy!.Enabled.Should().BeFalse();
    }

    [Fact]
    public void CircuitBreakerAndTimeoutPolicy_ShouldConfigureOptions()
    {
        HttpClientOptions cb = HttpClientBuilder.Create()
            .WithCircuitBreaker(3, TimeSpan.FromSeconds(15))
            .Build();
        HttpClientOptions cbConfigured = HttpClientBuilder.Create()
            .WithCircuitBreaker(c => c.Enabled = true)
            .WithoutCircuitBreaker()
            .Build();
        HttpClientOptions timeoutPolicy = HttpClientBuilder.Create()
            .WithTimeoutPolicy(TimeSpan.FromSeconds(8))
            .WithoutTimeoutPolicy()
            .Build();

        cb.CircuitBreakerPolicy!.Enabled.Should().BeTrue();
        cb.CircuitBreakerPolicy.FailureThreshold.Should().Be(3);
        cbConfigured.CircuitBreakerPolicy!.Enabled.Should().BeFalse();
        timeoutPolicy.TimeoutPolicy!.Enabled.Should().BeFalse();
    }

    [Fact]
    public void HandlerLifetimeAndPropagation_ShouldConfigureOptions()
    {
        HttpClientOptions options = HttpClientBuilder.Create()
            .WithHandlerLifetime(TimeSpan.FromMinutes(10))
            .WithCorrelationIdPropagation(false)
            .WithAuthorizationPropagation(true)
            .WithHeaderPropagation("X-Tenant", "X-Trace")
            .Build();

        options.HandlerLifetime.Should().Be(TimeSpan.FromMinutes(10));
        options.PropagateCorrelationId.Should().BeFalse();
        options.PropagateAuthorization.Should().BeTrue();
        options.PropagateHeaders.Should().Contain("X-Tenant");
        options.PropagateHeaders.Should().Contain("X-Trace");
    }

    [Fact]
    public void LoggingConfiguration_ShouldConfigureOptions()
    {
        HttpClientOptions detailed = HttpClientBuilder.Create()
            .WithDetailedLogging()
            .Build();
        HttpClientOptions configured = HttpClientBuilder.Create()
            .WithLogging(o =>
            {
                o.LogRequestBody = true;
                o.LogResponseBody = false;
            })
            .WithoutLogging()
            .Build();

        detailed.EnableLogging.Should().BeTrue();
        detailed.LoggingOptions!.LogRequestBody.Should().BeTrue();
        configured.EnableLogging.Should().BeFalse();
    }

    [Fact]
    public void TelemetryProxyAndHttpVersion_ShouldConfigureOptions()
    {
        HttpClientOptions options = HttpClientBuilder.Create()
            .WithTelemetry(false)
            .WithProxy("http://proxy.local:8080", "proxy-user", "proxy-pass")
            .UseHttp2()
            .UseHttp11()
            .Build();

        options.EnableTelemetry.Should().BeFalse();
        options.Proxy!.Enabled.Should().BeTrue();
        options.Proxy.Address.Should().Be("http://proxy.local:8080");
        options.HttpVersion.Should().Be(new Version(1, 1));
    }

    [Fact]
    public void RedirectCookieAndDecompression_ShouldConfigureOptions()
    {
        HttpClientOptions options = HttpClientBuilder.Create()
            .WithRedirects(follow: false, maxRedirects: 10)
            .WithoutRedirects()
            .WithCookies(false)
            .WithoutCookies()
            .WithDecompression(false)
            .WithoutDecompression()
            .Build();

        options.FollowRedirects.Should().BeFalse();
        options.UseCookies.Should().BeFalse();
        options.EnableDecompression.Should().BeFalse();
    }

    [Fact]
    public void ImplicitConversion_ShouldReturnBuiltOptions()
    {
        HttpClientOptions options = HttpClientBuilder.Create("Implicit").WithTimeout(10);

        options.Name.Should().Be("Implicit");
        options.Timeout.Should().Be(TimeSpan.FromSeconds(10));
    }
}
