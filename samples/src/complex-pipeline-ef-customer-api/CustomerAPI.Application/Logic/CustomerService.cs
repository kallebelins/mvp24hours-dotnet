using AutoMapper;
using CustomerAPI.Application.Pipe.Operations.Customers;
using CustomerAPI.Core.Contract.Logic;
using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Resources;
using CustomerAPI.Core.Specifications.Customers;
using CustomerAPI.Core.ValueObjects.Customers;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Application.Logic;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerAPI.Application.Logic;

/// <summary>
/// Customer queries use the repository/UoW path. Integration seeding uses a correlated pipeline with explicit persistence boundaries.
/// </summary>
public class CustomerService(
    IUnitOfWorkAsync unitOfWork,
    IPipelineAsync pipeline,
    IMapper mapper,
    ILogger<CustomerService> logger) : RepositoryPagingServiceAsync<Customer, IUnitOfWorkAsync>(unitOfWork), ICustomerService
{
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
            return Messages.RECORD_NOT_FOUND
                .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND), MessageType.Error)
                .ToBusinessPaging<IList<CustomerResult>>();
        }

        return mapper.MapPagingTo<IList<Customer>, IList<CustomerResult>>(result);
    }

    public async Task<IBusinessResult<CustomerIdResult>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var paging = new PagingCriteriaExpression<Customer>(3, 0);
        paging.NavigationExpr.Add(x => x.Contacts);

        return await mapper
            .MapBusinessToAsync<Customer, CustomerIdResult>(GetByIdAsync(id, paging, cancellationToken));
    }

    /// <summary>
    /// Integration pipeline: validate store (read) → fetch remote → map (ACL) → persist (single UoW SaveChanges).
    /// Correlation id travels on the pipeline message for step logging.
    /// </summary>
    public async Task<IBusinessResult<int>> RunDataSeed(CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString("N");

        pipeline
            .Add<ValidateCustomerRepositoryStep>()
            .Add<GetCustomerClientStep>()
            .Add<GetByCustomerMapperResponseStep>()
            .Add<CreateCustomerRepositoryStep>();

        var message = new Mvp24Hours.Infrastructure.Pipe.PipelineMessage();
        message.AddContent("correlationId", correlationId);
        message.AddContent("cancellationToken", cancellationToken);

        logger.LogInformation("Starting integration seed pipeline. CorrelationId={CorrelationId}", correlationId);
        await pipeline.ExecuteAsync(message);

        var pipelineMessage = pipeline.GetMessage();

        if (pipelineMessage.IsFaulty)
        {
            logger.LogWarning("Integration seed pipeline failed. CorrelationId={CorrelationId}", correlationId);
            return pipelineMessage.Messages
                .ToBusiness<int>(
                    defaultMessage: Messages.OPERATION_FAIL
                        .ToMessageResult(nameof(Messages.OPERATION_FAIL), MessageType.Error));
        }

        var numberOfRecords = pipelineMessage.GetContent<int>("numberOfRecords");

        if (numberOfRecords == 0)
        {
            return Messages.RECORD_NOT_FOUND
                .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND), MessageType.Error)
                .ToBusiness<int>();
        }

        logger.LogInformation(
            "Integration seed completed. CorrelationId={CorrelationId}, Records={RecordCount}",
            correlationId,
            numberOfRecords);

        return numberOfRecords.ToBusiness(
            Messages.OPERATION_SUCCESS
                .ToMessageResult(nameof(Messages.OPERATION_SUCCESS), MessageType.Success));
    }
}
