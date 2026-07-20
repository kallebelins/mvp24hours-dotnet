//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Sms.Contract;
using Mvp24Hours.Infrastructure.Sms.Extensions;
using Mvp24Hours.Infrastructure.Sms.Options;
using Mvp24Hours.Infrastructure.Sms.Providers;
using Mvp24Hours.Infrastructure.Sms.Services;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.Sms.Extensions;

[Trait("Category", "Unit")]
public class SmsServiceExtensionsTest
{
    [Fact]
    public void AddSmsService_WithNullServices_ShouldThrowArgumentNullException()
    {
        Action act = () => SmsServiceExtensions.AddSmsService(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddSmsService_ShouldRegisterDefaultInMemoryProvider()
    {
        var services = new ServiceCollection();
        services.AddSmsService(options => options.DefaultFrom = "+5511888888888");

        ServiceProvider sp = services.BuildServiceProvider();
        ISmsService smsService = sp.GetRequiredService<ISmsService>();

        smsService.Should().BeOfType<InMemorySmsProvider>();
        sp.GetRequiredService<IOptions<SmsOptions>>().Value.DefaultFrom.Should().Be("+5511888888888");
    }

    [Fact]
    public void AddInMemorySmsService_ShouldResolveISmsService()
    {
        var services = new ServiceCollection();
        services.AddInMemorySmsService();

        ISmsService smsService = services.BuildServiceProvider().GetRequiredService<ISmsService>();

        smsService.Should().BeOfType<InMemorySmsProvider>();
    }

    [Fact]
    public void AddSmsServiceWithProvider_WithNullFactory_ShouldThrowArgumentNullException()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddSmsServiceWithProvider(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("factory");
    }

    [Fact]
    public void AddSmsServiceWithProvider_ShouldUseCustomFactory()
    {
        var services = new ServiceCollection();
        services.AddSmsServiceWithProvider((_, options) => new InMemorySmsProvider(options));

        ISmsService smsService = services.BuildServiceProvider().GetRequiredService<ISmsService>();

        smsService.Should().BeOfType<InMemorySmsProvider>();
    }

    [Fact]
    public void AddTwilioSmsService_WithNullConfigure_ShouldThrowArgumentNullException()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddTwilioSmsService(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("configureTwilio");
    }

    [Fact]
    public void AddTwilioSmsService_ShouldRegisterTwilioProvider()
    {
        var services = new ServiceCollection();
        TwilioSmsOptions twilioOptions = SmsTestHelpers.CreateTwilioOptions();
        services.AddTwilioSmsService(
            twilio =>
            {
                twilio.AccountSid = twilioOptions.AccountSid;
                twilio.AuthToken = twilioOptions.AuthToken;
                twilio.ApiBaseUrl = twilioOptions.ApiBaseUrl;
            },
            sms => sms.DefaultFrom = "+5511888888888");

        ServiceProvider sp = services.BuildServiceProvider();

        sp.GetRequiredService<IHttpClientFactory>().Should().NotBeNull();
        sp.GetRequiredService<ISmsService>().Should().BeOfType<TwilioSmsProvider>();
    }

    [Fact]
    public void AddAzureCommunicationSmsService_ShouldRegisterAzureProvider()
    {
        var services = new ServiceCollection();
        AzureCommunicationSmsOptions azureOptions = SmsTestHelpers.CreateAzureOptions();
        services.AddAzureCommunicationSmsService(azure =>
        {
            azure.ConnectionString = azureOptions.ConnectionString;
            azure.Endpoint = azureOptions.Endpoint;
            azure.AccessKey = azureOptions.AccessKey;
            azure.EnableDeliveryReports = azureOptions.EnableDeliveryReports;
        });

        ServiceProvider sp = services.BuildServiceProvider();

        sp.GetRequiredService<IHttpClientFactory>().Should().NotBeNull();
        sp.GetRequiredService<ISmsService>().Should().BeOfType<AzureCommunicationSmsProvider>();
    }

    [Fact]
    public void AddSmsTemplateService_ShouldRegisterTemplateService()
    {
        var services = new ServiceCollection();
        services.AddSmsTemplateService();

        ISmsTemplateService templateService = services.BuildServiceProvider().GetRequiredService<ISmsTemplateService>();

        templateService.Should().BeOfType<InMemorySmsTemplateService>();
    }

    [Fact]
    public void AddSmsRateLimiter_ShouldRegisterRateLimiter()
    {
        var services = new ServiceCollection();
        services.AddSmsRateLimiter(options =>
        {
            options.Enabled = true;
            options.MaxMessagesPerDestination = 5;
            options.TimeWindow = TimeSpan.FromHours(1);
        });

        ServiceProvider sp = services.BuildServiceProvider();

        sp.GetRequiredService<ISmsRateLimiter>().Should().BeOfType<InMemorySmsRateLimiter>();
        sp.GetRequiredService<IOptions<SmsRateLimitOptions>>().Value.Enabled.Should().BeTrue();
    }

    [Fact]
    public void AddSmsTemplateService_WithNullServices_ShouldThrowArgumentNullException()
    {
        Action act = () => SmsServiceExtensions.AddSmsTemplateService(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

}
