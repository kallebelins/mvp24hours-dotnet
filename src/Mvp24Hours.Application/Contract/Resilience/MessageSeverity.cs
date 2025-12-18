//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
namespace Mvp24Hours.Application.Contract.Resilience
{
    /// <summary>
    /// Defines the severity level of a result message.
    /// Used to distinguish between critical errors and non-blocking warnings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Message severity determines how the result should be handled:
    /// <list type="bullet">
    /// <item><c>Error</c>: The operation failed and should not proceed.</item>
    /// <item><c>Warning</c>: The operation completed but with caveats.</item>
    /// <item><c>Info</c>: Informational message, operation succeeded.</item>
    /// </list>
    /// </para>
    /// </remarks>
    public enum MessageSeverity
    {
        /// <summary>
        /// Informational message. The operation completed successfully.
        /// </summary>
        Info = 0,

        /// <summary>
        /// Warning message. The operation completed but with potential issues.
        /// Warnings don't cause the result to be considered a failure.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Error message. The operation failed and should not proceed.
        /// Errors cause the result to be considered a failure.
        /// </summary>
        Error = 2
    }
}

