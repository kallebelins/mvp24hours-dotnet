//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Email.Bulk;
using Mvp24Hours.Infrastructure.Email.Contract;
using Mvp24Hours.Infrastructure.Email.Extensions;
using Mvp24Hours.Infrastructure.Email.Options;
using Mvp24Hours.Infrastructure.Email.Providers;
using Mvp24Hours.Infrastructure.Email.Queue;
using Mvp24Hours.Infrastructure.Email.RateLimiting;
using Mvp24Hours.Infrastructure.Email.Templates;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.Email.Extensions;

[Trait("Category", "Unit")]
public class EmailServiceExtensionsTest
{
    [Fact]
    public void AddEmailService_WithNullServices_ShouldThrowArgumentNullException()
    {
        Action act = () => EmailServiceExtensions.AddEmailService(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddEmailService_ShouldRegisterDefaultInMemoryProvider()
    {
        var services = new ServiceCollection();
        services.AddEmailService(options => options.DefaultFrom = "noreply@example.com");

        ServiceProvider sp = services.BuildServiceProvider();
        IEmailService emailService = sp.GetRequiredService<IEmailService>();

        emailService.Should().BeOfType<InMemoryEmailProvider>();
        sp.GetRequiredService<IOptions<EmailOptions>>().Value.DefaultFrom.Should().Be("noreply@example.com");
    }

    [Fact]
    public void AddInMemoryEmailService_ShouldResolveIEmailService()
    {
        var services = new ServiceCollection();
        services.AddInMemoryEmailService();

        IEmailService emailService = services.BuildServiceProvider().GetRequiredService<IEmailService>();

        emailService.Should().BeOfType<InMemoryEmailProvider>();
    }

    [Fact]
    public void AddEmailServiceWithProvider_WithNullFactory_ShouldThrowArgumentNullException()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddEmailServiceWithProvider(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("factory");
    }

    [Fact]
    public void AddEmailServiceWithProvider_ShouldUseCustomFactory()
    {
        var services = new ServiceCollection();
        services.AddEmailServiceWithProvider((_, _) => new InMemoryEmailProvider(EmailTestHelpers.CreateEmailOptions()));

        IEmailService emailService = services.BuildServiceProvider().GetRequiredService<IEmailService>();

        emailService.Should().BeOfType<InMemoryEmailProvider>();
    }

    [Fact]
    public void AddSmtpEmailService_WithNullConfigure_ShouldThrowArgumentNullException()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddSmtpEmailService(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("configureSmtp");
    }

    [Fact]
    public void AddSmtpEmailService_ShouldRegisterSmtpProvider()
    {
        var services = new ServiceCollection();
        SmtpEmailOptions smtpOptions = EmailTestHelpers.CreateSmtpOptions();
        services.AddSmtpEmailService(
            smtp =>
            {
                smtp.Host = smtpOptions.Host;
                smtp.Port = smtpOptions.Port;
                smtp.Username = smtpOptions.Username;
                smtp.Password = smtpOptions.Password;
                smtp.Timeout = smtpOptions.Timeout;
                smtp.UseDefaultCredentials = smtpOptions.UseDefaultCredentials;
                smtp.EnableStartTls = smtpOptions.EnableStartTls;
                smtp.EnableSsl = smtpOptions.EnableSsl;
            },
            email => email.DefaultFrom = "noreply@example.com");

        IEmailService emailService = services.BuildServiceProvider().GetRequiredService<IEmailService>();

        emailService.Should().BeOfType<SmtpEmailProvider>();
    }

    [Fact]
    public void AddSendGridEmailService_ShouldRegisterSendGridProvider()
    {
        var services = new ServiceCollection();
        SendGridEmailOptions sendGridOptions = EmailTestHelpers.CreateSendGridOptions();
        services.AddSendGridEmailService(sendGrid =>
        {
            sendGrid.ApiKey = sendGridOptions.ApiKey;
            sendGrid.ApiBaseUrl = sendGridOptions.ApiBaseUrl;
            sendGrid.DefaultFrom = sendGridOptions.DefaultFrom;
            sendGrid.DefaultFromName = sendGridOptions.DefaultFromName;
            sendGrid.EnableClickTracking = sendGridOptions.EnableClickTracking;
            sendGrid.EnableOpenTracking = sendGridOptions.EnableOpenTracking;
        });

        ServiceProvider sp = services.BuildServiceProvider();

        sp.GetRequiredService<IHttpClientFactory>().Should().NotBeNull();
        sp.GetRequiredService<IEmailService>().Should().BeOfType<SendGridEmailProvider>();
    }

    [Fact]
    public void AddAzureCommunicationEmailService_ShouldRegisterAzureProvider()
    {
        var services = new ServiceCollection();
        AzureCommunicationEmailOptions azureOptions = EmailTestHelpers.CreateAzureOptions();
        services.AddAzureCommunicationEmailService(azure =>
        {
            azure.ConnectionString = azureOptions.ConnectionString;
            azure.Endpoint = azureOptions.Endpoint;
            azure.DefaultFrom = azureOptions.DefaultFrom;
            azure.DefaultFromName = azureOptions.DefaultFromName;
            azure.EnableUserEngagementTracking = azureOptions.EnableUserEngagementTracking;
        });

        ServiceProvider sp = services.BuildServiceProvider();

        sp.GetRequiredService<IHttpClientFactory>().Should().NotBeNull();
        sp.GetRequiredService<IEmailService>().Should().BeOfType<AzureCommunicationEmailProvider>();
    }

    [Fact]
    public void AddEmailTemplateRenderer_WithScriban_ShouldRegisterScribanRenderer()
    {
        var services = new ServiceCollection();
        services.AddEmailTemplateRenderer(TemplateEngine.Scriban);

        IEmailTemplateRenderer renderer = services.BuildServiceProvider().GetRequiredService<IEmailTemplateRenderer>();

        renderer.Should().BeOfType<ScribanEmailTemplateRenderer>();
    }

    [Fact]
    public void AddEmailTemplateRenderer_WithRazor_ShouldRegisterRazorRenderer()
    {
        var services = new ServiceCollection();
        services.AddEmailTemplateRenderer(TemplateEngine.Razor);

        IEmailTemplateRenderer renderer = services.BuildServiceProvider().GetRequiredService<IEmailTemplateRenderer>();

        renderer.Should().BeOfType<RazorEmailTemplateRenderer>();
    }

    [Fact]
    public void AddEmailTemplateRenderer_WithUnknownEngine_ShouldThrowArgumentException()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddEmailTemplateRenderer((TemplateEngine)999);

        act.Should().Throw<ArgumentException>().WithParameterName("templateEngine");
    }

    [Fact]
    public void AddEmailQueue_ShouldRegisterQueueAndHostedProcessor()
    {
        var services = new ServiceCollection();
        services.AddInMemoryEmailService();
        services.AddEmailQueue(configureProcessor: options => options.PollInterval = TimeSpan.FromSeconds(5));

        ServiceProvider sp = services.BuildServiceProvider();

        sp.GetRequiredService<IEmailQueue>().Should().BeOfType<InMemoryEmailQueue>();
        sp.GetServices<IHostedService>().Should().Contain(s => s is EmailQueueProcessor);
    }

    [Fact]
    public void AddEmailQueue_WithoutProcessor_ShouldRegisterQueueOnly()
    {
        var services = new ServiceCollection();
        services.AddEmailQueue(startProcessor: false);

        ServiceProvider sp = services.BuildServiceProvider();

        sp.GetRequiredService<IEmailQueue>().Should().BeOfType<InMemoryEmailQueue>();
        sp.GetServices<IHostedService>().Should().NotContain(s => s is EmailQueueProcessor);
    }

    [Fact]
    public void AddEmailRateLimiter_ShouldRegisterRateLimiter()
    {
        var services = new ServiceCollection();
        services.AddEmailRateLimiter(options =>
        {
            options.MaxRequestsPerWindow = 50;
            options.WindowSize = TimeSpan.FromMinutes(1);
        });

        EmailRateLimiter rateLimiter = services.BuildServiceProvider().GetRequiredService<EmailRateLimiter>();

        rateLimiter.Should().NotBeNull();
    }

    [Fact]
    public void AddEmailBulkSender_ShouldRegisterScopedBulkSender()
    {
        var services = new ServiceCollection();
        services.AddInMemoryEmailService();
        services.AddEmailBulkSender(options => options.MaxRequestsPerWindow = 10);

        ServiceProvider sp = services.BuildServiceProvider();

        using IServiceScope scope = sp.CreateScope();
        EmailBulkSender bulkSender = scope.ServiceProvider.GetRequiredService<EmailBulkSender>();

        bulkSender.Should().NotBeNull();
    }

}
