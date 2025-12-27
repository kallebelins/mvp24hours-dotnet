//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.ReadWriteSplitting
{
    /// <summary>
    /// Default implementation of replica selector with support for multiple load balancing strategies.
    /// </summary>
    public class ReplicaSelector : IReplicaSelector
    {
        private readonly ReadWriteOptions _options;
        private readonly ILogger<ReplicaSelector> _logger;
        private readonly ConcurrentDictionary<string, ReplicaState> _replicaStates;
        private readonly Random _random;
        private int _roundRobinIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplicaSelector"/> class.
        /// </summary>
        public ReplicaSelector(
            IOptions<ReadWriteOptions> options,
            ILogger<ReplicaSelector> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _replicaStates = new ConcurrentDictionary<string, ReplicaState>();
            _random = new Random();
            _roundRobinIndex = 0;

            InitializeReplicaStates();
        }

        private void InitializeReplicaStates()
        {
            foreach (var connectionString in _options.ReplicaConnectionStrings)
            {
                _replicaStates.TryAdd(connectionString, new ReplicaState
                {
                    IsHealthy = true,
                    ConsecutiveFailures = 0,
                    LastCheckTime = DateTime.UtcNow
                });
            }
        }

        /// <inheritdoc/>
        public Task<string?> SelectReplicaAsync(CancellationToken cancellationToken = default)
        {
            var healthyReplicas = GetHealthyReplicas();

            if (healthyReplicas.Count == 0)
            {
                if (_options.FallbackToPrimaryOnReplicaFailure)
                {
                    _logger.LogWarning("No healthy replicas available, falling back to primary");
                    return Task.FromResult<string?>(GetPrimaryConnectionString());
                }

                _logger.LogError("No healthy replicas available and fallback to primary is disabled");
                return Task.FromResult<string?>(null);
            }

            var selected = _options.LoadBalancing switch
            {
                ReplicaLoadBalancing.RoundRobin => SelectRoundRobin(healthyReplicas),
                ReplicaLoadBalancing.Random => SelectRandom(healthyReplicas),
                ReplicaLoadBalancing.Weighted => SelectWeighted(healthyReplicas),
                ReplicaLoadBalancing.LeastLatency => SelectLeastLatency(healthyReplicas),
                ReplicaLoadBalancing.LeastConnections => SelectLeastConnections(healthyReplicas),
                ReplicaLoadBalancing.Failover => SelectFailover(healthyReplicas),
                _ => SelectRoundRobin(healthyReplicas)
            };

            if (_options.LogReplicaSelection)
            {
                _logger.LogDebug(
                    "Selected replica using {Strategy}: {Replica}",
                    _options.LoadBalancing,
                    SanitizeConnectionString(selected));
            }

            return Task.FromResult<string?>(selected);
        }

        /// <inheritdoc/>
        public string GetPrimaryConnectionString() => _options.PrimaryConnectionString;

        /// <inheritdoc/>
        public void MarkReplicaFailed(string connectionString)
        {
            if (_replicaStates.TryGetValue(connectionString, out var state))
            {
                state.ConsecutiveFailures++;
                state.LastFailureTime = DateTime.UtcNow;

                if (state.ConsecutiveFailures >= _options.FailureThreshold)
                {
                    state.IsHealthy = false;
                    _logger.LogWarning(
                        "Replica marked as unhealthy after {Failures} consecutive failures: {Replica}",
                        state.ConsecutiveFailures,
                        SanitizeConnectionString(connectionString));
                }

                _replicaStates[connectionString] = state;
            }
        }

        /// <inheritdoc/>
        public void MarkReplicaRecovered(string connectionString)
        {
            if (_replicaStates.TryGetValue(connectionString, out var state))
            {
                state.IsHealthy = true;
                state.ConsecutiveFailures = 0;
                state.LastCheckTime = DateTime.UtcNow;

                _logger.LogInformation(
                    "Replica marked as recovered: {Replica}",
                    SanitizeConnectionString(connectionString));

                _replicaStates[connectionString] = state;
            }
        }

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, ReplicaHealth> GetReplicaHealthStatus()
        {
            return _replicaStates.ToDictionary(
                kvp => kvp.Key,
                kvp => new ReplicaHealth
                {
                    IsHealthy = kvp.Value.IsHealthy,
                    ConsecutiveFailures = kvp.Value.ConsecutiveFailures,
                    LatencyMs = kvp.Value.Latency?.TotalMilliseconds,
                    ActiveConnections = kvp.Value.ActiveConnections,
                    LastCheckTime = kvp.Value.LastCheckTime,
                    LastFailureTime = kvp.Value.LastFailureTime
                });
        }

        private List<string> GetHealthyReplicas()
        {
            var now = DateTime.UtcNow;
            var healthyReplicas = new List<string>();

            foreach (var kvp in _replicaStates)
            {
                var state = kvp.Value;

                // Check if unhealthy replica should be retried
                if (!state.IsHealthy && 
                    state.LastFailureTime.HasValue &&
                    now - state.LastFailureTime.Value >= _options.RecoveryTimeout)
                {
                    // Allow retry - mark as tentatively healthy
                    state.IsHealthy = true;
                    _replicaStates[kvp.Key] = state;
                    
                    _logger.LogDebug(
                        "Allowing retry for previously unhealthy replica: {Replica}",
                        SanitizeConnectionString(kvp.Key));
                }

                if (state.IsHealthy)
                {
                    healthyReplicas.Add(kvp.Key);
                }
            }

            return healthyReplicas;
        }

        private string SelectRoundRobin(List<string> replicas)
        {
            var index = Interlocked.Increment(ref _roundRobinIndex) % replicas.Count;
            return replicas[index];
        }

        private string SelectRandom(List<string> replicas)
        {
            var index = _random.Next(replicas.Count);
            return replicas[index];
        }

        private string SelectWeighted(List<string> replicas)
        {
            if (_options.ReplicaWeights.Count == 0)
            {
                return SelectRoundRobin(replicas);
            }

            var totalWeight = 0;
            var weights = new List<int>();

            for (int i = 0; i < replicas.Count; i++)
            {
                var originalIndex = _options.ReplicaConnectionStrings.IndexOf(replicas[i]);
                var weight = originalIndex >= 0 && originalIndex < _options.ReplicaWeights.Count
                    ? _options.ReplicaWeights[originalIndex]
                    : 1;
                weights.Add(weight);
                totalWeight += weight;
            }

            var randomValue = _random.Next(totalWeight);
            var cumulative = 0;

            for (int i = 0; i < replicas.Count; i++)
            {
                cumulative += weights[i];
                if (randomValue < cumulative)
                {
                    return replicas[i];
                }
            }

            return replicas[^1];
        }

        private string SelectLeastLatency(List<string> replicas)
        {
            var bestReplica = replicas[0];
            var bestLatency = TimeSpan.MaxValue;

            foreach (var replica in replicas)
            {
                if (_replicaStates.TryGetValue(replica, out var state) && state.Latency.HasValue)
                {
                    if (state.Latency.Value < bestLatency)
                    {
                        bestLatency = state.Latency.Value;
                        bestReplica = replica;
                    }
                }
            }

            return bestReplica;
        }

        private string SelectLeastConnections(List<string> replicas)
        {
            var bestReplica = replicas[0];
            var leastConnections = int.MaxValue;

            foreach (var replica in replicas)
            {
                if (_replicaStates.TryGetValue(replica, out var state) && state.ActiveConnections.HasValue)
                {
                    if (state.ActiveConnections.Value < leastConnections)
                    {
                        leastConnections = state.ActiveConnections.Value;
                        bestReplica = replica;
                    }
                }
            }

            return bestReplica;
        }

        private string SelectFailover(List<string> replicas)
        {
            // Return the first healthy replica in order
            return replicas[0];
        }

        private static string SanitizeConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return "Unknown";

            return System.Text.RegularExpressions.Regex.Replace(
                connectionString,
                @"(Password|Pwd|User Id|Uid|User)=[^;]+",
                "$1=***",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        private class ReplicaState
        {
            public bool IsHealthy { get; set; }
            public int ConsecutiveFailures { get; set; }
            public TimeSpan? Latency { get; set; }
            public int? ActiveConnections { get; set; }
            public DateTime LastCheckTime { get; set; }
            public DateTime? LastFailureTime { get; set; }
        }
    }
}

