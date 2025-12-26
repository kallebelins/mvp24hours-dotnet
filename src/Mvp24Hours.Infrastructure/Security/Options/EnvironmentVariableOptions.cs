//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.Security.Options
{
    /// <summary>
    /// Configuration options for Environment Variable secret provider.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These options configure the behavior when using environment variables as the secret provider.
    /// </para>
    /// </remarks>
    public class EnvironmentVariableOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentVariableOptions"/> class.
        /// </summary>
        public EnvironmentVariableOptions()
        {
        }

        /// <summary>
        /// Gets or sets the target environment variable scope.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Determines whether to read from process, user, or machine environment variables.
        /// </para>
        /// <para>
        /// Default is <see cref="EnvironmentVariableTarget.Process"/>.
        /// </para>
        /// </remarks>
        public EnvironmentVariableTarget Target { get; set; } = EnvironmentVariableTarget.Process;

        /// <summary>
        /// Gets or sets the default environment variable name prefix.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When specified, this prefix is automatically prepended to all secret names
        /// when retrieving secrets. Useful for organizing secrets by environment or application.
        /// </para>
        /// <para>
        /// Example: If prefix is "MYAPP_", secret name "ApiKey" becomes "MYAPP_ApiKey".
        /// </para>
        /// </remarks>
        public string? VariableNamePrefix { get; set; }

        /// <summary>
        /// Gets or sets whether variable names are case-sensitive.
        /// </summary>
        /// <remarks>
        /// <para>
        /// On Windows, environment variable names are case-insensitive by default.
        /// On Linux/macOS, they are case-sensitive.
        /// </para>
        /// <para>
        /// Default is <c>false</c> (case-insensitive) for cross-platform compatibility.
        /// </para>
        /// </remarks>
        public bool CaseSensitive { get; set; } = false;
    }
}

