//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
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
    /// In-memory SMS provider implementation for testing and development.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provider stores sent SMS messages in memory and does not actually send them.
    /// It is useful for:
    /// - Unit testing (capture SMS messages sent during tests)
    /// - Development (avoid sending real SMS during development)
    /// - Integration testing (verify SMS content)
    /// </para>
    /// <para>
    /// <strong>SMS Storage:</strong>
    /// Sent SMS messages are stored in memory and can be accessed via <see cref="SentMessages"/>.
    /// The storage is cleared when the application restarts.
    /// </para>
    /// <para>
    /// <strong>Thread Safety:</strong>
    /// This implementation is thread-safe and can be used concurrently.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register in-memory provider for testing
    /// services.AddInMemorySmsService(options =>
    /// {
    ///     options.DefaultFrom = "+5511888888888";
    /// });
    /// 
    /// // In tests, access sent messages
    /// var smsService = serviceProvider.GetRequiredService&lt;ISmsService&gt;();
    /// var inMemoryProvider = smsService as InMemorySmsProvider;
    /// var sentMessages = inMemoryProvider.SentMessages;
    /// </code>
    /// </example>
    public class InMemorySmsProvider : BaseSmsProvider
    {
        private readonly List<SmsMessage> _sentMessages;
        private readonly List<MmsMessage> _sentMmsMessages;
        private readonly object _lockObject = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemorySmsProvider"/> class.
        /// </summary>
        /// <param name="options">The SMS options.</param>
        /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
        public InMemorySmsProvider(IOptions<SmsOptions> options)
            : base(options)
        {
            _sentMessages = new List<SmsMessage>();
            _sentMmsMessages = new List<MmsMessage>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemorySmsProvider"/> class.
        /// </summary>
        /// <param name="options">The SMS options.</param>
        /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
        public InMemorySmsProvider(SmsOptions options)
            : base(options)
        {
            _sentMessages = new List<SmsMessage>();
            _sentMmsMessages = new List<MmsMessage>();
        }

        /// <summary>
        /// Gets the list of SMS messages that have been sent (stored in memory).
        /// </summary>
        /// <remarks>
        /// This property provides access to all SMS messages sent through this provider.
        /// The list is thread-safe and can be accessed concurrently.
        /// </remarks>
        public IReadOnlyList<SmsMessage> SentMessages
        {
            get
            {
                lock (_lockObject)
                {
                    return _sentMessages.ToList().AsReadOnly();
                }
            }
        }

        /// <summary>
        /// Gets the list of MMS messages that have been sent (stored in memory).
        /// </summary>
        /// <remarks>
        /// This property provides access to all MMS messages sent through this provider.
        /// The list is thread-safe and can be accessed concurrently.
        /// </remarks>
        public IReadOnlyList<MmsMessage> SentMmsMessages
        {
            get
            {
                lock (_lockObject)
                {
                    return _sentMmsMessages.ToList().AsReadOnly();
                }
            }
        }

        /// <summary>
        /// Clears all sent SMS messages from memory.
        /// </summary>
        /// <remarks>
        /// This method is useful for resetting state between tests or test runs.
        /// </remarks>
        public void ClearSentMessages()
        {
            lock (_lockObject)
            {
                _sentMessages.Clear();
                _sentMmsMessages.Clear();
            }
        }

        /// <summary>
        /// Sends the SMS message (stores it in memory).
        /// </summary>
        /// <param name="message">The SMS message to send (with defaults applied).</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A result indicating success with a generated message ID.</returns>
        protected override Task<SmsSendResult> SendSmsAsync(
            SmsMessage message,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Store SMS in memory
            lock (_lockObject)
            {
                _sentMessages.Add(message);
            }

            // Generate a fake message ID
            var messageId = Guid.NewGuid().ToString("N");

            return Task.FromResult(SmsSendResult.Successful(messageId, SmsDeliveryStatus.Queued));
        }

        /// <summary>
        /// Sends the MMS message (stores it in memory).
        /// </summary>
        /// <param name="message">The MMS message to send (with defaults applied).</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A result indicating success with a generated message ID.</returns>
        protected override Task<SmsSendResult> SendMmsMessageAsync(
            MmsMessage message,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Store MMS in memory
            lock (_lockObject)
            {
                _sentMmsMessages.Add(message);
            }

            // Generate a fake message ID
            var messageId = Guid.NewGuid().ToString("N");

            return Task.FromResult(SmsSendResult.Successful(messageId, SmsDeliveryStatus.Queued));
        }
    }
}

