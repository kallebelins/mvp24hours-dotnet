using CustomerAPI.Core.Contract.Logic;
using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Resources;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Application.Logic;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerAPI.Application.Logic
{
    public class ContactService(
        IUnitOfWorkAsync unitOfWork,
        IValidator<Contact> validator,
        TimeProvider timeProvider,
        ILogger<ContactService> logger) : RepositoryPagingServiceAsync<Contact, IUnitOfWorkAsync>(unitOfWork, validator), IContactService
    {
        #region [ Queries ]

        public async Task<IBusinessResult<IList<Contact>>> GetBy(int customerId, CancellationToken cancellationToken = default)
        {
            Expression<Func<Contact, bool>> clause = x => x.CustomerId == customerId;

            var result = await GetByAsync(clause, cancellationToken: cancellationToken);

            if (!result.HasData())
            {
                return Messages.RECORD_NOT_FOUND
                    .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND), MessageType.Error)
                    .ToBusiness<IList<Contact>>();
            }

            return result;
        }

        #endregion

        #region [ Commands ]

        public async Task<IBusinessResult<int>> Create(int customerId, Contact entityModel, CancellationToken cancellationToken = default)
        {
            entityModel.Active = true;
            entityModel.Created = timeProvider.GetUtcNow().UtcDateTime;
            entityModel.CustomerId = customerId;

            var errors = entityModel.TryValidate(Validator);
            if (errors.AnySafe())
            {
                return errors.ToBusiness<int>();
            }

            await Repository.AddAsync(entityModel, cancellationToken: cancellationToken);
            if (await UnitOfWork.SaveChangesAsync(cancellationToken: cancellationToken) > 0)
            {
                logger.LogInformation("Created contact {ContactId} for customer {CustomerId}", entityModel.Id, customerId);
                return entityModel.Id.ToBusiness(
                    Messages.OPERATION_SUCCESS
                        .ToMessageResult(nameof(Messages.OPERATION_SUCCESS), MessageType.Success));
            }

            return Messages.OPERATION_FAIL
                .ToMessageResult(nameof(Messages.OPERATION_FAIL), MessageType.Error)
                .ToBusiness<int>();
        }

        public async Task<IBusinessResult<int>> Update(int customerId, int id, Contact entityModel, CancellationToken cancellationToken = default)
        {
            var entityDb = await Repository
                .GetByAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken: cancellationToken)
                .FirstOrDefaultAsync();
            if (entityDb == null)
            {
                return Messages.RECORD_NOT_FOUND_FOR_ID
                    .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND_FOR_ID), MessageType.Error)
                        .ToBusiness<int>();
            }

            entityModel.Id = id;
            entityModel.CustomerId = customerId;
            entityModel.Created = entityDb.Created;
            entityModel.CopyPropertiesTo(entityDb);

            var errors = entityDb.TryValidate(Validator);
            if (errors.AnySafe())
            {
                return errors.ToBusiness<int>();
            }

            await Repository.ModifyAsync(entityDb, cancellationToken: cancellationToken);
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
            var entity = await Repository
                .GetByAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken: cancellationToken)
                .FirstOrDefaultAsync();
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
}
