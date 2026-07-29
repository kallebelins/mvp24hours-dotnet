using System.Linq.Expressions;
using AutoMapper;
using CustomerAPI.Core.Contract.Logic;
using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Resources;
using CustomerAPI.Core.ValueObjects.Contacts;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Application.Logic;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;

namespace CustomerAPI.Application.Logic;

public class ContactService(
    IUnitOfWorkAsync unitOfWork,
    IValidator<Contact> validator,
    IMapper mapper,
    TimeProvider timeProvider,
    ILogger<ContactService> logger) : RepositoryPagingServiceAsync<Contact, IUnitOfWorkAsync>(unitOfWork, validator), IContactService
{
    #region [ Fields ]
    private readonly IMapper mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly TimeProvider timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    private readonly ILogger<ContactService> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    #endregion

    #region [ Queries ]

    public async Task<IBusinessResult<IList<ContactIdResult>>> GetBy(int customerId, CancellationToken cancellationToken = default)
    {
        Expression<Func<Contact, bool>> clause = x => x.CustomerId == customerId;

        var result = await GetByAsync(clause, cancellationToken: cancellationToken);

        if (!result.HasData())
        {
            return Messages.RECORD_NOT_FOUND
                .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND), MessageType.Error)
                .ToBusiness<IList<ContactIdResult>>();
        }

        return mapper.MapBusinessTo<IList<Contact>, IList<ContactIdResult>>(result);
    }

    #endregion

    #region [ Commands ]

    public async Task<IBusinessResult<int>> Create(int customerId, ContactCreate dto, CancellationToken cancellationToken = default)
    {
        var entity = mapper.Map<Contact>(dto);
        entity.CustomerId = customerId;
        entity.Created = timeProvider.GetUtcNow().UtcDateTime;
        entity.Active = true;

        var errors = entity.TryValidate(Validator);
        if (errors.AnySafe())
        {
            return errors.ToBusiness<int>();
        }

        await Repository.AddAsync(entity, cancellationToken: cancellationToken);
        if (await UnitOfWork.SaveChangesAsync(cancellationToken: cancellationToken) > 0)
        {
            logger.LogInformation("Created contact {ContactId} for customer {CustomerId}", entity.Id, customerId);
            return entity.Id.ToBusiness(
                Messages.OPERATION_SUCCESS
                    .ToMessageResult(nameof(Messages.OPERATION_SUCCESS), MessageType.Success));
        }

        return Messages.OPERATION_FAIL
            .ToMessageResult(nameof(Messages.OPERATION_FAIL), MessageType.Error)
            .ToBusiness<int>();
    }

    public async Task<IBusinessResult<int>> Update(int customerId, int id, ContactUpdate dto, CancellationToken cancellationToken = default)
    {
        var entity = await Repository.GetByAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken: cancellationToken).FirstOrDefaultAsync();
        if (entity == null)
        {
            return Messages.RECORD_NOT_FOUND_FOR_ID
                .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND_FOR_ID), MessageType.Error)
                    .ToBusiness<int>();
        }

        mapper.Map(dto, entity);

        var errors = entity.TryValidate(Validator);
        if (errors.AnySafe())
        {
            return errors.ToBusiness<int>();
        }

        await Repository.ModifyAsync(entity, cancellationToken: cancellationToken);
        int affectedRows = await UnitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
        if (affectedRows > 0)
        {
            logger.LogInformation("Updated contact {ContactId} for customer {CustomerId}", id, customerId);
            return affectedRows.ToBusiness(
                Messages.OPERATION_SUCCESS
                    .ToMessageResult(nameof(Messages.OPERATION_SUCCESS), MessageType.Success));
        }

        return Messages.OPERATION_FAIL
            .ToMessageResult(nameof(Messages.OPERATION_FAIL), MessageType.Error)
            .ToBusiness<int>();
    }

    public async Task<IBusinessResult<int>> Delete(int customerId, int id, CancellationToken cancellationToken = default)
    {
        var entity = await Repository.GetByAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken: cancellationToken).FirstOrDefaultAsync();
        if (entity == null)
        {
            return Messages.RECORD_NOT_FOUND_FOR_ID
                .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND_FOR_ID), MessageType.Error)
                    .ToBusiness<int>();
        }

        await Repository.RemoveAsync(entity, cancellationToken: cancellationToken);
        int affectedRows = await UnitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
        if (affectedRows > 0)
        {
            logger.LogInformation("Deleted contact {ContactId} for customer {CustomerId}", id, customerId);
            return affectedRows.ToBusiness(
                Messages.OPERATION_SUCCESS
                    .ToMessageResult(nameof(Messages.OPERATION_SUCCESS), MessageType.Success));
        }

        return Messages.OPERATION_FAIL
            .ToMessageResult(nameof(Messages.OPERATION_FAIL), MessageType.Error)
            .ToBusiness<int>();
    }

    #endregion
}
