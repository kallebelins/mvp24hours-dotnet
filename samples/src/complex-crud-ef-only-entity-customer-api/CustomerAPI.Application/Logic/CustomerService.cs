using CustomerAPI.Core.Contract.Logic;
using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Resources;
using CustomerAPI.Core.Specifications.Customers;
using CustomerAPI.Core.ValueObjects.Customers;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Application.Logic;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerAPI.Application.Logic
{
    public class CustomerService(
        IUnitOfWorkAsync unitOfWork,
        IValidator<Customer> validator,
        TimeProvider timeProvider,
        ILogger<CustomerService> logger) : RepositoryPagingServiceAsync<Customer, IUnitOfWorkAsync>(unitOfWork, validator), ICustomerService
    {
        #region [ Queries ]

        public async Task<IPagingResult<IList<Customer>>> GetBy(CustomerQuery model, IPagingCriteria criteria, CancellationToken cancellationToken = default)
        {
            Expression<Func<Customer, bool>> clause =
                x => (string.IsNullOrEmpty(model.Name) || x.Name.Contains(model.Name))
                    && (model.Active == null || model.Active.Value);

            if (model.HasCellContact)
            {
                clause = clause.And<Customer, CustomerHasCellContactSpec>();
            }

            if (model.HasEmailContact)
            {
                clause = clause.And<Customer, CustomerHasEmailContactSpec>();
            }

            if (model.HasNoContact)
            {
                clause = clause.And<Customer, CustomerHasNoContactSpec>();
            }

            if (model.IsProspect)
            {
                clause = clause.And<Customer, CustomerIsPropectSpec>();
            }

            var result = await GetByWithPaginationAsync(clause, criteria, cancellationToken: cancellationToken);

            if (!result.HasData())
            {
                return Messages.RECORD_NOT_FOUND
                    .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND), MessageType.Error)
                    .ToBusinessPaging<IList<Customer>>();
            }

            return result;
        }

        public async Task<IBusinessResult<Customer>> GetById(int id, CancellationToken cancellationToken = default)
        {
            var paging = new PagingCriteriaExpression<Customer>(3, 0);
            paging.NavigationExpr.Add(x => x.Contacts);

            return await GetByIdAsync(id, paging, cancellationToken);
        }

        #endregion

        #region [ Commands ]

        public async Task<IBusinessResult<int>> Create(Customer entityModel, CancellationToken cancellationToken = default)
        {
            entityModel.Active = true;
            entityModel.Created = timeProvider.GetUtcNow().UtcDateTime;

            var errors = entityModel.TryValidate(Validator);
            if (errors.AnySafe())
            {
                return errors.ToBusiness<int>();
            }

            await Repository.AddAsync(entityModel, cancellationToken: cancellationToken);
            if (await UnitOfWork.SaveChangesAsync(cancellationToken: cancellationToken) > 0)
            {
                logger.LogInformation("Created customer {CustomerId}", entityModel.Id);
                return entityModel.Id.ToBusiness(
                    Messages.OPERATION_SUCCESS
                        .ToMessageResult(nameof(Messages.OPERATION_SUCCESS), MessageType.Success));
            }

            return Messages.OPERATION_FAIL
                .ToMessageResult(nameof(Messages.OPERATION_FAIL), MessageType.Error)
                .ToBusiness<int>();
        }

        public async Task<IBusinessResult<int>> Update(int id, Customer entityModel, CancellationToken cancellationToken = default)
        {
            var entityDb = await Repository.GetByIdAsync(id, cancellationToken: cancellationToken);
            if (entityDb == null)
            {
                return Messages.RECORD_NOT_FOUND_FOR_ID
                    .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND_FOR_ID), MessageType.Error)
                        .ToBusiness<int>();
            }

            entityModel.Id = id;
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
                logger.LogInformation("Updated customer {CustomerId}", id);
                return affectedRows.ToBusiness(
                    Messages.OPERATION_SUCCESS
                        .ToMessageResult(nameof(Messages.OPERATION_SUCCESS), MessageType.Success));
            }

            return Messages.OPERATION_FAIL
                .ToMessageResult(nameof(Messages.OPERATION_FAIL), MessageType.Error)
                .ToBusiness<int>();
        }

        public async Task<IBusinessResult<int>> Delete(int id, CancellationToken cancellationToken = default)
        {
            var entity = await Repository.GetByIdAsync(id, cancellationToken: cancellationToken);
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
                logger.LogInformation("Deleted customer {CustomerId}", id);
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
