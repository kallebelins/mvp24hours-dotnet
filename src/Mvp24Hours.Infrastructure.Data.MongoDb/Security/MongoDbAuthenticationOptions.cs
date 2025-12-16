//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Driver;
using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Security
{
    /// <summary>
    /// MongoDB authentication mechanism types.
    /// </summary>
    public enum MongoDbAuthMechanism
    {
        /// <summary>
        /// Default authentication (auto-detect based on server version).
        /// </summary>
        Default,

        /// <summary>
        /// SCRAM-SHA-1 authentication (MongoDB 3.0+).
        /// </summary>
        ScramSha1,

        /// <summary>
        /// SCRAM-SHA-256 authentication (MongoDB 4.0+).
        /// Recommended for better security.
        /// </summary>
        ScramSha256,

        /// <summary>
        /// X.509 certificate-based authentication.
        /// Most secure option for production environments.
        /// </summary>
        X509,

        /// <summary>
        /// AWS IAM authentication for MongoDB Atlas.
        /// </summary>
        AwsIam,

        /// <summary>
        /// LDAP authentication (Enterprise feature).
        /// </summary>
        Ldap,

        /// <summary>
        /// Kerberos/GSSAPI authentication (Enterprise feature).
        /// </summary>
        Gssapi
    }

    /// <summary>
    /// Configuration options for MongoDB authentication.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Supports multiple authentication mechanisms including:
    /// <list type="bullet">
    ///   <item>SCRAM-SHA-1/SCRAM-SHA-256 (username/password)</item>
    ///   <item>X.509 certificate authentication</item>
    ///   <item>AWS IAM (for MongoDB Atlas)</item>
    ///   <item>LDAP/Kerberos (Enterprise)</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // SCRAM-SHA-256 authentication:
    /// services.AddMvp24HoursDbContext(options =>
    /// {
    ///     options.ConnectionString = "mongodb://localhost:27017";
    ///     options.DatabaseName = "mydb";
    ///     options.Authentication = new MongoDbAuthenticationOptions
    ///     {
    ///         Mechanism = MongoDbAuthMechanism.ScramSha256,
    ///         Username = "myuser",
    ///         Password = "mypassword",
    ///         AuthDatabase = "admin"
    ///     };
    /// });
    /// 
    /// // X.509 certificate authentication:
    /// services.AddMvp24HoursDbContext(options =>
    /// {
    ///     options.ConnectionString = "mongodb://localhost:27017";
    ///     options.DatabaseName = "mydb";
    ///     options.Authentication = new MongoDbAuthenticationOptions
    ///     {
    ///         Mechanism = MongoDbAuthMechanism.X509,
    ///         CertificatePath = "/path/to/client.pem",
    ///         CertificatePassword = "cert-password"
    ///     };
    /// });
    /// </code>
    /// </example>
    public class MongoDbAuthenticationOptions
    {
        /// <summary>
        /// Gets or sets the authentication mechanism to use.
        /// </summary>
        public MongoDbAuthMechanism Mechanism { get; set; } = MongoDbAuthMechanism.Default;

        /// <summary>
        /// Gets or sets the username for SCRAM authentication.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the password for SCRAM authentication.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the authentication database (default: "admin").
        /// </summary>
        public string AuthDatabase { get; set; } = "admin";

        /// <summary>
        /// Gets or sets the path to the X.509 client certificate.
        /// </summary>
        public string CertificatePath { get; set; }

        /// <summary>
        /// Gets or sets the password for the X.509 certificate (if encrypted).
        /// </summary>
        public string CertificatePassword { get; set; }

        /// <summary>
        /// Gets or sets the X.509 certificate directly (alternative to CertificatePath).
        /// </summary>
        public X509Certificate2 Certificate { get; set; }

        /// <summary>
        /// Gets or sets the CA certificate path for server verification.
        /// </summary>
        public string CaCertificatePath { get; set; }

        /// <summary>
        /// Gets or sets whether to validate server certificates.
        /// Default is true. Set to false only for development.
        /// </summary>
        public bool ValidateServerCertificate { get; set; } = true;

        /// <summary>
        /// Gets or sets the AWS access key for AWS IAM authentication.
        /// </summary>
        public string AwsAccessKeyId { get; set; }

        /// <summary>
        /// Gets or sets the AWS secret key for AWS IAM authentication.
        /// </summary>
        public string AwsSecretAccessKey { get; set; }

        /// <summary>
        /// Gets or sets the AWS session token for temporary credentials.
        /// </summary>
        public string AwsSessionToken { get; set; }

        /// <summary>
        /// Gets or sets the LDAP bind DN for LDAP authentication.
        /// </summary>
        public string LdapBindDn { get; set; }

        /// <summary>
        /// Gets or sets the Kerberos service name for GSSAPI.
        /// </summary>
        public string KerberosServiceName { get; set; }

        /// <summary>
        /// Gets or sets the allowed TLS protocols. Default is TLS 1.2+.
        /// </summary>
        public SslProtocols AllowedTlsProtocols { get; set; } = SslProtocols.Tls12 | SslProtocols.Tls13;

        /// <summary>
        /// Applies the authentication settings to a MongoClientSettings instance.
        /// </summary>
        /// <param name="settings">The settings to configure.</param>
        public void ApplyTo(MongoClientSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            // Configure authentication credential
            settings.Credential = CreateCredential();

            // Configure TLS/SSL
            ConfigureTls(settings);
        }

        /// <summary>
        /// Creates a MongoCredential based on the configured mechanism.
        /// </summary>
        /// <returns>The configured credential, or null if no authentication is configured.</returns>
        public MongoCredential CreateCredential()
        {
            return Mechanism switch
            {
                MongoDbAuthMechanism.Default => CreateDefaultCredential(),
                MongoDbAuthMechanism.ScramSha1 => CreateScramSha1Credential(),
                MongoDbAuthMechanism.ScramSha256 => CreateScramSha256Credential(),
                MongoDbAuthMechanism.X509 => CreateX509Credential(),
                MongoDbAuthMechanism.AwsIam => CreateAwsCredential(),
                MongoDbAuthMechanism.Ldap => CreateLdapCredential(),
                MongoDbAuthMechanism.Gssapi => CreateGssapiCredential(),
                _ => null
            };
        }

        private MongoCredential CreateDefaultCredential()
        {
            if (string.IsNullOrEmpty(Username))
            {
                return null;
            }

            return MongoCredential.CreateCredential(AuthDatabase, Username, Password);
        }

        private MongoCredential CreateScramSha1Credential()
        {
            if (string.IsNullOrEmpty(Username))
            {
                throw new InvalidOperationException("Username is required for SCRAM-SHA-1 authentication.");
            }

            return MongoCredential.CreateCredential(AuthDatabase, Username, Password);
        }

        private MongoCredential CreateScramSha256Credential()
        {
            if (string.IsNullOrEmpty(Username))
            {
                throw new InvalidOperationException("Username is required for SCRAM-SHA-256 authentication.");
            }

            // SCRAM-SHA-256 requires specifying the mechanism explicitly
            var identity = new MongoInternalIdentity(AuthDatabase, Username);
            var evidence = new PasswordEvidence(Password);
            return new MongoCredential("SCRAM-SHA-256", identity, evidence);
        }

        private MongoCredential CreateX509Credential()
        {
            // For X.509, the username is derived from the certificate subject
            var username = GetX509Username();
            return MongoCredential.CreateMongoX509Credential(username);
        }

        private MongoCredential CreateAwsCredential()
        {
            // AWS IAM authentication requires MongoDB Atlas or special setup
            // The mechanism is "MONGODB-AWS" but requires proper configuration
            if (string.IsNullOrEmpty(AwsAccessKeyId))
            {
                // When no credentials provided, rely on environment variables or instance metadata
                // AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY, AWS_SESSION_TOKEN
                var identity = new MongoExternalIdentity("$external", string.Empty);
                var evidence = new ExternalEvidence();
                return new MongoCredential("MONGODB-AWS", identity, evidence);
            }

            // Use explicit AWS credentials
            var awsIdentity = new MongoExternalIdentity("$external", AwsAccessKeyId);
            var awsEvidence = new PasswordEvidence(AwsSecretAccessKey);
            var credential = new MongoCredential("MONGODB-AWS", awsIdentity, awsEvidence);

            if (!string.IsNullOrEmpty(AwsSessionToken))
            {
                credential = credential.WithMechanismProperty("AWS_SESSION_TOKEN", AwsSessionToken);
            }

            return credential;
        }

        private MongoCredential CreateLdapCredential()
        {
            if (string.IsNullOrEmpty(Username))
            {
                throw new InvalidOperationException("Username is required for LDAP authentication.");
            }

            return MongoCredential.CreatePlainCredential("$external", Username, Password);
        }

        private MongoCredential CreateGssapiCredential()
        {
            var username = Username ?? Environment.UserName;
            var credential = MongoCredential.CreateGssapiCredential(username, Password);

            if (!string.IsNullOrEmpty(KerberosServiceName))
            {
                credential = credential.WithMechanismProperty("SERVICE_NAME", KerberosServiceName);
            }

            return credential;
        }

        private void ConfigureTls(MongoClientSettings settings)
        {
            // Enable TLS for X.509 or if certificate is provided
            if (Mechanism == MongoDbAuthMechanism.X509 || !string.IsNullOrEmpty(CertificatePath) || Certificate != null)
            {
                settings.UseTls = true;

                settings.SslSettings = new SslSettings
                {
                    EnabledSslProtocols = AllowedTlsProtocols,
                    CheckCertificateRevocation = ValidateServerCertificate
                };

                // Add client certificate
                var clientCert = GetClientCertificate();
                if (clientCert != null)
                {
                    settings.SslSettings.ClientCertificates = new[] { clientCert };
                }

                // Configure server certificate validation
                if (!ValidateServerCertificate)
                {
                    settings.SslSettings.ServerCertificateValidationCallback = 
                        (sender, certificate, chain, errors) => true;
                }
                else if (!string.IsNullOrEmpty(CaCertificatePath))
                {
                    var caCert = new X509Certificate2(CaCertificatePath);
                    settings.SslSettings.ServerCertificateValidationCallback = 
                        CreateCaValidationCallback(caCert);
                }
            }
        }

        private X509Certificate2 GetClientCertificate()
        {
            if (Certificate != null)
            {
                return Certificate;
            }

            if (!string.IsNullOrEmpty(CertificatePath))
            {
                return string.IsNullOrEmpty(CertificatePassword)
                    ? new X509Certificate2(CertificatePath)
                    : new X509Certificate2(CertificatePath, CertificatePassword);
            }

            return null;
        }

        private string GetX509Username()
        {
            var cert = GetClientCertificate();
            if (cert != null)
            {
                // MongoDB uses the certificate subject as the username for X.509
                return cert.Subject;
            }

            return null;
        }

        private static RemoteCertificateValidationCallback CreateCaValidationCallback(X509Certificate2 caCert)
        {
            return (sender, certificate, chain, errors) =>
            {
                if (errors == SslPolicyErrors.None)
                {
                    return true;
                }

                // If the only error is an untrusted root, check against our CA
                if ((errors & SslPolicyErrors.RemoteCertificateChainErrors) != 0)
                {
                    chain.ChainPolicy.ExtraStore.Add(caCert);
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                    
                    var isValid = chain.Build(new X509Certificate2(certificate));
                    if (isValid)
                    {
                        // Verify the root is our CA
                        var root = chain.ChainElements[chain.ChainElements.Count - 1].Certificate;
                        return root.Thumbprint == caCert.Thumbprint;
                    }
                }

                return false;
            };
        }
    }

    /// <summary>
    /// Extension methods for applying authentication options to MongoClientSettings.
    /// </summary>
    public static class MongoDbAuthenticationExtensions
    {
        /// <summary>
        /// Configures the MongoClientSettings with authentication options.
        /// </summary>
        /// <param name="settings">The settings to configure.</param>
        /// <param name="authOptions">The authentication options.</param>
        /// <returns>The configured settings for chaining.</returns>
        public static MongoClientSettings WithAuthentication(
            this MongoClientSettings settings,
            MongoDbAuthenticationOptions authOptions)
        {
            authOptions?.ApplyTo(settings);
            return settings;
        }

        /// <summary>
        /// Configures the MongoClientSettings with SCRAM-SHA-256 authentication.
        /// </summary>
        /// <param name="settings">The settings to configure.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="authDatabase">The authentication database (default: "admin").</param>
        /// <returns>The configured settings for chaining.</returns>
        public static MongoClientSettings WithScramSha256(
            this MongoClientSettings settings,
            string username,
            string password,
            string authDatabase = "admin")
        {
            var options = new MongoDbAuthenticationOptions
            {
                Mechanism = MongoDbAuthMechanism.ScramSha256,
                Username = username,
                Password = password,
                AuthDatabase = authDatabase
            };

            options.ApplyTo(settings);
            return settings;
        }

        /// <summary>
        /// Configures the MongoClientSettings with X.509 certificate authentication.
        /// </summary>
        /// <param name="settings">The settings to configure.</param>
        /// <param name="certificatePath">Path to the client certificate.</param>
        /// <param name="certificatePassword">Certificate password (if encrypted).</param>
        /// <returns>The configured settings for chaining.</returns>
        public static MongoClientSettings WithX509Certificate(
            this MongoClientSettings settings,
            string certificatePath,
            string certificatePassword = null)
        {
            var options = new MongoDbAuthenticationOptions
            {
                Mechanism = MongoDbAuthMechanism.X509,
                CertificatePath = certificatePath,
                CertificatePassword = certificatePassword
            };

            options.ApplyTo(settings);
            return settings;
        }

        /// <summary>
        /// Configures the MongoClientSettings with X.509 certificate authentication.
        /// </summary>
        /// <param name="settings">The settings to configure.</param>
        /// <param name="certificate">The client certificate.</param>
        /// <returns>The configured settings for chaining.</returns>
        public static MongoClientSettings WithX509Certificate(
            this MongoClientSettings settings,
            X509Certificate2 certificate)
        {
            var options = new MongoDbAuthenticationOptions
            {
                Mechanism = MongoDbAuthMechanism.X509,
                Certificate = certificate
            };

            options.ApplyTo(settings);
            return settings;
        }
    }
}

