//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Integration.Test.Entities;
using Mvp24Hours.Application.Integration.Test.Fixtures;
using Mvp24Hours.Application.Integration.Test.Services;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Extensions;

namespace Mvp24Hours.Application.Integration.Test;

/// <summary>
/// Integration tests for RepositoryPagingServiceAsync using real SQL Server via Testcontainers.
/// These tests verify pagination operations with a real database.
/// </summary>
[Collection("SqlServer")]
public class RepositoryPagingServiceIntegrationTest : IAsyncLifetime
{
    private readonly SqlServerContainerFixture _fixture;

    public RepositoryPagingServiceIntegrationTest(SqlServerContainerFixture fixture)
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

        // Create categories
        var electronics = new Category { Name = "Electronics", Description = "Electronic devices", IsActive = true };
        var clothing = new Category { Name = "Clothing", Description = "Apparel", IsActive = true };
        var books = new Category { Name = "Books", Description = "Literature", IsActive = true };

        await categoryService.AddAsync(new List<Category> { electronics, clothing, books });

        // Create 50 products for pagination testing
        var products = new List<Product>();
        for (int i = 1; i <= 50; i++)
        {
            products.Add(new Product
            {
                Name = $"Product {i:D3}",
                Description = $"Description for product {i}",
                Price = 10m + (i * 5m),
                StockQuantity = i * 10,
                IsActive = i % 5 != 0, // Every 5th product is inactive
                CategoryId = i <= 20 ? electronics.Id : (i <= 35 ? clothing.Id : books.Id)
            });
        }
        await productService.AddAsync(products);
    }

    #region [ Basic Pagination ]

    [Fact]
    public async Task ListWithPaginationAsync_FirstPage_ShouldReturnPagedResults()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var productPagingService = scope.ServiceProvider.GetRequiredService<ProductPagingService>();

        var pagingCriteria = new PagingCriteria(10, 0); // Page 1, 10 items

        // Act
        var result = await productPagingService.ListWithPaginationAsync(pagingCriteria);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.HasData().Should().BeTrue();
        result.Paging.Should().NotBeNull();
        result.Paging!.Limit.Should().Be(10);
        result.Paging!.Offset.Should().Be(0);
    }

    [Fact]
    public async Task ListWithPaginationAsync_SecondPage_ShouldReturnDifferentResults()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var productPagingService = scope.ServiceProvider.GetRequiredService<ProductPagingService>();

        // Note: In PagingCriteria, offset is PAGE INDEX (0-based), not record offset
        // So page 0 = first page, page 1 = second page, etc.
        var firstPageCriteria = new PagingCriteria(10, 0);  // First page (10 items, page index 0)
        var secondPageCriteria = new PagingCriteria(10, 1); // Second page (10 items, page index 1)

        // Act
        var firstPageResult = await productPagingService.ListWithPaginationAsync(firstPageCriteria);
        var secondPageResult = await productPagingService.ListWithPaginationAsync(secondPageCriteria);

        // Assert
        firstPageResult.HasData().Should().BeTrue("First page should have data");
        secondPageResult.HasData().Should().BeTrue("Second page should have data (need at least 20 products)");

        var firstPageIds = firstPageResult.GetDataValue()!.Select(p => p.Id).ToList();
        var secondPageIds = secondPageResult.GetDataValue()!.Select(p => p.Id).ToList();

        firstPageIds.Should().NotIntersectWith(secondPageIds);
    }

    [Fact]
    public async Task ListWithPaginationAsync_WithDifferentPageSizes_ShouldRespectLimit()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var productPagingService = scope.ServiceProvider.GetRequiredService<ProductPagingService>();

        // Act
        var result5 = await productPagingService.ListWithPaginationAsync(new PagingCriteria(5, 0));
        var result15 = await productPagingService.ListWithPaginationAsync(new PagingCriteria(15, 0));
        var result25 = await productPagingService.ListWithPaginationAsync(new PagingCriteria(25, 0));

        // Assert
        result5.GetDataValue()!.Count.Should().Be(5);
        result15.GetDataValue()!.Count.Should().Be(15);
        result25.GetDataValue()!.Count.Should().Be(25);
    }

    #endregion

    #region [ Filtered Pagination ]

    [Fact]
    public async Task GetByWithPaginationAsync_WithFilter_ShouldReturnFilteredPagedResults()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var productPagingService = scope.ServiceProvider.GetRequiredService<ProductPagingService>();

        var pagingCriteria = new PagingCriteria(10, 0);

        // Act
        var result = await productPagingService.GetByWithPaginationAsync(
            p => p.IsActive,
            pagingCriteria);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.HasData().Should().BeTrue();
        result.GetDataValue()!.Should().AllSatisfy(p => p.IsActive.Should().BeTrue());
    }

    [Fact]
    public async Task GetByWithPaginationAsync_WithPriceFilter_ShouldReturnCorrectProducts()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var productPagingService = scope.ServiceProvider.GetRequiredService<ProductPagingService>();

        var pagingCriteria = new PagingCriteria(100, 0);

        // Act
        var result = await productPagingService.GetByWithPaginationAsync(
            p => p.Price > 100m,
            pagingCriteria);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.HasData().Should().BeTrue();
        result.GetDataValue()!.Should().AllSatisfy(p => p.Price.Should().BeGreaterThan(100m));
    }

    [Fact]
    public async Task GetByWithPaginationAsync_WithCategoryFilter_ShouldReturnProductsFromCategory()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var productPagingService = scope.ServiceProvider.GetRequiredService<ProductPagingService>();
        var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();

        var categoriesResult = await categoryService.GetByAsync(c => c.Name == "Electronics");
        var electronicsId = categoriesResult.GetDataValue()!.First().Id;

        var pagingCriteria = new PagingCriteria(10, 0);

        // Act
        var result = await productPagingService.GetByWithPaginationAsync(
            p => p.CategoryId == electronicsId,
            pagingCriteria);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.HasData().Should().BeTrue();
        result.GetDataValue()!.Should().AllSatisfy(p => p.CategoryId.Should().Be(electronicsId));
    }

    #endregion

    #region [ Counting ]

    [Fact]
    public async Task ListCountAsync_ShouldReturnTotalCount()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var productPagingService = scope.ServiceProvider.GetRequiredService<ProductPagingService>();

        // Act
        var result = await productPagingService.ListCountAsync();

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.GetDataValue().Should().Be(50);
    }

    [Fact]
    public async Task GetByCountAsync_WithFilter_ShouldReturnFilteredCount()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var productPagingService = scope.ServiceProvider.GetRequiredService<ProductPagingService>();

        // Act
        var activeCount = await productPagingService.GetByCountAsync(p => p.IsActive);
        var inactiveCount = await productPagingService.GetByCountAsync(p => !p.IsActive);

        // Assert
        activeCount.GetDataValue().Should().Be(40); // 50 - (50/5) = 40 active
        inactiveCount.GetDataValue().Should().Be(10); // 50/5 = 10 inactive
    }

    #endregion

    #region [ Edge Cases ]

    [Fact]
    public async Task ListWithPaginationAsync_BeyondLastPage_ShouldReturnEmptyList()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var productPagingService = scope.ServiceProvider.GetRequiredService<ProductPagingService>();

        var pagingCriteria = new PagingCriteria(10, 1000); // Way beyond available data

        // Act
        var result = await productPagingService.ListWithPaginationAsync(pagingCriteria);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.HasData().Should().BeFalse();
    }

    [Fact]
    public async Task ListWithPaginationAsync_LastPartialPage_ShouldReturnRemainingItems()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var productPagingService = scope.ServiceProvider.GetRequiredService<ProductPagingService>();

        // First verify we have 50 items
        var totalCount = await productPagingService.ListCountAsync();
        totalCount.HasData().Should().BeTrue("Need test data to be seeded");
        totalCount.GetDataValue().Should().Be(50, "Should have 50 products for this test");

        // Note: In PagingCriteria, offset is PAGE INDEX (0-based), not record offset
        // Skip(limit * offset) = Skip(15 * 3) = Skip(45), so page index 3 gets last 5 items
        var pagingCriteria = new PagingCriteria(15, 3); // Page 4 (index 3), 15 items per page

        // Act
        var result = await productPagingService.ListWithPaginationAsync(pagingCriteria);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.HasData().Should().BeTrue("Should have partial page data");
        result.GetDataValue()!.Count.Should().Be(5); // 50 - 45 = 5 remaining items
    }

    [Fact]
    public async Task ListWithPaginationAsync_SingleItemPage_ShouldWork()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var productPagingService = scope.ServiceProvider.GetRequiredService<ProductPagingService>();

        var pagingCriteria = new PagingCriteria(1, 0);

        // Act
        var result = await productPagingService.ListWithPaginationAsync(pagingCriteria);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.GetDataValue()!.Count.Should().Be(1);
    }

    #endregion

    #region [ Complex Queries ]

    [Fact]
    public async Task GetByWithPaginationAsync_ComplexFilter_ShouldReturnCorrectResults()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var productPagingService = scope.ServiceProvider.GetRequiredService<ProductPagingService>();

        var pagingCriteria = new PagingCriteria(100, 0);

        // Act - Products that are active, price between 50 and 150, and stock > 100
        var result = await productPagingService.GetByWithPaginationAsync(
            p => p.IsActive && p.Price >= 50m && p.Price <= 150m && p.StockQuantity > 100,
            pagingCriteria);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.HasData().Should().BeTrue();
        result.GetDataValue()!.Should().AllSatisfy(p =>
        {
            p.IsActive.Should().BeTrue();
            p.Price.Should().BeInRange(50m, 150m);
            p.StockQuantity.Should().BeGreaterThan(100);
        });
    }

    [Fact]
    public async Task GetByWithPaginationAsync_ContainsFilter_ShouldWork()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var productPagingService = scope.ServiceProvider.GetRequiredService<ProductPagingService>();

        var pagingCriteria = new PagingCriteria(100, 0);

        // Act
        var result = await productPagingService.GetByWithPaginationAsync(
            p => p.Name.Contains("01"), // Product 01, 010, etc.
            pagingCriteria);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.HasData().Should().BeTrue();
        result.GetDataValue()!.Should().AllSatisfy(p => p.Name.Should().Contain("01"));
    }

    #endregion
}
