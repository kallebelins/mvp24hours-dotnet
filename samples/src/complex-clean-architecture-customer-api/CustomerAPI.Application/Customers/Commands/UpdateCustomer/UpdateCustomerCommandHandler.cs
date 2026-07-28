using AutoMapper;
using CustomerAPI.Domain.Entities;
using CustomerAPI.Domain.Resources;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Customers.Commands.UpdateCustomer;

public sealed class UpdateCustomerCommandHandler(
    IUnitOfWorkAsync unitOfWork,
    IMapper mapper,
    ILogger<UpdateCustomerCommandHandler> logger)
    : IMediatorCommandHandler<UpdateCustomerCommand, IBusinessResult<int>>
{
    public async Task<IBusinessResult<int>> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.GetRepository<Customer>();
        var entity = await repository.GetByIdAsync(request.Id, cancellationToken: cancellationToken);
        if (entity == null)
        {
            return Messages.RECORD_NOT_FOUND_FOR_ID
                .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND_FOR_ID), MessageType.Error)
                .ToBusiness<int>();
        }

        mapper.Map(request.Model, entity);
        await repository.ModifyAsync(entity, cancellationToken: cancellationToken);

        int affectedRows = await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
        if (affectedRows > 0)
        {
            logger.LogInformation("Updated customer {CustomerId}", request.Id);
            return affectedRows.ToBusiness(
                Messages.OPERATION_SUCCESS
                    .ToMessageResult(nameof(Messages.OPERATION_SUCCESS), MessageType.Success));
        }

        return Messages.OPERATION_FAIL
            .ToMessageResult(nameof(Messages.OPERATION_FAIL), MessageType.Error)
            .ToBusiness<int>();
    }
}
