using App.Application.DTOs;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;

namespace App.Application.UseCases;

/// <summary>
/// Inbound port (application use case). Controllers depend on this abstraction.
/// </summary>
public interface IItemUseCase
{
    Task<IBusinessResult<List<ItemResult>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IBusinessResult<ItemResult>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IBusinessResult<int>> CreateAsync(ItemCreate dto, CancellationToken cancellationToken = default);
}
