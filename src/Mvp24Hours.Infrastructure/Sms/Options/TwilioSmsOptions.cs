//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Sms.Options
{
    /// <summary>
    /// Configuration options for Twilio SMS provider.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These options configure the Twilio SMS client, including account SID, auth token,
    /// and Twilio-specific settings.
    /// </para>
    /// <para>
    /// <strong>Dependencies:</strong>
    /// This provider requires the Twilio NuGet package. Install it via:
    /// <c>dotnet add package Twilio</c>
    /// </para>
    /// </remarks>
    public class TwilioSmsOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TwilioSmsOptions"/> class.
        /// </summary>
        public TwilioSmsOptions()
        {
        }

        /// <summary>
        /// Gets or sets the Twilio Account SID.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is your Twilio Account SID, which can be found in your Twilio Console dashboard.
        /// It starts with "AC" followed by 32 characters.
        /// </para>
        /// <para>
        /// <strong>Security:</strong>
        /// Store this value securely (e.g., in Azure Key Vault, AWS Secrets Manager, or
        /// environment variables). Never commit credentials to source control.
        /// </para>
        /// <para>
        /// This property is required.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// options.AccountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
        /// </code>
        /// </example>
        public string AccountSid { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Twilio Auth Token.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is your Twilio Auth Token, which can be found in your Twilio Console dashboard.
        /// It's used to authenticate API requests.
        /// </para>
        /// <para>
        /// <strong>Security:</strong>
        /// Store this value securely (e.g., in Azure Key Vault, AWS Secrets Manager, or
        /// environment variables). Never commit credentials to source control.
        /// </para>
        /// <para>
        /// This property is required.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// options.AuthToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");
        /// </code>
        /// </example>
        public string AuthToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Twilio API base URL.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The base URL for Twilio API. Defaults to "https://api.twilio.com" if not specified.
        /// You typically don't need to change this unless using a Twilio proxy or custom endpoint.
        /// </para>
        /// </remarks>
        public string? ApiBaseUrl { get; set; }

        /// <summary>
        /// Gets or sets whether to validate phone numbers using Twilio Lookup API before sending.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When enabled, the provider will validate phone numbers using Twilio's Lookup API
        /// before attempting to send SMS. This helps catch invalid numbers early but adds
        /// an extra API call and cost.
        /// </para>
        /// <para>
        /// Default is <c>false</c>.
        /// </para>
        /// </remarks>
        public bool ValidatePhoneNumbers { get; set; } = false;

        /// <summary>
        /// Validates the configuration options.
        /// </summary>
        /// <returns>A list of validation errors, or an empty list if valid.</returns>
        public IList<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(AccountSid))
            {
                errors.Add("Twilio Account SID is required.");
            }
            else if (!AccountSid.StartsWith("AC", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Twilio Account SID must start with 'AC'.");
            }

            if (string.IsNullOrWhiteSpace(AuthToken))
            {
                errors.Add("Twilio Auth Token is required.");
            }

            return errors;
        }
    }
}

