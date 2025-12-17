//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Infrastructure.RabbitMQ.Configuration.Fluent;
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Configuration
{
    /// <summary>
    /// Configuration for RabbitMQ connection settings.
    /// </summary>
    [Serializable]
    public class RabbitMQConnection
    {
        /// <summary>
        /// Gets or sets the host name or IP address.
        /// </summary>
        public string? HostName { get; set; }

        /// <summary>
        /// Gets or sets the port number.
        /// Default is 5672.
        /// </summary>
        public int Port { get; set; } = 5672;

        /// <summary>
        /// Gets or sets the username for authentication.
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// Gets or sets the password for authentication.
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Gets or sets the virtual host.
        /// Default is "/".
        /// </summary>
        public string? VirtualHost { get; set; } = "/";

        /// <summary>
        /// Gets or sets the SSL/TLS configuration.
        /// </summary>
        public RabbitMQSslConfiguration? Ssl { get; set; }

        /// <summary>
        /// Gets or sets the heartbeat timeout in seconds.
        /// Default is 60 seconds.
        /// </summary>
        public ushort RequestedHeartbeat { get; set; } = 60;

        /// <summary>
        /// Gets or sets the connection timeout in seconds.
        /// Default is 30 seconds.
        /// </summary>
        public int ConnectionTimeout { get; set; } = 30;

        /// <summary>
        /// Gets or sets the network recovery interval in seconds.
        /// Default is 5 seconds.
        /// </summary>
        public int NetworkRecoveryInterval { get; set; } = 5;

        /// <summary>
        /// Gets or sets whether automatic recovery is enabled.
        /// Default is true.
        /// </summary>
        public bool AutomaticRecoveryEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the client-provided connection name.
        /// Useful for identifying connections in RabbitMQ management UI.
        /// </summary>
        public string? ClientProvidedName { get; set; }

        /// <summary>
        /// Gets or sets whether to use topology recovery.
        /// Default is true.
        /// </summary>
        public bool TopologyRecoveryEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of channels per connection.
        /// Default is 2047 (RabbitMQ default).
        /// </summary>
        public ushort RequestedChannelMax { get; set; } = 2047;

        /// <summary>
        /// Gets or sets the maximum frame size in bytes.
        /// Default is 131072 (128KB).
        /// </summary>
        public uint RequestedFrameMax { get; set; } = 131072;

        /// <summary>
        /// Creates an AMQP connection string from this configuration.
        /// </summary>
        /// <returns>The AMQP connection string.</returns>
        public string ToConnectionString()
        {
            var scheme = Ssl?.Enabled == true ? "amqps" : "amqp";
            var host = HostName ?? "localhost";
            var port = Port > 0 ? Port : (Ssl?.Enabled == true ? 5671 : 5672);
            var vhost = Uri.EscapeDataString(VirtualHost ?? "/");
            var user = Uri.EscapeDataString(UserName ?? "guest");
            var pass = Uri.EscapeDataString(Password ?? "guest");

            return $"{scheme}://{user}:{pass}@{host}:{port}/{vhost}";
        }
    }
}
