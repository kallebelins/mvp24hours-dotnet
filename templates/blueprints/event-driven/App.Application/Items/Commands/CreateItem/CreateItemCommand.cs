using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace App.Application.Items.Commands.CreateItem;

public sealed class CreateItemCommand : IMediatorCommand<IBusinessResult<int>>
{
    public required ItemCreate Model { get; init; }
}
