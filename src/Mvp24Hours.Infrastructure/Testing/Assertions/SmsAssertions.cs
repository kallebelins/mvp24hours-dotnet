//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
#nullable enable
using Mvp24Hours.Infrastructure.Sms.Models;
using Mvp24Hours.Infrastructure.Testing.Fakes;
using System;
using System.Linq;

namespace Mvp24Hours.Infrastructure.Testing.Assertions
{
    /// <summary>
    /// Provides assertion helpers for SMS operations in tests.
    /// </summary>
    public static class SmsAssertions
    {
        /// <summary>
        /// Asserts that at least one SMS was sent.
        /// </summary>
        public static void AssertSmsSent(IFakeSmsService smsService)
        {
            if (smsService == null) throw new ArgumentNullException(nameof(smsService));

            if (!smsService.SentMessages.Any())
            {
                throw new AssertionException("Expected at least one SMS to be sent, but none were sent.");
            }
        }

        /// <summary>
        /// Asserts that exactly the specified number of SMS messages were sent.
        /// </summary>
        public static void AssertSmsCount(IFakeSmsService smsService, int expectedCount)
        {
            if (smsService == null) throw new ArgumentNullException(nameof(smsService));

            var actualCount = smsService.SentMessages.Count;
            if (actualCount != expectedCount)
            {
                throw new AssertionException(
                    $"Expected {expectedCount} SMS message(s) to be sent, but {actualCount} were sent.");
            }
        }

        /// <summary>
        /// Asserts that an SMS was sent to the specified phone number.
        /// </summary>
        public static void AssertSmsSentTo(IFakeSmsService smsService, string phoneNumber)
        {
            if (smsService == null) throw new ArgumentNullException(nameof(smsService));
            if (string.IsNullOrEmpty(phoneNumber)) throw new ArgumentNullException(nameof(phoneNumber));

            var messages = smsService.GetMessagesSentTo(phoneNumber);
            if (!messages.Any())
            {
                throw new AssertionException(
                    $"Expected an SMS to be sent to '{phoneNumber}', but no such message was found.");
            }
        }

        /// <summary>
        /// Asserts that an SMS was sent containing the specified text.
        /// </summary>
        public static void AssertSmsSentContaining(IFakeSmsService smsService, string text)
        {
            if (smsService == null) throw new ArgumentNullException(nameof(smsService));
            if (string.IsNullOrEmpty(text)) throw new ArgumentNullException(nameof(text));

            if (!smsService.WasMessageSentContaining(text))
            {
                throw new AssertionException(
                    $"Expected an SMS containing '{text}', but no such message was found.");
            }
        }

        /// <summary>
        /// Asserts that an SMS was sent matching the specified predicate.
        /// </summary>
        public static void AssertSmsSent(IFakeSmsService smsService, Func<SmsMessage, bool> predicate)
        {
            if (smsService == null) throw new ArgumentNullException(nameof(smsService));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            if (!smsService.SentMessages.Any(predicate))
            {
                throw new AssertionException("Expected an SMS matching the specified criteria, but none was found.");
            }
        }

        /// <summary>
        /// Asserts that no SMS messages were sent.
        /// </summary>
        public static void AssertNoSmsSent(IFakeSmsService smsService)
        {
            if (smsService == null) throw new ArgumentNullException(nameof(smsService));

            if (smsService.SentMessages.Any())
            {
                throw new AssertionException(
                    $"Expected no SMS messages to be sent, but {smsService.SentMessages.Count} were sent.");
            }
        }

        /// <summary>
        /// Gets the last sent SMS, throwing if none exist.
        /// </summary>
        public static SmsMessage GetLastSentSms(IFakeSmsService smsService)
        {
            if (smsService == null) throw new ArgumentNullException(nameof(smsService));

            var last = smsService.GetLastSentMessage();
            if (last == null)
            {
                throw new AssertionException("No SMS messages were sent.");
            }

            return last;
        }
    }
}

