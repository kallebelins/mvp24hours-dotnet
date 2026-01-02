//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Integration.Test.Entities;
using Mvp24Hours.Core.Contract.Domain.Specifications;
using System.Linq.Expressions;

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
public class PriceRangeSpecification : ISpecificationQuery<Product>
{
    private readonly decimal _minPrice;
    private readonly decimal _maxPrice;

    public PriceRangeSpecification(decimal minPrice, decimal maxPrice)
    {
        _minPrice = minPrice;
        _maxPrice = maxPrice;
    }

    public Expression<Func<Product, bool>> IsSatisfiedByExpression =>
        p => p.Price >= _minPrice && p.Price <= _maxPrice;
}

/// <summary>
/// Specification for products with low stock.
/// </summary>
public class LowStockSpecification : ISpecificationQuery<Product>
{
    private readonly int _threshold;

    public LowStockSpecification(int threshold = 50)
    {
        _threshold = threshold;
    }

    public Expression<Func<Product, bool>> IsSatisfiedByExpression =>
        p => p.StockQuantity < _threshold;
}

