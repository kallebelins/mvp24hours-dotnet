//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Mvp24Hours.Infrastructure.Email.Options
{
    /// <summary>
    /// Configuration options for SMTP email provider.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These options configure the SMTP client connection settings, including server address,
    /// port, authentication credentials, and SSL/TLS settings.
    /// </para>
    /// </remarks>
    public class SmtpEmailOptions
    {
        /// <summary>
        /// Gets or sets the SMTP server host name or IP address.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the hostname or IP address of the SMTP server to connect to.
        /// Common examples: "smtp.gmail.com", "smtp.office365.com", "smtp.sendgrid.net"
        /// </para>
        /// <para>
        /// This property is required.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// options.Host = "smtp.gmail.com";
        /// </code>
        /// </example>
        public string Host { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the SMTP server port number.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Common SMTP ports:
        /// - 25 (non-encrypted, often blocked by ISPs)
        /// - 587 (STARTTLS, recommended for most providers)
        /// - 465 (SSL/TLS, legacy but still used)
        /// </para>
        /// <para>
        /// Default is 587 (STARTTLS).
        /// </para>
        /// </remarks>
        public int Port { get; set; } = 587;

        /// <summary>
        /// Gets or sets the username for SMTP authentication.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is typically the email address or username used to authenticate with the SMTP server.
        /// Some providers require the full email address, while others accept just the username.
        /// </para>
        /// <para>
        /// This property is required if authentication is enabled.
        /// </para>
        /// </remarks>
        public string? Username { get; set; }

        /// <summary>
        /// Gets or sets the password for SMTP authentication.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the password or app-specific password used to authenticate with the SMTP server.
        /// For security, consider storing this in configuration secrets or environment variables.
        /// </para>
        /// <para>
        /// This property is required if authentication is enabled.
        /// </para>
        /// </remarks>
        public string? Password { get; set; }

        /// <summary>
        /// Gets or sets whether to enable SSL/TLS encryption.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <c>true</c>, the SMTP connection will use SSL/TLS encryption. This is typically
        /// used with port 465.
        /// </para>
        /// <para>
        /// When <c>false</c>, the connection may use STARTTLS (if <see cref="EnableStartTls"/> is true)
        /// or no encryption (not recommended for production).
        /// </para>
        /// <para>
        /// Default is <c>false</c> (use STARTTLS instead).
        /// </para>
        /// </remarks>
        public bool EnableSsl { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to enable STARTTLS encryption.
        /// </summary>
        /// <remarks>
        /// <para>
        /// STARTTLS upgrades a plain connection to an encrypted one after the initial handshake.
        /// This is the recommended approach for most modern SMTP servers (typically used with port 587).
        /// </para>
        /// <para>
        /// Default is <c>true</c>.
        /// </para>
        /// </remarks>
        public bool EnableStartTls { get; set; } = true;

        /// <summary>
        /// Gets or sets the connection timeout in milliseconds.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the maximum time to wait when establishing a connection to the SMTP server.
        /// </para>
        /// <para>
        /// Default is 30,000 milliseconds (30 seconds).
        /// </para>
        /// </remarks>
        public int Timeout { get; set; } = 30000;

        /// <summary>
        /// Gets or sets a callback to validate the server certificate.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This callback is used to validate the SSL/TLS certificate presented by the SMTP server.
        /// If not specified, the default certificate validation is used.
        /// </para>
        /// <para>
        /// <strong>Security Warning:</strong>
        /// Returning <c>true</c> from this callback bypasses certificate validation. Only use this
        /// for development/testing or when you understand the security implications.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Accept all certificates (NOT RECOMMENDED FOR PRODUCTION)
        /// options.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
        /// 
        /// // Custom validation logic
        /// options.ServerCertificateValidationCallback = (sender, certificate, chain, errors) =>
        /// {
        ///     // Your custom validation logic here
        ///     return errors == SslPolicyErrors.None;
        /// };
        /// </code>
        /// </example>
        public RemoteCertificateValidationCallback? ServerCertificateValidationCallback { get; set; }

        /// <summary>
        /// Gets or sets whether to use default credentials (Windows authentication).
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <c>true</c>, the SMTP client will use the current Windows user's credentials
        /// for authentication. This is typically used in corporate environments with integrated
        /// Windows authentication.
        /// </para>
        /// <para>
        /// Default is <c>false</c>.
        /// </para>
        /// </remarks>
        public bool UseDefaultCredentials { get; set; } = false;

        /// <summary>
        /// Validates the SMTP configuration options.
        /// </summary>
        /// <returns>A list of validation errors, or an empty list if valid.</returns>
        public System.Collections.Generic.IList<string> Validate()
        {
            var errors = new System.Collections.Generic.List<string>();

            if (string.IsNullOrWhiteSpace(Host))
            {
                errors.Add("SMTP Host is required.");
            }

            if (Port <= 0 || Port > 65535)
            {
                errors.Add($"SMTP Port must be between 1 and 65535. Current value: {Port}");
            }

            if (!UseDefaultCredentials)
            {
                if (string.IsNullOrWhiteSpace(Username))
                {
                    errors.Add("SMTP Username is required when not using default credentials.");
                }

                if (string.IsNullOrWhiteSpace(Password))
                {
                    errors.Add("SMTP Password is required when not using default credentials.");
                }
            }

            if (Timeout <= 0)
            {
                errors.Add("SMTP Timeout must be greater than zero.");
            }

            return errors;
        }
    }
}

