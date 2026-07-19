//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Text;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Sms.Models;
using Mvp24Hours.Infrastructure.Sms.Options;
using Mvp24Hours.Infrastructure.Testing.Http;

namespace Mvp24Hours.Infrastructure.Test.Support;

internal static class SmsTestHelpers
{
    public static SmsOptions CreateSmsOptions(
        string? defaultFrom = "+5511888888888",
        string? defaultCountryCode = null,
        int? maxMessageLength = null,
        bool validatePhoneNumbers = true)
    {
        return new SmsOptions
        {
            DefaultFrom = defaultFrom,
            DefaultCountryCode = defaultCountryCode,
            MaxMessageLength = maxMessageLength,
            ValidatePhoneNumbers = validatePhoneNumbers
        };
    }

    public static IOptions<T> AsOptions<T>(T value) where T : class
    {
        return Options.Create(value);
    }

    public static SmsMessage CreateValidMessage(
        string to = "+5511999999999",
        string body = "Hello SMS",
        string? from = null)
    {
        return new SmsMessage
        {
            To = to,
            Body = body,
            From = from
        };
    }

    public static MmsMessage CreateValidMmsMessage(
        string to = "+5511999999999",
        string body = "Hello MMS",
        string? from = null)
    {
        return new MmsMessage
        {
            To = to,
            Body = body,
            From = from,
            Attachments =
            [
                new MmsAttachment([1, 2, 3, 4], "image/jpeg", "photo.jpg")
            ]
        };
    }

    public static TwilioSmsOptions CreateTwilioOptions(
        string accountSid = "ACaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
        string authToken = "test-auth-token",
        string? apiBaseUrl = "https://api.twilio.com")
    {
        return new TwilioSmsOptions
        {
            AccountSid = accountSid,
            AuthToken = authToken,
            ApiBaseUrl = apiBaseUrl
        };
    }

    public static AzureCommunicationSmsOptions CreateAzureOptions(
        string? endpoint = "https://contoso.communication.azure.com",
        string? accessKey = null,
        bool enableDeliveryReports = false)
    {
        accessKey ??= Convert.ToBase64String(Encoding.UTF8.GetBytes("test-access-key"));
        return new AzureCommunicationSmsOptions
        {
            ConnectionString = $"endpoint={endpoint}/;accesskey={accessKey}",
            Endpoint = endpoint,
            AccessKey = accessKey,
            EnableDeliveryReports = enableDeliveryReports
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
