using App.Core.ValueObjects.Items;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace App.Application.Items.Queries.GetItems;

public class GetItemsQuery : IMediatorQuery<IPagingResult<IList<ItemResult>>>
{
    public ItemQuery Filter { get; set; } = new();
    public required IPagingCriteria Paging { get; set; }
}
