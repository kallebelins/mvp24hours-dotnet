using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Email.Contract;
using Mvp24Hours.Infrastructure.FileStorage.Contract;
using Mvp24Hours.Infrastructure.Sms.Contract;
using Mvp24Hours.Infrastructure.Testing;
using Mvp24Hours.Infrastructure.Testing.Extensions;
using Mvp24Hours.Infrastructure.Testing.Fakes;
using Mvp24Hours.Infrastructure.Testing.Http;
using Mvp24Hours.Infrastructure.Testing.Logging;
using Mvp24Hours.Infrastructure.Testing.Observability;

namespace Mvp24Hours.Infrastructure.Test.Testing;

[Trait("Category", "Unit")]
public class TestingServiceExtensionsTest
{
    [Fact]
    public void AddMvpTestingInfrastructure_ShouldRegisterAllFakeServices()
    {
        var services = new ServiceCollection();

        IServiceCollection result = services.AddMvpTestingInfrastructure();

        result.Should().BeSameAs(services);
        ServiceProvider provider = services.BuildServiceProvider();
        provider.GetService<IClock>().Should().NotBeNull();
        provider.GetService<IEmailService>().Should().NotBeNull();
        provider.GetService<ISmsService>().Should().NotBeNull();
        provider.GetService<IFileStorage>().Should().NotBeNull();
        provider.GetService<TestHttpMessageHandler>().Should().NotBeNull();
        provider.GetService<HttpClient>().Should().NotBeNull();
    }

    [Fact]
    public void AddMockClock_WithInitialTime_ShouldReturnConfiguredClock()
    {
        var services = new ServiceCollection();
        DateTime initial = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        services.AddMockClock(initial);
        MockClock clock = services.BuildServiceProvider().GetRequiredService<MockClock>();

        clock.UtcNow.Should().Be(initial);
    }

    [Fact]
    public void AddFakeEmailService_WithConfigure_ShouldApplyConfiguration()
    {
        var services = new ServiceCollection();

        services.AddFakeEmailService(email => email.ShouldFail = true);
        FakeEmailService fake = services.BuildServiceProvider().GetRequiredService<FakeEmailService>();

        fake.ShouldFail.Should().BeTrue();
        services.BuildServiceProvider().GetRequiredService<IEmailService>().Should().BeSameAs(fake);
    }

    [Fact]
    public void AddFakeSmsService_WithConfigure_ShouldApplyConfiguration()
    {
        var services = new ServiceCollection();

        services.AddFakeSmsService(sms => sms.ShouldFail = true);
        FakeSmsService fake = services.BuildServiceProvider().GetRequiredService<FakeSmsService>();

        fake.ShouldFail.Should().BeTrue();
    }

    [Fact]
    public void AddFakeFileStorage_ShouldRegisterConcreteAndInterface()
    {
        var services = new ServiceCollection();
        services.AddFakeFileStorage(storage => storage.ShouldUploadFail = true);

        ServiceProvider provider = services.BuildServiceProvider();
        FakeFileStorage fake = provider.GetRequiredService<FakeFileStorage>();
        fake.ShouldUploadFail.Should().BeTrue();
        provider.GetRequiredService<IFileStorage>().Should().BeSameAs(fake);
    }

    [Fact]
    public void AddTestHttpHandler_ShouldRegisterHandlerAndClient()
    {
        var services = new ServiceCollection();
        services.AddTestHttpHandler(handler => handler.RespondWith(System.Net.HttpStatusCode.Accepted));

        ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<TestHttpMessageHandler>().Should().NotBeNull();
        provider.GetRequiredService<HttpClient>().Should().NotBeNull();
    }

    [Fact]
    public void AddTestHttpClient_ShouldRegisterNamedClient()
    {
        var services = new ServiceCollection();
        services.AddTestHttpClient("orders-api", client => client.BaseAddress = new Uri("https://orders.test"));

        ServiceProvider provider = services.BuildServiceProvider();
        IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();
        HttpClient client = factory.CreateClient("orders-api");

        client.BaseAddress.Should().Be(new Uri("https://orders.test"));
    }

    [Fact]
    public void AddInMemoryLoggerProvider_ShouldCaptureLogs()
    {
        var services = new ServiceCollection();
        services.AddInMemoryLoggerProvider();

        ServiceProvider provider = services.BuildServiceProvider();
        InMemoryLoggerProvider loggerProvider = provider.GetRequiredService<InMemoryLoggerProvider>();
        ILoggerFactory loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        ILogger logger = loggerFactory.CreateLogger("TestingServiceExtensionsTest");

        logger.LogInformation("captured message");

        loggerProvider.ContainsLog("captured message").Should().BeTrue();
    }

    [Fact]
    public void AddObservabilityTesting_ShouldRegisterLoggingTracingAndMetrics()
    {
        var services = new ServiceCollection();
        services.AddObservabilityTesting("Mvp24Hours.*");

        ServiceProvider provider = services.BuildServiceProvider();
        provider.GetService<InMemoryLoggerProvider>().Should().NotBeNull();
        provider.GetService<FakeActivityListener>().Should().NotBeNull();
        provider.GetService<FakeMeterListener>().Should().NotBeNull();
    }

    [Fact]
    public void ReplaceWithTestInfrastructure_ShouldReplaceExistingRegistrations()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEmailService, FakeEmailService>();
        services.AddSingleton<ISmsService, FakeSmsService>();
        services.AddSingleton<IFileStorage, FakeFileStorage>();
        services.AddSingleton<IClock, MockClock>();
        services.AddSingleton(new HttpClient());

        services.ReplaceWithTestInfrastructure(options =>
        {
            options.UseMockClock = true;
            options.InitialClockTime = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            options.UseFakeEmail = true;
            options.EmailShouldFail = true;
            options.UseFakeSms = true;
            options.SmsShouldFail = true;
            options.UseFakeFileStorage = true;
            options.UseFakeHttp = true;
        });

        ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<MockClock>().UtcNow.Should().Be(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        provider.GetRequiredService<FakeEmailService>().ShouldFail.Should().BeTrue();
        provider.GetRequiredService<FakeSmsService>().ShouldFail.Should().BeTrue();
        provider.GetRequiredService<TestHttpMessageHandler>().Should().NotBeNull();
    }

    [Fact]
    public void TestInfrastructureOptions_ShouldHaveExpectedDefaults()
    {
        var options = new TestInfrastructureOptions();

        options.UseMockClock.Should().BeTrue();
        options.UseFakeEmail.Should().BeTrue();
        options.UseFakeSms.Should().BeTrue();
        options.UseFakeFileStorage.Should().BeTrue();
        options.UseFakeHttp.Should().BeTrue();
        options.EmailShouldFail.Should().BeFalse();
        options.SmsShouldFail.Should().BeFalse();
    }
}
