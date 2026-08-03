using App.Application.Events;
using App.Domain.Entities;
using IntegrationEventPublisher = App.Application.Integration.IIntegrationEventPublisher;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace App.Application.Items.Commands.CreateItem;

/// <summary>
/// Persists the item, then publishes an in-memory integration event (no RabbitMQ required).
/// </summary>
public sealed class CreateItemCommandHandler(
    IUnitOfWorkAsync unitOfWork,
    IntegrationEventPublisher publisher,
    TimeProvider timeProvider,
    ILogger<CreateItemCommandHandler> logger)
    : IMediatorCommandHandler<CreateItemCommand, IBusinessResult<int>>
{
    public async Task<IBusinessResult<int>> Handle(CreateItemCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Model.Name))
        {
            return "VALIDATION_ERROR".ToMessageResult("Name is required.", MessageType.Error)
                .ToBusiness<int>();
        }

        var item = new Item
        {
            Created = timeProvider.GetUtcNow().UtcDateTime,
            Name = request.Model.Name.Trim(),
            Note = request.Model.Note,
            Active = true
        };

        var repository = unitOfWork.GetRepository<Item>();
        await repository.AddAsync(item, cancellationToken: cancellationToken);

        if (await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken) <= 0)
        {
            return "OPERATION_FAIL".ToMessageResult("OPERATION_FAIL", MessageType.Error).ToBusiness<int>();
        }

        logger.LogInformation("Created item {ItemId}", item.Id);

        await publisher.PublishAsync(new ItemCreatedIntegrationEvent
        {
            ItemId = item.Id,
            ItemName = item.Name
        }, cancellationToken);

        return item.Id.ToBusiness("OPERATION_SUCCESS".ToMessageResult("OPERATION_SUCCESS", MessageType.Success));
    }
}
