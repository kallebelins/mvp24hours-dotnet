using AutoMapper;
using CustomerAPI.Core.Contract.Logic;
using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Resources;
using CustomerAPI.Core.ValueObjects.Customers;
using CustomerAPI.Infrastructure.Extensions;
using Dapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Application.Logic;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<IPagingResult<IEnumerable<CustomerResult>>> GetBy(CustomerQuery filter, IPagingCriteria criteria, CancellationToken cancellationToken = default)
        {
            // Spec-aligned SQL filters (Dapper reads do not compose ISpecification expressions).
            var filterList = new List<string>
            {
                "(@Active is null or Active = @Active) and (@Name is null or Name like CONCAT('%',@Name,'%'))"
            };

            if (filter.HasCellContact)
            {
                filterList.Add("((select count(0) from Contact where Contact.CustomerId = Customer.Id and Contact.Type = 0 and Contact.Active = 1) > 0 and Active = 1)");
            }

            if (filter.HasEmailContact)
            {
                filterList.Add("((select count(0) from Contact where Contact.CustomerId = Customer.Id and Contact.Type = 3 and Contact.Active = 1) > 0 and Active = 1)");
            }

            if (filter.HasNoContact)
            {
                filterList.Add("((select count(0) from Contact where Contact.CustomerId = Customer.Id and Contact.Active = 1) = 0)");
            }

            if (filter.IsProspect)
            {
                filterList.Add("(Customer.Note like '%prospect%')");
            }

            var result = await UnitOfWork
                .GetConnection()
                .QueryPagingResultAsync<Customer>(
                    criteria,
                    string.Join(" and ", filterList),
                    new { filter.Name, filter.Active },
                    cancellationToken: cancellationToken);

            if (result == null || result.Summary.TotalCount == 0)
            {
                return Messages.RECORD_NOT_FOUND
                    .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND), MessageType.Error)
                    .ToBusinessPaging<IEnumerable<CustomerResult>>();
            }

            return mapper.MapPagingTo<IEnumerable<Customer>, IEnumerable<CustomerResult>>(result);
        }

        public async Task<IBusinessResult<CustomerIdResult>> GetById(int id, CancellationToken cancellationToken = default)
        {
            string query = @"
                    select * from Customer where Id = @id;
                    select * from Contact where CustomerId = @id;
                ";

            Customer model = null;
            var command = new CommandDefinition(query, new { id }, cancellationToken: cancellationToken);

            using (var result = await UnitOfWork
                .GetConnection()
                .QueryMultipleAsync(command))
            {
                model = await result.ReadSingleOrDefaultAsync<Customer>();
                if (model != null)
                {
                    model.Contacts = (await result.ReadAsync<Contact>())?.ToList();
                }
            }

            if (model == null)
            {
                return Messages.RECORD_NOT_FOUND
                    .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND), MessageType.Error)
                    .ToBusiness<CustomerIdResult>();
            }

            return mapper.Map<CustomerIdResult>(model).ToBusiness();
        }

        #endregion

        #region [ Commands ]

        public async Task<IBusinessResult<int>> Create(CustomerCreate dto, CancellationToken cancellationToken = default)
        {
            var entity = mapper.Map<Customer>(dto);
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
                logger.LogInformation("Created customer {CustomerId}", entity.Id);
                return entity.Id.ToBusiness(
                    Messages.OPERATION_SUCCESS
                        .ToMessageResult(nameof(Messages.OPERATION_SUCCESS), MessageType.Success));
            }

            return Messages.OPERATION_FAIL
                .ToMessageResult(nameof(Messages.OPERATION_FAIL), MessageType.Error)
                .ToBusiness<int>();
        }

        public async Task<IBusinessResult<int>> Update(int id, CustomerUpdate dto, CancellationToken cancellationToken = default)
        {
            var entity = await Repository.GetByIdAsync(id, cancellationToken: cancellationToken);
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
