using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace App.Application.Items.Commands.DeleteItem;

public class DeleteItemCommand : IMediatorCommand<IBusinessResult<int>>
{
    public int Id { get; set; }
}
