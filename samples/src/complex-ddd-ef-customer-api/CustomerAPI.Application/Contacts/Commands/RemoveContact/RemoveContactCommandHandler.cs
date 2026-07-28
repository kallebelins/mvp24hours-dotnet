using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Exceptions;
using CustomerAPI.Core.Resources;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Contacts.Commands.RemoveContact;

/// <summary>
/// Demonstrates aggregate-driven removal: the domain decides whether the removal is allowed,
/// not the infrastructure.
/// </summary>
public sealed class RemoveContactCommandHandler(
    IUnitOfWorkAsync unitOfWork,
    ILogger<RemoveContactCommandHandler> logger)
    : IMediatorCommandHandler<RemoveContactCommand, IBusinessResult<int>>
{
    public async Task<IBusinessResult<int>> Handle(RemoveContactCommand request, CancellationToken cancellationToken)
    {
        var paging = new PagingCriteriaExpression<Customer>(100, 0);
        paging.NavigationExpr.Add(x => x.Contacts);

        var repository = unitOfWork.GetRepository<Customer>();
        var customer = await repository.GetByIdAsync(request.CustomerId, paging, cancellationToken);

        if (customer is null)
        {
            return Messages.RECORD_NOT_FOUND_FOR_ID
                .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND_FOR_ID), MessageType.Error)
                .ToBusiness<int>();
        }

        try
        {
            customer.RemoveContact(request.Id);
        }
        catch (DomainException ex)
        {
            return ex.Message
                .ToMessageResult("DOMAIN_RULE", MessageType.Error)
                .ToBusiness<int>();
        }

        int rows = await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
        if (rows > 0)
        {
            logger.LogInformation("Removed contact {ContactId} from customer {CustomerId}", request.Id, request.CustomerId);
            return rows.ToBusiness(
                Messages.OPERATION_SUCCESS
                    .ToMessageResult(nameof(Messages.OPERATION_SUCCESS), MessageType.Success));
        }

        return Messages.OPERATION_FAIL
            .ToMessageResult(nameof(Messages.OPERATION_FAIL), MessageType.Error)
            .ToBusiness<int>();
    }
}
