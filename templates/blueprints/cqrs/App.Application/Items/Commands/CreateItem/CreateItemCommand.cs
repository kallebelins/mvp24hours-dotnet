using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace App.Application.Items.Commands.CreateItem;

public class CreateItemCommand : IMediatorCommand<IBusinessResult<int>>
{
    public required string Name { get; set; }
    public string? Note { get; set; }
}
