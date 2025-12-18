//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Http.Resilience
{
    /// <summary>
    /// Policy wrapper that composes multiple resilience policies into a single policy.
    /// Policies are executed in order: outermost to innermost (left to right).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Policy composition follows the order: Fallback → Retry → CircuitBreaker → Bulkhead → Timeout
    /// This ensures that:
    /// <list type="bullet">
    /// <item>Fallback is the outermost policy (last resort)</item>
    /// <item>Retry attempts are made before giving up</item>
    /// <item>Circuit breaker prevents cascading failures</item>
    /// <item>Bulkhead limits concurrent executions</item>
    /// <item>Timeout is the innermost policy (applied first)</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class PolicyWrap : IHttpResiliencePolicy
    {
        private readonly ILogger<PolicyWrap>? _logger;
        private readonly List<IHttpResiliencePolicy> _policies;
        private readonly IAsyncPolicy<HttpResponseMessage> _wrappedPolicy;

        /// <summary>
        /// Initializes a new instance of the <see cref="PolicyWrap"/> class.
        /// </summary>
        /// <param name="policies">The policies to wrap, in execution order (outermost to innermost).</param>
        /// <param name="logger">Optional logger instance.</param>
        public PolicyWrap(
            IEnumerable<IHttpResiliencePolicy> policies,
            ILogger<PolicyWrap>? logger = null)
        {
            _policies = policies?.ToList() ?? throw new ArgumentNullException(nameof(policies));
            _logger = logger;

            if (_policies.Count == 0)
            {
                throw new ArgumentException("At least one policy must be provided.", nameof(policies));
            }

            _wrappedPolicy = CreateWrappedPolicy();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PolicyWrap"/> class with a single policy.
        /// </summary>
        /// <param name="policy">The policy to wrap.</param>
        /// <param name="logger">Optional logger instance.</param>
        public PolicyWrap(IHttpResiliencePolicy policy, ILogger<PolicyWrap>? logger = null)
            : this(new[] { policy }, logger)
        {
        }

        /// <inheritdoc/>
        public string PolicyName => $"PolicyWrap({string.Join(", ", _policies.Select(p => p.PolicyName))})";

        /// <summary>
        /// Gets the policies in this wrap.
        /// </summary>
        public IReadOnlyList<IHttpResiliencePolicy> Policies => _policies;

        /// <inheritdoc/>
        public Task<HttpResponseMessage> ExecuteAsync(
            Func<HttpRequestMessage> requestFactory,
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync,
            CancellationToken cancellationToken = default)
        {
            if (requestFactory == null)
            {
                throw new ArgumentNullException(nameof(requestFactory));
            }

            if (sendAsync == null)
            {
                throw new ArgumentNullException(nameof(sendAsync));
            }

            return _wrappedPolicy.ExecuteAsync(
                async (ct) =>
                {
                    var request = requestFactory();
                    return await sendAsync(request, ct);
                },
                cancellationToken);
        }

        /// <inheritdoc/>
        public IAsyncPolicy<HttpResponseMessage> GetPollyPolicy() => _wrappedPolicy;

        /// <summary>
        /// Creates a new policy wrap by combining this wrap with another policy.
        /// </summary>
        /// <param name="policy">The policy to combine.</param>
        /// <returns>A new policy wrap containing all policies.</returns>
        public PolicyWrap Wrap(IHttpResiliencePolicy policy)
        {
            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            var combinedPolicies = new List<IHttpResiliencePolicy>(_policies) { policy };
            return new PolicyWrap(combinedPolicies, _logger);
        }

        /// <summary>
        /// Creates a new policy wrap by combining multiple policies.
        /// </summary>
        /// <param name="policies">The policies to combine.</param>
        /// <returns>A new policy wrap containing all policies.</returns>
        public static PolicyWrap Combine(params IHttpResiliencePolicy[] policies)
        {
            if (policies == null || policies.Length == 0)
            {
                throw new ArgumentException("At least one policy must be provided.", nameof(policies));
            }

            return new PolicyWrap(policies);
        }

        private IAsyncPolicy<HttpResponseMessage> CreateWrappedPolicy()
        {
            var policies = _policies.Select(p => p.GetPollyPolicy()).ToList();

            if (policies.Count == 1)
            {
                return policies[0];
            }

            // Wrap policies from innermost to outermost (right to left)
            // This means the first policy in the list is the outermost (executed last)
            var wrapped = policies[0];
            for (int i = 1; i < policies.Count; i++)
            {
                wrapped = Policy.WrapAsync(policies[i], wrapped);
            }

            _logger?.LogDebug(
                "Created policy wrap with {PolicyCount} policies: {PolicyNames}",
                policies.Count,
                string.Join(" → ", _policies.Select(p => p.PolicyName)));

            return wrapped;
        }
    }
}

