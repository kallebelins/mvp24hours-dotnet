using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test;

[Trait("Category", "Unit")]
public class UnitOfWorkTest : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly string _databaseName = $"UowSync_{Guid.NewGuid():N}";

    public UnitOfWorkTest()
    {
        _provider = EfCoreTestHelpers.CreateSyncServices(_databaseName);
    }

    public void Dispose()
    {
        _provider.Dispose();
    }

    [Fact]
    public void DictionaryCtor_GetRepository_ShouldReturnSameInstance()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        var repository = new Repository<TestEntity>(context, EfCoreTestHelpers.CreateRepositoryOptions());
        var repositories = new Dictionary<Type, object> { [typeof(TestEntity)] = repository };
        using var unitOfWork = new UnitOfWork(context, repositories);

        IRepository<TestEntity> first = unitOfWork.GetRepository<TestEntity>();
        IRepository<TestEntity> second = unitOfWork.GetRepository<TestEntity>();

        first.Should().BeSameAs(repository);
        second.Should().BeSameAs(first);
    }

    [Fact]
    public void DictionaryCtor_GetRepository_WhenTypeMissing_ShouldThrow()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        using var unitOfWork = new UnitOfWork(context, new Dictionary<Type, object>());

        Action act = () => unitOfWork.GetRepository<TestEntity>();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*without a service provider*");
    }

    [Fact]
    public void ServiceProviderCtor_GetRepository_ShouldResolveIRepository()
    {
        using IServiceScope scope = _provider.CreateScope();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        IRepository<TestEntity> repository = unitOfWork.GetRepository<TestEntity>();

        repository.Should().NotBeNull();
        repository.Should().BeAssignableTo<IRepository<TestEntity>>();
    }

    [Fact]
    public void SaveChanges_ShouldPersistEntity()
    {
        using IServiceScope scope = _provider.CreateScope();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IRepository<TestEntity> repository = unitOfWork.GetRepository<TestEntity>();

        repository.Add(new TestEntity { Name = "Persisted", Active = true, Score = 10 });
        int rows = unitOfWork.SaveChanges();

        rows.Should().BeGreaterThan(0);
        repository.ListCount().Should().Be(1);
    }

    [Fact]
    public void SaveChanges_WithCancelledToken_ShouldReturnZeroAndDetachAdded()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        var repository = new Repository<TestEntity>(context, EfCoreTestHelpers.CreateRepositoryOptions());
        using var unitOfWork = new UnitOfWork(context, new Dictionary<Type, object>
        {
            [typeof(TestEntity)] = repository
        });

        var entity = new TestEntity { Name = "Cancelled" };
        context.Entities.Add(entity);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        int rows = unitOfWork.SaveChanges(cts.Token);

        rows.Should().Be(0);
        context.Entry(entity).State.Should().Be(EntityState.Detached);
    }

    [Fact]
    public void Rollback_ShouldResetAddedModifiedAndDeletedStates()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        using var unitOfWork = new UnitOfWork(context, new Dictionary<Type, object>());

        var modified = new TestEntity { Name = "Original", Active = true, Score = 1 };
        var deleted = new TestEntity { Name = "Deleted", Active = true, Score = 2 };
        context.Entities.AddRange(modified, deleted);
        context.SaveChanges();

        var added = new TestEntity { Name = "Added" };
        context.Entities.Add(added);
        context.Entry(added).State.Should().Be(EntityState.Added);

        modified.Name = "Changed";
        context.Entry(modified).State.Should().Be(EntityState.Modified);

        context.Entities.Remove(deleted);
        context.Entry(deleted).State.Should().Be(EntityState.Deleted);

        unitOfWork.Rollback();

        context.Entry(added).State.Should().Be(EntityState.Detached);
        context.Entry(modified).State.Should().Be(EntityState.Unchanged);
        modified.Name.Should().Be("Original");
        context.Entry(deleted).State.Should().Be(EntityState.Unchanged);
    }

    [Fact]
    public void GetConnection_ShouldReturnNonNull()
    {
        DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer("Server=localhost;Database=Mvp24HoursGetConnectionTest;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var context = new TestDbContext(options);
        using var unitOfWork = new UnitOfWork(context, new Dictionary<Type, object>());

        IDbConnection connection = unitOfWork.GetConnection();

        connection.Should().NotBeNull();
    }

    [Fact]
    public void Dispose_ShouldBeSafeToCallMultipleTimes()
    {
        TestDbContext context = EfCoreTestHelpers.CreateContext();
        var unitOfWork = new UnitOfWork(context, []);

        unitOfWork.Dispose();
        Action act = () => unitOfWork.Dispose();

        act.Should().NotThrow();
    }
}
