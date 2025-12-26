//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
#nullable enable
using Mvp24Hours.Infrastructure.Sms.Models;
using Mvp24Hours.Infrastructure.Sms.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Testing.Fakes
{
    /// <summary>
    /// Fake SMS service implementation with configurable behavior for testing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation allows you to:
    /// - Simulate success or failure scenarios
    /// - Add delays to simulate real-world conditions
    /// - Access all sent messages for verification
    /// - Use custom result factories for complex scenarios
    /// </para>
    /// </remarks>
    public class FakeSmsService : IFakeSmsService
    {
        private readonly List<SmsMessage> _sentMessages = new();
        private readonly List<MmsMessage> _sentMmsMessages = new();
        private readonly object _lock = new();

        /// <inheritdoc />
        public IReadOnlyList<SmsMessage> SentMessages
        {
            get
            {
                lock (_lock)
                {
                    return _sentMessages.ToList().AsReadOnly();
                }
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<MmsMessage> SentMmsMessages
        {
            get
            {
                lock (_lock)
                {
                    return _sentMmsMessages.ToList().AsReadOnly();
                }
            }
        }

        /// <inheritdoc />
        public bool ShouldFail { get; set; }

        /// <inheritdoc />
        public string FailureMessage { get; set; } = "SMS sending failed (simulated).";

        /// <inheritdoc />
        public TimeSpan? SimulatedDelay { get; set; }

        /// <inheritdoc />
        public Func<SmsMessage, SmsSendResult>? CustomResultFactory { get; set; }

        /// <inheritdoc />
        public async Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null)
            {
                return SmsSendResult.Failed("SMS message cannot be null.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Simulate delay if configured
            if (SimulatedDelay.HasValue)
            {
                await Task.Delay(SimulatedDelay.Value, cancellationToken);
            }

            // Store the message
            lock (_lock)
            {
                _sentMessages.Add(message);
            }

            // Use custom factory if provided
            if (CustomResultFactory != null)
            {
                return CustomResultFactory(message);
            }

            // Return failure if configured
            if (ShouldFail)
            {
                return SmsSendResult.Failed(FailureMessage);
            }

            // Return success
            return SmsSendResult.Successful(Guid.NewGuid().ToString("N"), SmsDeliveryStatus.Queued);
        }

        /// <inheritdoc />
        public async Task<SmsSendResult> SendMmsAsync(MmsMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null)
            {
                return SmsSendResult.Failed("MMS message cannot be null.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Simulate delay if configured
            if (SimulatedDelay.HasValue)
            {
                await Task.Delay(SimulatedDelay.Value, cancellationToken);
            }

            // Store the message
            lock (_lock)
            {
                _sentMmsMessages.Add(message);
            }

            // Return failure if configured
            if (ShouldFail)
            {
                return SmsSendResult.Failed(FailureMessage);
            }

            // Return success
            return SmsSendResult.Successful(Guid.NewGuid().ToString("N"), SmsDeliveryStatus.Queued);
        }

        /// <inheritdoc />
        public void ClearSentMessages()
        {
            lock (_lock)
            {
                _sentMessages.Clear();
                _sentMmsMessages.Clear();
            }
        }

        /// <inheritdoc />
        public SmsMessage? GetLastSentMessage()
        {
            lock (_lock)
            {
                return _sentMessages.LastOrDefault();
            }
        }

        /// <inheritdoc />
        public IEnumerable<SmsMessage> GetMessagesSentTo(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return Enumerable.Empty<SmsMessage>();
            }

            lock (_lock)
            {
                return _sentMessages
                    .Where(m => m.To?.Equals(phoneNumber, StringComparison.OrdinalIgnoreCase) ?? false)
                    .ToList();
            }
        }

        /// <inheritdoc />
        public bool WasMessageSentContaining(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            lock (_lock)
            {
                return _sentMessages.Any(m =>
                    m.Body?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false);
            }
        }
    }
}

