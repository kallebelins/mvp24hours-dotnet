using System.Linq.Expressions;
using AutoMapper;
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

namespace CustomerAPI.Application.Logic;

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
        Expression<Func<Customer, bool>> clause =
            x => (string.IsNullOrEmpty(filter.Name) || x.Name.Contains(filter.Name))
                && (filter.Active == null || filter.Active.Value);

        if (filter.HasCellContact)
        {
            clause = clause.And<Customer, CustomerHasCellContactSpec>();
        }

        if (filter.HasEmailContact)
        {
            clause = clause.And<Customer, CustomerHasEmailContactSpec>();
        }

        if (filter.HasNoContact)
        {
            clause = clause.And<Customer, CustomerHasNoContactSpec>();
        }

        if (filter.IsProspect)
        {
            clause = clause.And<Customer, CustomerIsPropectSpec>();
        }

        var result = await GetByWithPaginationAsync(clause, criteria, cancellationToken: cancellationToken);

        if (!result.HasData())
        {
            return Messages.RECORD_NOT_FOUND.ToMessageResult(nameof(Messages.RECORD_NOT_FOUND), MessageType.Error)
                .ToBusinessPaging<IList<CustomerResult>>();
        }

        return mapper.MapPagingTo<IList<Customer>, IList<CustomerResult>>(result);
    }

    public async Task<IBusinessResult<CustomerIdResult>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var paging = new PagingCriteriaExpression<Customer>(3, 0);
        paging.NavigationExpr.Add(x => x.Contacts);

        return await mapper.MapBusinessToAsync<Customer, CustomerIdResult>(GetByIdAsync(id, paging, cancellationToken));
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
