using App.Core.Contract.Logic;
using Microsoft.Extensions.Logging;

namespace App.Application.Logic;

public class ItemProcessor(ILogger<ItemProcessor> logger) : IItemProcessor
{
    public Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing items at {TimeUtc}", DateTime.UtcNow);
        return Task.CompletedTask;
    }
}
