//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace Mvp24Hours.Application.Integration.Test.Entities;

/// <summary>
/// Category entity for integration tests.
/// </summary>
public class Category : EntityBase<int>
{
    public Category()
    {
        Products = new List<Product>();
    }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation property
    public virtual ICollection<Product> Products { get; set; }
}

