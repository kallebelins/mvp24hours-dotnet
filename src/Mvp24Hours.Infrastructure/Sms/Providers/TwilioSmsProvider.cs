//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Sms.Models;
using Mvp24Hours.Infrastructure.Sms.Options;
using Mvp24Hours.Infrastructure.Sms.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Sms.Providers
{
    /// <summary>
    /// Twilio SMS provider implementation using Twilio REST API.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provider sends SMS messages using the Twilio REST API. It supports international
    /// messaging, alphanumeric sender IDs, and Twilio-specific features like delivery status callbacks.
    /// </para>
    /// <para>
    /// <strong>Dependencies:</strong>
    /// This provider requires the Twilio NuGet package. Install it via:
    /// <c>dotnet add package Twilio</c>
    /// </para>
    /// <para>
    /// <strong>Alternative Implementation:</strong>
    /// For a more robust implementation, consider using the official Twilio SDK:
    /// <c>dotnet add package Twilio</c>
    /// Then use <c>Twilio.Rest.Api.V2010.Account.MessageResource</c> instead of direct HTTP calls.
    /// </para>
    /// <para>
    /// <strong>Configuration:</strong>
    /// Configure Twilio settings via <see cref="TwilioSmsOptions"/> when registering the service.
    /// You'll need a Twilio Account SID and Auth Token from your Twilio Console.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register Twilio SMS service
    /// services.AddTwilioSmsService(options =>
    /// {
    ///     options.DefaultFrom = "+5511888888888";
    /// }, twilioOptions =>
    /// {
    ///     twilioOptions.AccountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
    ///     twilioOptions.AuthToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");
    /// });
    /// </code>
    /// </example>
    public class TwilioSmsProvider : BaseSmsProvider
    {
        private readonly TwilioSmsOptions _twilioOptions;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TwilioSmsProvider>? _logger;
        private readonly string _apiBaseUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="TwilioSmsProvider"/> class.
        /// </summary>
        /// <param name="smsOptions">The SMS options.</param>
        /// <param name="twilioOptions">The Twilio-specific options.</param>
        /// <param name="httpClientFactory">The HTTP client factory for creating HTTP clients.</param>
        /// <param name="logger">Optional logger for diagnostic information.</param>
        /// <exception cref="ArgumentNullException">Thrown when smsOptions, twilioOptions, or httpClientFactory is null.</exception>
        public TwilioSmsProvider(
            IOptions<SmsOptions> smsOptions,
            IOptions<TwilioSmsOptions> twilioOptions,
            IHttpClientFactory httpClientFactory,
            ILogger<TwilioSmsProvider>? logger = null)
            : base(smsOptions)
        {
            _twilioOptions = twilioOptions?.Value ?? throw new ArgumentNullException(nameof(twilioOptions));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger;

            // Validate Twilio options
            var validationErrors = _twilioOptions.Validate();
            if (validationErrors.Count > 0)
            {
                var errorMessage = string.Join("; ", validationErrors);
                throw new InvalidOperationException($"Invalid Twilio configuration: {errorMessage}");
            }

            _apiBaseUrl = !string.IsNullOrWhiteSpace(_twilioOptions.ApiBaseUrl)
                ? _twilioOptions.ApiBaseUrl
                : "https://api.twilio.com";
        }

        /// <summary>
        /// Sends the SMS message using Twilio REST API.
        /// </summary>
        /// <param name="message">The SMS message to send (with defaults applied).</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A result indicating success or failure with message ID and status information.</returns>
        protected override async Task<SmsSendResult> SendSmsAsync(
            SmsMessage message,
            CancellationToken cancellationToken)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                // Twilio API endpoint: POST /2010-04-01/Accounts/{AccountSid}/Messages.json
                var url = $"{_apiBaseUrl}/2010-04-01/Accounts/{_twilioOptions.AccountSid}/Messages.json";

                // Create Basic Auth header (AccountSid:AuthToken base64 encoded)
                var authBytes = Encoding.UTF8.GetBytes($"{_twilioOptions.AccountSid}:{_twilioOptions.AuthToken}");
                var authHeader = Convert.ToBase64String(authBytes);
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", authHeader);

                // Prepare form data
                var formData = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("To", message.To!),
                    new KeyValuePair<string, string>("Body", message.Body!)
                };

                if (!string.IsNullOrWhiteSpace(message.From))
                {
                    formData.Add(new KeyValuePair<string, string>("From", message.From));
                }

                // Add metadata if supported
                if (message.Metadata != null)
                {
                    foreach (var metadata in message.Metadata)
                    {
                        formData.Add(new KeyValuePair<string, string>($"Meta{metadata.Key}", metadata.Value));
                    }
                }

                var formContent = new FormUrlEncodedContent(formData);

                _logger?.LogDebug(
                    "Sending SMS via Twilio to {To} from {From}",
                    message.To,
                    message.From ?? Options.DefaultFrom);

                var response = await httpClient.PostAsync(url, formContent, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    // Parse Twilio response
                    var responseJson = JsonDocument.Parse(responseContent);
                    var messageId = responseJson.RootElement.GetProperty("sid").GetString();
                    var status = responseJson.RootElement.GetProperty("status").GetString();

                    _logger?.LogInformation(
                        "SMS sent successfully via Twilio. MessageId: {MessageId}, Status: {Status}",
                        messageId,
                        status);

                    // Map Twilio status to our enum
                    var deliveryStatus = MapTwilioStatus(status);

                    return SmsSendResult.Successful(messageId, deliveryStatus);
                }
                else
                {
                    // Parse error response
                    string? errorMessage = null;
                    try
                    {
                        var errorJson = JsonDocument.Parse(responseContent);
                        if (errorJson.RootElement.TryGetProperty("message", out var messageElement))
                        {
                            errorMessage = messageElement.GetString();
                        }
                    }
                    catch
                    {
                        // If parsing fails, use status code
                        errorMessage = $"Twilio API returned status code: {(int)response.StatusCode}";
                    }

                    _logger?.LogError(
                        "Failed to send SMS via Twilio. Status: {StatusCode}, Error: {Error}",
                        response.StatusCode,
                        errorMessage ?? responseContent);

                    return SmsSendResult.Failed(
                        errorMessage ?? $"Twilio API error: {response.StatusCode}",
                        new HttpRequestException($"Twilio API returned status code: {(int)response.StatusCode}"));
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger?.LogError(ex, "Timeout while sending SMS via Twilio");
                return SmsSendResult.Failed("SMS sending timed out", ex);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sending SMS via Twilio");
                return SmsSendResult.Failed(ex);
            }
        }

        /// <summary>
        /// Maps Twilio message status to our delivery status enum.
        /// </summary>
        /// <param name="twilioStatus">The Twilio status string.</param>
        /// <returns>The mapped delivery status.</returns>
        private static SmsDeliveryStatus MapTwilioStatus(string? twilioStatus)
        {
            return twilioStatus?.ToUpperInvariant() switch
            {
                "QUEUED" => SmsDeliveryStatus.Queued,
                "SENT" => SmsDeliveryStatus.Sent,
                "DELIVERED" => SmsDeliveryStatus.Delivered,
                "FAILED" => SmsDeliveryStatus.Failed,
                "UNDELIVERED" => SmsDeliveryStatus.Undelivered,
                _ => SmsDeliveryStatus.Unknown
            };
        }
    }
}

