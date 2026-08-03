using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace App.Application.Items.Commands.UpdateItem;

public class UpdateItemCommand : IMediatorCommand<IBusinessResult<int>>
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Note { get; set; }
}
