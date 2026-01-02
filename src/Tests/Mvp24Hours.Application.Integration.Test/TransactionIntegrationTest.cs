//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Integration.Test.Data;
using Mvp24Hours.Application.Integration.Test.Entities;
using Mvp24Hours.Application.Integration.Test.Fixtures;
using Mvp24Hours.Application.Integration.Test.Services;
using Mvp24Hours.Extensions;

namespace Mvp24Hours.Application.Integration.Test;

/// <summary>
/// Integration tests for transaction handling using real SQL Server via Testcontainers.
/// </summary>
[Collection("SqlServer")]
public class TransactionIntegrationTest : IAsyncLifetime
{
    private readonly SqlServerContainerFixture _fixture;

    public TransactionIntegrationTest(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ClearDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    #region [ DbContext Transaction Tests ]

    [Fact]
    public async Task Transaction_RollbackOnError_ShouldNotPersist()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        var initialCount = await dbContext.Categories.CountAsync();

        // Act - Try to add and then throw exception within transaction
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var category = new Category
            {
                Name = "Transaction Test",
                Description = "Should be rolled back",
                IsActive = true
            };
            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync();

            // Simulate an error
            throw new InvalidOperationException("Simulated error");
        }
        catch (InvalidOperationException)
        {
            await transaction.RollbackAsync();
        }

        // Assert - Count should remain the same
        var finalCount = await dbContext.Categories.CountAsync();
        finalCount.Should().Be(initialCount);
    }

    [Fact]
    public async Task Transaction_CommitOnSuccess_ShouldPersist()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        var initialCount = await dbContext.Categories.CountAsync();

        // Act
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var category = new Category
            {
                Name = "Committed Transaction",
                Description = "Should persist",
                IsActive = true
            };
            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        // Assert
        var finalCount = await dbContext.Categories.CountAsync();
        finalCount.Should().Be(initialCount + 1);
    }

    [Fact]
    public async Task Transaction_NestedOperations_ShouldWorkCorrectly()
    {
        // Arrange
        using var scope = _fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        // Act
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            // Add category
            var category = new Category
            {
                Name = "Nested Transaction Category",
                Description = "Test",
                IsActive = true
            };
            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync();

            // Add products referencing the category
            var products = new List<Product>
            {
                new() { Name = "Nested P1", Description = "D1", Price = 10m, CategoryId = category.Id },
                new() { Name = "Nested P2", Description = "D2", Price = 20m, CategoryId = category.Id }
            };
            dbContext.Products.AddRange(products);
            await dbContext.SaveChangesAsync();

            await transaction.CommitAsync();

            // Verify
            var savedCategory = await dbContext.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Name == "Nested Transaction Category");

            savedCategory.Should().NotBeNull();
            savedCategory!.Products.Should().HaveCount(2);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    #endregion

    #region [ Concurrent Operations Tests ]

    [Fact]
    public async Task ConcurrentOperations_ShouldHandleIsolation()
    {
        // Arrange
        using var scope1 = _fixture.CreateScope();
        using var scope2 = _fixture.CreateScope();

        var service1 = scope1.ServiceProvider.GetRequiredService<CategoryService>();
        var service2 = scope2.ServiceProvider.GetRequiredService<CategoryService>();

        // Act - Add categories concurrently
        var task1 = service1.AddAsync(new Category { Name = "Concurrent 1", IsActive = true });
        var task2 = service2.AddAsync(new Category { Name = "Concurrent 2", IsActive = true });

        await Task.WhenAll(task1, task2);

        // Assert
        var result1 = await task1;
        var result2 = await task2;

        result1.HasErrors.Should().BeFalse();
        result2.HasErrors.Should().BeFalse();

        // Verify both were saved
        using var verifyScope = _fixture.CreateScope();
        var verifyService = verifyScope.ServiceProvider.GetRequiredService<CategoryService>();
        var allResult = await verifyService.GetByAsync(c => c.Name.StartsWith("Concurrent"));
        allResult.GetDataValue()!.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    #endregion
}
