using AutoMapper;
using CustomerAPI.Core.Contract.Logic;
using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Resources;
using CustomerAPI.Core.Specifications.Customers;
using CustomerAPI.Core.ValueObjects.Customers;
using FluentValidation;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
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
    public class CustomerService(
        IUnitOfWorkAsync unitOfWork,
        IValidator<Customer> validator,
        IMapper mapper,
        TimeProvider timeProvider,
        ILogger<CustomerService> logger) : RepositoryPagingServiceAsync<Customer, IUnitOfWorkAsync>(unitOfWork, validator), ICustomerService
    {
        #region [ Fields ]
        private readonly IMapper mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        private readonly TimeProvider timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        private readonly ILogger<CustomerService> logger = logger ?? throw new ArgumentNullException(nameof(logger));
        #endregion

        #region [ Queries ]

        public async Task<IPagingResult<IList<CustomerResult>>> GetBy(CustomerQuery filter, IPagingCriteria criteria, CancellationToken cancellationToken = default)
        {
            // apply filter default
            Expression<Func<Customer, bool>> clause = x => true;

            if (filter.Name.HasValue())
            {
                clause = clause.And(x => x.Name.Contains(filter.Name));
            }
            if (filter.Active.HasValue)
            {
                clause = clause.And(x => x.Active == filter.Active);
            }

            // has cell
            if (filter.HasCellContact)
            {
                clause = clause.And<Customer, CustomerHasCellContactSpec>();
            }

            // has email
            if (filter.HasEmailContact)
            {
                clause = clause.And<Customer, CustomerHasEmailContactSpec>();
            }

            // has no
            if (filter.HasNoContact)
            {
                clause = clause.And<Customer, CustomerHasNoContactSpec>();
            }

            // is prospect
            if (filter.IsProspect)
            {
                clause = clause.And<Customer, CustomerIsPropectSpec>();
            }

            // try to get the paginated data
            var result = await GetByWithPaginationAsync(clause, criteria, cancellationToken: cancellationToken);

            // checks if there are any records in the database from the filter
            if (!result.HasData())
            {
                // reply with standard message for record not found
                return Messages.RECORD_NOT_FOUND
                    .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND), MessageType.Error)
                    .ToBusinessPaging<IList<CustomerResult>>();
            }

            // apply mapping
            return mapper.MapPagingTo<IList<Customer>, IList<CustomerResult>>(result);
        }

        public async Task<IBusinessResult<CustomerIdResult>> GetById(string id, CancellationToken cancellationToken = default)
        {
            // try to retrieve identifier
            return await mapper.MapBusinessToAsync<Customer, CustomerIdResult>(GetByIdAsync(id, cancellationToken));
        }

        #endregion

        #region [ Commands ]

        public async Task<IBusinessResult<string>> Create(CustomerCreate dto, CancellationToken cancellationToken = default)
        {
            var entity = mapper.Map<Customer>(dto);
            ApplyCreateDefaults(entity);

            // apply data validation to the model/entity with FluentValidation or DataAnnotation
            var errors = entity.TryValidate(Validator);
            if (errors.AnySafe())
            {
                return errors.ToBusiness<string>();
            }

            // perform create action on the database
            await Repository.AddAsync(entity, cancellationToken: cancellationToken);
            if (await UnitOfWork.SaveChangesAsync(cancellationToken: cancellationToken) > 0)
            {
                return entity.Id.ToBusiness(
                    Messages.OPERATION_SUCCESS
                        .ToMessageResult(nameof(Messages.OPERATION_SUCCESS), MessageType.Success));
            }

            // unknown error
            logger.LogWarning("Customer create failed after validation for {CustomerName}", entity.Name);
            return Messages.OPERATION_FAIL
                .ToMessageResult(nameof(Messages.OPERATION_FAIL), MessageType.Error)
                .ToBusiness<string>();
        }

        public async Task<IBusinessResult<int>> Update(string id, CustomerUpdate dto, CancellationToken cancellationToken = default)
        {
            // gets entity through the identifier informed in the resource
            var entity = await Repository.GetByIdAsync(id, cancellationToken: cancellationToken);
            if (entity == null)
            {
                return Messages.RECORD_NOT_FOUND_FOR_ID
                    .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND_FOR_ID), MessageType.Error)
                        .ToBusiness<int>();
            }

            // entity populating with DTO properties
            mapper.Map(dto, entity);

            // apply data validation to the model/entity with FluentValidation or DataAnnotation
            var errors = entity.TryValidate(Validator);
            if (errors.AnySafe())
            {
                return errors.ToBusiness<int>();
            }

            // apply changes to database
            await Repository.ModifyAsync(entity, cancellationToken: cancellationToken);
            int affectedRows = await UnitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
            if (affectedRows > 0)
            {
                return affectedRows.ToBusiness(
                    Messages.OPERATION_SUCCESS
                        .ToMessageResult(nameof(Messages.OPERATION_SUCCESS), MessageType.Success));
            }

            // unknown error
            return Messages.OPERATION_FAIL
                .ToMessageResult(nameof(Messages.OPERATION_FAIL), MessageType.Error)
                .ToBusiness<int>();
        }

        public async Task<IBusinessResult<int>> Delete(string id, CancellationToken cancellationToken = default)
        {
            // try to retrieve entity by identifier
            var entity = await Repository.GetByIdAsync(id, cancellationToken: cancellationToken);
            if (entity == null)
            {
                return Messages.RECORD_NOT_FOUND_FOR_ID
                    .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND_FOR_ID), MessageType.Error)
                        .ToBusiness<int>();
            }

            // performs delete action on the database
            await Repository.RemoveAsync(entity, cancellationToken: cancellationToken);
            int affectedRows = await UnitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
            if (affectedRows > 0)
            {
                return affectedRows.ToBusiness(
                    Messages.OPERATION_SUCCESS
                        .ToMessageResult(nameof(Messages.OPERATION_SUCCESS), MessageType.Success));
            }

            // unknown error
            return Messages.OPERATION_FAIL
                .ToMessageResult(nameof(Messages.OPERATION_FAIL), MessageType.Error)
                .ToBusiness<int>();
        }

        #endregion

        #region [ Helpers ]

        private void ApplyCreateDefaults(Customer entity)
        {
            var utcNow = timeProvider.GetUtcNow().UtcDateTime;
            entity.Id ??= ObjectId.GenerateNewId().ToString();
            entity.Created = utcNow;

            if (entity.Contacts == null)
            {
                return;
            }

            foreach (var contact in entity.Contacts)
            {
                contact.Id ??= ObjectId.GenerateNewId().ToString();
                contact.Created = utcNow;
                contact.CustomerId = entity.Id;
            }
        }

        #endregion
    }
}
