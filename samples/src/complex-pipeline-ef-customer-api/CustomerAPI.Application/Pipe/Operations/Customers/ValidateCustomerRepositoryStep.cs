using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Resources;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Pipe.Operations;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerAPI.Application.Pipe.Operations.Customers;

/// <summary>
/// Persistence boundary (read): checks whether the store already has customers. Does not open a write transaction.
/// </summary>
public class ValidateCustomerRepositoryStep(
    IUnitOfWorkAsync unitOfWorkAsync,
    ILogger<ValidateCustomerRepositoryStep> logger) : OperationBaseAsync
{
    public override async Task ExecuteAsync(IPipelineMessage input)
    {
        var cancellationToken = GetCancellationToken(input);
        var correlationId = GetCorrelationId(input);

        var repo = unitOfWorkAsync.GetRepository<Customer>();

        if (await repo.ListAnyAsync(cancellationToken))
        {
            logger.LogWarning(
                "Seed skipped because customers already exist. CorrelationId={CorrelationId}",
                correlationId);
            input.Messages.AddMessage("ValidateCustomerRepositoryStep", Messages.RECORD_NOT_SEED_DATA, Mvp24Hours.Core.Enums.MessageType.Error);
        }
    }

    private static CancellationToken GetCancellationToken(IPipelineMessage input) =>
        input.HasContent("cancellationToken")
            ? input.GetContent<CancellationToken>("cancellationToken")
            : CancellationToken.None;

    private static string GetCorrelationId(IPipelineMessage input) =>
        input.HasContent("correlationId")
            ? input.GetContent<string>("correlationId")
            : "n/a";
}
