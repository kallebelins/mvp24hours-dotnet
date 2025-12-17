//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
namespace Mvp24Hours.Infrastructure.RabbitMQ.Configuration.Fluent
{
    /// <summary>
    /// Builder for configuring RabbitMQ host connection settings.
    /// </summary>
    public class HostConfigurationBuilder
    {
        private readonly RabbitMQConnectionOptions _options;

        /// <summary>
        /// Creates a new instance of the host configuration builder.
        /// </summary>
        /// <param name="options">The connection options to configure.</param>
        public HostConfigurationBuilder(RabbitMQConnectionOptions options)
        {
            _options = options;
            _options.Configuration ??= new RabbitMQConnection();
        }

        /// <summary>
        /// Sets the username for authentication.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns>The builder for chaining.</returns>
        public HostConfigurationBuilder Username(string username)
        {
            _options.Configuration!.UserName = username;
            return this;
        }

        /// <summary>
        /// Sets the password for authentication.
        /// </summary>
        /// <param name="password">The password.</param>
        /// <returns>The builder for chaining.</returns>
        public HostConfigurationBuilder Password(string password)
        {
            _options.Configuration!.Password = password;
            return this;
        }

        /// <summary>
        /// Sets the virtual host.
        /// </summary>
        /// <param name="virtualHost">The virtual host name.</param>
        /// <returns>The builder for chaining.</returns>
        public HostConfigurationBuilder VirtualHost(string virtualHost)
        {
            _options.Configuration!.VirtualHost = virtualHost;
            return this;
        }

        /// <summary>
        /// Sets the host name.
        /// </summary>
        /// <param name="hostName">The host name or IP address.</param>
        /// <returns>The builder for chaining.</returns>
        public HostConfigurationBuilder HostName(string hostName)
        {
            _options.Configuration!.HostName = hostName;
            return this;
        }

        /// <summary>
        /// Sets the port number.
        /// </summary>
        /// <param name="port">The port number.</param>
        /// <returns>The builder for chaining.</returns>
        public HostConfigurationBuilder Port(int port)
        {
            _options.Configuration!.Port = port;
            return this;
        }

        /// <summary>
        /// Sets the number of retry attempts for connection.
        /// </summary>
        /// <param name="count">The retry count.</param>
        /// <returns>The builder for chaining.</returns>
        public HostConfigurationBuilder RetryCount(int count)
        {
            _options.RetryCount = count;
            return this;
        }

        /// <summary>
        /// Sets whether to dispatch consumers asynchronously.
        /// </summary>
        /// <param name="dispatch">True to dispatch asynchronously.</param>
        /// <returns>The builder for chaining.</returns>
        public HostConfigurationBuilder DispatchConsumersAsync(bool dispatch = true)
        {
            _options.DispatchConsumersAsync = dispatch;
            return this;
        }

        /// <summary>
        /// Enables SSL/TLS for the connection.
        /// </summary>
        /// <param name="serverName">The server name for SSL certificate validation.</param>
        /// <returns>The builder for chaining.</returns>
        public HostConfigurationBuilder UseSsl(string? serverName = null)
        {
            _options.Configuration!.Ssl = new RabbitMQSslConfiguration
            {
                Enabled = true,
                ServerName = serverName
            };
            return this;
        }

        /// <summary>
        /// Configures SSL/TLS with detailed settings.
        /// </summary>
        /// <param name="configure">Configuration action for SSL settings.</param>
        /// <returns>The builder for chaining.</returns>
        public HostConfigurationBuilder UseSsl(System.Action<SslConfigurationBuilder> configure)
        {
            var sslConfig = new RabbitMQSslConfiguration { Enabled = true };
            _options.Configuration!.Ssl = sslConfig;

            var builder = new SslConfigurationBuilder(sslConfig);
            configure(builder);

            return this;
        }

        /// <summary>
        /// Sets the heartbeat timeout.
        /// </summary>
        /// <param name="seconds">The heartbeat timeout in seconds.</param>
        /// <returns>The builder for chaining.</returns>
        public HostConfigurationBuilder Heartbeat(ushort seconds)
        {
            _options.Configuration!.RequestedHeartbeat = seconds;
            return this;
        }

