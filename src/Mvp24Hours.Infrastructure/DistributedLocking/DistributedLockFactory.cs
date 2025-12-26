//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.DistributedLocking.Contract;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.Infrastructure.DistributedLocking
{
    /// <summary>
    /// Factory implementation for creating distributed lock instances.
    /// </summary>
    /// <remarks>
    /// This factory manages multiple distributed lock providers and allows
    /// selection by name or default provider.
    /// </remarks>
    public class DistributedLockFactory : IDistributedLockFactory
    {
        private readonly Dictionary<string, IDistributedLock> _providers;
        private readonly string? _defaultProviderName;

        /// <summary>
        /// Initializes a new instance of the <see cref="DistributedLockFactory"/> class.
        /// </summary>
        /// <param name="providers">Dictionary of provider names to lock instances.</param>
        /// <param name="defaultProviderName">Optional default provider name.</param>
        public DistributedLockFactory(
            Dictionary<string, IDistributedLock> providers,
            string? defaultProviderName = null)
        {
            _providers = providers ?? throw new ArgumentNullException(nameof(providers));
            _defaultProviderName = defaultProviderName;

            if (_providers.Count == 0)
            {
                throw new ArgumentException("At least one provider must be registered.", nameof(providers));
            }
        }

        /// <inheritdoc />
        public IDistributedLock Create()
        {
            if (!string.IsNullOrWhiteSpace(_defaultProviderName))
            {
                return Create(_defaultProviderName);
            }

            // Use first registered provider as default
            var firstProvider = _providers.First();
            return firstProvider.Value;
        }

        /// <inheritdoc />
        public IDistributedLock Create(string providerName)
        {
            if (string.IsNullOrWhiteSpace(providerName))
                throw new ArgumentException("Provider name cannot be null or empty.", nameof(providerName));

            // Case-insensitive lookup
            var key = _providers.Keys.FirstOrDefault(
                k => string.Equals(k, providerName, StringComparison.OrdinalIgnoreCase));

            if (key == null || !_providers.TryGetValue(key, out var provider))
            {
                var availableProviders = string.Join(", ", _providers.Keys);
                throw new ArgumentException(
                    $"Provider '{providerName}' is not registered. Available providers: {availableProviders}",
                    nameof(providerName));
            }

            return provider;
        }
    }
}

