using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Infrastructure.Data.EFCore.Testing;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test;

[Trait("Category", "Unit")]
public sealed class UnitOfWorkFakeTest
{
    [Fact]
    public void SaveChanges_ShouldTrackCallCountAndCommittedChanges()
    {
        UnitOfWorkFake unitOfWork = new();
        RepositoryFake<SampleEntity> repository = unitOfWork.GetFakeRepository<SampleEntity>();
        repository.Add(new SampleEntity { Id = 1 });

        int changes = unitOfWork.SaveChanges();

        changes.Should().BeGreaterThan(0);
        unitOfWork.SaveChangesCallCount.Should().Be(1);
        unitOfWork.TotalChanges.Should().Be(changes);
    }

    [Fact]
    public void SaveChanges_WhenConfiguredToThrow_ShouldPropagateException()
    {
        UnitOfWorkFake unitOfWork = new()
        {
            SaveChangesException = new InvalidOperationException("boom")
        };

        Action act = () => unitOfWork.SaveChanges();

        act.Should().Throw<InvalidOperationException>().WithMessage("boom");
    }

    [Fact]
    public void Rollback_ShouldIncrementRollbackCounter()
    {
        UnitOfWorkFake unitOfWork = new();

        unitOfWork.Rollback();

        unitOfWork.RollbackCallCount.Should().Be(1);
    }

    [Fact]
    public void RegisterRepository_ShouldReturnRegisteredInstance()
    {
        UnitOfWorkFake unitOfWork = new();
        RepositoryFake<SampleEntity> repository = new();

        unitOfWork.RegisterRepository(repository);

        unitOfWork.GetRepository<SampleEntity>().Should().BeSameAs(repository);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldTrackAsyncCallCount()
    {
        UnitOfWorkFakeAsync unitOfWork = new();
        RepositoryFakeAsync<SampleEntity> repository = unitOfWork.GetFakeRepository<SampleEntity>();
        await repository.AddAsync(new SampleEntity { Id = 2 });

        int changes = await unitOfWork.SaveChangesAsync();

        changes.Should().BeGreaterThan(0);
        unitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task RollbackAsync_ShouldIncrementRollbackCounter()
    {
        UnitOfWorkFakeAsync unitOfWork = new();

        await unitOfWork.RollbackAsync();

        unitOfWork.RollbackCallCount.Should().Be(1);
    }

    private sealed class SampleEntity : IEntityBase
    {
        public int Id { get; set; }

        public object? EntityKey => Id;
    }
}
