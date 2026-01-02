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
/// Integration tests for RepositoryServiceAsync using real SQL Server via Testcontainers.
/// These tests verify CRUD operations with a real database.
/// </summary>
[Collection("SqlServer")]
public class RepositoryServiceIntegrationTest : IAsyncLifetime
{
    private readonly SqlServerContainerFixture _fixture;

    public RepositoryServiceIntegrationTest(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ClearDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    #region [ Add Operations ]

    [Fact]
    public async Task AddAsync_SingleEntity_ShouldPersistInDatabase()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();

        var category = new Category
        {
            Name = "Electronics",
            Description = "Electronic devices and accessories",
            IsActive = true
        };

        // Act
        var result = await categoryService.AddAsync(category);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        category.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AddAsync_MultipleEntities_ShouldPersistAllInDatabase()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();

        var categories = new List<Category>
        {
            new() { Name = "Books", Description = "Books and literature", IsActive = true },
            new() { Name = "Games", Description = "Video games and board games", IsActive = true },
            new() { Name = "Sports", Description = "Sports equipment", IsActive = true }
        };

        // Act
        var result = await categoryService.AddAsync(categories);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        categories.Should().AllSatisfy(c => c.Id.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task AddAsync_EntityWithRelationship_ShouldPersistWithChildren()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();
        var productService = scope.ServiceProvider.GetRequiredService<ProductService>();

        // First add category
        var category = new Category
        {
            Name = "Clothing",
            Description = "Apparel and fashion",
            IsActive = true
        };
        await categoryService.AddAsync(category);

        // Then add product with category
        var product = new Product
        {
            Name = "T-Shirt",
            Description = "Cotton T-Shirt",
            Price = 29.99m,
            StockQuantity = 100,
            IsActive = true,
            CategoryId = category.Id
        };

        // Act
        var result = await productService.AddAsync(product);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        product.Id.Should().BeGreaterThan(0);
        product.CategoryId.Should().Be(category.Id);
    }

    #endregion

    #region [ Get Operations ]

    [Fact]
    public async Task GetByIdAsync_ExistingEntity_ShouldReturnEntity()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();

        var category = new Category
        {
            Name = "Furniture",
            Description = "Home furniture",
            IsActive = true
        };
        await categoryService.AddAsync(category);

        // Act
        var result = await categoryService.GetByIdAsync(category.Id);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.HasData().Should().BeTrue();
        result.GetDataValue()!.Id.Should().Be(category.Id);
        result.GetDataValue()!.Name.Should().Be("Furniture");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingEntity_ShouldReturnNullData()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();

        // Act
        var result = await categoryService.GetByIdAsync(999999);

        // Assert
        result.Should().NotBeNull();
        result.HasData().Should().BeFalse();
    }

    [Fact]
    public async Task ListAsync_WithData_ShouldReturnAllEntities()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();

        var categories = new List<Category>
        {
            new() { Name = "Category A", IsActive = true },
            new() { Name = "Category B", IsActive = true },
            new() { Name = "Category C", IsActive = true }
        };
        await categoryService.AddAsync(categories);

        // Act
        var result = await categoryService.ListAsync();

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.HasData().Should().BeTrue();
        result.GetDataValue()!.Count.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task GetByAsync_WithExpression_ShouldReturnFilteredEntities()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();

        var categories = new List<Category>
        {
            new() { Name = "Active 1", IsActive = true },
            new() { Name = "Active 2", IsActive = true },
            new() { Name = "Inactive", IsActive = false }
        };
        await categoryService.AddAsync(categories);

