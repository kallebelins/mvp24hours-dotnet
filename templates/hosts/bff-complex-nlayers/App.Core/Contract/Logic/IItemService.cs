using App.Core.ValueObjects.Items;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;

namespace App.Core.Contract.Logic;

public interface IItemService
{
    Task<IPagingResult<IList<ItemResult>>> GetBy(ItemQuery filter, IPagingCriteria criteria, CancellationToken cancellationToken = default);
    Task<IBusinessResult<ItemResult>> GetById(int id, CancellationToken cancellationToken = default);
    Task<IBusinessResult<int>> Create(ItemCreate dto, CancellationToken cancellationToken = default);
}
