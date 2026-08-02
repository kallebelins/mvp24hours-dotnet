//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Integration.Test.Entities;
using Mvp24Hours.Application.Integration.Test.Fixtures;
using Mvp24Hours.Application.Integration.Test.Services;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Extensions;

namespace Mvp24Hours.Application.Integration.Test;

/// <summary>
/// Integration tests for RepositoryPagingServiceAsync using real SQL Server via Testcontainers.
/// These tests verify pagination operations with a real database.
/// </summary>
[Collection("SqlServer")]
[Trait("Category", "Integration")]
public class RepositoryPagingServiceIntegrationTest(SqlServerContainerFixture fixture) : IAsyncLifetime
{
    private readonly SqlServerContainerFixture _fixture = fixture;

    public async Task InitializeAsync()
    {
        if (!_fixture.IsAvailable)
        {
            return;
        }

        await _fixture.ClearDatabaseAsync();
        await SeedTestDataAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private async Task SeedTestDataAsync()
    {
        using IServiceScope scope = _fixture.CreateScope();
        CategoryService categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();
        ProductService productService = scope.ServiceProvider.GetRequiredService<ProductService>();

        // Create categories
        var electronics = new Category { Name = "Electronics", Description = "Electronic devices", IsActive = true };
        var clothing = new Category { Name = "Clothing", Description = "Apparel", IsActive = true };
        var books = new Category { Name = "Books", Description = "Literature", IsActive = true };

        await categoryService.AddAsync([electronics, clothing, books]);

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

    [DockerFact]
    public async Task ListWithPaginationAsync_FirstPage_ShouldReturnPagedResults()
    {
        // Arrange
        using IServiceScope scope = _fixture.CreateScope();
        ProductPagingService productPagingService = scope.ServiceProvider.GetRequiredService<ProductPagingService>();

        var pagingCriteria = new PagingCriteria(10, 0); // Page 1, 10 items

        // Act
        IPagingResult<IList<Product>> result = await productPagingService.ListWithPaginationAsync(pagingCriteria);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.HasData().Should().BeTrue();
        result.Paging.Should().NotBeNull();
        result.Paging!.Limit.Should().Be(10);
        result.Paging!.Offset.Should().Be(0);
    }

    [DockerFact]
    public async Task ListWithPaginationAsync_SecondPage_ShouldReturnDifferentResults()
    {
        // Arrange
        using IServiceScope scope = _fixture.CreateScope();
        ProductPagingService productPagingService = scope.ServiceProvider.GetRequiredService<ProductPagingService>();

        // Note: In PagingCriteria, offset is PAGE INDEX (0-based), not record offset
        // So page 0 = first page, page 1 = second page, etc.
        var firstPageCriteria = new PagingCriteria(10, 0);  // First page (10 items, page index 0)
        var secondPageCriteria = new PagingCriteria(10, 1); // Second page (10 items, page index 1)

        // Act
        IPagingResult<IList<Product>> firstPageResult = await productPagingService.ListWithPaginationAsync(firstPageCriteria);
        IPagingResult<IList<Product>> secondPageResult = await productPagingService.ListWithPaginationAsync(secondPageCriteria);

        // Assert
        firstPageResult.HasData().Should().BeTrue("First page should have data");
        secondPageResult.HasData().Should().BeTrue("Second page should have data (need at least 20 products)");

        var firstPageIds = firstPageResult.GetDataValue()!.Select(p => p.Id).ToList();
        var secondPageIds = secondPageResult.GetDataValue()!.Select(p => p.Id).ToList();

        firstPageIds.Should().NotIntersectWith(secondPageIds);
    }

    [DockerFact]
    public async Task ListWithPaginationAsync_WithDifferentPageSizes_ShouldRespectLimit()
    {
        // Arrange
        using IServiceScope scope = _fixture.CreateScope();
        ProductPagingService productPagingService = scope.ServiceProvider.GetRequiredService<ProductPagingService>();

        // Act
        IPagingResult<IList<Product>> result5 = await productPagingService.ListWithPaginationAsync(new PagingCriteria(5, 0));
        IPagingResult<IList<Product>> result15 = await productPagingService.ListWithPaginationAsync(new PagingCriteria(15, 0));
        IPagingResult<IList<Product>> result25 = await productPagingService.ListWithPaginationAsync(new PagingCriteria(25, 0));

        // Assert
        result5.GetDataValue()!.Count.Should().Be(5);
        result15.GetDataValue()!.Count.Should().Be(15);
        result25.GetDataValue()!.Count.Should().Be(25);
    }

    #endregion

    #region [ Filtered Pagination ]

    [DockerFact]
    public async Task GetByWithPaginationAsync_WithFilter_ShouldReturnFilteredPagedResults()
    {
        // Arrange
        using IServiceScope scope = _fixture.CreateScope();
        ProductPagingService productPagingService = scope.ServiceProvider.GetRequiredService<ProductPagingService>();

        var pagingCriteria = new PagingCriteria(10, 0);

        // Act
        IPagingResult<IList<Product>> result = await productPagingService.GetByWithPaginationAsync(
            p => p.IsActive,
            pagingCriteria);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.HasData().Should().BeTrue();
        result.GetDataValue()!.Should().AllSatisfy(p => p.IsActive.Should().BeTrue());
    }

    [DockerFact]
    public async Task GetByWithPaginationAsync_WithPriceFilter_ShouldReturnCorrectProducts()
    {
        // Arrange
        using IServiceScope scope = _fixture.CreateScope();
        ProductPagingService productPagingService = scope.ServiceProvider.GetRequiredService<ProductPagingService>();

        var pagingCriteria = new PagingCriteria(100, 0);

        // Act
        IPagingResult<IList<Product>> result = await productPagingService.GetByWithPaginationAsync(
            p => p.Price > 100m,
            pagingCriteria);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.HasData().Should().BeTrue();
        result.GetDataValue()!.Should().AllSatisfy(p => p.Price.Should().BeGreaterThan(100m));
    }

    [DockerFact]
    public async Task GetByWithPaginationAsync_WithCategoryFilter_ShouldReturnProductsFromCategory()
    {
        // Arrange
        using IServiceScope scope = _fixture.CreateScope();
        ProductPagingService productPagingService = scope.ServiceProvider.GetRequiredService<ProductPagingService>();
        CategoryService categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();

        IBusinessResult<IList<Category>> categoriesResult = await categoryService.GetByAsync(c => c.Name == "Electronics");
        int electronicsId = categoriesResult.GetDataValue()!.First().Id;

        var pagingCriteria = new PagingCriteria(10, 0);

        // Act
        IPagingResult<IList<Product>> result = await productPagingService.GetByWithPaginationAsync(
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

    [DockerFact]
    public async Task ListCountAsync_ShouldReturnTotalCount()
    {
        // Arrange
        using IServiceScope scope = _fixture.CreateScope();
        ProductPagingService productPagingService = scope.ServiceProvider.GetRequiredService<ProductPagingService>();

        // Act
        IBusinessResult<int> result = await productPagingService.ListCountAsync();

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.GetDataValue().Should().Be(50);
    }

    [DockerFact]
    public async Task GetByCountAsync_WithFilter_ShouldReturnFilteredCount()
    {
        // Arrange
        using IServiceScope scope = _fixture.CreateScope();
        ProductPagingService productPagingService = scope.ServiceProvider.GetRequiredService<ProductPagingService>();

        // Act
        IBusinessResult<int> activeCount = await productPagingService.GetByCountAsync(p => p.IsActive);
        IBusinessResult<int> inactiveCount = await productPagingService.GetByCountAsync(p => !p.IsActive);

        // Assert
        activeCount.GetDataValue().Should().Be(40); // 50 - (50/5) = 40 active
        inactiveCount.GetDataValue().Should().Be(10); // 50/5 = 10 inactive
    }

    #endregion

    #region [ Edge Cases ]

    [DockerFact]
    public async Task ListWithPaginationAsync_BeyondLastPage_ShouldReturnEmptyList()
    {
        // Arrange
        using IServiceScope scope = _fixture.CreateScope();
        ProductPagingService productPagingService = scope.ServiceProvider.GetRequiredService<ProductPagingService>();

        var pagingCriteria = new PagingCriteria(10, 1000); // Way beyond available data

        // Act
        IPagingResult<IList<Product>> result = await productPagingService.ListWithPaginationAsync(pagingCriteria);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.HasData().Should().BeFalse();
    }

    [DockerFact]
    public async Task ListWithPaginationAsync_LastPartialPage_ShouldReturnRemainingItems()
    {
        // Arrange
        using IServiceScope scope = _fixture.CreateScope();
        ProductPagingService productPagingService = scope.ServiceProvider.GetRequiredService<ProductPagingService>();

        // First verify we have 50 items
        IBusinessResult<int> totalCount = await productPagingService.ListCountAsync();
        totalCount.HasData().Should().BeTrue("Need test data to be seeded");
        totalCount.GetDataValue().Should().Be(50, "Should have 50 products for this test");

        // Note: In PagingCriteria, offset is PAGE INDEX (0-based), not record offset
        // Skip(limit * offset) = Skip(15 * 3) = Skip(45), so page index 3 gets last 5 items
        var pagingCriteria = new PagingCriteria(15, 3); // Page 4 (index 3), 15 items per page

        // Act
        IPagingResult<IList<Product>> result = await productPagingService.ListWithPaginationAsync(pagingCriteria);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.HasData().Should().BeTrue("Should have partial page data");
        result.GetDataValue()!.Count.Should().Be(5); // 50 - 45 = 5 remaining items
    }

    [DockerFact]
    public async Task ListWithPaginationAsync_SingleItemPage_ShouldWork()
    {
        // Arrange
        using IServiceScope scope = _fixture.CreateScope();
        ProductPagingService productPagingService = scope.ServiceProvider.GetRequiredService<ProductPagingService>();

        var pagingCriteria = new PagingCriteria(1, 0);

        // Act
        IPagingResult<IList<Product>> result = await productPagingService.ListWithPaginationAsync(pagingCriteria);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.GetDataValue()!.Count.Should().Be(1);
    }

    #endregion

    #region [ Complex Queries ]

    [DockerFact]
    public async Task GetByWithPaginationAsync_ComplexFilter_ShouldReturnCorrectResults()
    {
        // Arrange
        using IServiceScope scope = _fixture.CreateScope();
        ProductPagingService productPagingService = scope.ServiceProvider.GetRequiredService<ProductPagingService>();

        var pagingCriteria = new PagingCriteria(100, 0);

        // Act - Products that are active, price between 50 and 150, and stock > 100
        IPagingResult<IList<Product>> result = await productPagingService.GetByWithPaginationAsync(
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

    [DockerFact]
    public async Task GetByWithPaginationAsync_ContainsFilter_ShouldWork()
    {
        // Arrange
        using IServiceScope scope = _fixture.CreateScope();
        ProductPagingService productPagingService = scope.ServiceProvider.GetRequiredService<ProductPagingService>();

        var pagingCriteria = new PagingCriteria(100, 0);

        // Act
        IPagingResult<IList<Product>> result = await productPagingService.GetByWithPaginationAsync(
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
