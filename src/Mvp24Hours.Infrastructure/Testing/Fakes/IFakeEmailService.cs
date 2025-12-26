//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
#nullable enable
using Mvp24Hours.Infrastructure.Email.Contract;
using Mvp24Hours.Infrastructure.Email.Models;
using Mvp24Hours.Infrastructure.Email.Results;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Testing.Fakes
{
    /// <summary>
    /// Fake email service interface with configurable behavior for testing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface extends <see cref="IEmailService"/> to provide additional
    /// capabilities for testing, such as:
    /// - Configuring success/failure behavior
    /// - Accessing sent emails
    /// - Simulating various scenarios
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var fakeEmail = new FakeEmailService();
    /// fakeEmail.ShouldFail = true;
    /// fakeEmail.FailureMessage = "SMTP server unavailable";
    /// 
    /// var result = await fakeEmail.SendAsync(message);
    /// Assert.False(result.Success);
    /// </code>
    /// </example>
    public interface IFakeEmailService : IEmailService
    {
        /// <summary>
        /// Gets the list of all emails that have been sent.
        /// </summary>
        IReadOnlyList<EmailMessage> SentEmails { get; }

        /// <summary>
        /// Gets or sets whether the service should simulate failures.
        /// </summary>
        bool ShouldFail { get; set; }

        /// <summary>
        /// Gets or sets the failure message to return when ShouldFail is true.
        /// </summary>
        string FailureMessage { get; set; }

        /// <summary>
        /// Gets or sets the delay to simulate before sending emails.
        /// </summary>
        TimeSpan? SimulatedDelay { get; set; }

        /// <summary>
        /// Gets or sets a custom result factory for advanced scenarios.
        /// </summary>
        Func<EmailMessage, EmailSendResult>? CustomResultFactory { get; set; }

        /// <summary>
        /// Clears all sent emails.
        /// </summary>
        void ClearSentEmails();

        /// <summary>
        /// Gets the last email sent.
        /// </summary>
        EmailMessage? GetLastSentEmail();

        /// <summary>
        /// Gets emails sent to a specific address.
        /// </summary>
        /// <param name="toAddress">The recipient email address.</param>
        IEnumerable<EmailMessage> GetEmailsSentTo(string toAddress);

        /// <summary>
        /// Verifies that an email was sent with the specified subject.
        /// </summary>
        /// <param name="subject">The subject to search for.</param>
        bool WasEmailSentWithSubject(string subject);
    }
}

