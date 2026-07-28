using AutoMapper;
using CustomerAPI.Application.Customers.Notifications;
using CustomerAPI.Domain.Entities;
using CustomerAPI.Domain.Resources;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Customers.Commands.CreateCustomer;

public sealed class CreateCustomerCommandHandler(
    IUnitOfWorkAsync unitOfWork,
    IMapper mapper,
    TimeProvider timeProvider,
    IMediator mediator,
    ILogger<CreateCustomerCommandHandler> logger)
    : IMediatorCommandHandler<CreateCustomerCommand, IBusinessResult<int>>
{
    public async Task<IBusinessResult<int>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var entity = mapper.Map<Customer>(request.Model);
        entity.Created = timeProvider.GetUtcNow().UtcDateTime;
        entity.Active = true;

        var repository = unitOfWork.GetRepository<Customer>();
        await repository.AddAsync(entity, cancellationToken: cancellationToken);

        if (await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken) <= 0)
        {
            return Messages.OPERATION_FAIL
                .ToMessageResult(nameof(Messages.OPERATION_FAIL), MessageType.Error)
                .ToBusiness<int>();
        }

        logger.LogInformation("Created customer {CustomerId}", entity.Id);

        await mediator.PublishAsync(
            new CustomerCreatedNotification(entity.Id, entity.Name),
            cancellationToken);

        return entity.Id.ToBusiness(
            Messages.OPERATION_SUCCESS
                .ToMessageResult(nameof(Messages.OPERATION_SUCCESS), MessageType.Success));
    }
}
