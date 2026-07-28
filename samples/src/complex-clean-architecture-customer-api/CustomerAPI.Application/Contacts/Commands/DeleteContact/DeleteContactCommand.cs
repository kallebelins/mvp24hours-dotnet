using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Contacts.Commands.DeleteContact;

public sealed class DeleteContactCommand : IMediatorCommand<IBusinessResult<int>>
{
    public int CustomerId { get; init; }
    public int Id { get; init; }
}
