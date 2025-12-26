//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Email.Models;
using Mvp24Hours.Infrastructure.Email.Options;
using Mvp24Hours.Infrastructure.Email.Results;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Email.Providers
{
    /// <summary>
    /// In-memory email provider implementation for testing and development.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provider stores sent emails in memory and does not actually send them.
    /// It is useful for:
    /// - Unit testing (capture emails sent during tests)
    /// - Development (avoid sending real emails during development)
    /// - Integration testing (verify email content)
    /// </para>
    /// <para>
    /// <strong>Email Storage:</strong>
    /// Sent emails are stored in memory and can be accessed via <see cref="SentEmails"/>.
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
    /// services.AddInMemoryEmailService(options =>
    /// {
    ///     options.DefaultFrom = "test@example.com";
    /// });
    /// 
    /// // In tests, access sent emails
    /// var emailService = serviceProvider.GetRequiredService&lt;IEmailService&gt;();
    /// var inMemoryProvider = emailService as InMemoryEmailProvider;
    /// var sentEmails = inMemoryProvider.SentEmails;
    /// </code>
    /// </example>
    public class InMemoryEmailProvider : BaseEmailProvider
    {
        private readonly List<EmailMessage> _sentEmails;
        private readonly object _lockObject = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryEmailProvider"/> class.
        /// </summary>
        /// <param name="options">The email options.</param>
        /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
        public InMemoryEmailProvider(IOptions<EmailOptions> options)
            : base(options)
        {
            _sentEmails = new List<EmailMessage>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryEmailProvider"/> class.
        /// </summary>
        /// <param name="options">The email options.</param>
        /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
        public InMemoryEmailProvider(EmailOptions options)
            : base(options)
        {
            _sentEmails = new List<EmailMessage>();
        }

        /// <summary>
        /// Gets the list of emails that have been sent (stored in memory).
        /// </summary>
        /// <remarks>
        /// This property provides access to all emails sent through this provider.
        /// The list is thread-safe and can be accessed concurrently.
        /// </remarks>
        public IReadOnlyList<EmailMessage> SentEmails
        {
            get
            {
                lock (_lockObject)
                {
                    return _sentEmails.ToList().AsReadOnly();
                }
            }
        }

        /// <summary>
        /// Clears all sent emails from memory.
        /// </summary>
        /// <remarks>
        /// This method is useful for resetting state between tests or test runs.
        /// </remarks>
        public void ClearSentEmails()
        {
            lock (_lockObject)
            {
                _sentEmails.Clear();
            }
        }

        /// <summary>
        /// Sends the email message (stores it in memory).
        /// </summary>
        /// <param name="message">The email message to send (with defaults applied).</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A result indicating success with a generated message ID.</returns>
        protected override Task<EmailSendResult> SendEmailAsync(
            EmailMessage message,
            CancellationToken cancellationToken)
        {
            // Store email in memory
            lock (_lockObject)
            {
                _sentEmails.Add(message);
            }

            // Generate a fake message ID
            var messageId = Guid.NewGuid().ToString("N");

            return Task.FromResult(EmailSendResult.Successful(messageId));
        }
    }
}

