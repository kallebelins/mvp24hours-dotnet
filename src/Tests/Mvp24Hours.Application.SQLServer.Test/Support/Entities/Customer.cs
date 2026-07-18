//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.ComponentModel.DataAnnotations;
using Mvp24Hours.Core.Entities;

namespace Mvp24Hours.Application.SQLServer.Test.Support.Entities;

public class Customer : EntityBase<int>
{
    public Customer()
    {
        Contacts = [];
    }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public bool Active { get; set; }

    // collections

    public ICollection<Contact> Contacts { get; set; }
}
