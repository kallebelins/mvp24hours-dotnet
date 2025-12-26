//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
#nullable enable
using Mvp24Hours.Infrastructure.Email.Models;
using Mvp24Hours.Infrastructure.Email.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Testing.Fakes
{
    /// <summary>
    /// Fake email service implementation with configurable behavior for testing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation allows you to:
    /// - Simulate success or failure scenarios
    /// - Add delays to simulate real-world conditions
    /// - Access all sent emails for verification
    /// - Use custom result factories for complex scenarios
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple usage
    /// var fakeEmail = new FakeEmailService();
    /// await fakeEmail.SendAsync(message);
    /// Assert.Single(fakeEmail.SentEmails);
    /// 
    /// // Simulate failure
    /// fakeEmail.ShouldFail = true;
    /// fakeEmail.FailureMessage = "SMTP server unavailable";
    /// var result = await fakeEmail.SendAsync(message);
    /// Assert.False(result.Success);
    /// 
    /// // Custom behavior
    /// fakeEmail.CustomResultFactory = msg => 
    ///     msg.To.Contains("blocked@") 
    ///         ? EmailSendResult.Failed("Address blocked")
    ///         : EmailSendResult.Successful(Guid.NewGuid().ToString());
    /// </code>
    /// </example>
    public class FakeEmailService : IFakeEmailService
    {
        private readonly List<EmailMessage> _sentEmails = new();
        private readonly object _lock = new();

        /// <inheritdoc />
        public IReadOnlyList<EmailMessage> SentEmails
        {
            get
            {
                lock (_lock)
                {
                    return _sentEmails.ToList().AsReadOnly();
                }
            }
        }

        /// <inheritdoc />
        public bool ShouldFail { get; set; }

        /// <inheritdoc />
        public string FailureMessage { get; set; } = "Email sending failed (simulated).";

        /// <inheritdoc />
        public TimeSpan? SimulatedDelay { get; set; }

        /// <inheritdoc />
        public Func<EmailMessage, EmailSendResult>? CustomResultFactory { get; set; }

        /// <inheritdoc />
        public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null)
            {
                return EmailSendResult.Failed("Email message cannot be null.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Simulate delay if configured
            if (SimulatedDelay.HasValue)
            {
                await Task.Delay(SimulatedDelay.Value, cancellationToken);
            }

            // Store the email
            lock (_lock)
            {
                _sentEmails.Add(message);
            }

            // Use custom factory if provided
            if (CustomResultFactory != null)
            {
                return CustomResultFactory(message);
            }

            // Return failure if configured
            if (ShouldFail)
            {
                return EmailSendResult.Failed(FailureMessage);
            }

            // Return success
            return EmailSendResult.Successful(Guid.NewGuid().ToString("N"));
        }

        /// <inheritdoc />
        public void ClearSentEmails()
        {
            lock (_lock)
            {
                _sentEmails.Clear();
            }
        }

        /// <inheritdoc />
        public EmailMessage? GetLastSentEmail()
        {
            lock (_lock)
            {
                return _sentEmails.LastOrDefault();
            }
        }

        /// <inheritdoc />
        public IEnumerable<EmailMessage> GetEmailsSentTo(string toAddress)
        {
            if (string.IsNullOrWhiteSpace(toAddress))
            {
                return Enumerable.Empty<EmailMessage>();
            }

            lock (_lock)
            {
                return _sentEmails
                    .Where(e => e.To.Any(t => t.Equals(toAddress, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }
        }

        /// <inheritdoc />
        public bool WasEmailSentWithSubject(string subject)
        {
            if (string.IsNullOrWhiteSpace(subject))
            {
                return false;
            }

            lock (_lock)
            {
                return _sentEmails.Any(e => 
                    e.Subject?.Contains(subject, StringComparison.OrdinalIgnoreCase) ?? false);
            }
        }
    }
}

