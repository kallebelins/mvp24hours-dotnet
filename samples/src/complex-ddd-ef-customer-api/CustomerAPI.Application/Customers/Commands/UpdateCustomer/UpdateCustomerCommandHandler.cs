using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Exceptions;
using CustomerAPI.Core.Resources;
using CustomerAPI.Core.ValueObjects.Domain;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Customers.Commands.UpdateCustomer;

/// <summary>
/// Demonstrates DDD update: loads aggregate, delegates state change to domain methods,
/// then persists — no direct property assignment outside the aggregate.
/// </summary>
public sealed class UpdateCustomerCommandHandler(
    IUnitOfWorkAsync unitOfWork,
    ILogger<UpdateCustomerCommandHandler> logger)
    : IMediatorCommandHandler<UpdateCustomerCommand, IBusinessResult<int>>
{
    public async Task<IBusinessResult<int>> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.GetRepository<Customer>();
        var customer = await repository.GetByIdAsync(request.Id, cancellationToken: cancellationToken);

        if (customer is null)
        {
            return Messages.RECORD_NOT_FOUND_FOR_ID
                .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND_FOR_ID), MessageType.Error)
                .ToBusiness<int>();
        }

        try
        {
            var newName = new CustomerName(request.Model.Name);
            customer.Rename(newName);
        }
        catch (ArgumentException ex)
        {
            return ex.Message
                .ToMessageResult("DOMAIN_VALIDATION", MessageType.Error)
                .ToBusiness<int>();
        }
        catch (DomainException ex)
        {
            return ex.Message
                .ToMessageResult("DOMAIN_RULE", MessageType.Error)
                .ToBusiness<int>();
        }

        customer.UpdateNote(request.Model.Note);

        await repository.ModifyAsync(customer, cancellationToken: cancellationToken);
        int rows = await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        if (rows > 0)
        {
            logger.LogInformation("Updated customer {CustomerId}", customer.Id);
            return rows.ToBusiness(
                Messages.OPERATION_SUCCESS
                    .ToMessageResult(nameof(Messages.OPERATION_SUCCESS), MessageType.Success));
        }

        return Messages.OPERATION_FAIL
            .ToMessageResult(nameof(Messages.OPERATION_FAIL), MessageType.Error)
            .ToBusiness<int>();
    }
}
