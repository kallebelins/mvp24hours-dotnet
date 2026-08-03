//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Integration.Test.Data;
using Mvp24Hours.Application.Integration.Test.Entities;
using Mvp24Hours.Application.Integration.Test.Fixtures;
using Mvp24Hours.Application.Integration.Test.Services;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Infrastructure.Data.EFCore.Extensions;

namespace Mvp24Hours.Application.Integration.Test;

/// <summary>
/// Integration tests for bulk ExecuteUpdate/ExecuteDelete against real SQL Server.
/// Replaces InMemory-skipped tests in EFCore and SQLServer test projects.
/// </summary>
[Collection("SqlServer")]
[Trait("Category", "Integration")]
public class BulkOperationsIntegrationTest(SqlServerContainerFixture fixture) : IAsyncLifetime
{
    private readonly SqlServerContainerFixture _fixture = fixture;

    public Task InitializeAsync()
    {
        if (!_fixture.IsAvailable)
        {
            return Task.CompletedTask;
        }

        return _fixture.ClearDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [DockerFact]
    public async Task ExecuteUpdateAsync_ShouldUpdateMatchingProducts()
    {
        using IServiceScope scope = _fixture.CreateScope();
        IBulkOperationsRepositoryAsync<Product> repository =
            scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<Product>>();
        CategoryService categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();

        var category = new Category { Name = "BulkCat", Description = "Bulk category", IsActive = true };
        await categoryService.AddAsync(category);

        List<Product> products =
        [
            new() { Name = "Active-1", Description = "d", Price = 1, StockQuantity = 10, IsActive = true, CategoryId = category.Id },
            new() { Name = "Active-2", Description = "d", Price = 2, StockQuantity = 20, IsActive = true, CategoryId = category.Id },
            new() { Name = "Inactive-1", Description = "d", Price = 3, StockQuantity = 30, IsActive = false, CategoryId = category.Id }
        ];
        await repository.BulkInsertAsync(products);

        int rowsAffected = await repository.ExecuteUpdateAsync(
            p => p.IsActive == true,
            p => p.StockQuantity,
            0);

        rowsAffected.Should().Be(2);

        using IServiceScope verifyScope = _fixture.CreateScope();
        IBulkOperationsRepositoryAsync<Product> verifyRepository =
            verifyScope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<Product>>();
        IList<Product> updated = await verifyRepository.GetByAsync(p => p.Name == "Active-1");
        updated.Should().ContainSingle();
        updated[0].StockQuantity.Should().Be(0);
    }

    [DockerFact]
    public async Task ExecuteDeleteAsync_ShouldDeleteMatchingProducts()
    {
        using IServiceScope scope = _fixture.CreateScope();
        IBulkOperationsRepositoryAsync<Product> repository =
            scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<Product>>();
        CategoryService categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();

        var category = new Category { Name = "DeleteCat", Description = "Delete category", IsActive = true };
        await categoryService.AddAsync(category);

        List<Product> products =
        [
            new() { Name = "Keep-1", Description = "d", Price = 1, StockQuantity = 1, IsActive = true, CategoryId = category.Id },
            new() { Name = "Remove-1", Description = "d", Price = 2, StockQuantity = 2, IsActive = false, CategoryId = category.Id },
            new() { Name = "Remove-2", Description = "d", Price = 3, StockQuantity = 3, IsActive = false, CategoryId = category.Id }
        ];
        await repository.BulkInsertAsync(products);

        int rowsAffected = await repository.ExecuteDeleteAsync(p => !p.IsActive);

        rowsAffected.Should().Be(2);
        (await repository.ListCountAsync()).Should().Be(1);
    }

    [DockerFact]
    public async Task ExecuteDeleteAsync_WithNoMatches_ShouldReturnZero()
    {
        using IServiceScope scope = _fixture.CreateScope();
        IBulkOperationsRepositoryAsync<Product> repository =
            scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<Product>>();

        int rowsAffected = await repository.ExecuteDeleteAsync(p => p.Name == "NonExistentProduct");

        rowsAffected.Should().Be(0);
    }

    [DockerFact]
    public async Task DbContextExtension_ExecuteDeleteAsync_ShouldDeleteMatchingProducts()
    {
        using IServiceScope scope = _fixture.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        CategoryService categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();

        var category = new Category { Name = "ExtCat", Description = "Extension category", IsActive = true };
        await categoryService.AddAsync(category);

        List<Product> products =
        [
            new() { Name = "Ext-Keep", Description = "d", Price = 1, StockQuantity = 1, IsActive = true, CategoryId = category.Id },
            new() { Name = "Ext-Remove", Description = "d", Price = 2, StockQuantity = 2, IsActive = false, CategoryId = category.Id }
        ];
        await dbContext.BulkInsertAsync(products);
        await dbContext.SaveChangesAsync();

        int rowsAffected = await dbContext.ExecuteDeleteAsync<Product>(p => p.Name.StartsWith("Ext-Remove"));

        rowsAffected.Should().Be(1);
        dbContext.Products.Count(p => p.Name.StartsWith("Ext-")).Should().Be(1);
    }
}