        /// <summary>
        /// Sets the connection timeout.
        /// </summary>
        /// <param name="seconds">The connection timeout in seconds.</param>
        /// <returns>The builder for chaining.</returns>
        public HostConfigurationBuilder ConnectionTimeout(int seconds)
        {
            _options.Configuration!.ConnectionTimeout = seconds;
            return this;
        }

        /// <summary>
        /// Sets the network recovery interval.
        /// </summary>
        /// <param name="seconds">The recovery interval in seconds.</param>
        /// <returns>The builder for chaining.</returns>
        public HostConfigurationBuilder NetworkRecoveryInterval(int seconds)
        {
            _options.Configuration!.NetworkRecoveryInterval = seconds;
            return this;
        }

        /// <summary>
        /// Enables automatic recovery.
        /// </summary>
        /// <param name="enabled">True to enable automatic recovery.</param>
        /// <returns>The builder for chaining.</returns>
        public HostConfigurationBuilder AutomaticRecoveryEnabled(bool enabled = true)
        {
            _options.Configuration!.AutomaticRecoveryEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Sets the client-provided connection name.
        /// </summary>
        /// <param name="name">The connection name.</param>
        /// <returns>The builder for chaining.</returns>
        public HostConfigurationBuilder ClientProvidedName(string name)
        {
            _options.Configuration!.ClientProvidedName = name;
            return this;
        }
    }

    /// <summary>
    /// Builder for configuring SSL/TLS settings.
    /// </summary>
    public class SslConfigurationBuilder
    {
        private readonly RabbitMQSslConfiguration _config;

        /// <summary>
        /// Creates a new instance of the SSL configuration builder.
        /// </summary>
        /// <param name="config">The SSL configuration to build.</param>
        public SslConfigurationBuilder(RabbitMQSslConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Sets the server name for certificate validation.
        /// </summary>
        /// <param name="serverName">The server name.</param>
        /// <returns>The builder for chaining.</returns>
        public SslConfigurationBuilder ServerName(string serverName)
        {
            _config.ServerName = serverName;
            return this;
        }

        /// <summary>
        /// Sets the certificate path.
        /// </summary>
        /// <param name="path">The path to the certificate file.</param>
        /// <returns>The builder for chaining.</returns>
        public SslConfigurationBuilder CertificatePath(string path)
        {
            _config.CertificatePath = path;
            return this;
        }

        /// <summary>
        /// Sets the certificate passphrase.
        /// </summary>
        /// <param name="passphrase">The certificate passphrase.</param>
        /// <returns>The builder for chaining.</returns>
        public SslConfigurationBuilder CertificatePassphrase(string passphrase)
        {
            _config.CertificatePassphrase = passphrase;
            return this;
        }

        /// <summary>
        /// Sets whether to accept untrusted certificates (not recommended for production).
        /// </summary>
        /// <param name="accept">True to accept untrusted certificates.</param>
        /// <returns>The builder for chaining.</returns>
        public SslConfigurationBuilder AcceptablePolicyErrors(bool accept = true)
        {
            _config.AcceptablePolicyErrors = accept;
            return this;
        }
    }

    /// <summary>
    /// Configuration for RabbitMQ SSL/TLS.
    /// </summary>
    public class RabbitMQSslConfiguration
    {
        /// <summary>
        /// Gets or sets whether SSL is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the server name for certificate validation.
        /// </summary>
        public string? ServerName { get; set; }

        /// <summary>
        /// Gets or sets the path to the certificate file.
        /// </summary>
        public string? CertificatePath { get; set; }

        /// <summary>
        /// Gets or sets the certificate passphrase.
        /// </summary>
        public string? CertificatePassphrase { get; set; }

        /// <summary>
        /// Gets or sets whether to accept untrusted certificates.
        /// </summary>
        public bool AcceptablePolicyErrors { get; set; }
    }
}

