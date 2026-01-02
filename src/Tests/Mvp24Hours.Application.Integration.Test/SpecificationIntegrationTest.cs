//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Integration.Test.Entities;
using Mvp24Hours.Application.Integration.Test.Fixtures;
using Mvp24Hours.Application.Integration.Test.Services;
using Mvp24Hours.Application.Integration.Test.Specifications;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Extensions;

namespace Mvp24Hours.Application.Integration.Test;

/// <summary>
/// Integration tests for Specification Pattern using real SQL Server via Testcontainers.
/// </summary>
[Collection("SqlServer")]
public class SpecificationIntegrationTest : IAsyncLifetime
{
    private readonly SqlServerContainerFixture _fixture;

    public SpecificationIntegrationTest(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ClearDatabaseAsync();
        await SeedTestDataAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task SeedTestDataAsync()
    {
        using var scope = _fixture.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();
        var productService = scope.ServiceProvider.GetRequiredService<ProductService>();

        // Create a category
        var category = new Category { Name = "Spec Test Category", IsActive = true };
        await categoryService.AddAsync(category);

        // Create products with varying attributes
        var products = new List<Product>
        {
            // Active products
            new() { Name = "Active Cheap", Description = "Low price", Price = 25m, StockQuantity = 200, IsActive = true, CategoryId = category.Id },
            new() { Name = "Active Mid", Description = "Mid price", Price = 75m, StockQuantity = 100, IsActive = true, CategoryId = category.Id },
            new() { Name = "Active Expensive", Description = "High price", Price = 150m, StockQuantity = 30, IsActive = true, CategoryId = category.Id },
            new() { Name = "Active Premium", Description = "Premium price", Price = 300m, StockQuantity = 10, IsActive = true, CategoryId = category.Id },
            
            // Inactive products
            new() { Name = "Inactive Cheap", Description = "Low price inactive", Price = 20m, StockQuantity = 500, IsActive = false, CategoryId = category.Id },
            new() { Name = "Inactive Expensive", Description = "High price inactive", Price = 200m, StockQuantity = 5, IsActive = false, CategoryId = category.Id },
        };

        await productService.AddAsync(products);
    }

    #region [ ActiveProductSpecification Tests ]

    [Fact]
    public async Task ActiveProductSpecification_ShouldReturnOnlyActiveProducts()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var productService = scope.ServiceProvider.GetRequiredService<ProductService>();
        var specification = new ActiveProductSpecification();

        // Act
        var result = await productService.GetByAsync(specification.IsSatisfiedByExpression);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.HasData().Should().BeTrue();
        result.GetDataValue()!.Should().HaveCount(4);
        result.GetDataValue()!.Should().AllSatisfy(p => p.IsActive.Should().BeTrue());
    }

    [Fact]
    public async Task ActiveProductSpecification_Expression_ShouldFilterCorrectly()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var productService = scope.ServiceProvider.GetRequiredService<ProductService>();
        var specification = new ActiveProductSpecification();

        // Act
        var countResult = await productService.GetByCountAsync(specification.IsSatisfiedByExpression);

