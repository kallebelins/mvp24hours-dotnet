//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Infrastructure.Migrations
{
    /// <summary>
    /// Represents a migration history entry stored in MongoDB.
    /// </summary>
    /// <remarks>
    /// This entity is stored in the "_migrations" collection and tracks
    /// which migrations have been applied to the database.
    /// </remarks>
    public sealed class MongoDbMigrationHistory
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the migration version number.
        /// </summary>
        [BsonElement("version")]
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets the migration description.
        /// </summary>
        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the fully qualified type name of the migration class.
        /// </summary>
        [BsonElement("typeName")]
        public string TypeName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date and time when the migration was applied.
        /// </summary>
        [BsonElement("appliedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime AppliedAt { get; set; }

        /// <summary>
        /// Gets or sets the duration of the migration execution in milliseconds.
        /// </summary>
        [BsonElement("durationMs")]
        public long DurationMs { get; set; }

        /// <summary>
        /// Gets or sets the user or service that applied the migration.
        /// </summary>
        [BsonElement("appliedBy")]
        public string? AppliedBy { get; set; }

        /// <summary>
        /// Gets or sets the machine name where the migration was applied.
        /// </summary>
        [BsonElement("machineName")]
        public string? MachineName { get; set; }

        /// <summary>
        /// Gets or sets any error that occurred during migration.
        /// </summary>
        [BsonElement("error")]
        [BsonIgnoreIfNull]
        public string? Error { get; set; }

        /// <summary>
        /// Gets or sets the migration status.
        /// </summary>
        [BsonElement("status")]
        public MigrationStatus Status { get; set; }
    }

    /// <summary>
    /// Status of a migration execution.
    /// </summary>
    public enum MigrationStatus
    {
        /// <summary>
        /// Migration is pending execution.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Migration is currently running.
        /// </summary>
        Running = 1,

        /// <summary>
        /// Migration completed successfully.
        /// </summary>
        Completed = 2,

        /// <summary>
        /// Migration failed during execution.
        /// </summary>
        Failed = 3,

        /// <summary>
        /// Migration was rolled back.
        /// </summary>
        RolledBack = 4
    }
}

