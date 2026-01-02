//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace Mvp24Hours.Application.Integration.Test.Entities;

/// <summary>
/// Product entity for integration tests.
/// </summary>
public class Product : EntityBase<int>
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public bool IsActive { get; set; } = true;

    public int CategoryId { get; set; }

    public virtual Category? Category { get; set; }
}

