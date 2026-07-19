//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Text;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Email.Models;
using Mvp24Hours.Infrastructure.Email.Options;
using Mvp24Hours.Infrastructure.Testing.Http;

namespace Mvp24Hours.Infrastructure.Test.Support;

internal static class EmailTestHelpers
{
    public static EmailOptions CreateEmailOptions(
        string? defaultFrom = "noreply@example.com",
        string? defaultReplyTo = null,
        string? defaultSubjectPrefix = null,
        int? maxRecipientsPerEmail = null,
        int? maxAttachmentsPerEmail = null,
        long? maxAttachmentSize = null)
    {
        return new EmailOptions
        {
            DefaultFrom = defaultFrom,
            DefaultReplyTo = defaultReplyTo,
            DefaultSubjectPrefix = defaultSubjectPrefix,
            MaxRecipientsPerEmail = maxRecipientsPerEmail,
            MaxAttachmentsPerEmail = maxAttachmentsPerEmail,
            MaxAttachmentSize = maxAttachmentSize
        };
    }

    public static IOptions<T> AsOptions<T>(T value) where T : class
    {
        return Options.Create(value);
    }

    public static EmailMessage CreateValidMessage(
        string to = "user@example.com",
        string subject = "Test Subject",
        string? plainTextBody = "Hello",
        string? htmlBody = null,
        string? from = null)
    {
        return new EmailMessage
        {
            To = [to],
            Subject = subject,
            PlainTextBody = plainTextBody,
            HtmlBody = htmlBody,
            From = from
        };
    }

    public static SmtpEmailOptions CreateSmtpOptions(
        string host = "smtp.example.com",
        int port = 587,
        string? username = "user",
        string? password = "pass",
        int timeout = 1000,
        bool useDefaultCredentials = false)
    {
        return new SmtpEmailOptions
        {
            Host = host,
            Port = port,
            Username = username,
            Password = password,
            Timeout = timeout,
            UseDefaultCredentials = useDefaultCredentials,
            EnableStartTls = true,
            EnableSsl = false
        };
    }

    public static SendGridEmailOptions CreateSendGridOptions(
        string apiKey = "SG.test-api-key",
        string apiBaseUrl = "https://api.sendgrid.com/v3")
    {
        return new SendGridEmailOptions
        {
            ApiKey = apiKey,
            ApiBaseUrl = apiBaseUrl,
            DefaultFrom = "noreply@example.com",
            DefaultFromName = "Example",
            EnableClickTracking = true,
            EnableOpenTracking = true
        };
    }

    public static AzureCommunicationEmailOptions CreateAzureOptions(
        string? endpoint = "https://contoso.communication.azure.com",
        string? accessKey = null)
    {
        accessKey ??= Convert.ToBase64String(Encoding.UTF8.GetBytes("test-access-key"));
        return new AzureCommunicationEmailOptions
        {
            ConnectionString = $"endpoint={endpoint}/;accesskey={accessKey}",
            Endpoint = endpoint,
            DefaultFrom = "DoNotReply@contoso.com",
            DefaultFromName = "Contoso",
            EnableUserEngagementTracking = true
        };
    }

    public static IHttpClientFactory CreateHttpClientFactory(TestHttpMessageHandler handler)
    {
        return new TestHttpClientFactory(handler);
    }

    private sealed class TestHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient(handler, disposeHandler: false);
        }
    }
}
