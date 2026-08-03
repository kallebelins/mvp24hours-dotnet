using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Http.Contract;
using Mvp24Hours.Infrastructure.Http.Extensions;
using Mvp24Hours.Infrastructure.Http.Options;
using Mvp24Hours.Infrastructure.Http.Serializers;

namespace Mvp24Hours.Infrastructure.Test.Http.Extensions;

[Trait("Category", "Unit")]
public class HttpClientServiceExtensionsTest
{
    [Fact]
    public void AddMvpTypedHttpClient_WithOptions_ShouldRegisterTypedClient()
    {
        var services = new ServiceCollection();

        IHttpClientBuilder builder = services.AddMvpTypedHttpClient<TestApiMarker>(options =>
        {
            options.BaseAddress = new Uri("https://api.example.com/");
            options.Timeout = TimeSpan.FromSeconds(30);
        });

        builder.Should().NotBeNull();
        services.Should().Contain(d =>
            d.ServiceType == typeof(IHttpClientSerializer) &&
            d.ImplementationType == typeof(JsonHttpClientSerializer));
        services.Should().Contain(d =>
            d.ServiceType == typeof(ITypedHttpClient<TestApiMarker>));
    }

    [Fact]
    public void AddMvpTypedHttpClient_WithHttpClientOptionsInstance_ShouldRegisterClient()
    {
        var services = new ServiceCollection();

        var options = new HttpClientOptions
        {
            Name = "TestApi",
            BaseAddress = new Uri("https://api.example.com/"),
        };

        services.AddMvpTypedHttpClient<TestApiMarker>(options);

        services.Should().Contain(d => d.ServiceType == typeof(ITypedHttpClient<TestApiMarker>));
    }

    [Fact]
    public void AddMvpTypedHttpClient_WithBuilder_ShouldApplyFluentConfiguration()
    {
        var services = new ServiceCollection();

        services.AddMvpTypedHttpClient<TestApiMarker>(builder =>
            builder.WithBaseAddress("https://api.example.com/")
                .WithTimeout(TimeSpan.FromMinutes(1)));

        services.Should().Contain(d => d.ServiceType == typeof(ITypedHttpClient<TestApiMarker>));
    }

    [Fact]
    public void AddMvpHttpClientSerializer_ShouldRegisterJsonSerializer()
    {
        var services = new ServiceCollection();

        services.AddMvpHttpClientSerializer();

        services.Should().ContainSingle(d =>
            d.ServiceType == typeof(IHttpClientSerializer) &&
            d.ImplementationType == typeof(JsonHttpClientSerializer));
    }

    [Fact]
    public void AddMvpXmlHttpClientSerializer_ShouldRegisterXmlSerializer()
    {
        var services = new ServiceCollection();

        services.AddMvpXmlHttpClientSerializer();

        services.Should().Contain(d =>
            d.ServiceType == typeof(IHttpClientSerializer) &&
            d.ImplementationType == typeof(XmlHttpClientSerializer));
    }

    [Fact]
    public void AddMvpHttpClient_WithOptions_ShouldRegisterNamedClient()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        IHttpClientBuilder builder = services.AddMvpHttpClient("ExternalApi", options =>
        {
            options.BaseAddress = new Uri("https://external.example.com/");
            options.Timeout = TimeSpan.FromSeconds(15);
            options.AcceptHeader = "application/json";
            options.UserAgent = "Mvp24Hours.Test";
            options.DefaultHeaders["X-Custom"] = "test";
        });

        builder.Should().NotBeNull();
        services.Should().Contain(d => d.ServiceType == typeof(IHttpClientFactory));
    }

    [Fact]
    public void AddMvpHttpClient_WithBuilder_ShouldApplyFluentConfiguration()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvpHttpClient("PartnerApi", builder =>
            builder.WithBaseAddress("https://partner.example.com/")
                .WithTimeout(TimeSpan.FromSeconds(20)));

        services.Should().Contain(d => d.ServiceType == typeof(IHttpClientFactory));
    }

    [Fact]
    public void AddMvpTypedHttpClient_WithResilienceOptions_ShouldRegisterPolicies()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvpTypedHttpClient<TestApiMarker>(options =>
        {
            options.BaseAddress = new Uri("https://api.example.com/");
            options.EnableLogging = true;
            options.EnableTelemetry = true;
            options.PropagateCorrelationId = true;
            options.RetryPolicy = new RetryPolicyOptions
            {
                Enabled = true,
                MaxRetries = 2,
                InitialDelay = TimeSpan.FromMilliseconds(100),
                BackoffType = BackoffType.Exponential
            };
            options.CircuitBreakerPolicy = new CircuitBreakerPolicyOptions
            {
                Enabled = true,
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(10)
            };
            options.TimeoutPolicy = new TimeoutPolicyOptions
            {
                Enabled = true,
                Timeout = TimeSpan.FromSeconds(5)
            };
        });

        services.Should().Contain(d => d.ServiceType == typeof(ITypedHttpClient<TestApiMarker>));
    }

    [Fact]
    public void AddMvpHttpClientSerializerGeneric_ShouldRegisterCustomSerializer()
    {
        var services = new ServiceCollection();

        services.AddMvpHttpClientSerializer<XmlHttpClientSerializer>();

        services.Should().Contain(d =>
            d.ServiceType == typeof(IHttpClientSerializer) &&
            d.ImplementationType == typeof(XmlHttpClientSerializer));
        services.Should().Contain(d =>
            d.ServiceType == typeof(IHttpContentSerializer) &&
            d.ImplementationType == typeof(XmlHttpClientSerializer));
    }

    [Fact]
    public void AddMvpMessagePackHttpClientSerializer_ShouldRegisterMessagePackSerializer()
    {
        var services = new ServiceCollection();

        services.AddMvpMessagePackHttpClientSerializer();

        services.Should().Contain(d =>
            d.ServiceType == typeof(IHttpClientSerializer) &&
            d.ImplementationType == typeof(MessagePackHttpClientSerializer));
    }

    [Fact]
    public void AddMvpStandardHandlers_ShouldChainHandlerExtensions()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        IHttpClientBuilder builder = services.AddHttpClient("Standard")
            .AddMvpStandardHandlers(options =>
            {
                options.EnableTelemetry = true;
                options.EnableLogging = true;
                options.PropagateCorrelationId = true;
                options.EnableCompression = true;
            });

        builder.Should().NotBeNull();
    }

    [Fact]
    public void AddMvpBearerAuthentication_WithStaticToken_ShouldRegisterHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        IHttpClientBuilder builder = services.AddHttpClient("AuthApi")
            .AddMvpBearerAuthentication("static-token");

        builder.Should().NotBeNull();
    }

    [Fact]
    public void AddMvpApiKeyAuthentication_ShouldRegisterHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        IHttpClientBuilder builder = services.AddHttpClient("KeyApi")
            .AddMvpApiKeyAuthentication("secret-key");

        builder.Should().NotBeNull();
    }

    [Fact]
    public void AddMvpBasicAuthentication_ShouldRegisterHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        IHttpClientBuilder builder = services.AddHttpClient("BasicApi")
            .AddMvpBasicAuthentication("user", "pass");

        builder.Should().NotBeNull();
    }

    private sealed class TestApiMarker;
}
