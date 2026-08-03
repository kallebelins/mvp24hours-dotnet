using System.Linq.Expressions;

using Mvp24Hours.Application.Test.Support;

using Mvp24Hours.Core.ValueObjects.Logic;



namespace Mvp24Hours.Application.Test.Logic.Async;



[Trait("Category", "Unit")]

public class RepositoryPagingServiceAsyncTest

{

    [Fact]

    public async Task ListWithPaginationAsync_ShouldReturnPagedResult()

    {

        var items = new List<AppTestEntity>

        {

            new() { Id = 1, Name = "A" },

            new() { Id = 2, Name = "B" },

            new() { Id = 3, Name = "C" }

        };

        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();

        ApplicationTestHelpers.SetupListCount(repo, 8);

        repo.Setup(r => r.ListAsync(It.IsAny<IPagingCriteria?>(), It.IsAny<CancellationToken>()))

            .ReturnsAsync(items);

        var service = new TestRepositoryPagingService(uow.Object);

        var paging = new PagingCriteria(limit: 3, offset: 0);



        IPagingResult<IList<AppTestEntity>> result = await service.ListWithPaginationAsync(paging);



        result.Data.Should().HaveCount(3);

        result.Summary.TotalCount.Should().Be(8);

    }



    [Fact]

    public async Task GetByWithPaginationAsync_ShouldFilterAndPage()

    {

        Expression<Func<AppTestEntity, bool>> clause = e => e.Active;

        var items = new List<AppTestEntity>

        {

            new() { Id = 1, Name = "A", Active = true },

            new() { Id = 2, Name = "B", Active = true }

        };

        (Mock<IUnitOfWorkAsync> uow, Mock<IRepositoryAsync<AppTestEntity>> repo) = ApplicationTestHelpers.CreateRepositoryMocks<AppTestEntity>();

        ApplicationTestHelpers.SetupGetBy(repo, clause, items);

        var service = new TestRepositoryPagingService(uow.Object);



        IPagingResult<IList<AppTestEntity>> result = await service.GetByWithPaginationAsync(

            clause,

            new PagingCriteria(limit: 2, offset: 0));



        result.Data.Should().HaveCountLessThanOrEqualTo(2);

        result.Data.Should().OnlyContain(e => e.Active);

    }

}


