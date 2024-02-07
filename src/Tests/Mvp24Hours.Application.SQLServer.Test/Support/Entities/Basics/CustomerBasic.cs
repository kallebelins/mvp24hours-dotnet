//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Entity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Mvp24Hours.Application.SQLServer.Test.Support.Entities.Basics
{
    public class CustomerBasic : IEntityBase
    {
        [JsonIgnore]
        [IgnoreDataMember]
        public object EntityKey => Id; // class instance identifier

        public CustomerBasic()
        {
            Contacts = new List<ContactBasic>();
        }

        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public bool Active { get; set; }

        // collections

        public ICollection<ContactBasic> Contacts { get; set; }

    }
}
