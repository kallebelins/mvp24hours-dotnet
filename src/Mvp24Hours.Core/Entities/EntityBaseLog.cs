//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Entity;
using System;

namespace Mvp24Hours.Core.Entities
{
    /// <summary>
    /// Represents an entity with characteristics for logging (creation, modification and logical exclusion)
    /// </summary>
    /// <typeparam name="TKey">Represents entity</typeparam>
    /// <typeparam name="TForeignKey">Represents data type used to log</typeparam>
    public abstract class EntityBaseLog<TKey, TForeignKey> : EntityBase<TKey>, IEntityLog<TForeignKey>
    {
        #region [ Log ]
        /// <summary>
        /// Creation date
        /// </summary>
        public DateTime Created { get; set; }
        /// <summary>
        /// Application or database user who created this record
        /// </summary>
        public TForeignKey CreatedBy { get; set; } = default!;
        /// <summary>
        /// Modified date
        /// </summary>
        public DateTime? Modified { get; set; }
        /// <summary>
        /// Application or database user who modified this record
        /// </summary>
        public TForeignKey? ModifiedBy { get; set; }
        /// <summary>
        /// Logical exclusion date
        /// </summary>
        public DateTime? Removed { get; set; }
        /// <summary>
        /// Application or database user who logically deleted this record
        /// </summary>
        public TForeignKey? RemovedBy { get; set; }
        #endregion
    }
}
