using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test;

[Trait("Category", "Unit")]
public class UnitOfWorkAsyncTest : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly string _databaseName = $"UowAsync_{Guid.NewGuid():N}";

    public UnitOfWorkAsyncTest()
    {
        _provider = EfCoreTestHelpers.CreateAsyncServices(_databaseName);
    }

    public void Dispose() => _provider.Dispose();

    [Fact]
    public void DictionaryCtor_GetRepository_ShouldReturnSameInstance()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        var repository = new RepositoryAsync<TestEntity>(context, EfCoreTestHelpers.CreateRepositoryOptions());
        var repositories = new Dictionary<Type, object> { [typeof(TestEntity)] = repository };
        using var unitOfWork = new UnitOfWorkAsync(context, repositories);

        IRepositoryAsync<TestEntity> first = unitOfWork.GetRepository<TestEntity>();
        IRepositoryAsync<TestEntity> second = unitOfWork.GetRepository<TestEntity>();

        first.Should().BeSameAs(repository);
        second.Should().BeSameAs(first);
    }

    [Fact]
    public void DictionaryCtor_GetRepository_WhenTypeMissing_ShouldThrow()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        using var unitOfWork = new UnitOfWorkAsync(context, new Dictionary<Type, object>());

        Action act = () => unitOfWork.GetRepository<TestEntity>();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*without a service provider*");
    }

    [Fact]
    public void ServiceProviderCtor_GetRepository_ShouldResolveIRepositoryAsync()
    {
        using IServiceScope scope = _provider.CreateScope();
        IUnitOfWorkAsync unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();

        IRepositoryAsync<TestEntity> repository = unitOfWork.GetRepository<TestEntity>();

        repository.Should().NotBeNull();
        repository.Should().BeAssignableTo<IRepositoryAsync<TestEntity>>();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldPersistEntity()
    {
        using IServiceScope scope = _provider.CreateScope();
        IUnitOfWorkAsync unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();
        IRepositoryAsync<TestEntity> repository = unitOfWork.GetRepository<TestEntity>();

        await repository.AddAsync(new TestEntity { Name = "PersistedAsync", Active = true, Score = 10 });
        int rows = await unitOfWork.SaveChangesAsync();

        rows.Should().BeGreaterThan(0);
        (await repository.ListCountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SaveChangesAsync_WithCancelledToken_ShouldReturnZeroAndDetachAdded()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        var repository = new RepositoryAsync<TestEntity>(context, EfCoreTestHelpers.CreateRepositoryOptions());
        using var unitOfWork = new UnitOfWorkAsync(context, new Dictionary<Type, object>
        {
            [typeof(TestEntity)] = repository
        });

        var entity = new TestEntity { Name = "CancelledAsync" };
        context.Entities.Add(entity);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        int rows = await unitOfWork.SaveChangesAsync(cts.Token);

        rows.Should().Be(0);
        context.Entry(entity).State.Should().Be(EntityState.Detached);
    }

    [Fact]
    public async Task RollbackAsync_ShouldResetAddedModifiedAndDeletedStates()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        using var unitOfWork = new UnitOfWorkAsync(context, new Dictionary<Type, object>());

        var modified = new TestEntity { Name = "OriginalAsync", Active = true, Score = 1 };
        var deleted = new TestEntity { Name = "DeletedAsync", Active = true, Score = 2 };
        context.Entities.AddRange(modified, deleted);
        await context.SaveChangesAsync();

        var added = new TestEntity { Name = "AddedAsync" };
        context.Entities.Add(added);
        context.Entry(added).State.Should().Be(EntityState.Added);

        modified.Name = "ChangedAsync";
        context.Entry(modified).State.Should().Be(EntityState.Modified);

        context.Entities.Remove(deleted);
        context.Entry(deleted).State.Should().Be(EntityState.Deleted);

        await unitOfWork.RollbackAsync();

        context.Entry(added).State.Should().Be(EntityState.Detached);
        context.Entry(modified).State.Should().Be(EntityState.Unchanged);
        modified.Name.Should().Be("OriginalAsync");
        context.Entry(deleted).State.Should().Be(EntityState.Unchanged);
    }

    [Fact]
    public void GetConnection_ShouldReturnNonNull()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer("Server=localhost;Database=Mvp24HoursGetConnectionAsyncTest;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var context = new TestDbContext(options);
        using var unitOfWork = new UnitOfWorkAsync(context, new Dictionary<Type, object>());

        IDbConnection connection = unitOfWork.GetConnection();

        connection.Should().NotBeNull();
    }

    [Fact]
    public void Dispose_ShouldBeSafeToCallMultipleTimes()
    {
        TestDbContext context = EfCoreTestHelpers.CreateContext();
        var unitOfWork = new UnitOfWorkAsync(context, new Dictionary<Type, object>());

        unitOfWork.Dispose();
        Action act = () => unitOfWork.Dispose();

        act.Should().NotThrow();
    }
}
