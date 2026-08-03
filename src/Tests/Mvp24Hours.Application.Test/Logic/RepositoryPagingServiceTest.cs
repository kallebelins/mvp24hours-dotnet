using System.Linq.Expressions;
using Mvp24Hours.Application.Test.Support;
using Mvp24Hours.Core.ValueObjects.Logic;

namespace Mvp24Hours.Application.Test.Logic;

[Trait("Category", "Unit")]
public class RepositoryPagingServiceTest
{
    [Fact]
    public void Constructor_WithValidUnitOfWork_ShouldNotThrow()
    {
        (Mock<IUnitOfWork> uow, _) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();

        Action act = () => _ = new TestSyncRepositoryPagingService(uow.Object);

        act.Should().NotThrow();
    }

    [Fact]
    public void ListWithPagination_ShouldReturnPagedResult()
    {
        var items = new List<AppTestEntity>
        {
            new() { Id = 1, Name = "A" },
            new() { Id = 2, Name = "B" },
            new() { Id = 3, Name = "C" }
        };
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupListCount(repo, 8);
        repo.Setup(r => r.List(It.IsAny<IPagingCriteria?>())).Returns(items);
        var service = new TestSyncRepositoryPagingService(uow.Object);
        var paging = new PagingCriteria(limit: 3, offset: 0);

        IPagingResult<IList<AppTestEntity>> result = service.ListWithPagination(paging);

        result.Data.Should().HaveCount(3);
        result.Summary.TotalCount.Should().Be(8);
    }

    [Fact]
    public void GetByWithPagination_ShouldFilterAndPage()
    {
        Expression<Func<AppTestEntity, bool>> clause = e => e.Active;
        var items = new List<AppTestEntity>
        {
            new() { Id = 1, Name = "A", Active = true },
            new() { Id = 2, Name = "B", Active = true }
        };
        (Mock<IUnitOfWork> uow, Mock<IRepository<AppTestEntity>> repo) = ApplicationTestHelpers.CreateSyncRepositoryMocks<AppTestEntity>();
        ApplicationTestHelpers.SetupGetBy(repo, clause, items);
        var service = new TestSyncRepositoryPagingService(uow.Object);

        IPagingResult<IList<AppTestEntity>> result = service.GetByWithPagination(
            clause,
            new PagingCriteria(limit: 2, offset: 0));

        result.Data.Should().HaveCountLessThanOrEqualTo(2);
        result.Data.Should().OnlyContain(e => e.Active);
    }
}
