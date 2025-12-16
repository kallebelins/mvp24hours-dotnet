//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Configuration
{
    /// <summary>
    /// Configuration options for headers exchange routing.
    /// </summary>
    [Serializable]
    public class HeadersExchangeOptions
    {
        /// <summary>
        /// Gets or sets whether headers exchange is enabled. Default is false.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the match type for headers binding.
        /// "all" = all headers must match, "any" = any header can match.
        /// Default is "all".
        /// </summary>
        public string MatchType { get; set; } = "all";

        /// <summary>
        /// Gets or sets the headers for binding.
        /// </summary>
        public Dictionary<string, object>? BindingHeaders { get; set; }

        /// <summary>
        /// Gets or sets the default headers to include in published messages.
        /// </summary>
        public Dictionary<string, object>? DefaultMessageHeaders { get; set; }
    }
}

