//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.HealthChecks
{
    /// <summary>
    /// Health check implementation for MongoDB replica set status verification.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This health check monitors replica set health including:
    /// <list type="bullet">
    ///   <item>Primary node availability and status</item>
    ///   <item>Secondary nodes status and replication lag</item>
    ///   <item>Replica set member health states</item>
    ///   <item>Overall cluster health assessment</item>
    /// </list>
    /// </para>
    /// <para>
    /// Requires connection to a MongoDB replica set and may require elevated privileges.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddHealthChecks()
    ///     .AddMongoDbReplicaSetHealthCheck("mongodb-replicaset",
    ///         options =>
    ///         {
    ///             options.MinSecondaryNodes = 1;
    ///             options.MaxReplicationLagSeconds = 30;
    ///         },
    ///         failureStatus: HealthStatus.Degraded,
    ///         tags: new[] { "database", "cluster" });
    /// </code>
    /// </example>
    public sealed class MongoDbReplicaSetHealthCheck : IHealthCheck
    {
        private readonly MongoDbOptions _options;
        private readonly MongoDbReplicaSetHealthCheckOptions _healthCheckOptions;
        private IMongoClient? _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbReplicaSetHealthCheck"/> class.
        /// </summary>
        /// <param name="options">The MongoDB configuration options.</param>
        /// <param name="healthCheckOptions">The replica set health check specific options.</param>
        public MongoDbReplicaSetHealthCheck(
            IOptions<MongoDbOptions> options,
            IOptions<MongoDbReplicaSetHealthCheckOptions>? healthCheckOptions = null)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _healthCheckOptions = healthCheckOptions?.Value ?? new MongoDbReplicaSetHealthCheckOptions();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbReplicaSetHealthCheck"/> class with explicit options.
        /// </summary>
        /// <param name="options">The MongoDB configuration options.</param>
        /// <param name="healthCheckOptions">The replica set health check specific options.</param>
        public MongoDbReplicaSetHealthCheck(
            MongoDbOptions options,
            MongoDbReplicaSetHealthCheckOptions? healthCheckOptions = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _healthCheckOptions = healthCheckOptions ?? new MongoDbReplicaSetHealthCheckOptions();
        }

        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var data = new Dictionary<string, object>
            {
                ["timestamp"] = DateTimeOffset.UtcNow
            };

            try
            {
                var client = GetOrCreateClient();
                var adminDb = client.GetDatabase("admin");

                // Get replica set status
                var replSetStatusCommand = new BsonDocument("replSetGetStatus", 1);
                var replSetStatus = await adminDb.RunCommandAsync<BsonDocument>(
                    replSetStatusCommand,
                    readPreference: null,
                    cancellationToken);

                if (replSetStatus == null)
                {
                    return new HealthCheckResult(
                        context?.Registration?.FailureStatus ?? HealthStatus.Unhealthy,
                        "Unable to retrieve replica set status.",
                        data: data);
                }

                // Extract replica set information
                var setName = replSetStatus.GetValue("set", "unknown").AsString;
                data["replicaSetName"] = setName;

                var members = replSetStatus.GetValue("members", new BsonArray()).AsBsonArray;
                var memberInfoList = new List<ReplicaSetMemberInfo>();

                int primaryCount = 0;
                int secondaryCount = 0;
                int healthyCount = 0;
                int unhealthyCount = 0;
                double maxLagSeconds = 0;

                // Get optimeDate from primary for lag calculation
                DateTime? primaryOptimeDate = null;

                foreach (var member in members.OfType<BsonDocument>())
                {
                    var memberInfo = new ReplicaSetMemberInfo
                    {
                        Id = member.GetValue("_id", 0).ToInt32(),
                        Name = member.GetValue("name", "unknown").AsString,
                        State = member.GetValue("state", 0).ToInt32(),
                        StateStr = member.GetValue("stateStr", "unknown").AsString,
                        Health = member.GetValue("health", 0).ToDouble(),
                        Self = member.GetValue("self", false).ToBoolean()
                    };

                    // Get optime for lag calculation
                    if (member.TryGetValue("optimeDate", out var optimeValue))
                    {
                        memberInfo.OptimeDate = optimeValue.ToUniversalTime();
                    }

                    memberInfoList.Add(memberInfo);

                    if (memberInfo.StateStr.Equals("PRIMARY", StringComparison.OrdinalIgnoreCase))
                    {
                        primaryCount++;
                        primaryOptimeDate = memberInfo.OptimeDate;
                    }
                    else if (memberInfo.StateStr.Equals("SECONDARY", StringComparison.OrdinalIgnoreCase))
                    {
                        secondaryCount++;
                    }

                    if (memberInfo.Health >= 1)
                    {
                        healthyCount++;
                    }
                    else
                    {
                        unhealthyCount++;
                    }
                }

                // Calculate replication lag for secondaries
                if (primaryOptimeDate.HasValue)
                {
                    foreach (var member in memberInfoList.Where(m => m.StateStr.Equals("SECONDARY", StringComparison.OrdinalIgnoreCase)))
                    {
                        if (member.OptimeDate.HasValue)
                        {
                            var lag = (primaryOptimeDate.Value - member.OptimeDate.Value).TotalSeconds;
                            member.ReplicationLagSeconds = Math.Max(0, lag);
                            maxLagSeconds = Math.Max(maxLagSeconds, member.ReplicationLagSeconds);
                        }
                    }
                }

                data["primaryCount"] = primaryCount;
                data["secondaryCount"] = secondaryCount;
                data["healthyMemberCount"] = healthyCount;
                data["unhealthyMemberCount"] = unhealthyCount;
                data["maxReplicationLagSeconds"] = maxLagSeconds;
                data["totalMembers"] = members.Count;

                if (_healthCheckOptions.IncludeMemberDetails)
                {
                    data["members"] = memberInfoList.Select(m => new
                    {
                        m.Id,
                        m.Name,
                        m.StateStr,
                        m.Health,
                        m.Self,
                        m.ReplicationLagSeconds
                    }).ToList();
                }

                // Evaluate health based on configuration
                var issues = new List<string>();

                if (primaryCount == 0)
                {
                    issues.Add("No primary node available.");
                }

                if (primaryCount > 1)
                {
                    issues.Add($"Multiple primary nodes detected ({primaryCount}).");
                }

                if (_healthCheckOptions.MinSecondaryNodes > 0 && secondaryCount < _healthCheckOptions.MinSecondaryNodes)
                {
                    issues.Add($"Insufficient secondary nodes: {secondaryCount}/{_healthCheckOptions.MinSecondaryNodes} required.");
                }

                if (_healthCheckOptions.MaxReplicationLagSeconds > 0 && maxLagSeconds > _healthCheckOptions.MaxReplicationLagSeconds)
                {
                    issues.Add($"Replication lag ({maxLagSeconds:F1}s) exceeds threshold ({_healthCheckOptions.MaxReplicationLagSeconds}s).");
                }

                if (unhealthyCount > 0 && !_healthCheckOptions.AllowUnhealthyMembers)
                {
                    issues.Add($"{unhealthyCount} unhealthy member(s) detected.");
                }

                if (issues.Count > 0)
                {
                    var message = $"Replica set '{setName}' has issues: {string.Join(" ", issues)}";
                    
                    // Determine if it's degraded or unhealthy
                    var status = primaryCount > 0 && secondaryCount > 0
                        ? HealthStatus.Degraded
                        : context?.Registration?.FailureStatus ?? HealthStatus.Unhealthy;

                    return new HealthCheckResult(status, message, data: data);
                }

                return HealthCheckResult.Healthy(
                    $"Replica set '{setName}' is healthy with {primaryCount} primary, {secondaryCount} secondaries.",
                    data);
            }
            catch (MongoCommandException ex) when (ex.CodeName == "NotYetInitialized" || ex.Code == 94)
            {
                data["error"] = "Replica set not initialized";
                data["errorType"] = "NotInitialized";

                return new HealthCheckResult(
                    context?.Registration?.FailureStatus ?? HealthStatus.Unhealthy,
                    "MongoDB replica set is not initialized.",
                    ex,
                    data);
            }
            catch (MongoCommandException ex) when (ex.CodeName == "NoReplicationEnabled" || ex.Code == 76)
            {
                data["error"] = "Not a replica set";
                data["errorType"] = "StandaloneMode";

                // Standalone mode - might be acceptable depending on configuration
                if (_healthCheckOptions.AllowStandaloneMode)
                {
                    return HealthCheckResult.Healthy(
                        "MongoDB is running in standalone mode (replica set not configured).",
                        data);
                }

                return new HealthCheckResult(
                    context?.Registration?.FailureStatus ?? HealthStatus.Unhealthy,
                    "MongoDB is not configured as a replica set.",
                    ex,
                    data);
            }
            catch (Exception ex)
            {
                data["error"] = ex.Message;
                data["errorType"] = ex.GetType().Name;

                return new HealthCheckResult(
                    context?.Registration?.FailureStatus ?? HealthStatus.Unhealthy,
                    $"Replica set health check failed: {ex.Message}",
                    ex,
                    data);
            }
        }

        private IMongoClient GetOrCreateClient()
        {
            if (_client != null) return _client;

            var settings = MongoClientSettings.FromConnectionString(_options.ConnectionString);
            settings.ConnectTimeout = TimeSpan.FromSeconds(_healthCheckOptions.ConnectionTimeoutSeconds);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(_healthCheckOptions.ServerSelectionTimeoutSeconds);

            _client = new MongoClient(settings);
            return _client;
        }

        private sealed class ReplicaSetMemberInfo
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int State { get; set; }
            public string StateStr { get; set; } = string.Empty;
            public double Health { get; set; }
            public bool Self { get; set; }
            public DateTime? OptimeDate { get; set; }
            public double ReplicationLagSeconds { get; set; }
        }
    }

    /// <summary>
    /// Configuration options for MongoDB replica set health checks.
    /// </summary>
    public sealed class MongoDbReplicaSetHealthCheckOptions
    {
        /// <summary>
        /// Gets or sets the minimum number of secondary nodes required.
        /// Default is 0 (no minimum).
        /// </summary>
        public int MinSecondaryNodes { get; set; }

        /// <summary>
        /// Gets or sets the maximum allowed replication lag in seconds.
        /// Default is 0 (no limit).
        /// </summary>
        public int MaxReplicationLagSeconds { get; set; }

        /// <summary>
        /// Gets or sets whether to allow unhealthy members.
        /// Default is true.
        /// </summary>
        public bool AllowUnhealthyMembers { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to allow standalone mode (not a replica set).
        /// Default is false.
        /// </summary>
        public bool AllowStandaloneMode { get; set; }

        /// <summary>
        /// Gets or sets whether to include detailed member information.
        /// Default is true.
        /// </summary>
        public bool IncludeMemberDetails { get; set; } = true;

        /// <summary>
        /// Gets or sets the connection timeout in seconds.
        /// Default is 5 seconds.
        /// </summary>
        public int ConnectionTimeoutSeconds { get; set; } = 5;

        /// <summary>
        /// Gets or sets the server selection timeout in seconds.
        /// Default is 5 seconds.
        /// </summary>
        public int ServerSelectionTimeoutSeconds { get; set; } = 5;
    }
}

