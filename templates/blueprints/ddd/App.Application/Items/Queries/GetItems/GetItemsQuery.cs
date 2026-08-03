using App.Core.ValueObjects.Items;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace App.Application.Items.Queries.GetItems;

public sealed class GetItemsQuery : IMediatorQuery<IPagingResult<List<ItemResult>>>
{
    public required ItemQuery Filter { get; init; }
    public required IPagingCriteria Criteria { get; init; }
}
