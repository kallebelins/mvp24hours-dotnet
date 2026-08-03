using System.Linq.Expressions;
using Moq;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.DTOs.Models;
using Mvp24Hours.Core.Entities;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.ValueObjects.Logic;

namespace Mvp24Hours.Core.Test.Extensions;

[Trait("Category", "Unit")]
public class BusinessPagingAsyncExtensionsTest
{
    [Fact]
    public async Task ToPagingCriteriaAsync_ShouldMapRequestFields()
    {
        var request = new PagingCriteriaRequest
        {
            Limit = 20,
            Offset = 40,
            OrderBy = ["Name"],
            Navigation = ["Items"]
        };

        IPagingCriteria criteria = await Task.FromResult(request).ToPagingCriteriaAsync();

        criteria.Limit.Should().Be(20);
        criteria.Offset.Should().Be(40);
        criteria.OrderBy.Should().ContainSingle("Name");
        criteria.Navigation.Should().ContainSingle("Items");
    }

    [Fact]
    public async Task ToPagingCriteriaAsync_WithOverrides_ShouldPreferExplicitValues()
    {
        var request = new PagingCriteriaRequest { Limit = 10, Offset = 0 };

        IPagingCriteria criteria = await Task.FromResult(request)
            .ToPagingCriteriaAsync(limit: 50, offset: 100, orderBy: ["Id"]);

        criteria.Limit.Should().Be(50);
        criteria.Offset.Should().Be(100);
        criteria.OrderBy.Should().ContainSingle("Id");
    }

    [Fact]
    public async Task ToPagingCriteriaExpressionAsync_ShouldCreateTypedCriteria()
    {
        var request = new PagingCriteriaRequest { Limit = 15, Offset = 30, OrderBy = ["CreatedAt"] };

        IPagingCriteriaExpression<PagingTestEntity> criteria = await Task.FromResult(request)
            .ToPagingCriteriaExpressionAsync<PagingTestEntity>();

        criteria.Limit.Should().Be(15);
        criteria.Offset.Should().Be(30);
        criteria.OrderBy.Should().ContainSingle("CreatedAt");
    }

    [Fact]
    public async Task NewCriteriaAsync_ShouldMergeWithExistingCriteria()
    {
        IPagingCriteria existing = new PagingCriteria(10, 5, ["Name"], ["Child"]);

        IPagingCriteria criteria = await Task.FromResult(existing).NewCriteriaAsync(limit: 25);

        criteria.Limit.Should().Be(25);
        criteria.Offset.Should().Be(5);
        criteria.OrderBy.Should().ContainSingle("Name");
    }

    [Fact]
    public async Task NewCriteriaExpressionAsync_ShouldConvertCriteria()
    {
        IPagingCriteria existing = new PagingCriteria(8, 16, ["Score"], ["Tags"]);

        IPagingCriteriaExpression<PagingTestEntity> criteria = await Task.FromResult(existing)
            .NewCriteriaExpressionAsync<PagingTestEntity>();

        criteria.Limit.Should().Be(8);
        criteria.Offset.Should().Be(16);
        criteria.OrderBy.Should().ContainSingle("Score");
    }

    [Fact]
    public async Task ToBusinessPagingAsync_WithPageAndSummary_ShouldBuildResult()
    {
        var page = new PageResult(10, 0, 3);
        var summary = new SummaryResult(100, 10);

        IPagingResult<List<int>> result = await Task.FromResult(new List<int> { 1, 2, 3 })
            .ToBusinessPagingAsync(page, summary, tokenDefault: "paging-tok");

        result.Data.Should().HaveCount(3);
        result.Paging.Limit.Should().Be(10);
        result.Summary.TotalCount.Should().Be(100);
        result.Token.Should().Be("paging-tok");
    }

    [Fact]
    public async Task ToBusinessPagingAsync_WithMessageList_ShouldIncludeMessages()
    {
        IList<IMessageResult> messages = [new MessageResult("info", MessageType.Info)];

        IPagingResult<string> result = await Task.FromResult(messages).ToBusinessPagingAsync<string>("tok");

        result.Messages.Should().ContainSingle();
        result.Token.Should().Be("tok");
    }

    [Fact]
    public async Task ToBusinessPagingAsync_WithSingleMessage_ShouldWrapMessage()
    {
        var message = new MessageResult("warn", MessageType.Warning);

        IPagingResult<int> result = await Task.FromResult(5)
            .ToBusinessPagingAsync(message, tokenDefault: "single");

        result.Data.Should().Be(5);
        result.Messages.Should().ContainSingle();
    }

    [Fact]
    public async Task ToBusinessPagingAsync_FromRepositoryWithClause_ShouldPageEntities()
    {
        List<PagingTestEntity> items =
        [
            new() { Id = 1, Name = "A" },
            new() { Id = 2, Name = "B" },
            new() { Id = 3, Name = "C" },
            new() { Id = 4, Name = "D" },
            new() { Id = 5, Name = "E" }
        ];
        var repo = new Mock<IRepositoryAsync<PagingTestEntity>>();
        repo.Setup(r => r.GetByCountAsync(It.IsAny<Expression<Func<PagingTestEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        repo.Setup(r => r.GetByAsync(It.IsAny<Expression<Func<PagingTestEntity, bool>>>(), It.IsAny<IPagingCriteria?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<PagingTestEntity, bool>> _, IPagingCriteria? criteria, CancellationToken _) =>
                [.. items.Skip(criteria?.Offset ?? 0).Take(criteria?.Limit ?? items.Count)]);

        IPagingCriteria criteria = new PagingCriteria(2, 0);
        IPagingResult<IList<PagingTestEntity>> result = await repo.Object
            .ToBusinessPagingAsync(e => e.Id > 0, criteria, maxQtyByQueryDefault: 2);

        result.Data.Should().HaveCount(2);
        result.Summary.TotalCount.Should().Be(5);
        result.Summary.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task ToBusinessPagingAsync_FromRepositoryWithoutClause_ShouldListAll()
    {
        List<PagingTestEntity> items =
        [
            new() { Id = 1, Name = "A" },
            new() { Id = 2, Name = "B" },
            new() { Id = 3, Name = "C" }
        ];
        var repo = new Mock<IRepositoryAsync<PagingTestEntity>>();
        repo.Setup(r => r.ListCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(3);
        repo.Setup(r => r.ListAsync(It.IsAny<IPagingCriteria?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IPagingCriteria? criteria, CancellationToken _) => items);

        IPagingResult<IList<PagingTestEntity>> result = await repo.Object
            .ToBusinessPagingAsync(criteria: new PagingCriteria(10, 0));

        result.Data.Should().HaveCount(3);
        result.Summary.TotalCount.Should().Be(3);
        result.Summary.TotalPages.Should().Be(1);
    }
}

public sealed class PagingTestEntity : EntityBase<int>
{
    public string Name { get; set; } = string.Empty;
}
