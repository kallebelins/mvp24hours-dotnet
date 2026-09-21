using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test;

[Trait("Category", "Unit")]
public class UnitOfWorkUnitTest
{
    [Fact]
    public void Constructor_WithNullDbContext_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new UnitOfWork(null!, new Dictionary<Type, object>());

        act.Should().Throw<ArgumentNullException>().WithParameterName("dbContext");
    }

    [Fact]
    public void Constructor_WithNullRepositoriesDictionary_ShouldThrowArgumentNullException()
    {
        Mvp24HoursContext context = MongoDbTestContextFactory.Create();

        Action act = () => _ = new UnitOfWork(context, (Dictionary<Type, object>)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("repositories");
    }

    [Fact]
    public void GetRepository_WithoutServiceProviderAndUnregisteredType_ShouldThrowInvalidOperationException()
    {
        Mvp24HoursContext context = MongoDbTestContextFactory.Create();
        var unitOfWork = new UnitOfWork(context, new Dictionary<Type, object>());

        Action act = () => unitOfWork.GetRepository<TestEntity>();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*without a service provider*");
    }

    [Fact]
    public void GetRepository_WithPreSeededRepository_ShouldReturnIt()
    {
        Mvp24HoursContext context = MongoDbTestContextFactory.Create();
        var repository = new TestableRepository(context, MongoDbTestContextFactory.CreateRepositoryOptions());
        var unitOfWork = new UnitOfWork(context, new Dictionary<Type, object> { [typeof(TestEntity)] = repository });

        global::Mvp24Hours.Core.Contract.Data.IRepository<TestEntity> resolved = unitOfWork.GetRepository<TestEntity>();

        resolved.Should().BeSameAs(repository);
    }

    [Fact]
    public void GetConnection_ShouldThrowNotSupportedException()
    {
        Mvp24HoursContext context = MongoDbTestContextFactory.Create();
        var unitOfWork = new UnitOfWork(context, new Dictionary<Type, object>());

#pragma warning disable CS0618 // intentional: exercising the obsolete-but-still-callable guard
        Action act = () => unitOfWork.GetConnection();
#pragma warning restore CS0618

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        Mvp24HoursContext context = MongoDbTestContextFactory.Create();
        var unitOfWork = new UnitOfWork(context, new Dictionary<Type, object>());

        Action act = unitOfWork.Dispose;

        act.Should().NotThrow();
    }
}
