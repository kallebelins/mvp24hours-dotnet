using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.BackgroundJobs.Extensions;
using Mvp24Hours.Infrastructure.Configuration;
using Mvp24Hours.Infrastructure.DistributedLocking.Extensions;
using Mvp24Hours.Infrastructure.Email.Options;
using Mvp24Hours.Infrastructure.FileStorage.Options;
using Mvp24Hours.Infrastructure.Http.Options;
using Mvp24Hours.Infrastructure.Observability.Extensions;
using Mvp24Hours.Infrastructure.Sms.Options;

namespace Mvp24Hours.Infrastructure.Test.Configuration;

[Trait("Category", "Unit")]
public class InfrastructureBuilderTest
{
    [Fact]
    public void Constructor_WithNullServices_ShouldThrow()
    {
        Action act = () => _ = new InfrastructureBuilder(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void Constructor_ShouldExposeServicesConfigurationAndDefaultOptions()
    {
        var services = new ServiceCollection();

        var builder = new InfrastructureBuilder(services);

        builder.Services.Should().BeSameAs(services);
        builder.Configuration.Should().BeNull();
        builder.Options.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureHttp_WithNullConfigure_ShouldThrow()
    {
        var builder = new InfrastructureBuilder(new ServiceCollection());

        Action act = () => builder.ConfigureHttp(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("configure");
    }

    [Fact]
    public void ConfigureHttp_ShouldInitializeAndApplyOptions()
    {
        var builder = new InfrastructureBuilder(new ServiceCollection());

        IInfrastructureBuilder result = builder.ConfigureHttp(o => o.Timeout = TimeSpan.FromSeconds(42));

        result.Should().BeSameAs(builder);
        builder.Options.Http.Should().NotBeNull();
        builder.Options.Http!.Timeout.Should().Be(TimeSpan.FromSeconds(42));
    }

    [Fact]
    public void ConfigureEmail_WithNullConfigure_ShouldThrow()
    {
        var builder = new InfrastructureBuilder(new ServiceCollection());

        Action act = () => builder.ConfigureEmail(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("configure");
    }

    [Fact]
    public void ConfigureEmail_ShouldInitializeAndApplyOptions()
    {
        var builder = new InfrastructureBuilder(new ServiceCollection());

        builder.ConfigureEmail(o => o.DefaultFrom = "no-reply@example.com");

        builder.Options.Email.Should().NotBeNull();
        builder.Options.Email!.DefaultFrom.Should().Be("no-reply@example.com");
    }

    [Fact]
    public void ConfigureSms_WithNullConfigure_ShouldThrow()
    {
        var builder = new InfrastructureBuilder(new ServiceCollection());

        Action act = () => builder.ConfigureSms(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("configure");
    }

    [Fact]
    public void ConfigureSms_ShouldInitializeOptions()
    {
        var builder = new InfrastructureBuilder(new ServiceCollection());
        bool invoked = false;

        builder.ConfigureSms(_ => invoked = true);

        builder.Options.Sms.Should().NotBeNull();
        invoked.Should().BeTrue();
    }

    [Fact]
    public void ConfigureFileStorage_WithNullConfigure_ShouldThrow()
    {
        var builder = new InfrastructureBuilder(new ServiceCollection());

        Action act = () => builder.ConfigureFileStorage(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("configure");
    }

    [Fact]
    public void ConfigureFileStorage_ShouldInitializeAndApplyOptions()
    {
        var builder = new InfrastructureBuilder(new ServiceCollection());

        builder.ConfigureFileStorage(o => o.MaxFileSize = 2048);

        builder.Options.FileStorage.Should().NotBeNull();
        builder.Options.FileStorage!.MaxFileSize.Should().Be(2048);
    }

    [Fact]
    public void ConfigureObservability_WithNullConfigure_ShouldThrow()
    {
        var builder = new InfrastructureBuilder(new ServiceCollection());

        Action act = () => builder.ConfigureObservability(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("configure");
    }

    [Fact]
    public void ConfigureObservability_ShouldInitializeOptions()
    {
        var builder = new InfrastructureBuilder(new ServiceCollection());
        bool invoked = false;

        builder.ConfigureObservability(_ => invoked = true);

        builder.Options.Observability.Should().NotBeNull();
        invoked.Should().BeTrue();
    }

    [Fact]
    public void ConfigureResilience_WithNullConfigure_ShouldThrow()
    {
        var builder = new InfrastructureBuilder(new ServiceCollection());

        Action act = () => builder.ConfigureResilience(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("configure");
    }

    [Fact]
    public void ConfigureResilience_ShouldInitializeOptions()
    {
        var builder = new InfrastructureBuilder(new ServiceCollection());
        bool invoked = false;

        builder.ConfigureResilience(_ => invoked = true);

        builder.Options.Resilience.Should().NotBeNull();
        invoked.Should().BeTrue();
    }

    [Fact]
    public void ConfigureSecurity_WithNullConfigure_ShouldThrow()
    {
        var builder = new InfrastructureBuilder(new ServiceCollection());

        Action act = () => builder.ConfigureSecurity(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("configure");
    }

    [Fact]
    public void ConfigureSecurity_ShouldInitializeOptions()
    {
        var builder = new InfrastructureBuilder(new ServiceCollection());
        bool invoked = false;

        builder.ConfigureSecurity(_ => invoked = true);

        builder.Options.Security.Should().NotBeNull();
        invoked.Should().BeTrue();
    }

    [Fact]
    public void ConfigureDistributedLocking_WithNullConfigure_ShouldThrow()
    {
        var builder = new InfrastructureBuilder(new ServiceCollection());

        Action act = () => builder.ConfigureDistributedLocking(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("configure");
    }

    [Fact]
    public void ConfigureDistributedLocking_ShouldStoreDelegateWithoutInvokingItImmediately()
    {
        var builder = new InfrastructureBuilder(new ServiceCollection());
        bool invoked = false;

        builder.ConfigureDistributedLocking(_ => invoked = true);

        invoked.Should().BeFalse("the delegate must only run when ExecuteBuilderConfigurations is called");
    }

    [Fact]
    public void ConfigureBackgroundJobs_WithNullConfigure_ShouldThrow()
    {
        var builder = new InfrastructureBuilder(new ServiceCollection());

        Action act = () => builder.ConfigureBackgroundJobs(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("configure");
    }

    [Fact]
    public void ConfigureBackgroundJobs_ShouldStoreDelegateWithoutInvokingItImmediately()
    {
        var builder = new InfrastructureBuilder(new ServiceCollection());
        bool invoked = false;

        builder.ConfigureBackgroundJobs(_ => invoked = true);

        invoked.Should().BeFalse("the delegate must only run when ExecuteBuilderConfigurations is called");
    }

    [Fact]
    public void ExecuteBuilderConfigurations_WithNoStoredDelegates_ShouldNotThrow()
    {
        var builder = new InfrastructureBuilder(new ServiceCollection());

        Action act = builder.ExecuteBuilderConfigurations;

        act.Should().NotThrow();
    }

    [Fact]
    public void ExecuteBuilderConfigurations_WithDistributedLockingConfigured_ShouldInvokeStoredDelegate()
    {
        var services = new ServiceCollection();
        var builder = new InfrastructureBuilder(services);
        bool invoked = false;
        builder.ConfigureDistributedLocking(_ => invoked = true);

        builder.ExecuteBuilderConfigurations();

        invoked.Should().BeTrue();
    }

    [Fact]
    public void ExecuteBuilderConfigurations_WithBackgroundJobsConfigured_ShouldInvokeStoredDelegate()
    {
        var services = new ServiceCollection();
        var builder = new InfrastructureBuilder(services);
        bool invoked = false;
        // AddBackgroundJobs builds the configured IBackgroundJobsBuilder immediately, which
        // requires a provider to have been selected, so the delegate must select one.
        builder.ConfigureBackgroundJobs(b =>
        {
            invoked = true;
            b.AddInMemoryProvider();
        });

        builder.ExecuteBuilderConfigurations();

        invoked.Should().BeTrue();
    }

    [Fact]
    public void ExecuteBuilderConfigurations_WithBothConfigured_ShouldInvokeBothStoredDelegates()
    {
        var services = new ServiceCollection();
        var builder = new InfrastructureBuilder(services);
        bool distributedLockingInvoked = false;
        bool backgroundJobsInvoked = false;
        builder.ConfigureDistributedLocking(_ => distributedLockingInvoked = true);
        builder.ConfigureBackgroundJobs(b =>
        {
            backgroundJobsInvoked = true;
            b.AddInMemoryProvider();
        });

        builder.ExecuteBuilderConfigurations();

        distributedLockingInvoked.Should().BeTrue();
        backgroundJobsInvoked.Should().BeTrue();
    }
}
