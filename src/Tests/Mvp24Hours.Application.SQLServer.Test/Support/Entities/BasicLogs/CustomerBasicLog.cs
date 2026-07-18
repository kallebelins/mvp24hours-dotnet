//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.ComponentModel.DataAnnotations;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Entities;

namespace Mvp24Hours.Application.SQLServer.Test.Support.Entities.BasicLogs;

public class CustomerBasicLog : EntityBase<int>, IEntityDateLog
{
    public CustomerBasicLog()
    {
        Contacts = [];
    }

    [Required]
    public DateTime Created { get; set; }

    public DateTime? Modified { get; set; }

    public DateTime? Removed { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public bool Active { get; set; }


    // collections

    public ICollection<ContactBasicLog> Contacts { get; set; }
}
