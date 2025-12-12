//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
namespace Mvp24Hours.Core.Contract.Domain.Entity
{
    /// <summary>
    /// Represents an entity with data to log
    ///  <see cref="Mvp24Hours.Core.Contract.Domain.Entity.IEntityBase"/>
    /// </summary>
    public interface IEntityLog<TForeignKey> : IEntityDateLog
    {
        /// <summary>
        /// Registration of who requested the creation of this entity
        /// </summary>
        TForeignKey CreatedBy { get; set; }
        /// <summary>
        /// Registration of who requested the modification of this entity
        /// </summary>
        TForeignKey? ModifiedBy { get; set; }
        /// <summary>
        /// Record of who requested the logical exclusion of that entity
        /// </summary>
        TForeignKey? RemovedBy { get; set; }
    }
}
