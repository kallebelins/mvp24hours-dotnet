using App.Core.Entities;
using App.Core.ValueObjects.Domain;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace App.Application.Items.Commands.CreateItem;

public sealed class CreateItemCommandHandler(
    IUnitOfWorkAsync unitOfWork,
    TimeProvider timeProvider,
    ILogger<CreateItemCommandHandler> logger)
    : IMediatorCommandHandler<CreateItemCommand, IBusinessResult<int>>
{
    public async Task<IBusinessResult<int>> Handle(CreateItemCommand request, CancellationToken cancellationToken)
    {
        ItemName name;
        try
        {
            name = new ItemName(request.Model.Name);
        }
        catch (ArgumentException ex)
        {
            return ex.Message
                .ToMessageResult("DOMAIN_VALIDATION", MessageType.Error)
                .ToBusiness<int>();
        }

        var item = Item.Create(name, timeProvider, request.Model.Note);

        var repository = unitOfWork.GetRepository<Item>();
        await repository.AddAsync(item, cancellationToken: cancellationToken);

        if (await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken) <= 0)
        {
            return "OPERATION_FAIL".ToMessageResult("OPERATION_FAIL", MessageType.Error).ToBusiness<int>();
        }

        logger.LogInformation("Created item {ItemId} ({ItemName})", item.Id, item.Name);
        item.ClearDomainEvents();

        return item.Id.ToBusiness("OPERATION_SUCCESS".ToMessageResult("OPERATION_SUCCESS", MessageType.Success));
    }
}
