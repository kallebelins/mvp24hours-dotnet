using AutoMapper;
using CustomerAPI.Core.Entities;
using CustomerAPI.Core.ValueObjects.Customers;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Operations;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerAPI.Application.Pipe.Operations.Customers;

/// <summary>
/// Persistence boundary (write): maps create DTOs to entities and commits a single Unit of Work SaveChanges.
/// </summary>
public class CreateCustomerRepositoryStep(
    IUnitOfWorkAsync unitOfWorkAsync,
    IMapper mapper,
    ILogger<CreateCustomerRepositoryStep> logger) : OperationBaseAsync
{
    public override async Task ExecuteAsync(IPipelineMessage input)
    {
        if (!input.HasContent("model-customers"))
        {
            input.SetLock();
            return;
        }

        var correlationId = input.HasContent("correlationId")
            ? input.GetContent<string>("correlationId")
            : "n/a";
        var cancellationToken = input.HasContent("cancellationToken")
            ? input.GetContent<CancellationToken>("cancellationToken")
            : CancellationToken.None;

        var customers = input.GetContent<IList<CustomerCreate>>("model-customers");
        var repo = unitOfWorkAsync.GetRepository<Customer>();

        foreach (var c in customers)
        {
            await repo.AddAsync(mapper.Map<Customer>(c), cancellationToken);
        }

        // Single UoW commit for the integration pipeline write boundary
        int numberOfRecords = await unitOfWorkAsync.SaveChangesAsync(cancellationToken);
        input.AddContent("numberOfRecords", numberOfRecords);

        logger.LogInformation(
            "Persisted seeded customers. CorrelationId={CorrelationId}, Records={RecordCount}",
            correlationId,
            numberOfRecords);
    }
}
