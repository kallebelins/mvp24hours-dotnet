//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.BackgroundJobs.Contract;
using Mvp24Hours.Infrastructure.BackgroundJobs.Extensions;
using Mvp24Hours.Infrastructure.BackgroundJobs.Options;
using Mvp24Hours.Infrastructure.BackgroundJobs.Providers;

namespace Mvp24Hours.Infrastructure.Test.BackgroundJobs.Extensions;

[Trait("Category", "Unit")]
public class BackgroundJobsServiceExtensionsTest
{
    [Fact]
    public void AddBackgroundJobs_WithNullServices_ShouldThrowArgumentNullException()
    {
        Action act = () => BackgroundJobsServiceExtensions.AddBackgroundJobs(null!, _ => { });

        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddBackgroundJobs_WithNullConfigure_ShouldThrowArgumentNullException()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddBackgroundJobs(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("configure");
    }

    [Fact]
    public void AddBackgroundJobs_WithInMemoryProvider_ShouldRegisterScheduler()
    {
        var services = new ServiceCollection();
        services.AddBackgroundJobs(builder => builder.AddInMemoryProvider());

        IJobScheduler scheduler = services.BuildServiceProvider().GetRequiredService<IJobScheduler>();

        scheduler.Should().BeOfType<InMemoryJobProvider>();
    }

    [Fact]
    public void AddBackgroundJobs_WithoutProvider_ShouldThrowInvalidOperationException()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddBackgroundJobs(_ => { });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*No background job provider was selected*");
    }

    [Fact]
    public void AddInMemoryBackgroundJobs_ShouldRegisterScheduler()
    {
        var services = new ServiceCollection();
        services.AddInMemoryBackgroundJobs();

        IJobScheduler scheduler = services.BuildServiceProvider().GetRequiredService<IJobScheduler>();

        scheduler.Should().BeOfType<InMemoryJobProvider>();
    }

    [Fact]
    public void AddHangfireBackgroundJobs_ShouldRegisterHangfireProvider()
    {
        var services = new ServiceCollection();
        services.AddHangfireBackgroundJobs(options => options.ConnectionString = "Server=localhost;Database=Hangfire;Trusted_Connection=True;");

        ServiceProvider sp = services.BuildServiceProvider();

        sp.GetRequiredService<IJobScheduler>().Should().BeOfType<HangfireJobProvider>();
        sp.GetRequiredService<IOptions<HangfireJobOptions>>().Value.ConnectionString.Should().Contain("Hangfire");
    }

    [Fact]
    public void AddQuartzBackgroundJobs_ShouldRegisterQuartzProvider()
    {
        var services = new ServiceCollection();
        services.AddQuartzBackgroundJobs(options => options.ConnectionString = "Server=localhost;Database=Quartz;Trusted_Connection=True;");

        ServiceProvider sp = services.BuildServiceProvider();

        sp.GetRequiredService<IJobScheduler>().Should().BeOfType<QuartzJobProvider>();
        sp.GetRequiredService<IOptions<QuartzJobOptions>>().Value.ConnectionString.Should().Contain("Quartz");
    }

    [Fact]
    public void AddBackgroundJobs_WithHangfireBuilder_ShouldRegisterHangfireProvider()
    {
        var services = new ServiceCollection();
        services.AddBackgroundJobs(builder =>
            builder.AddHangfireProvider(options => options.ConnectionString = "Server=.;Database=Hangfire;"));

        IJobScheduler scheduler = services.BuildServiceProvider().GetRequiredService<IJobScheduler>();

        scheduler.Should().BeOfType<HangfireJobProvider>();
    }

    [Fact]
    public void AddBackgroundJobs_WithQuartzBuilder_ShouldRegisterQuartzProvider()
    {
        var services = new ServiceCollection();
        services.AddBackgroundJobs(builder =>
            builder.AddQuartzProvider(options => options.ConnectionString = "Server=.;Database=Quartz;"));

        IJobScheduler scheduler = services.BuildServiceProvider().GetRequiredService<IJobScheduler>();

        scheduler.Should().BeOfType<QuartzJobProvider>();
    }

    [Fact]
    public void AddInMemoryBackgroundJobs_WithNullServices_ShouldThrowArgumentNullException()
    {
        Action act = () => BackgroundJobsServiceExtensions.AddInMemoryBackgroundJobs(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }
}
