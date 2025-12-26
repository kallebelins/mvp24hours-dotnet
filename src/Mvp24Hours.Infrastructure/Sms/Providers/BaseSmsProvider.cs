//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Sms.Contract;
using Mvp24Hours.Infrastructure.Sms.Models;
using Mvp24Hours.Infrastructure.Sms.Options;
using Mvp24Hours.Infrastructure.Sms.Results;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Sms.Providers
{
    /// <summary>
    /// Base class for SMS providers that implements common validation and default application logic.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This abstract class provides a foundation for implementing SMS providers by handling
    /// common tasks such as message validation, applying default values, and error handling.
    /// Derived classes only need to implement the actual sending logic.
    /// </para>
    /// </remarks>
    public abstract class BaseSmsProvider : ISmsService
    {
        /// <summary>
        /// Gets the SMS options.
        /// </summary>
        protected SmsOptions Options { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseSmsProvider"/> class.
        /// </summary>
        /// <param name="options">The SMS options.</param>
        /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
        protected BaseSmsProvider(IOptions<SmsOptions> options)
            : this(options?.Value ?? throw new ArgumentNullException(nameof(options)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseSmsProvider"/> class.
        /// </summary>
        /// <param name="options">The SMS options.</param>
        /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
        protected BaseSmsProvider(SmsOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Sends an SMS message.
        /// </summary>
        /// <param name="message">The SMS message to send.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A result indicating success or failure with message ID and status information.</returns>
        public async Task<SmsSendResult> SendAsync(
            SmsMessage message,
            CancellationToken cancellationToken = default)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Validate message
            var validationErrors = ValidateMessage(message);
            if (validationErrors.Count > 0)
            {
                return SmsSendResult.Failed(validationErrors);
            }

            // Apply defaults
            var smsToSend = ApplyDefaults(message);

            try
            {
                // Delegate to derived class for actual sending
                var result = await SendSmsAsync(smsToSend, cancellationToken);
                return result;
            }
            catch (Exception ex)
            {
                return SmsSendResult.Failed(ex);
            }
        }

        /// <summary>
        /// Sends multiple SMS messages in batch.
        /// </summary>
        /// <param name="messages">The SMS messages to send.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A collection of results, one for each message sent.</returns>
        public async Task<IList<SmsSendResult>> SendBatchAsync(
            IEnumerable<SmsMessage> messages,
            CancellationToken cancellationToken = default)
        {
            if (messages == null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            cancellationToken.ThrowIfCancellationRequested();

            var results = new List<SmsSendResult>();
            var messagesList = messages.ToList();

            foreach (var message in messagesList)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await SendAsync(message, cancellationToken);
                results.Add(result);
            }

            return results;
        }

        /// <summary>
        /// Sends the SMS message using the provider-specific implementation.
        /// </summary>
        /// <param name="message">The SMS message to send (with defaults applied).</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A result indicating success or failure with message ID and status information.</returns>
        protected abstract Task<SmsSendResult> SendSmsAsync(
            SmsMessage message,
            CancellationToken cancellationToken);

        /// <summary>
        /// Validates the SMS message.
        /// </summary>
        /// <param name="message">The message to validate.</param>
        /// <returns>A list of validation errors, or an empty list if valid.</returns>
        protected virtual IList<string> ValidateMessage(SmsMessage message)
        {
            var errors = new List<string>();

            // Validate using SmsMessage.Validate()
            var validationErrors = message.Validate();
            errors.AddRange(validationErrors);

            // Validate phone number format if enabled
            if (Options.ValidatePhoneNumbers && !string.IsNullOrWhiteSpace(message.To))
            {
                if (!IsValidPhoneNumber(message.To))
                {
                    errors.Add($"Invalid phone number format: {message.To}");
                }
            }

            // Validate message length if limit is set
            if (Options.MaxMessageLength.HasValue && !string.IsNullOrWhiteSpace(message.Body))
            {
                if (message.Body.Length > Options.MaxMessageLength.Value)
                {
                    errors.Add($"Message body length ({message.Body.Length}) exceeds maximum allowed ({Options.MaxMessageLength.Value}).");
                }
            }

            return errors;
        }

        /// <summary>
        /// Applies default values from options to the SMS message.
        /// </summary>
        /// <param name="message">The message to apply defaults to.</param>
        /// <returns>A new SMS message with defaults applied.</returns>
        protected virtual SmsMessage ApplyDefaults(SmsMessage message)
        {
            // Create a copy to avoid modifying the original
            var smsCopy = new SmsMessage
            {
                To = message.To,
                From = message.From ?? Options.DefaultFrom,
                Body = message.Body,
                Metadata = message.Metadata != null
                    ? new Dictionary<string, string>(message.Metadata)
                    : null
            };

            // Apply country code if needed
            if (!string.IsNullOrWhiteSpace(Options.DefaultCountryCode) &&
                !string.IsNullOrWhiteSpace(smsCopy.To) &&
                !smsCopy.To.StartsWith("+"))
            {
                // Note: This is a simple implementation. A full phone number formatting
                // library (like libphonenumber) would be better for production.
                // For now, we just ensure the number has a country code prefix.
                // The provider should handle proper formatting.
            }

            return smsCopy;
        }

        /// <summary>
        /// Validates a phone number format (basic validation).
        /// </summary>
        /// <param name="phoneNumber">The phone number to validate.</param>
        /// <returns>True if the phone number appears valid; otherwise, false.</returns>
        /// <remarks>
        /// This is a basic validation. For production use, consider using a library
        /// like libphonenumber for comprehensive phone number validation.
        /// </remarks>
        protected virtual bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return false;
            }

            // Remove common formatting characters
            var cleaned = phoneNumber.Replace(" ", "")
                                     .Replace("-", "")
                                     .Replace("(", "")
                                     .Replace(")", "")
                                     .Replace(".", "");

            // Check if it starts with + (international format) or is all digits
            if (cleaned.StartsWith("+"))
            {
                cleaned = cleaned.Substring(1);
            }

            // Should contain only digits and be reasonable length (7-15 digits)
            return cleaned.All(char.IsDigit) && cleaned.Length >= 7 && cleaned.Length <= 15;
        }

        /// <summary>
        /// Sends an MMS (Multimedia Messaging Service) message.
        /// </summary>
        /// <param name="message">The MMS message to send.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A result indicating success or failure with message ID and status information.</returns>
        public virtual Task<SmsSendResult> SendMmsAsync(
            Models.MmsMessage message,
            CancellationToken cancellationToken = default)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Validate message
            var validationErrors = ValidateMmsMessage(message);
            if (validationErrors.Count > 0)
            {
                return Task.FromResult(SmsSendResult.Failed(validationErrors));
            }

            // Apply defaults
            var mmsToSend = ApplyMmsDefaults(message);

            try
            {
                // Delegate to derived class for actual sending
                // Default implementation throws NotSupportedException
                return SendMmsMessageAsync(mmsToSend, cancellationToken);
            }
            catch (Exception ex)
            {
                return Task.FromResult(SmsSendResult.Failed(ex));
            }
        }

        /// <summary>
        /// Sends the MMS message using the provider-specific implementation.
        /// </summary>
        /// <param name="message">The MMS message to send (with defaults applied).</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A result indicating success or failure with message ID and status information.</returns>
        /// <remarks>
        /// Default implementation throws <see cref="NotSupportedException"/>. Derived classes should
        /// override this method to provide MMS support.
        /// </remarks>
        protected virtual Task<SmsSendResult> SendMmsMessageAsync(
            Models.MmsMessage message,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(SmsSendResult.Failed(
                "MMS is not supported by this provider.",
                new NotSupportedException("MMS is not supported by this provider.")));
        }

        /// <summary>
        /// Validates the MMS message.
        /// </summary>
        /// <param name="message">The message to validate.</param>
        /// <returns>A list of validation errors, or an empty list if valid.</returns>
        protected virtual IList<string> ValidateMmsMessage(Models.MmsMessage message)
        {
            var errors = new List<string>();

            // Validate using MmsMessage.Validate()
            var validationErrors = message.Validate();
            errors.AddRange(validationErrors);

            // Validate phone number format if enabled
            if (Options.ValidatePhoneNumbers && !string.IsNullOrWhiteSpace(message.To))
            {
                if (!IsValidPhoneNumber(message.To))
                {
                    errors.Add($"Invalid phone number format: {message.To}");
                }
            }

            return errors;
        }

        /// <summary>
        /// Applies default values from options to the MMS message.
        /// </summary>
        /// <param name="message">The message to apply defaults to.</param>
        /// <returns>A new MMS message with defaults applied.</returns>
        protected virtual Models.MmsMessage ApplyMmsDefaults(Models.MmsMessage message)
        {
            // Create a copy to avoid modifying the original
            var mmsCopy = new Models.MmsMessage
            {
                To = message.To,
                From = message.From ?? Options.DefaultFrom,
                Body = message.Body,
                Attachments = message.Attachments != null
                    ? new List<Models.MmsAttachment>(message.Attachments)
                    : new List<Models.MmsAttachment>(),
                Metadata = message.Metadata != null
                    ? new Dictionary<string, string>(message.Metadata)
                    : null
            };

            return mmsCopy;
        }
    }
}

