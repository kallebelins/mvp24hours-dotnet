//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Mvp24Hours.Infrastructure.BackgroundJobs.Dashboard;

namespace Mvp24Hours.Infrastructure.Test.BackgroundJobs.Dashboard;

[Trait("Category", "Unit")]
public class DashboardIntegrationHelpersTest
{
    [Fact]
    public void UseHangfireDashboard_ShouldThrowNotSupportedException()
    {
        IApplicationBuilder app = CreateAppBuilder();

        Action act = () => app.UseHangfireDashboard("/hangfire");

        act.Should().Throw<NotSupportedException>().WithMessage("*Hangfire*");
    }

    [Fact]
    public void UseQuartzDashboard_ShouldThrowNotSupportedException()
    {
        IApplicationBuilder app = CreateAppBuilder();

        Action act = () => app.UseQuartzDashboard("/quartz");

        act.Should().Throw<NotSupportedException>().WithMessage("*Quartz*");
    }

    [Fact]
    public void MapJobHealthChecks_WithNullEndpoints_ShouldThrowArgumentNullException()
    {
        Action act = () => DashboardIntegrationHelpers.MapJobHealthChecks(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("endpoints");
    }

    [Fact]
    public void MapJobHealthChecks_ShouldReturnSameBuilder()
    {
        IEndpointRouteBuilder endpoints = CreateEndpointBuilder();

        IEndpointRouteBuilder result = endpoints.MapJobHealthChecks("/health/jobs");

        result.Should().BeSameAs(endpoints);
    }

    private static IApplicationBuilder CreateAppBuilder()
    {
        return WebApplication.Create();
    }

    private static IEndpointRouteBuilder CreateEndpointBuilder()
    {
        return WebApplication.Create();
    }
}
