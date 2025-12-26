//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Email.Models;
using Mvp24Hours.Infrastructure.Email.Options;
using Mvp24Hours.Infrastructure.Email.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Email.Providers
{
    /// <summary>
    /// Azure Communication Services email provider implementation using Azure Communication Services Email API.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provider sends emails using the Azure Communication Services Email API. It supports HTML and plain text
    /// emails, attachments, and Azure-specific features like user engagement tracking.
    /// </para>
    /// <para>
    /// <strong>Dependencies:</strong>
    /// This provider requires the Azure Communication Services Email NuGet package. Install it via:
    /// <c>dotnet add package Azure.Communication.Email</c>
    /// </para>
    /// <para>
    /// <strong>Configuration:</strong>
    /// Configure Azure Communication Services settings via <see cref="AzureCommunicationEmailOptions"/> when registering the service.
    /// You'll need a connection string from your Azure Communication Services resource.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register Azure Communication Services email service
    /// services.AddAzureCommunicationEmailService(options =>
    /// {
    ///     options.DefaultFrom = "noreply@example.com";
    /// }, azureOptions =>
    /// {
    ///     azureOptions.ConnectionString = Environment.GetEnvironmentVariable("AZURE_COMMUNICATION_CONNECTION_STRING");
    ///     azureOptions.EnableUserEngagementTracking = true;
    /// });
    /// </code>
    /// </example>
    public class AzureCommunicationEmailProvider : BaseEmailProvider
    {
        private readonly AzureCommunicationEmailOptions _azureOptions;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AzureCommunicationEmailProvider>? _logger;
        private readonly string _accessKey;
        private readonly string _endpoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureCommunicationEmailProvider"/> class.
        /// </summary>
        /// <param name="emailOptions">The email options.</param>
        /// <param name="azureOptions">The Azure Communication Services-specific options.</param>
        /// <param name="httpClientFactory">The HTTP client factory for creating HTTP clients.</param>
        /// <param name="logger">Optional logger for diagnostic information.</param>
        /// <exception cref="ArgumentNullException">Thrown when emailOptions, azureOptions, or httpClientFactory is null.</exception>
        public AzureCommunicationEmailProvider(
            IOptions<EmailOptions> emailOptions,
            IOptions<AzureCommunicationEmailOptions> azureOptions,
            IHttpClientFactory httpClientFactory,
            ILogger<AzureCommunicationEmailProvider>? logger = null)
            : base(emailOptions)
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
        }

        /// <summary>
        /// Sends the email message using Azure Communication Services API.
        /// </summary>
        /// <param name="message">The email message to send (with defaults applied).</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A result indicating success or failure with message ID and error information.</returns>
        protected override async Task<EmailSendResult> SendEmailAsync(
            EmailMessage message,
            CancellationToken cancellationToken)
        {
            try
            {
                using var httpClient = _httpClientFactory.CreateClient();
                
                // Azure Communication Services uses HMAC-SHA256 authentication
                var requestUri = $"{_endpoint}/emails:send?api-version=2023-03-31";
                httpClient.BaseAddress = new Uri(_endpoint);

                var requestBody = CreateAzureCommunicationRequest(message);
                var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Create authorization header using HMAC-SHA256
                // Note: For production, consider using Azure.Communication.Email SDK which handles this automatically
                var dateHeader = DateTimeOffset.UtcNow.ToString("r");
                var signature = GenerateHmacSignature("POST", requestUri, dateHeader, jsonContent, _accessKey);
                
                httpClient.DefaultRequestHeaders.Add("x-ms-date", dateHeader);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"HMAC-SHA256 SignedHeaders=x-ms-date;host;Signature={signature}");
                httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");

                var response = await httpClient.PostAsync("/emails:send?api-version=2023-03-31", httpContent, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    var responseJson = JsonDocument.Parse(responseContent);
                    
                    // Extract message ID from response
                    string? messageId = null;
                    if (responseJson.RootElement.TryGetProperty("messageId", out var messageIdElement))
                    {
                        messageId = messageIdElement.GetString();
                    }
                    else
                    {
                        messageId = Guid.NewGuid().ToString("N");
                    }

                    _logger?.LogDebug("Email sent successfully via Azure Communication Services. MessageId: {MessageId}, To: {To}, Subject: {Subject}",
                        messageId, string.Join(", ", message.To), message.Subject);

                    return EmailSendResult.Successful(messageId);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    var errorMessage = $"Azure Communication Services API error: {response.StatusCode} - {errorContent}";

                    _logger?.LogError("Azure Communication Services API error. StatusCode: {StatusCode}, Response: {Response}, To: {To}, Subject: {Subject}",
                        response.StatusCode, errorContent, string.Join(", ", message.To), message.Subject);

                    return EmailSendResult.Failed(errorMessage);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "HTTP error while sending email via Azure Communication Services. To: {To}, Subject: {Subject}",
                    string.Join(", ", message.To), message.Subject);

                return EmailSendResult.Failed(
                    $"HTTP error while sending email: {ex.Message}",
                    ex);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error while sending email via Azure Communication Services. To: {To}, Subject: {Subject}",
                    string.Join(", ", message.To), message.Subject);

                return EmailSendResult.Failed(
                    $"Failed to send email: {ex.Message}",
                    ex);
            }
        }

        /// <summary>
        /// Parses the Azure Communication Services connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to parse.</param>
        /// <returns>A tuple containing the endpoint and access key.</returns>
        private (string endpoint, string accessKey) ParseConnectionString(string connectionString)
        {
            var parts = connectionString.Split(';');
            string? endpoint = null;
            string? accessKey = null;

            foreach (var part in parts)
            {
                var keyValue = part.Split('=', 2);
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim().ToLowerInvariant();
                    var value = keyValue[1].Trim();

                    if (key == "endpoint")
                    {
                        endpoint = value;
                    }
                    else if (key == "accesskey")
                    {
                        accessKey = value;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(accessKey))
            {
                throw new InvalidOperationException("Connection string must contain both 'endpoint' and 'accesskey'.");
            }

            return (endpoint, accessKey);
        }

        /// <summary>
        /// Generates HMAC-SHA256 signature for Azure Communication Services authentication.
        /// </summary>
        /// <param name="method">The HTTP method.</param>
        /// <param name="requestUri">The request URI.</param>
        /// <param name="dateHeader">The date header value.</param>
        /// <param name="content">The request content.</param>
        /// <param name="accessKey">The access key.</param>
        /// <returns>The HMAC signature.</returns>
        private string GenerateHmacSignature(string method, string requestUri, string dateHeader, string content, string accessKey)
        {
            // Note: This is a simplified implementation. For production, use Azure.Communication.Email SDK
            // which handles authentication properly.
            using var hmac = new System.Security.Cryptography.HMACSHA256(Convert.FromBase64String(accessKey));
            var stringToSign = $"{method}\n{requestUri}\n{dateHeader}\n{content}";
            var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
            return Convert.ToBase64String(signatureBytes);
        }

        /// <summary>
        /// Creates an Azure Communication Services API request object from an EmailMessage.
        /// </summary>
        /// <param name="message">The email message to convert.</param>
        /// <returns>An Azure Communication Services request object.</returns>
        private object CreateAzureCommunicationRequest(EmailMessage message)
        {
            var from = ParseEmailAddress(message.From ?? _azureOptions.DefaultFrom ?? Options.DefaultFrom ?? string.Empty);
            
            var recipients = new
            {
                to = (message.To ?? Enumerable.Empty<string>()).Select(ParseEmailAddress).ToArray(),
                cc = (message.Cc ?? Enumerable.Empty<string>()).Any() 
                    ? (message.Cc ?? Enumerable.Empty<string>()).Select(ParseEmailAddress).ToArray() 
                    : null,
                bcc = (message.Bcc ?? Enumerable.Empty<string>()).Any() 
                    ? (message.Bcc ?? Enumerable.Empty<string>()).Select(ParseEmailAddress).ToArray() 
                    : null
            };

            var content = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(message.HtmlBody))
            {
                content["html"] = message.HtmlBody;
            }
            if (!string.IsNullOrWhiteSpace(message.PlainTextBody))
            {
                content["plainText"] = message.PlainTextBody;
            }

            var request = new
            {
                senderAddress = from.email,
                content = new
                {
                    subject = message.Subject ?? string.Empty,
                    plainText = message.PlainTextBody,
                    html = message.HtmlBody
                },
                recipients = recipients,
                replyTo = !string.IsNullOrWhiteSpace(message.ReplyTo) 
                    ? new[] { ParseEmailAddress(message.ReplyTo) } 
                    : (!string.IsNullOrWhiteSpace(Options.DefaultReplyTo) 
                        ? new[] { ParseEmailAddress(Options.DefaultReplyTo) } 
                        : null),
                attachments = (message.Attachments ?? Enumerable.Empty<Contract.IEmailAttachment>())
                    .Select(CreateAzureCommunicationAttachment)
                    .ToArray(),
                userEngagementTrackingDisabled = !_azureOptions.EnableUserEngagementTracking,
                headers = message.Headers.Any() 
                    ? message.Headers.ToDictionary(h => h.Key, h => h.Value) 
                    : null
            };

            return request;
        }

        /// <summary>
        /// Parses an email address string into Azure Communication Services format.
        /// </summary>
        /// <param name="address">The email address string (can include display name).</param>
        /// <returns>An Azure Communication Services email address object.</returns>
        private object ParseEmailAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException("Email address cannot be null or empty.", nameof(address));
            }

            // Try to parse as "Display Name <email@domain.com>" format
            if (address.Contains('<') && address.Contains('>'))
            {
                var startIndex = address.IndexOf('<');
                var endIndex = address.IndexOf('>');
                var displayName = address.Substring(0, startIndex).Trim().Trim('"');
                var emailAddress = address.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();

                return new { email = emailAddress, displayName = displayName };
            }

            // Simple email address
            var defaultName = _azureOptions.DefaultFromName;
            return new { email = address, displayName = defaultName };
        }

        /// <summary>
        /// Creates an Azure Communication Services attachment object from an IEmailAttachment.
        /// </summary>
        /// <param name="attachment">The email attachment to convert.</param>
        /// <returns>An Azure Communication Services attachment object.</returns>
        private object CreateAzureCommunicationAttachment(Contract.IEmailAttachment attachment)
        {
            if (attachment == null)
            {
                throw new ArgumentNullException(nameof(attachment));
            }

            var contentBytes = attachment.Content;
            if (contentBytes == null)
            {
                using var stream = attachment.GetContentStream();
                if (stream == null)
                {
                    throw new InvalidOperationException($"Cannot get content for attachment '{attachment.FileName}'.");
                }

                using var memoryStream = new System.IO.MemoryStream();
                stream.CopyTo(memoryStream);
                contentBytes = memoryStream.ToArray();
            }

            var base64Content = Convert.ToBase64String(contentBytes);

            return new
            {
                name = attachment.FileName,
                contentType = attachment.ContentType,
                contentInBase64 = base64Content
            };
        }
    }
}

