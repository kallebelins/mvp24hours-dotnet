//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Mvp24Hours.Core.Entities
{
    /// <summary>
    /// Represents an entity
    /// </summary>
    public abstract class EntityBase<TKey> : IEntityBase
    {
        #region [ Primitive members ]

        /// <summary>
        /// Entity identifier used as a reference by the interface
        /// </summary>
        [JsonIgnore]
        [IgnoreDataMember]
        public virtual object? EntityKey => this.Id;
        /// <summary>
        /// Entity identifier
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [DataMember]
        public virtual TKey Id { get; set; } = default!;

        #endregion
    }
}