        // Assert
        countResult.GetDataValue().Should().Be(4);
    }

    #endregion

    #region [ PriceRangeSpecification Tests ]

    [Fact]
    public async Task PriceRangeSpecification_ShouldReturnProductsInRange()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var productService = scope.ServiceProvider.GetRequiredService<ProductService>();
        var specification = new PriceRangeSpecification(50m, 200m);

        // Act
        var result = await productService.GetByAsync(specification.IsSatisfiedByExpression);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.HasData().Should().BeTrue();
        result.GetDataValue()!.Should().AllSatisfy(p =>
        {
            p.Price.Should().BeGreaterThanOrEqualTo(50m);
            p.Price.Should().BeLessThanOrEqualTo(200m);
        });
    }

    [Theory]
    [InlineData(0, 30, 2)]     // Cheap products (25, 20)
    [InlineData(100, 200, 2)]  // Mid-range (150, 200)
    [InlineData(250, 500, 1)]  // Premium (300)
    [InlineData(500, 1000, 0)] // None in this range
    public async Task PriceRangeSpecification_VariousRanges_ShouldReturnCorrectCount(
        decimal minPrice, decimal maxPrice, int expectedCount)
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var productService = scope.ServiceProvider.GetRequiredService<ProductService>();
        var specification = new PriceRangeSpecification(minPrice, maxPrice);

        // Act
        var countResult = await productService.GetByCountAsync(specification.IsSatisfiedByExpression);

        // Assert
        countResult.GetDataValue().Should().Be(expectedCount);
    }

    #endregion

    #region [ LowStockSpecification Tests ]

    [Fact]
    public async Task LowStockSpecification_DefaultThreshold_ShouldReturnLowStockProducts()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var productService = scope.ServiceProvider.GetRequiredService<ProductService>();
        var specification = new LowStockSpecification(); // Default threshold = 50

        // Act
        var result = await productService.GetByAsync(specification.IsSatisfiedByExpression);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.HasData().Should().BeTrue();
        // Products with stock < 50: Active Expensive (30), Active Premium (10), Inactive Expensive (5)
        result.GetDataValue()!.Should().HaveCount(3);
        result.GetDataValue()!.Should().AllSatisfy(p => p.StockQuantity.Should().BeLessThan(50));
    }

    [Theory]
    [InlineData(10, 1)]   // Only "Inactive Expensive" with 5 stock
    [InlineData(50, 3)]   // 30, 10, 5
    [InlineData(100, 3)]  // 30, 10, 5 (100 is not < 100)
    [InlineData(150, 4)]  // 100, 30, 10, 5 (Stock < 150)
    [InlineData(1000, 6)] // All products
    public async Task LowStockSpecification_CustomThreshold_ShouldReturnCorrectCount(
        int threshold, int expectedCount)
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var productService = scope.ServiceProvider.GetRequiredService<ProductService>();
        var specification = new LowStockSpecification(threshold);

        // Act
        var countResult = await productService.GetByCountAsync(specification.IsSatisfiedByExpression);

        // Assert
        countResult.GetDataValue().Should().Be(expectedCount);
    }

    #endregion

    #region [ Combined Specifications Tests ]

    [Fact]
    public async Task CombinedSpecifications_ActiveAndPriceRange_ShouldWork()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var productService = scope.ServiceProvider.GetRequiredService<ProductService>();

        // Act
        var result = await productService.GetByAsync(p => p.IsActive && p.Price >= 50m && p.Price <= 200m);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.HasData().Should().BeTrue();
        result.GetDataValue()!.Should().AllSatisfy(p =>
        {
            p.IsActive.Should().BeTrue();
            p.Price.Should().BeInRange(50m, 200m);
        });
    }

    [Fact]
    public async Task CombinedSpecifications_ActiveAndLowStock_ShouldWork()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var productService = scope.ServiceProvider.GetRequiredService<ProductService>();

        // Active products with low stock (< 50)
        // Act
        var result = await productService.GetByAsync(p => p.IsActive && p.StockQuantity < 50);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.HasData().Should().BeTrue();
        // Active products with stock < 50: Active Expensive (30), Active Premium (10)
        result.GetDataValue()!.Should().HaveCount(2);
        result.GetDataValue()!.Should().AllSatisfy(p =>
        {
            p.IsActive.Should().BeTrue();
            p.StockQuantity.Should().BeLessThan(50);
        });
    }

    [Fact]
    public async Task CombinedSpecifications_AllThree_ShouldWork()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var productService = scope.ServiceProvider.GetRequiredService<ProductService>();

        // Active, price 50-200, low stock < 50
        // Act
        var result = await productService.GetByAsync(
            p => p.IsActive && p.Price >= 50m && p.Price <= 200m && p.StockQuantity < 50);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.HasData().Should().BeTrue();
        // Only "Active Expensive" (Price=150, Stock=30, Active=true)
        result.GetDataValue()!.Should().HaveCount(1);
        result.GetDataValue()!.First().Name.Should().Be("Active Expensive");
    }

    #endregion

    #region [ Specification with Pagination ]

    [Fact]
    public async Task SpecificationWithPagination_ShouldRespectBothFilters()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var productPagingService = scope.ServiceProvider.GetRequiredService<ProductPagingService>();
        var pagingCriteria = new PagingCriteria(2, 0);

        // Act
        var result = await productPagingService.GetByWithPaginationAsync(
            p => p.IsActive,
            pagingCriteria);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.GetDataValue()!.Should().HaveCount(2); // Respects page size
        result.GetDataValue()!.Should().AllSatisfy(p => p.IsActive.Should().BeTrue()); // Respects specification
    }

    #endregion
}