        // Act
        var result = await categoryService.GetByAsync(c => c.IsActive);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.HasData().Should().BeTrue();
        result.GetDataValue()!.Should().AllSatisfy(c => c.IsActive.Should().BeTrue());
    }

    #endregion

    #region [ Modify Operations ]

    [Fact]
    public async Task ModifyAsync_ExistingEntity_ShouldUpdateInDatabase()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();

        var category = new Category
        {
            Name = "Original Name",
            Description = "Original Description",
            IsActive = true
        };
        await categoryService.AddAsync(category);

        // Act
        category.Name = "Updated Name";
        category.Description = "Updated Description";
        var result = await categoryService.ModifyAsync(category);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();

        // Verify in database
        var updatedResult = await categoryService.GetByIdAsync(category.Id);
        updatedResult.GetDataValue()!.Name.Should().Be("Updated Name");
        updatedResult.GetDataValue()!.Description.Should().Be("Updated Description");
    }

    [Fact]
    public async Task ModifyAsync_MultipleEntities_ShouldUpdateAllInDatabase()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();

        var categories = new List<Category>
        {
            new() { Name = "Cat 1", IsActive = true },
            new() { Name = "Cat 2", IsActive = true }
        };
        await categoryService.AddAsync(categories);

        // Act
        foreach (var cat in categories)
        {
            cat.Name += " - Updated";
        }
        var result = await categoryService.ModifyAsync(categories);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();

        // Verify
        var allResult = await categoryService.GetByAsync(c => c.Name.Contains("Updated"));
        allResult.GetDataValue()!.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    #endregion

    #region [ Remove Operations ]

    [Fact]
    public async Task RemoveByIdAsync_ExistingEntity_ShouldDeleteFromDatabase()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();

        var category = new Category
        {
            Name = "To Be Deleted",
            IsActive = true
        };
        await categoryService.AddAsync(category);
        var categoryId = category.Id;

        // Act
        var result = await categoryService.RemoveByIdAsync(categoryId);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();

        // Verify deletion
        var getResult = await categoryService.GetByIdAsync(categoryId);
        getResult.HasData().Should().BeFalse();
    }

    [Fact]
    public async Task RemoveAsync_EntityInstance_ShouldDeleteFromDatabase()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();

        var category = new Category
        {
            Name = "Another To Delete",
            IsActive = true
        };
        await categoryService.AddAsync(category);

        // Act
        var result = await categoryService.RemoveAsync(category);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();

        // Verify deletion
        var getResult = await categoryService.GetByIdAsync(category.Id);
        getResult.HasData().Should().BeFalse();
    }

    #endregion

    #region [ ListAny and ListCount Operations ]

    [Fact]
    public async Task ListAnyAsync_WithData_ShouldReturnTrue()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();

        var category = new Category { Name = "Test Category", IsActive = true };
        await categoryService.AddAsync(category);

        // Act
        var result = await categoryService.ListAnyAsync();

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.GetDataValue().Should().BeTrue();
    }

    [Fact]
    public async Task GetByAnyAsync_WithExpression_ShouldReturnCorrectResult()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();

        var category = new Category { Name = "Special Category", IsActive = true };
        await categoryService.AddAsync(category);

        // Act
        var existsResult = await categoryService.GetByAnyAsync(c => c.Name == "Special Category");
        var notExistsResult = await categoryService.GetByAnyAsync(c => c.Name == "Non Existent Name XYZ");

        // Assert
        existsResult.GetDataValue().Should().BeTrue();
        notExistsResult.GetDataValue().Should().BeFalse();
    }

    [Fact]
    public async Task ListCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();

        var initialCountResult = await categoryService.ListCountAsync();
        var initialCount = initialCountResult.GetDataValue();

        var categories = new List<Category>
        {
            new() { Name = "Count Test 1", IsActive = true },
            new() { Name = "Count Test 2", IsActive = true },
            new() { Name = "Count Test 3", IsActive = true }
        };
        await categoryService.AddAsync(categories);

        // Act
        var result = await categoryService.ListCountAsync();

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.GetDataValue().Should().Be(initialCount + 3);
    }

    [Fact]
    public async Task GetByCountAsync_WithExpression_ShouldReturnFilteredCount()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();

        var categories = new List<Category>
        {
            new() { Name = "CountFilter Active", IsActive = true },
            new() { Name = "CountFilter Inactive 1", IsActive = false },
            new() { Name = "CountFilter Inactive 2", IsActive = false }
        };
        await categoryService.AddAsync(categories);

        // Act
        var activeCount = await categoryService.GetByCountAsync(c => c.Name.StartsWith("CountFilter") && c.IsActive);
        var inactiveCount = await categoryService.GetByCountAsync(c => c.Name.StartsWith("CountFilter") && !c.IsActive);

        // Assert
        activeCount.GetDataValue().Should().BeGreaterThanOrEqualTo(1);
        inactiveCount.GetDataValue().Should().BeGreaterThanOrEqualTo(2);
    }

    #endregion

    #region [ Paging with ListAsync ]

    [Fact]
    public async Task ListAsync_WithPaging_ShouldRespectLimit()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();

        var categories = new List<Category>();
        for (int i = 0; i < 10; i++)
        {
            categories.Add(new Category { Name = $"Paging Cat {i}", IsActive = true });
        }
        await categoryService.AddAsync(categories);

        var paging = new PagingCriteria(5, 0);

        // Act
        var result = await categoryService.ListAsync(paging);

        // Assert
        result.Should().NotBeNull();
        result.HasErrors.Should().BeFalse();
        result.HasDataCount(5).Should().BeTrue();
    }

    #endregion
}
