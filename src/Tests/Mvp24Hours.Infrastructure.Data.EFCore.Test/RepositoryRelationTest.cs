using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;
using Mvp24Hours.Infrastructure.Data.EFCore.Testing;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test;

[Trait("Category", "Unit")]
public class RepositoryRelationTest : IDisposable
{
    private readonly ServiceProvider _provider;

    public RepositoryRelationTest()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursInMemoryDbContext<TestRelationDbContext>($"Relation_{Guid.NewGuid():N}");
        services.AddMvp24HoursRepository(o => o.MaxQtyByQueryPage = 100);
        services.AddMvp24HoursRepositoryAsync(o => o.MaxQtyByQueryPage = 100);
        _provider = services.BuildServiceProvider();
        SeedRelationData();
    }

    public void Dispose()
    {
        _provider.Dispose();
    }

    [Fact]
    public void LoadRelation_Reference_ShouldLoadParentFromChild()
    {
        using IServiceScope scope = _provider.CreateScope();
        IRepository<TestChildEntity> repository = scope.ServiceProvider.GetRequiredService<IRepository<TestChildEntity>>();
        TestChildEntity child = repository.GetById(1)!;

        repository.LoadRelation(child, c => c.Parent);

        child.Parent.Should().NotBeNull();
        child.Parent.Name.Should().Be("Parent-A");
    }

    [Fact]
    public void LoadRelation_CollectionWithFilterAndLimit_ShouldLoadFilteredChildren()
    {
        using IServiceScope scope = _provider.CreateScope();
        IRepository<TestParentEntity> repository = scope.ServiceProvider.GetRequiredService<IRepository<TestParentEntity>>();
        TestParentEntity parent = repository.GetById(1)!;

        repository.LoadRelation(parent, p => p.Children, c => c.SortOrder >= 2, limit: 1);

        parent.Children.Should().HaveCount(1);
        parent.Children.Single().Label.Should().Be("Child-2");
    }

    [Fact]
    public void LoadRelationSortByAscending_ShouldOrderChildren()
    {
        using IServiceScope scope = _provider.CreateScope();
        IRepository<TestParentEntity> repository = scope.ServiceProvider.GetRequiredService<IRepository<TestParentEntity>>();
        TestParentEntity parent = repository.GetById(1)!;

        repository.LoadRelationSortByAscending(parent, p => p.Children, c => c.SortOrder);

        parent.Children.Select(c => c.SortOrder).Should().BeInAscendingOrder();
    }

    [Fact]
    public void LoadRelationSortByDescending_ShouldOrderChildrenDescending()
    {
        using IServiceScope scope = _provider.CreateScope();
        IRepository<TestParentEntity> repository = scope.ServiceProvider.GetRequiredService<IRepository<TestParentEntity>>();
        TestParentEntity parent = repository.GetById(1)!;

        repository.LoadRelationSortByDescending(parent, p => p.Children, c => c.SortOrder);

        parent.Children.Select(c => c.SortOrder).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task LoadRelationAsync_Reference_ShouldLoadParentFromChild()
    {
        using IServiceScope scope = _provider.CreateScope();
        IRepositoryAsync<TestChildEntity> repository = scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestChildEntity>>();
        TestChildEntity child = (await repository.GetByIdAsync(1))!;

        await repository.LoadRelationAsync(child, c => c.Parent);

        child.Parent.Should().NotBeNull();
        child.Parent.Name.Should().Be("Parent-A");
    }

    private void SeedRelationData()
    {
        using IServiceScope scope = _provider.CreateScope();
        TestRelationDbContext context = scope.ServiceProvider.GetRequiredService<TestRelationDbContext>();
        var parent = new TestParentEntity { Name = "Parent-A" };
        parent.Children.Add(new TestChildEntity { Label = "Child-1", SortOrder = 1 });
        parent.Children.Add(new TestChildEntity { Label = "Child-2", SortOrder = 2 });
        parent.Children.Add(new TestChildEntity { Label = "Child-3", SortOrder = 3 });
        context.Parents.Add(parent);
        context.SaveChanges();
    }
}
