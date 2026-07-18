//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Linq.Expressions;
using Mvp24Hours.Application.Integration.Test.Entities;
using Mvp24Hours.Core.Contract.Domain.Specifications;

namespace Mvp24Hours.Application.Integration.Test.Specifications;

/// <summary>
/// Specification for active products.
/// </summary>
public class ActiveProductSpecification : ISpecificationQuery<Product>
{
    public Expression<Func<Product, bool>> IsSatisfiedByExpression =>
        p => p.IsActive;
}

/// <summary>
/// Specification for products in a price range.
/// </summary>
public class PriceRangeSpecification(decimal minPrice, decimal maxPrice) : ISpecificationQuery<Product>
{
    private readonly decimal _minPrice = minPrice;
    private readonly decimal _maxPrice = maxPrice;

    public Expression<Func<Product, bool>> IsSatisfiedByExpression =>
        p => p.Price >= _minPrice && p.Price <= _maxPrice;
}

/// <summary>
/// Specification for products with low stock.
/// </summary>
public class LowStockSpecification(int threshold = 50) : ISpecificationQuery<Product>
{
    private readonly int _threshold = threshold;

    public Expression<Func<Product, bool>> IsSatisfiedByExpression =>
        p => p.StockQuantity < _threshold;
}

