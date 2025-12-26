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
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Sms.Providers
{
    /// <summary>
    /// Azure Communication Services SMS provider implementation using Azure Communication Services SMS API.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provider sends SMS messages using the Azure Communication Services SMS API. It supports
    /// international messaging, delivery reports, and Azure-specific features.
    /// </para>
    /// <para>
    /// <strong>Dependencies:</strong>
    /// This provider requires the Azure Communication Services SMS NuGet package. Install it via:
    /// <c>dotnet add package Azure.Communication.Sms</c>
    /// </para>
    /// <para>
    /// <strong>Alternative Implementation:</strong>
    /// For a more robust implementation, consider using the official Azure SDK:
    /// <c>dotnet add package Azure.Communication.Sms</c>
    /// Then use <c>Azure.Communication.Sms.SmsClient</c> instead of direct HTTP calls.
    /// </para>
    /// <para>
    /// <strong>Configuration:</strong>
    /// Configure Azure Communication Services settings via <see cref="AzureCommunicationSmsOptions"/> when registering the service.
    /// You'll need a connection string from your Azure Communication Services resource.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register Azure Communication Services SMS service
    /// services.AddAzureCommunicationSmsService(options =>
    /// {
    ///     options.DefaultFrom = "+5511888888888";
    /// }, azureOptions =>
    /// {
    ///     azureOptions.ConnectionString = Environment.GetEnvironmentVariable("AZURE_COMMUNICATION_CONNECTION_STRING");
    ///     azureOptions.EnableDeliveryReports = true;
    /// });
    /// </code>
    /// </example>
    public class AzureCommunicationSmsProvider : BaseSmsProvider
    {
        private readonly AzureCommunicationSmsOptions _azureOptions;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AzureCommunicationSmsProvider>? _logger;
        private readonly string _endpoint;
        private readonly string _accessKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureCommunicationSmsProvider"/> class.
        /// </summary>
        /// <param name="smsOptions">The SMS options.</param>
        /// <param name="azureOptions">The Azure Communication Services-specific options.</param>
        /// <param name="httpClientFactory">The HTTP client factory for creating HTTP clients.</param>
        /// <param name="logger">Optional logger for diagnostic information.</param>
        /// <exception cref="ArgumentNullException">Thrown when smsOptions, azureOptions, or httpClientFactory is null.</exception>
        public AzureCommunicationSmsProvider(
            IOptions<SmsOptions> smsOptions,
            IOptions<AzureCommunicationSmsOptions> azureOptions,
            IHttpClientFactory httpClientFactory,
            ILogger<AzureCommunicationSmsProvider>? logger = null)
            : base(smsOptions)
        {
            _azureOptions = azureOptions?.Value ?? throw new ArgumentNullException(nameof(azureOptions));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger;

            // Validate Azure options
            var validationErrors = _azureOptions.Validate();
            if (validationErrors.Count > 0)
            {
                var errorMessage = string.Join("; ", validationErrors);
                throw new InvalidOperationException($"Invalid Azure Communication Services configuration: {errorMessage}");
            }

            // Parse connection string
            (_endpoint, _accessKey) = ParseConnectionString(_azureOptions.ConnectionString);
            if (!string.IsNullOrWhiteSpace(_azureOptions.Endpoint))
            {
                _endpoint = _azureOptions.Endpoint;
            }
            if (!string.IsNullOrWhiteSpace(_azureOptions.AccessKey))
            {
                _accessKey = _azureOptions.AccessKey;
            }
        }

        /// <summary>
        /// Sends the SMS message using Azure Communication Services SMS API.
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

                // Azure Communication Services SMS API endpoint
                var url = $"{_endpoint}/sms?api-version=2021-03-07";

                // Create authorization header (HMAC-SHA256 signature)
                var dateHeader = DateTimeOffset.UtcNow.ToString("r");
                var signature = GenerateSignature("POST", url, dateHeader, _accessKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"HMAC-SHA256 SignedHeaders=x-ms-date;host;x-ms-content-sha256&Signature={signature}");
                httpClient.DefaultRequestHeaders.Add("x-ms-date", dateHeader);
                httpClient.DefaultRequestHeaders.Add("x-ms-content-sha256", ComputeSha256(""));

                // Prepare request body
                var requestBody = new
                {
                    from = message.From ?? Options.DefaultFrom,
                    smsRecipients = new[]
                    {
                        new
                        {
                            to = message.To,
                            repeatabilityRequestId = Guid.NewGuid().ToString(),
                            repeatabilityFirstSent = DateTimeOffset.UtcNow.ToString("O")
                        }
                    },
                    message = message.Body,
                    smsSendOptions = new
                    {
                        enableDeliveryReport = _azureOptions.EnableDeliveryReports
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger?.LogDebug(
                    "Sending SMS via Azure Communication Services to {To} from {From}",
                    message.To,
                    message.From ?? Options.DefaultFrom);

                var response = await httpClient.PostAsync(url, content, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    // Parse Azure response
                    var responseJson = JsonDocument.Parse(responseContent);
                    var valueArray = responseJson.RootElement.GetProperty("value");
                    if (valueArray.GetArrayLength() > 0)
                    {
                        var firstResult = valueArray[0];
                        var messageId = firstResult.TryGetProperty("messageId", out var messageIdElement)
                            ? messageIdElement.GetString()
                            : null;
                        var httpStatusCode = firstResult.TryGetProperty("httpStatusCode", out var statusElement)
                            ? statusElement.GetInt32()
                            : (int)response.StatusCode;

                        if (httpStatusCode >= 200 && httpStatusCode < 300)
                        {
                            _logger?.LogInformation(
                                "SMS sent successfully via Azure Communication Services. MessageId: {MessageId}",
                                messageId);

                            return SmsSendResult.Successful(messageId, SmsDeliveryStatus.Queued);
                        }
                        else
                        {
                            var errorMessage = firstResult.TryGetProperty("errorMessage", out var errorElement)
                                ? errorElement.GetString()
                                : $"Azure Communication Services returned status code: {httpStatusCode}";

                            _logger?.LogError(
                                "Failed to send SMS via Azure Communication Services. Status: {StatusCode}, Error: {Error}",
                                httpStatusCode,
                                errorMessage);

                            return SmsSendResult.Failed(
                                errorMessage ?? $"Azure Communication Services error: {httpStatusCode}");
                        }
                    }
                    else
                    {
                        _logger?.LogError("Azure Communication Services returned empty response");
                        return SmsSendResult.Failed("Azure Communication Services returned empty response");
                    }
                }
                else
                {
                    _logger?.LogError(
                        "Failed to send SMS via Azure Communication Services. Status: {StatusCode}, Error: {Error}",
                        response.StatusCode,
                        responseContent);

                    return SmsSendResult.Failed(
                        $"Azure Communication Services API error: {response.StatusCode}",
                        new HttpRequestException($"Azure Communication Services API returned status code: {(int)response.StatusCode}"));
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger?.LogError(ex, "Timeout while sending SMS via Azure Communication Services");
                return SmsSendResult.Failed("SMS sending timed out", ex);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sending SMS via Azure Communication Services");
                return SmsSendResult.Failed(ex);
            }
        }

        /// <summary>
        /// Parses the Azure Communication Services connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>A tuple containing the endpoint and access key.</returns>
        private static (string endpoint, string accessKey) ParseConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));
            }

            var parts = connectionString.Split(';');
            string? endpoint = null;
            string? accessKey = null;

            foreach (var part in parts)
            {
                var keyValue = part.Split('=', 2);
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim();
                    var value = keyValue[1].Trim();

                    if (key.Equals("endpoint", StringComparison.OrdinalIgnoreCase))
                    {
                        endpoint = value;
                    }
                    else if (key.Equals("accesskey", StringComparison.OrdinalIgnoreCase))
                    {
                        accessKey = value;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentException("Connection string must contain 'endpoint' parameter.", nameof(connectionString));
            }

            if (string.IsNullOrWhiteSpace(accessKey))
            {
                throw new ArgumentException("Connection string must contain 'accesskey' parameter.", nameof(connectionString));
            }

            return (endpoint, accessKey);
        }

        /// <summary>
        /// Generates HMAC-SHA256 signature for Azure Communication Services authentication.
        /// </summary>
        /// <param name="method">The HTTP method.</param>
        /// <param name="url">The request URL.</param>
        /// <param name="dateHeader">The date header value.</param>
        /// <param name="accessKey">The access key.</param>
        /// <returns>The signature string.</returns>
        private static string GenerateSignature(string method, string url, string dateHeader, string accessKey)
        {
            // Simplified signature generation - for production, use proper HMAC-SHA256
            // This is a placeholder implementation
            var stringToSign = $"{method}\n{url}\n{dateHeader}\n";
            var keyBytes = Convert.FromBase64String(accessKey);
            using var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes);
            var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
            return Convert.ToBase64String(signatureBytes);
        }

        /// <summary>
        /// Computes SHA256 hash of the content.
        /// </summary>
        /// <param name="content">The content to hash.</param>
        /// <returns>The base64-encoded hash.</returns>
        private static string ComputeSha256(string content)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
            return Convert.ToBase64String(hashBytes);
        }
    }
}

