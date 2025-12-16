//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Configuration
{
    /// <summary>
    /// Configuration options for request/response client.
    /// </summary>
    [Serializable]
    public class RequestClientOptions
    {
        /// <summary>
        /// Gets or sets the exchange name for requests.
        /// Default is empty (default exchange).
        /// </summary>
        public string Exchange { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the routing key for requests.
        /// If not specified, the request type name is used.
        /// </summary>
        public string? RoutingKey { get; set; }

        /// <summary>
        /// Gets or sets the timeout in milliseconds for waiting for a response.
        /// Default is 30000 (30 seconds).
        /// </summary>
        public int TimeoutMilliseconds { get; set; } = 30000;

        /// <summary>
        /// Gets or sets whether to throw an exception on timeout.
        /// Default is false (returns a Timeout response instead).
        /// </summary>
        public bool ThrowOnTimeout { get; set; } = false;
    }
}

