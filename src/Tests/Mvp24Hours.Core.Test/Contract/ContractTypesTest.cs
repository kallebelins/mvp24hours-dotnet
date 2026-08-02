using System.Security.Claims;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Core.Contract.Infrastructure.Channels;
using Mvp24Hours.Core.Contract.Infrastructure.Options;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;

namespace Mvp24Hours.Core.Test.Contract;

[Trait("Category", "Unit")]
public class ContractTypesTest
{
    private sealed class SampleOptions
    {
        public string? Name { get; set; }
        public int Port { get; set; }
    }

    [Fact]
    public void CacheEntryOptions_FactoryMethods_SetExpirations()
    {
        var duration = CacheEntryOptions.FromDuration(TimeSpan.FromMinutes(5));
        var sliding = CacheEntryOptions.WithSlidingExpiration(TimeSpan.FromMinutes(2));
        var both = CacheEntryOptions.WithBothExpirations(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(1));

        duration.AbsoluteExpirationRelativeToNow.Should().Be(TimeSpan.FromMinutes(5));
        sliding.SlidingExpiration.Should().Be(TimeSpan.FromMinutes(2));
        both.AbsoluteExpirationRelativeToNow.Should().Be(TimeSpan.FromMinutes(10));
        both.SlidingExpiration.Should().Be(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void MvpChannelOptions_Factories_SetExpectedDefaults()
    {
        var unbounded = MvpChannelOptions.Unbounded();
        var bounded = MvpChannelOptions.Bounded(50);
        var dropWrite = MvpChannelOptions.DropWrite(25);

        unbounded.IsBounded.Should().BeFalse();
        bounded.Capacity.Should().Be(50);
        dropWrite.FullMode.Should().Be(System.Threading.Channels.BoundedChannelFullMode.DropWrite);
    }

    [Fact]
    public void AsyncLocalCurrentUserProvider_StoresUserInAsyncContext()
    {
        AsyncLocalCurrentUserProvider.SetCurrentUser("user-1", "Alice");

        AsyncLocalCurrentUserProvider.Instance.UserId.Should().Be("user-1");
        AsyncLocalCurrentUserProvider.Instance.UserName.Should().Be("Alice");

        AsyncLocalCurrentUserProvider.ClearCurrentUser();
    }

    [Fact]
    public void SystemUserProvider_ReturnsConfiguredDefaults()
    {
        SystemUserProvider provider = SystemUserProvider.Default;

        provider.UserId.Should().Be("System");
        provider.UserName.Should().Be("System");
    }

    [Fact]
    public void AsyncLocalTenantProvider_StoresTenantData()
    {
        AsyncLocalTenantProvider.SetCurrentTenant("tenant-a", "conn", "schema");

        AsyncLocalTenantProvider.Instance.TenantId.Should().Be("tenant-a");
        AsyncLocalTenantProvider.Instance.HasTenant.Should().BeTrue();
        AsyncLocalTenantProvider.Instance.ConnectionString.Should().Be("conn");
        AsyncLocalTenantProvider.Instance.Schema.Should().Be("schema");

        AsyncLocalTenantProvider.ClearCurrentTenant();
        NoTenantProvider.Instance.HasTenant.Should().BeFalse();
    }

    [Fact]
    public void DefaultRequestContext_StoresItemsAndClaims()
    {
        var context = DefaultRequestContext.WithUser(
        [
            new Claim(ClaimTypes.NameIdentifier, "u1"),
            new Claim(ClaimTypes.Name, "Bob"),
            new Claim(ClaimTypes.Role, "Admin")
        ]);

        context.UserId.Should().Be("u1");
        context.UserName.Should().Be("Bob");
        context.IsAuthenticated.Should().BeTrue();
        context.IsInRole("Admin").Should().BeTrue();
        context.HasClaim(ClaimTypes.NameIdentifier).Should().BeTrue();

        context.SetItem("trace", "abc");
        context.GetItem<string>("trace").Should().Be("abc");
    }

    [Fact]
    public void PipelineValidationResult_ThrowIfInvalid_ThrowsOnFailure()
    {
        var success = PipelineValidationResult.Success();
        var failure = PipelineValidationResult.Failure(
            new PipelineValidationError("CODE", "Invalid pipeline", "Step1", 0));

        success.IsValid.Should().BeTrue();
        Action act = () => failure.ThrowIfInvalid();

        act.Should().Throw<PipelineValidationException>()
            .Which.Errors.Should().ContainSingle(e => e.Code == "CODE");
    }

    [Fact]
    public void OptionsValidationContext_AddsErrorsAndBuildsResult()
    {
        var context = new OptionsValidationContext<SampleOptions>("Sample");
        context.AddPropertyError(nameof(SampleOptions.Name), "Value is required.");
        context.AtLeastOne("Need one flag", false, false);
        context.ExactlyOne("Need exactly one flag", true, true);
        context.When(false, "Sample: custom rule failed");

        OptionsValidationResult result = context.ToResult();

        result.Succeeded.Should().BeFalse();
        result.Failures.Should().HaveCount(4);
    }
}
