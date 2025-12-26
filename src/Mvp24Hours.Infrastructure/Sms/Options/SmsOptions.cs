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
    /// Configuration options for SMS service operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These options control various aspects of SMS sending behavior, including default
    /// sender phone number or sender ID, country code, and provider-specific settings.
    /// </para>
    /// <para>
    /// <strong>Default Values:</strong>
    /// Default values specified here are used when not explicitly provided in individual
    /// SMS messages. For example, if an SMS message doesn't specify a From number, the
    /// <see cref="DefaultFrom"/> value is used.
    /// </para>
    /// </remarks>
    public class SmsOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SmsOptions"/> class.
        /// </summary>
        public SmsOptions()
        {
        }

        /// <summary>
        /// Gets or sets the default sender phone number or sender ID (From field).
        /// </summary>
        /// <remarks>
        /// <para>
        /// This number or sender ID is used as the sender when an SMS message doesn't explicitly
        /// specify a From number. It can be:
        /// - A phone number in E.164 format (e.g., +5511888888888)
        /// - An alphanumeric sender ID (e.g., "MyCompany", "BRAND")
        /// </para>
        /// <para>
        /// <strong>Phone Number Format:</strong>
        /// Phone numbers should be in E.164 format (e.g., +5511888888888) for international
        /// compatibility. Some providers also support national format, but E.164 is recommended.
        /// </para>
        /// <para>
        /// <strong>Alphanumeric Sender ID:</strong>
        /// Alphanumeric sender IDs are short text identifiers that appear as the sender instead
        /// of a phone number. They are subject to provider-specific restrictions:
        /// - Length limits (typically 3-11 characters)
        /// - Character restrictions (usually alphanumeric and spaces)
        /// - Registration/verification requirements
        /// - Country-specific regulations
        /// </para>
        /// <para>
        /// <strong>Provider Requirements:</strong>
        /// Different providers have different requirements for sender IDs:
        /// - Some require verification or registration before use
        /// - Some only allow verified phone numbers
        /// - Some have country-specific restrictions
        /// </para>
        /// <para>
        /// This is typically required and should be configured when setting up the SMS service.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Phone number
        /// options.DefaultFrom = "+5511888888888";
        /// 
        /// // Alphanumeric sender ID
        /// options.DefaultFrom = "MyCompany";
        /// </code>
        /// </example>
        public string? DefaultFrom { get; set; }

        /// <summary>
        /// Gets or sets the default country code.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This country code is used when phone numbers are provided in national format without
        /// a country code prefix. The country code should be in ISO 3166-1 alpha-2 format (e.g., "BR" for Brazil).
        /// </para>
        /// <para>
        /// <strong>Usage:</strong>
        /// When a phone number is provided without a country code (e.g., "11999999999" instead of "+5511999999999"),
        /// the provider will use this default country code to format the number correctly.
        /// </para>
        /// <para>
        /// <strong>ISO 3166-1 alpha-2 Format:</strong>
        /// The country code should be a two-letter code (e.g., "US", "BR", "GB", "FR").
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// options.DefaultCountryCode = "BR"; // Brazil
        /// options.DefaultCountryCode = "US"; // United States
        /// </code>
        /// </example>
        public string? DefaultCountryCode { get; set; }

        /// <summary>
        /// Gets or sets the maximum message length in characters.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This setting controls the maximum length of SMS message bodies. Messages longer than
        /// this limit may be rejected during validation or automatically split into multiple segments.
        /// </para>
        /// <para>
        /// <strong>Character Limits:</strong>
        /// - GSM 7-bit encoding: Typically 160 characters per segment
        /// - UCS-2/Unicode encoding: Typically 70 characters per segment
        /// </para>
        /// <para>
        /// <strong>Message Splitting:</strong>
        /// Most providers automatically split long messages into multiple segments. The recipient
        /// may be charged for multiple messages depending on their carrier plan.
        /// </para>
        /// <para>
        /// Set to <c>null</c> to disable message length validation (provider will handle splitting).
        /// </para>
        /// <para>
        /// Default is <c>null</c> (no limit enforced, provider handles splitting).
        /// </para>
        /// </remarks>
        public int? MaxMessageLength { get; set; } = null;

        /// <summary>
        /// Gets or sets whether to validate phone numbers before sending.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <c>true</c>, phone numbers are validated for basic format correctness before
        /// attempting to send. This helps catch common errors early.
        /// </para>
        /// <para>
        /// <strong>Validation Rules:</strong>
        /// Basic validation checks for:
        /// - Non-empty phone number
        /// - Valid characters (digits, plus sign, spaces, hyphens, parentheses)
        /// - Reasonable length (typically 7-15 digits)
        /// </para>
        /// <para>
        /// Note that full phone number validation (including country-specific rules) is complex
        /// and may not be performed. The SMS provider will perform final validation.
        /// </para>
        /// </remarks>
        public bool ValidatePhoneNumbers { get; set; } = true;

        /// <summary>
        /// Validates the configuration options.
        /// </summary>
        /// <returns>A list of validation errors, or an empty list if valid.</returns>
        /// <remarks>
        /// This method checks for logical inconsistencies in the configuration (e.g., negative
        /// message length limits, invalid country codes).
        /// </remarks>
        public IList<string> Validate()
        {
            var errors = new List<string>();

            if (MaxMessageLength.HasValue && MaxMessageLength.Value <= 0)
            {
                errors.Add("Maximum message length must be greater than zero.");
            }

            if (!string.IsNullOrWhiteSpace(DefaultCountryCode))
            {
                // Basic validation: ISO 3166-1 alpha-2 should be 2 uppercase letters
                if (DefaultCountryCode.Length != 2 || !IsAllLetters(DefaultCountryCode))
                {
                    errors.Add("Default country code must be a valid ISO 3166-1 alpha-2 code (2 letters).");
                }
            }

            return errors;
        }

        /// <summary>
        /// Creates default SMS options suitable for most scenarios.
        /// </summary>
        /// <returns>Default options instance.</returns>
        public static SmsOptions Default => new();

        private static bool IsAllLetters(string value)
        {
            foreach (char c in value)
            {
                if (!char.IsLetter(c))
                {
                    return false;
                }
            }
            return true;
        }
    }
}

