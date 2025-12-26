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
    /// SendGrid email provider implementation using SendGrid Web API v3.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provider sends emails using the SendGrid Web API v3. It supports HTML and plain text
    /// emails, attachments, categories, tracking, and other SendGrid-specific features.
    /// </para>
    /// <para>
    /// <strong>Dependencies:</strong>
    /// This provider requires the SendGrid NuGet package. Install it via:
    /// <c>dotnet add package SendGrid</c>
    /// </para>
    /// <para>
    /// <strong>Configuration:</strong>
    /// Configure SendGrid settings via <see cref="SendGridEmailOptions"/> when registering the service.
    /// You'll need a SendGrid API key from your SendGrid account.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register SendGrid email service
    /// services.AddSendGridEmailService(options =>
    /// {
    ///     options.DefaultFrom = "noreply@example.com";
    /// }, sendGridOptions =>
    /// {
    ///     sendGridOptions.ApiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
    ///     sendGridOptions.EnableClickTracking = true;
    ///     sendGridOptions.EnableOpenTracking = true;
    /// });
    /// </code>
    /// </example>
    public class SendGridEmailProvider : BaseEmailProvider
    {
        private readonly SendGridEmailOptions _sendGridOptions;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SendGridEmailProvider>? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendGridEmailProvider"/> class.
        /// </summary>
        /// <param name="emailOptions">The email options.</param>
        /// <param name="sendGridOptions">The SendGrid-specific options.</param>
        /// <param name="httpClientFactory">The HTTP client factory for creating HTTP clients.</param>
        /// <param name="logger">Optional logger for diagnostic information.</param>
        /// <exception cref="ArgumentNullException">Thrown when emailOptions, sendGridOptions, or httpClientFactory is null.</exception>
        public SendGridEmailProvider(
            IOptions<EmailOptions> emailOptions,
            IOptions<SendGridEmailOptions> sendGridOptions,
            IHttpClientFactory httpClientFactory,
            ILogger<SendGridEmailProvider>? logger = null)
            : base(emailOptions)
        {
            _sendGridOptions = sendGridOptions?.Value ?? throw new ArgumentNullException(nameof(sendGridOptions));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger;

            // Validate SendGrid options
            var validationErrors = _sendGridOptions.Validate();
            if (validationErrors.Count > 0)
            {
                var errorMessage = string.Join("; ", validationErrors);
                throw new InvalidOperationException($"Invalid SendGrid configuration: {errorMessage}");
            }
        }

        /// <summary>
        /// Sends the email message using SendGrid API.
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
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _sendGridOptions.ApiKey);
                httpClient.BaseAddress = new Uri(_sendGridOptions.ApiBaseUrl);

                var requestBody = CreateSendGridRequest(message);
                var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("/mail/send", httpContent, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    // SendGrid doesn't return a message ID in the response, so we generate one
                    var messageId = Guid.NewGuid().ToString("N");

                    _logger?.LogDebug("Email sent successfully via SendGrid. MessageId: {MessageId}, To: {To}, Subject: {Subject}",
                        messageId, string.Join(", ", message.To), message.Subject);

                    return EmailSendResult.Successful(messageId);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    var errorMessage = $"SendGrid API error: {response.StatusCode} - {errorContent}";

                    _logger?.LogError("SendGrid API error. StatusCode: {StatusCode}, Response: {Response}, To: {To}, Subject: {Subject}",
                        response.StatusCode, errorContent, string.Join(", ", message.To), message.Subject);

                    return EmailSendResult.Failed(errorMessage);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "HTTP error while sending email via SendGrid. To: {To}, Subject: {Subject}",
                    string.Join(", ", message.To), message.Subject);

                return EmailSendResult.Failed(
                    $"HTTP error while sending email: {ex.Message}",
                    ex);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error while sending email via SendGrid. To: {To}, Subject: {Subject}",
                    string.Join(", ", message.To), message.Subject);

                return EmailSendResult.Failed(
                    $"Failed to send email: {ex.Message}",
                    ex);
            }
        }

        /// <summary>
        /// Creates a SendGrid API request object from an EmailMessage.
        /// </summary>
        /// <param name="message">The email message to convert.</param>
        /// <returns>A SendGrid request object.</returns>
        private object CreateSendGridRequest(EmailMessage message)
        {
            var from = ParseEmailAddress(message.From ?? _sendGridOptions.DefaultFrom ?? Options.DefaultFrom ?? string.Empty);
            var personalizations = new List<object>
            {
                new
                {
                    to = (message.To ?? Enumerable.Empty<string>()).Select(ParseEmailAddress).ToArray(),
                    cc = (message.Cc ?? Enumerable.Empty<string>()).Any() 
                        ? (message.Cc ?? Enumerable.Empty<string>()).Select(ParseEmailAddress).ToArray() 
                        : null,
                    bcc = (message.Bcc ?? Enumerable.Empty<string>()).Any() 
                        ? (message.Bcc ?? Enumerable.Empty<string>()).Select(ParseEmailAddress).ToArray() 
                        : null,
                    subject = message.Subject ?? string.Empty
                }
            };

            var content = new List<object>();
            if (!string.IsNullOrWhiteSpace(message.HtmlBody))
            {
                content.Add(new { type = "text/html", value = message.HtmlBody });
            }
            if (!string.IsNullOrWhiteSpace(message.PlainTextBody))
            {
                content.Add(new { type = "text/plain", value = message.PlainTextBody });
            }

            var request = new
            {
                personalizations = personalizations,
                from = from,
                reply_to = !string.IsNullOrWhiteSpace(message.ReplyTo) 
                    ? ParseEmailAddress(message.ReplyTo) 
                    : (!string.IsNullOrWhiteSpace(Options.DefaultReplyTo) 
                        ? ParseEmailAddress(Options.DefaultReplyTo) 
                        : null),
                subject = message.Subject ?? string.Empty,
                content = content,
                attachments = (message.Attachments ?? Enumerable.Empty<Contract.IEmailAttachment>())
                    .Select(CreateSendGridAttachment)
                    .ToArray(),
                categories = _sendGridOptions.DefaultCategories.Any() 
                    ? _sendGridOptions.DefaultCategories.ToArray() 
                    : null,
                tracking_settings = new
                {
                    click_tracking = new { enable = _sendGridOptions.EnableClickTracking },
                    open_tracking = new { enable = _sendGridOptions.EnableOpenTracking }
                },
                headers = message.Headers.Any() 
                    ? message.Headers.ToDictionary(h => h.Key, h => h.Value) 
                    : null
            };

            return request;
        }

        /// <summary>
        /// Parses an email address string into SendGrid format.
        /// </summary>
        /// <param name="address">The email address string (can include display name).</param>
        /// <returns>A SendGrid email address object.</returns>
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

                return new { email = emailAddress, name = displayName };
            }

            // Simple email address
            var defaultName = _sendGridOptions.DefaultFromName;
            return new { email = address, name = defaultName };
        }

        /// <summary>
        /// Creates a SendGrid attachment object from an IEmailAttachment.
        /// </summary>
        /// <param name="attachment">The email attachment to convert.</param>
        /// <returns>A SendGrid attachment object.</returns>
        private object CreateSendGridAttachment(Contract.IEmailAttachment attachment)
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
                content = base64Content,
                filename = attachment.FileName,
                type = attachment.ContentType,
                disposition = attachment.IsInline ? "inline" : "attachment",
                content_id = attachment.ContentId
            };
        }
    }
}

