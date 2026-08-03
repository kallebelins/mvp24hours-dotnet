using App.Core.ValueObjects.Items;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace App.Application.Items.Queries.GetItemById;

public class GetItemByIdQuery : IMediatorQuery<IBusinessResult<ItemResult>>
{
    public int Id { get; set; }
}
