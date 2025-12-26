//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Security.Helpers
{
    /// <summary>
    /// Helper interface for managing secret rotation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface provides utilities for managing secret rotation, including:
    /// - Checking if a secret needs rotation
    /// - Rotating secrets automatically
    /// - Managing secret versions
    /// </para>
    /// <para>
    /// Secret rotation is important for security best practices. Secrets should be rotated
    /// periodically to reduce the risk of compromise.
    /// </para>
    /// </remarks>
    public interface ISecretRotationHelper
    {
        /// <summary>
        /// Checks if a secret needs rotation based on its age or other criteria.
        /// </summary>
        /// <param name="secretName">The name of the secret to check.</param>
        /// <param name="maxAge">The maximum age before rotation is required.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns><c>true</c> if the secret needs rotation, <c>false</c> otherwise.</returns>
        Task<bool> NeedsRotationAsync(
            string secretName,
            TimeSpan maxAge,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Rotates a secret by generating a new value and updating it in the secret store.
        /// </summary>
        /// <param name="secretName">The name of the secret to rotate.</param>
        /// <param name="generateNewSecret">Function to generate a new secret value.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The new secret value.</returns>
        Task<string> RotateSecretAsync(
            string secretName,
            Func<Task<string>> generateNewSecret,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the creation date of a secret.
        /// </summary>
        /// <param name="secretName">The name of the secret.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The creation date, or <c>null</c> if not available.</returns>
        Task<DateTime?> GetSecretCreationDateAsync(
            string secretName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the last rotation date of a secret.
        /// </summary>
        /// <param name="secretName">The name of the secret.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The last rotation date, or <c>null</c> if not available.</returns>
        Task<DateTime?> GetLastRotationDateAsync(
            string secretName,
            CancellationToken cancellationToken = default);
    }
}

