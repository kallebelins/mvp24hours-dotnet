using CustomerAPI.Core.Contract.Logic;
using CustomerAPI.Core.Contract.Pipe.Builders;
using CustomerAPI.Core.Resources;
using CustomerAPI.Core.ValueObjects.Customers;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerAPI.Application.Logic;

/// <summary>
/// Application service that depends on Core builder ports; Typicode adapters implement those ports.
/// </summary>
public class CustomerService(
    IPipelineAsync pipeline,
    IGetByCustomerBuilder getByCustomerBuilder,
    IGetByIdCustomerBuilder getByIdCustomerBuilder,
    ILogger<CustomerService> logger) : ICustomerService
{
    public async Task<IBusinessResult<IList<CustomerResult>>> GetBy(CustomerQuery filter, CancellationToken cancellationToken = default)
    {
        getByCustomerBuilder.Builder(pipeline);

        var message = filter.ToMessage();
        message.AddContent("cancellationToken", cancellationToken);

        logger.LogDebug("Executing ports-and-adapters get-by-customer pipeline");
        await pipeline.ExecuteAsync(message);

        IList<CustomerResult> result = pipeline.GetMessage()
            .GetContent<List<CustomerResult>>();

        if (!result.AnySafe())
        {
            return Messages.RECORD_NOT_FOUND
                .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND), MessageType.Error)
                .ToBusiness<IList<CustomerResult>>();
        }

        return result.ToBusiness();
    }

    public async Task<IBusinessResult<CustomerIdResult>> GetById(int id, CancellationToken cancellationToken = default)
    {
        getByIdCustomerBuilder.Builder(pipeline);

        var message = id.ToMessage("id");
        message.AddContent("cancellationToken", cancellationToken);

        logger.LogDebug("Executing ports-and-adapters get-by-id pipeline for customer {CustomerId}", id);
        await pipeline.ExecuteAsync(message);

        var result = pipeline.GetMessage()
            .GetContent<CustomerIdResult>();

        if (result == null)
        {
            return Messages.RECORD_NOT_FOUND_FOR_ID
                .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND_FOR_ID), MessageType.Error)
                .ToBusiness<CustomerIdResult>();
        }

        return result.ToBusiness();
    }
}
