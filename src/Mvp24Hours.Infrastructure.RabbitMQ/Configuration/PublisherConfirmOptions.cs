//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Configuration
{
    /// <summary>
    /// Configuration options for publisher confirms.
    /// </summary>
    [Serializable]
    public class PublisherConfirmOptions
    {
        /// <summary>
        /// Gets or sets whether publisher confirms are enabled. Default is true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the timeout in milliseconds for waiting for confirms.
        /// Default is 5000ms (5 seconds).
        /// </summary>
        public int TimeoutMilliseconds { get; set; } = 5000;

        /// <summary>
        /// Gets or sets whether to use async confirms with callbacks.
        /// When true, publishes return immediately and callbacks are invoked on confirm/nack.
        /// Default is false (synchronous confirms).
        /// </summary>
        public bool UseAsyncCallbacks { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to wait for all confirms to complete or die on nack.
        /// Default is true.
        /// </summary>
        public bool WaitForConfirmsOrDie { get; set; } = true;
    }
}

