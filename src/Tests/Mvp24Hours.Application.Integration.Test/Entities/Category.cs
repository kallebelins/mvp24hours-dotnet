//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.ComponentModel.DataAnnotations;
using Mvp24Hours.Core.Entities;

namespace Mvp24Hours.Application.Integration.Test.Entities;

/// <summary>
/// Category entity for integration tests.
/// </summary>
public class Category : EntityBase<int>
{
    public Category()
    {
        Products = [];
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

