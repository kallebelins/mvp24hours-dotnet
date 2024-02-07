//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Mvp24Hours.Application.SQLServer.Test.Support.Entities.BasicLogs
{
    public class CustomerBasicLog : EntityBase<int>, IEntityDateLog
    {
        public CustomerBasicLog()
        {
            Contacts = new List<ContactBasicLog>();
        }

        [Required]
        public DateTime Created { get; set; }

        public DateTime? Modified { get; set; }

        public DateTime? Removed { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public bool Active { get; set; }


        // collections

        public ICollection<ContactBasicLog> Contacts { get; set; }
    }
}
