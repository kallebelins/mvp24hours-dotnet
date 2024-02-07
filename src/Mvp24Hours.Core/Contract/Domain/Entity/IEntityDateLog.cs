//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Core.Contract.Domain.Entity
{
    /// <summary>
    /// Represents an entity with data to log
    ///  <see cref="Mvp24Hours.Core.Contract.Domain.Entity.IEntityBase"/>
    /// </summary>
    public interface IEntityDateLog
    {
        /// <summary>
        /// Entity creation date
        /// </summary>
        DateTime Created { get; set; }
        /// <summary>
        /// Entity modification date
        /// </summary>
        DateTime? Modified { get; set; }
        /// <summary>
        /// Entity logical exclusion date
        /// </summary>
        DateTime? Removed { get; set; }
    }
}
