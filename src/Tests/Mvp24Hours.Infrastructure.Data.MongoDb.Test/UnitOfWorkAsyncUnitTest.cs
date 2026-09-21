using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test;

[Trait("Category", "Unit")]
public class UnitOfWorkAsyncUnitTest
{
    [Fact]
    public void Constructor_WithNullDbContext_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new UnitOfWorkAsync(null!, new Dictionary<Type, object>());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullRepositoriesDictionary_ShouldThrowArgumentNullException()
    {
        Mvp24HoursContext context = MongoDbTestContextFactory.Create();

        Action act = () => _ = new UnitOfWorkAsync(context, (Dictionary<Type, object>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithoutRepositoriesOrServiceProvider_ShouldInitializeEmptyRepositoryMap()
    {
        Mvp24HoursContext context = MongoDbTestContextFactory.Create();

        Action act = () => _ = new UnitOfWorkAsync(context);

        act.Should().NotThrow();
    }

    [Fact]
    public void GetRepository_WithoutServiceProviderAndUnregisteredType_ShouldThrowInvalidOperationException()
    {
        Mvp24HoursContext context = MongoDbTestContextFactory.Create();
        var unitOfWork = new UnitOfWorkAsync(context, new Dictionary<Type, object>());

        Action act = () => unitOfWork.GetRepository<TestEntity>();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*without a service provider*");
    }

    [Fact]
    public void GetRepository_WithPreSeededRepository_ShouldReturnIt()
    {
        Mvp24HoursContext context = MongoDbTestContextFactory.Create();
        var repository = new TestableRepositoryAsync(context, MongoDbTestContextFactory.CreateRepositoryOptions());
        var unitOfWork = new UnitOfWorkAsync(context, new Dictionary<Type, object> { [typeof(TestEntity)] = repository });

        global::Mvp24Hours.Core.Contract.Data.IRepositoryAsync<TestEntity> resolved = unitOfWork.GetRepository<TestEntity>();

        resolved.Should().BeSameAs(repository);
    }

    [Fact]
    public void GetConnection_ShouldThrowNotSupportedException()
    {
        Mvp24HoursContext context = MongoDbTestContextFactory.Create();
        var unitOfWork = new UnitOfWorkAsync(context, new Dictionary<Type, object>());

        // UnitOfWorkAsync.GetConnection() is marked [Obsolete(error: true)], so it cannot be
        // called directly from C# source; invoke it via reflection instead to exercise the
        // NotSupportedException guard body.
        Action act = () =>
        {
            try
            {
                unitOfWork.GetType().GetMethod("GetConnection")!.Invoke(unitOfWork, null);
            }
            catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        };

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        Mvp24HoursContext context = MongoDbTestContextFactory.Create();
        var unitOfWork = new UnitOfWorkAsync(context, new Dictionary<Type, object>());

        Action act = unitOfWork.Dispose;

        act.Should().NotThrow();
    }
}
