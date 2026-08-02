using Mvp24Hours.WebAPI.Configuration;

namespace Mvp24Hours.WebAPI.Test.Configuration;

[Trait("Category", "Unit")]
public class MoreConfigurationOptionsTest
{
    // -----------------------------------------------------------------------
    // CacheControlOptions
    // -----------------------------------------------------------------------

    [Fact]
    public void CacheControlOptions_Should_BeEnabledByDefault()
    {
        var sut = new CacheControlOptions();

        sut.Enabled.Should().BeTrue();
        sut.DefaultPolicy.Should().BeNull();
        sut.RoutePolicies.Should().BeEmpty();
        sut.ExcludedPaths.Should().BeEmpty();
    }

    [Fact]
    public void CacheControlPolicy_Should_AllowSettingDirectives()
    {
        var sut = new CacheControlPolicy
        {
            Public = true,
            MaxAge = TimeSpan.FromMinutes(10),
            MustRevalidate = true,
            Immutable = false
        };

        sut.Public.Should().BeTrue();
        sut.MaxAge.Should().Be(TimeSpan.FromMinutes(10));
        sut.MustRevalidate.Should().BeTrue();
        sut.Immutable.Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // CompressionOptions
    // -----------------------------------------------------------------------

    [Fact]
    public void CompressionOptions_Should_HaveExpectedDefaults()
    {
        var sut = new CompressionOptions();

        sut.Enabled.Should().BeTrue();
        sut.EnableForHttps.Should().BeFalse();
        sut.UseBrotli.Should().BeTrue();
        sut.UseGzip.Should().BeTrue();
        sut.MinimumCompressionSize.Should().Be(1024);
        sut.CompressionLevel.Should().Be(6);
    }

    [Fact]
    public void CompressionOptions_Should_ContainDefaultMimeTypes()
    {
        var sut = new CompressionOptions();

        sut.MimeTypes.Should().Contain("application/json");
        sut.MimeTypes.Should().Contain("application/xml");
        sut.MimeTypes.Should().Contain("text/plain");
    }

    // -----------------------------------------------------------------------
    // ContentNegotiationOptions
    // -----------------------------------------------------------------------

    [Fact]
    public void ContentNegotiationOptions_Should_HaveExpectedDefaults()
    {
        var sut = new ContentNegotiationOptions();

        sut.Enabled.Should().BeTrue();
        sut.DefaultMediaType.Should().Be("application/json");
        sut.Return406WhenNoMatch.Should().BeFalse();
        sut.EnableFormatParameter.Should().BeTrue();
        sut.FormatParameterName.Should().Be("format");
        sut.AddVaryHeader.Should().BeTrue();
        sut.Charset.Should().Be("utf-8");
    }

    [Fact]
    public void ContentNegotiationOptions_Should_ContainJsonAndXmlMappings()
    {
        var sut = new ContentNegotiationOptions();

        sut.SupportedMediaTypes.Should().Contain(m => m.MediaType == "application/json");
        sut.SupportedMediaTypes.Should().Contain(m => m.MediaType == "application/xml");
    }

    [Fact]
    public void ContentNegotiationOptions_Should_ContainFormatMappings()
    {
        var sut = new ContentNegotiationOptions();

        sut.FormatMappings["json"].Should().Be("application/json");
        sut.FormatMappings["xml"].Should().Be("application/xml");
    }

    [Fact]
    public void ContentNegotiationOptions_Should_ExcludeHealthAndSwaggerByDefault()
    {
        var sut = new ContentNegotiationOptions();

        sut.ExcludedPaths.Should().Contain("/health");
        sut.ExcludedPaths.Should().Contain("/swagger");
    }

    // -----------------------------------------------------------------------
    // MediaTypeMapping
    // -----------------------------------------------------------------------

    [Fact]
    public void MediaTypeMapping_Should_AllowDefaultConstruction()
    {
        var sut = new MediaTypeMapping();

        sut.MediaType.Should().BeEmpty();
        sut.Format.Should().Be(ContentFormat.Json);
    }

    [Fact]
    public void MediaTypeMapping_Should_SetPropertiesFromConstructor()
    {
        var sut = new MediaTypeMapping("application/xml", ContentFormat.Xml);

        sut.MediaType.Should().Be("application/xml");
        sut.Format.Should().Be(ContentFormat.Xml);
    }

    // -----------------------------------------------------------------------
    // JsonSerializationOptions
    // -----------------------------------------------------------------------

    [Fact]
    public void JsonSerializationOptions_Should_HaveExpectedDefaults()
    {
        var sut = new JsonSerializationOptions();

        sut.UseCamelCase.Should().BeTrue();
        sut.WriteIndented.Should().BeFalse();
        sut.IgnoreNullValues.Should().BeFalse();
        sut.HandleReferenceLoops.Should().BeTrue();
        sut.MaxDepth.Should().Be(32);
    }

    // -----------------------------------------------------------------------
    // XmlSerializationOptions
    // -----------------------------------------------------------------------

    [Fact]
    public void XmlSerializationOptions_Should_HaveExpectedDefaults()
    {
        var sut = new XmlSerializationOptions();

        sut.OmitXmlDeclaration.Should().BeFalse();
        sut.Indent.Should().BeFalse();
        sut.CollectionRootName.Should().Be("ArrayOfItems");
        sut.CollectionItemName.Should().Be("Item");
        sut.UseDataContractSerializer.Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // RequestDecompressionOptions
    // -----------------------------------------------------------------------

    [Fact]
    public void RequestDecompressionOptions_Should_HaveExpectedDefaults()
    {
        var sut = new RequestDecompressionOptions();

        sut.Enabled.Should().BeTrue();
        sut.MaxRequestBodySize.Should().Be(10 * 1024 * 1024);
        sut.SupportedEncodings.Should().Contain("gzip");
        sut.SupportedEncodings.Should().Contain("deflate");
        sut.SupportedEncodings.Should().Contain("br");
    }

    // -----------------------------------------------------------------------
    // RequestTimeoutOptions
    // -----------------------------------------------------------------------

    [Fact]
    public void RequestTimeoutOptions_Should_HaveExpectedDefaults()
    {
        var sut = new RequestTimeoutOptions();

        sut.Enabled.Should().BeTrue();
        sut.DefaultTimeout.Should().Be(TimeSpan.FromSeconds(30));
        sut.SendRetryAfter.Should().BeFalse();
        sut.EndpointTimeouts.Should().BeEmpty();
        sut.MethodTimeouts.Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // ResponseCachingOptions
    // -----------------------------------------------------------------------

    [Fact]
    public void ResponseCachingOptions_Should_HaveExpectedDefaults()
    {
        var sut = new ResponseCachingOptions();

        sut.Enabled.Should().BeTrue();
        sut.MaximumBodySize.Should().Be(100 * 1024);
        sut.SizeLimit.Should().Be(100 * 1024 * 1024);
        sut.VaryByQueryKeys.Should().BeTrue();
    }

    [Fact]
    public void CacheProfile_Should_HaveDefaultLocation()
    {
        var sut = new CacheProfile();

        sut.Location.Should().Be(ResponseCacheLocation.Any);
        sut.NoStore.Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // OutputCachingOptions
    // -----------------------------------------------------------------------

    [Fact]
    public void OutputCachingOptions_Should_HaveExpectedDefaults()
    {
        var sut = new OutputCachingOptions();

        sut.Enabled.Should().BeTrue();
        sut.DefaultExpirationTimeSpan.Should().Be(TimeSpan.FromMinutes(5));
        sut.VaryByQueryStringByDefault.Should().BeTrue();
        sut.UseCaseSensitivePaths.Should().BeFalse();
    }

    [Fact]
    public void OutputCachingOptions_Should_AddPolicy()
    {
        var sut = new OutputCachingOptions();

        sut.AddPolicy("MyPolicy", p => p.Expire(TimeSpan.FromMinutes(15)));

        sut.Policies.Should().ContainKey("MyPolicy");
        sut.Policies["MyPolicy"].ExpirationTimeSpan.Should().Be(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void OutputCachingOptions_Should_AddDefaultPolicy()
    {
        var sut = new OutputCachingOptions();

        sut.AddDefaultPolicy(TimeSpan.FromMinutes(20));

        sut.Policies.Should().ContainKey("Default");
        sut.Policies["Default"].ExpirationTimeSpan.Should().Be(TimeSpan.FromMinutes(20));
    }

    [Fact]
    public void OutputCachingOptions_Should_AddStandardPolicies()
    {
        var sut = new OutputCachingOptions();

        sut.AddStandardPolicies();

        sut.Policies.Should().ContainKey("Default");
        sut.Policies.Should().ContainKey("Short");
        sut.Policies.Should().ContainKey("Medium");
        sut.Policies.Should().ContainKey("Long");
        sut.Policies.Should().ContainKey("NoCache");
    }

    [Fact]
    public void OutputCachePolicyOptions_Should_SupportFluentChaining()
    {
        var sut = new OutputCachePolicyOptions();

        OutputCachePolicyOptions result = sut
            .Expire(TimeSpan.FromMinutes(5))
            .SetTags("products", "catalog")
            .SetVaryByHeader("Accept", "Accept-Language")
            .SetVaryByQuery("page", "size");

        result.Should().BeSameAs(sut);
        sut.Tags.Should().Contain("products");
        sut.VaryByHeader.Should().Contain("Accept");
        sut.VaryByQueryKeys.Should().Contain("page");
    }

    [Fact]
    public void OutputCachePolicyOptions_Should_AllowAuthenticatedRequests()
    {
        var sut = new OutputCachePolicyOptions();

        sut.AllowAuthenticatedRequests();

        sut.CacheAuthenticatedRequests.Should().BeTrue();
    }

    // -----------------------------------------------------------------------
    // InputSanitizationOptions
    // -----------------------------------------------------------------------

    [Fact]
    public void InputSanitizationOptions_Should_HaveExpectedDefaults()
    {
        var sut = new InputSanitizationOptions();

        sut.Enabled.Should().BeTrue();
        sut.Mode.Should().Be(SanitizationMode.Validate);
        sut.EnableXssSanitization.Should().BeTrue();
        sut.EnableSqlInjectionDetection.Should().BeTrue();
        sut.SanitizeQueryStrings.Should().BeTrue();
        sut.SanitizeHeaders.Should().BeTrue();
    }

    // -----------------------------------------------------------------------
    // ApiKeyAuthenticationOptions
    // -----------------------------------------------------------------------

    [Fact]
    public void ApiKeyAuthenticationOptions_Should_HaveDefaultApiKeyRateLimit()
    {
        var sut = new ApiKeyAuthenticationOptions();

        sut.RateLimit.Enabled.Should().BeFalse();
        sut.RateLimit.DefaultRequestsPerMinute.Should().Be(60);
        sut.RateLimit.KeyLimits.Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // CorsOptions
    // -----------------------------------------------------------------------

    [Fact]
    public void CorsOptions_Should_AllowRequestOptionsBeDefault()
    {
        var sut = new CorsOptions();

        sut.AllowAll.Should().BeFalse();
        sut.AllowRequestOptions.Should().BeTrue();
        sut.Origin.Should().BeNull();
        sut.Methods.Should().BeNull();
        sut.Headers.Should().BeNull();
        sut.Credentials.Should().BeNull();
    }
}
