//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Integration.Test.Entities;
using Mvp24Hours.Application.Integration.Test.Fixtures;
using Mvp24Hours.Application.Integration.Test.Services;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;

namespace Mvp24Hours.Application.Integration.Test;

/// <summary>
/// Basic PostgreSQL integration smoke test using Testcontainers.
/// </summary>
[Collection("PostgreSql")]
[Trait("Category", "Integration")]
public class PostgreSqlRepositoryIntegrationTest(PostgreSqlContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgreSqlContainerFixture _fixture = fixture;

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
    public async Task AddAsync_Category_ShouldPersistInPostgreSql()
    {
        using IServiceScope scope = _fixture.CreateScope();
        CategoryService categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();

        var category = new Category
        {
            Name = "PostgreSQL Category",
            Description = "Created via Testcontainers",
            IsActive = true
        };

        IBusinessResult<int> result = await categoryService.AddAsync(category);

        result.HasErrors.Should().BeFalse();
        category.Id.Should().BeGreaterThan(0);
    }
}
