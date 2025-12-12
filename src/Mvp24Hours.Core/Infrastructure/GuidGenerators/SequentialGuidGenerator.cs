//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure;
using System;
using System.Security.Cryptography;

namespace Mvp24Hours.Core.Infrastructure.GuidGenerators
{
    /// <summary>
    /// Generates sequential GUIDs that are database-friendly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Sequential GUIDs help reduce index fragmentation in databases because
    /// new values are always greater than previous ones, resulting in sequential
    /// insertions at the end of the index.
    /// </para>
    /// <para>
    /// The generation strategy depends on the target database:
    /// - SQL Server: Timestamp bytes at the end (compatible with clustered indexes)
    /// - PostgreSQL/MySQL: Timestamp bytes at the beginning
    /// - Binary: Timestamp bytes at the end (for byte-array comparisons)
    /// </para>
    /// <para>
    /// Note: Sequential GUIDs are still globally unique but are predictable.
    /// Do not use them for security-sensitive purposes.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // For SQL Server
    /// var generator = new SequentialGuidGenerator(SequentialGuidType.SqlServer);
    /// var id = generator.NewGuid();
    /// 
    /// // For PostgreSQL or MySQL
    /// var pgGenerator = new SequentialGuidGenerator(SequentialGuidType.String);
    /// var pgId = pgGenerator.NewGuid();
    /// </code>
    /// </example>
    public sealed class SequentialGuidGenerator : IGuidGenerator
    {
        private static readonly object _lock = new();
        private readonly SequentialGuidType _type;

        /// <summary>
        /// Creates a new sequential GUID generator.
        /// </summary>
        /// <param name="type">The type of sequential GUID to generate.</param>
        public SequentialGuidGenerator(SequentialGuidType type = SequentialGuidType.SqlServer)
        {
            _type = type;
        }

        /// <summary>
        /// Singleton instances for common database types.
        /// </summary>
        public static SequentialGuidGenerator SqlServer => new(SequentialGuidType.SqlServer);
        public static SequentialGuidGenerator PostgreSql => new(SequentialGuidType.String);
        public static SequentialGuidGenerator MySql => new(SequentialGuidType.String);
        public static SequentialGuidGenerator Binary => new(SequentialGuidType.Binary);

        /// <inheritdoc />
        public Guid NewGuid()
        {
            return NewSequentialGuid(_type);
        }

        /// <summary>
        /// Generates a new sequential GUID.
        /// </summary>
        /// <param name="type">The type of sequential GUID to generate.</param>
        /// <returns>A new sequential GUID.</returns>
        public static Guid NewSequentialGuid(SequentialGuidType type = SequentialGuidType.SqlServer)
        {
            // Start with a random GUID to ensure uniqueness even across machines
            byte[] randomBytes = new byte[10];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            // Get timestamp for sequential ordering
            // Using ticks ensures high precision ordering
            long timestamp = DateTime.UtcNow.Ticks / 10000L; // Convert to milliseconds for better distribution
            byte[] timestampBytes = BitConverter.GetBytes(timestamp);

            // Ensure proper byte order (big-endian for consistent sorting)
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(timestampBytes);
            }

            byte[] guidBytes = new byte[16];

            switch (type)
            {
                case SequentialGuidType.String:
                case SequentialGuidType.Binary:
                    // Timestamp at the beginning for string/binary sorting
                    // Format: [timestamp 6 bytes][random 10 bytes]
                    Buffer.BlockCopy(timestampBytes, 2, guidBytes, 0, 6);
                    Buffer.BlockCopy(randomBytes, 0, guidBytes, 6, 10);
                    break;

                case SequentialGuidType.SqlServer:
                default:
                    // Timestamp at the end for SQL Server clustered index optimization
                    // SQL Server compares GUIDs starting from the last 6 bytes
                    // Format: [random 10 bytes][timestamp 6 bytes]
                    Buffer.BlockCopy(randomBytes, 0, guidBytes, 0, 10);
                    Buffer.BlockCopy(timestampBytes, 2, guidBytes, 10, 6);
                    break;
            }

            return new Guid(guidBytes);
        }
    }

    /// <summary>
    /// Specifies the type of sequential GUID generation strategy.
    /// </summary>
    public enum SequentialGuidType
    {
        /// <summary>
        /// Sequential GUIDs for SQL Server.
        /// Timestamp bytes are at the end for optimal clustered index performance.
        /// </summary>
        SqlServer,

        /// <summary>
        /// Sequential GUIDs that sort correctly as strings.
        /// Timestamp bytes are at the beginning.
        /// Use for PostgreSQL, MySQL, and other databases that compare GUIDs as strings.
        /// </summary>
        String,

        /// <summary>
        /// Sequential GUIDs that sort correctly in binary form.
        /// Timestamp bytes are at the beginning.
        /// </summary>
        Binary
    }
}

