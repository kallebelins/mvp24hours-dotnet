//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
#nullable enable
using Mvp24Hours.Infrastructure.Email.Models;
using Mvp24Hours.Infrastructure.Testing.Fakes;
using System;
using System.Linq;

namespace Mvp24Hours.Infrastructure.Testing.Assertions
{
    /// <summary>
    /// Provides assertion helpers for email operations in tests.
    /// </summary>
    public static class EmailAssertions
    {
        /// <summary>
        /// Asserts that at least one email was sent.
        /// </summary>
        public static void AssertEmailSent(IFakeEmailService emailService)
        {
            if (emailService == null) throw new ArgumentNullException(nameof(emailService));

            if (!emailService.SentEmails.Any())
            {
                throw new AssertionException("Expected at least one email to be sent, but none were sent.");
            }
        }

        /// <summary>
        /// Asserts that exactly the specified number of emails were sent.
        /// </summary>
        public static void AssertEmailCount(IFakeEmailService emailService, int expectedCount)
        {
            if (emailService == null) throw new ArgumentNullException(nameof(emailService));

            var actualCount = emailService.SentEmails.Count;
            if (actualCount != expectedCount)
            {
                throw new AssertionException(
                    $"Expected {expectedCount} email(s) to be sent, but {actualCount} were sent.");
            }
        }

        /// <summary>
        /// Asserts that an email was sent to the specified address.
        /// </summary>
        public static void AssertEmailSentTo(IFakeEmailService emailService, string toAddress)
        {
            if (emailService == null) throw new ArgumentNullException(nameof(emailService));
            if (string.IsNullOrEmpty(toAddress)) throw new ArgumentNullException(nameof(toAddress));

            var emails = emailService.GetEmailsSentTo(toAddress);
            if (!emails.Any())
            {
                throw new AssertionException(
                    $"Expected an email to be sent to '{toAddress}', but no such email was found.");
            }
        }

        /// <summary>
        /// Asserts that an email was sent with the specified subject.
        /// </summary>
        public static void AssertEmailSentWithSubject(IFakeEmailService emailService, string subject)
        {
            if (emailService == null) throw new ArgumentNullException(nameof(emailService));
            if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));

            if (!emailService.WasEmailSentWithSubject(subject))
            {
                throw new AssertionException(
                    $"Expected an email with subject containing '{subject}', but no such email was found.");
            }
        }

        /// <summary>
        /// Asserts that an email was sent matching the specified predicate.
        /// </summary>
        public static void AssertEmailSent(IFakeEmailService emailService, Func<EmailMessage, bool> predicate)
        {
            if (emailService == null) throw new ArgumentNullException(nameof(emailService));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            if (!emailService.SentEmails.Any(predicate))
            {
                throw new AssertionException("Expected an email matching the specified criteria, but none was found.");
            }
        }

        /// <summary>
        /// Asserts that no emails were sent.
        /// </summary>
        public static void AssertNoEmailsSent(IFakeEmailService emailService)
        {
            if (emailService == null) throw new ArgumentNullException(nameof(emailService));

            if (emailService.SentEmails.Any())
            {
                throw new AssertionException(
                    $"Expected no emails to be sent, but {emailService.SentEmails.Count} were sent.");
            }
        }

        /// <summary>
        /// Gets the last sent email, throwing if none exist.
        /// </summary>
        public static EmailMessage GetLastSentEmail(IFakeEmailService emailService)
        {
            if (emailService == null) throw new ArgumentNullException(nameof(emailService));

            var last = emailService.GetLastSentEmail();
            if (last == null)
            {
                throw new AssertionException("No emails were sent.");
            }

            return last;
        }
    }
}

