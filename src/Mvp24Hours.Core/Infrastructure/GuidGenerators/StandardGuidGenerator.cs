//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure;
using System;

namespace Mvp24Hours.Core.Infrastructure.GuidGenerators
{
    /// <summary>
    /// Standard GUID generator using Guid.NewGuid().
    /// </summary>
    /// <remarks>
    /// This generator produces random version 4 UUIDs which are:
    /// - Globally unique
    /// - Unpredictable
    /// - Not sequential (can cause index fragmentation in databases)
    /// </remarks>
    public sealed class StandardGuidGenerator : IGuidGenerator
    {
        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static readonly StandardGuidGenerator Instance = new();

        /// <inheritdoc />
        public Guid NewGuid() => Guid.NewGuid();
    }
}

