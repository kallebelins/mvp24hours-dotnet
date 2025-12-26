//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
#nullable enable
using Mvp24Hours.Infrastructure.Sms.Contract;
using Mvp24Hours.Infrastructure.Sms.Models;
using Mvp24Hours.Infrastructure.Sms.Results;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Testing.Fakes
{
    /// <summary>
    /// Fake SMS service interface with configurable behavior for testing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface extends <see cref="ISmsService"/> to provide additional
    /// capabilities for testing, such as:
    /// - Configuring success/failure behavior
    /// - Accessing sent messages
    /// - Simulating various scenarios
    /// </para>
    /// </remarks>
    public interface IFakeSmsService : ISmsService
    {
        /// <summary>
        /// Gets the list of all SMS messages that have been sent.
        /// </summary>
        IReadOnlyList<SmsMessage> SentMessages { get; }

        /// <summary>
        /// Gets the list of all MMS messages that have been sent.
        /// </summary>
        IReadOnlyList<MmsMessage> SentMmsMessages { get; }

        /// <summary>
        /// Gets or sets whether the service should simulate failures.
        /// </summary>
        bool ShouldFail { get; set; }

        /// <summary>
        /// Gets or sets the failure message to return when ShouldFail is true.
        /// </summary>
        string FailureMessage { get; set; }

        /// <summary>
        /// Gets or sets the delay to simulate before sending messages.
        /// </summary>
        TimeSpan? SimulatedDelay { get; set; }

        /// <summary>
        /// Gets or sets a custom result factory for advanced scenarios.
        /// </summary>
        Func<SmsMessage, SmsSendResult>? CustomResultFactory { get; set; }

        /// <summary>
        /// Clears all sent messages.
        /// </summary>
        void ClearSentMessages();

        /// <summary>
        /// Gets the last message sent.
        /// </summary>
        SmsMessage? GetLastSentMessage();

        /// <summary>
        /// Gets messages sent to a specific phone number.
        /// </summary>
        /// <param name="phoneNumber">The recipient phone number.</param>
        IEnumerable<SmsMessage> GetMessagesSentTo(string phoneNumber);

        /// <summary>
        /// Verifies that a message was sent containing the specified text.
        /// </summary>
        /// <param name="text">The text to search for.</param>
        bool WasMessageSentContaining(string text);
    }
}

