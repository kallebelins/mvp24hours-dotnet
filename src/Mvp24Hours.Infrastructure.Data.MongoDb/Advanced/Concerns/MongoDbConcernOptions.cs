//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Driver;
using System;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.Concerns
{
    /// <summary>
    /// Advanced read and write concern configuration for MongoDB operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Read and Write Concerns control the consistency and durability guarantees
    /// of MongoDB operations:
    /// <list type="bullet">
    ///   <item><b>Read Concern</b>: Controls data visibility for read operations</item>
    ///   <item><b>Write Concern</b>: Controls acknowledgment for write operations</item>
    ///   <item><b>Read Preference</b>: Controls which nodes to read from</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class MongoDbConcernOptions
    {
        /// <summary>
        /// Gets or sets the read concern level.
        /// </summary>
        public ReadConcernLevel? ReadConcernLevel { get; set; }

        /// <summary>
        /// Gets or sets the write concern mode.
        /// </summary>
        public WriteConcernMode? WriteConcernMode { get; set; }

        /// <summary>
        /// Gets or sets the number of nodes that must acknowledge a write.
        /// </summary>
        /// <remarks>
        /// Use -1 for "w: majority".
        /// </remarks>
        public int? W { get; set; }

        /// <summary>
        /// Gets or sets the write timeout.
        /// </summary>
        public TimeSpan? WTimeout { get; set; }

        /// <summary>
        /// Gets or sets whether writes should be journaled.
        /// </summary>
        public bool? Journal { get; set; }

        /// <summary>
        /// Gets or sets the read preference mode.
        /// </summary>
        public ReadPreferenceMode? ReadPreferenceMode { get; set; }

        /// <summary>
        /// Gets or sets the maximum staleness for secondary reads.
        /// </summary>
        public TimeSpan? MaxStaleness { get; set; }

        /// <summary>
        /// Converts to MongoDB ReadConcern.
        /// </summary>
        public ReadConcern ToReadConcern()
        {
            return ReadConcernLevel switch
            {
                Concerns.ReadConcernLevel.Local => ReadConcern.Local,
                Concerns.ReadConcernLevel.Available => ReadConcern.Available,
                Concerns.ReadConcernLevel.Majority => ReadConcern.Majority,
                Concerns.ReadConcernLevel.Linearizable => ReadConcern.Linearizable,
                Concerns.ReadConcernLevel.Snapshot => ReadConcern.Snapshot,
                _ => ReadConcern.Default
            };
        }

        /// <summary>
        /// Converts to MongoDB WriteConcern.
        /// </summary>
        public WriteConcern ToWriteConcern()
        {
            if (WriteConcernMode.HasValue)
            {
                return WriteConcernMode.Value switch
                {
                    Concerns.WriteConcernMode.Unacknowledged => WriteConcern.Unacknowledged,
                    Concerns.WriteConcernMode.Acknowledged => WriteConcern.Acknowledged,
                    Concerns.WriteConcernMode.W1 => WriteConcern.W1,
                    Concerns.WriteConcernMode.W2 => WriteConcern.W2,
                    Concerns.WriteConcernMode.W3 => WriteConcern.W3,
                    Concerns.WriteConcernMode.Majority => WriteConcern.WMajority,
                    _ => WriteConcern.Acknowledged
                };
            }

            if (W.HasValue || Journal.HasValue || WTimeout.HasValue)
            {
                if (W.HasValue && W.Value == -1)
                {
                    return WriteConcern.WMajority.With(wTimeout: WTimeout, journal: Journal);
                }

                return new WriteConcern(W ?? 1, WTimeout, Journal, null);
            }

            return WriteConcern.Acknowledged;
        }

        /// <summary>
        /// Converts to MongoDB ReadPreference.
        /// </summary>
        public ReadPreference ToReadPreference()
        {
            if (!ReadPreferenceMode.HasValue)
            {
                return ReadPreference.Primary;
            }

            var basePreference = ReadPreferenceMode.Value switch
            {
                Concerns.ReadPreferenceMode.Primary => ReadPreference.Primary,
                Concerns.ReadPreferenceMode.PrimaryPreferred => ReadPreference.PrimaryPreferred,
                Concerns.ReadPreferenceMode.Secondary => ReadPreference.Secondary,
                Concerns.ReadPreferenceMode.SecondaryPreferred => ReadPreference.SecondaryPreferred,
                Concerns.ReadPreferenceMode.Nearest => ReadPreference.Nearest,
                _ => ReadPreference.Primary
            };

            if (MaxStaleness.HasValue)
            {
                return basePreference.With(maxStaleness: MaxStaleness.Value);
            }

            return basePreference;
        }
    }

    /// <summary>
    /// Read concern levels for MongoDB.
    /// </summary>
    public enum ReadConcernLevel
    {
        /// <summary>
        /// Returns data from the instance with no guarantee of replication.
        /// </summary>
        Local,

        /// <summary>
        /// Returns data available to the node with no guarantee of having been written to a majority.
        /// </summary>
        Available,

        /// <summary>
        /// Returns data that has been acknowledged by a majority of replica set members.
        /// </summary>
        Majority,

        /// <summary>
        /// Returns data that reflects all successful majority-acknowledged writes.
        /// </summary>
        Linearizable,

        /// <summary>
        /// Returns data from a consistent snapshot (requires transactions).
        /// </summary>
        Snapshot
    }

    /// <summary>
    /// Write concern modes for MongoDB.
    /// </summary>
    public enum WriteConcernMode
    {
        /// <summary>
        /// No acknowledgment of write operations.
        /// </summary>
        Unacknowledged,

        /// <summary>
        /// Acknowledgment that write reached the primary.
        /// </summary>
        Acknowledged,

        /// <summary>
        /// Acknowledgment from 1 node.
        /// </summary>
        W1,

        /// <summary>
        /// Acknowledgment from 2 nodes.
        /// </summary>
        W2,

        /// <summary>
        /// Acknowledgment from 3 nodes.
        /// </summary>
        W3,

        /// <summary>
        /// Acknowledgment from a majority of nodes.
        /// </summary>
        Majority
    }

    /// <summary>
    /// Read preference modes for MongoDB.
    /// </summary>
    public enum ReadPreferenceMode
    {
        /// <summary>
        /// Read from primary only.
        /// </summary>
        Primary,

        /// <summary>
        /// Read from primary, fall back to secondary.
        /// </summary>
        PrimaryPreferred,

        /// <summary>
        /// Read from secondary only.
        /// </summary>
        Secondary,

        /// <summary>
        /// Read from secondary, fall back to primary.
        /// </summary>
        SecondaryPreferred,

        /// <summary>
        /// Read from the nearest node by network latency.
        /// </summary>
        Nearest
    }

    /// <summary>
    /// Predefined concern configurations for common scenarios.
    /// </summary>
    public static class ConcernPresets
    {
        /// <summary>
        /// Maximum durability - majority write concern with journaling.
        /// </summary>
        public static MongoDbConcernOptions MaxDurability => new()
        {
            WriteConcernMode = WriteConcernMode.Majority,
            Journal = true,
            ReadConcernLevel = ReadConcernLevel.Majority
        };

        /// <summary>
        /// Maximum consistency - linearizable read, majority write.
        /// </summary>
        public static MongoDbConcernOptions MaxConsistency => new()
        {
            WriteConcernMode = WriteConcernMode.Majority,
            Journal = true,
            ReadConcernLevel = ReadConcernLevel.Linearizable
        };

        /// <summary>
        /// Maximum performance - local reads, w:1 writes.
        /// </summary>
        public static MongoDbConcernOptions MaxPerformance => new()
        {
            WriteConcernMode = WriteConcernMode.W1,
            Journal = false,
            ReadConcernLevel = ReadConcernLevel.Local,
            ReadPreferenceMode = ReadPreferenceMode.SecondaryPreferred
        };

        /// <summary>
        /// Balanced - acknowledged writes, local reads.
        /// </summary>
        public static MongoDbConcernOptions Balanced => new()
        {
            WriteConcernMode = WriteConcernMode.Acknowledged,
            ReadConcernLevel = ReadConcernLevel.Local
        };

        /// <summary>
        /// Fire and forget - unacknowledged writes (use with caution).
        /// </summary>
        public static MongoDbConcernOptions FireAndForget => new()
        {
            WriteConcernMode = WriteConcernMode.Unacknowledged,
            ReadConcernLevel = ReadConcernLevel.Local
        };

        /// <summary>
        /// Reporting/Analytics - secondary reads for load distribution.
        /// </summary>
        public static MongoDbConcernOptions Analytics => new()
        {
            ReadPreferenceMode = ReadPreferenceMode.SecondaryPreferred,
            MaxStaleness = TimeSpan.FromSeconds(90),
            ReadConcernLevel = ReadConcernLevel.Local
        };
    }
}

