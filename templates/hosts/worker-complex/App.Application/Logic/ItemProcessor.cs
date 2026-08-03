using App.Core.Contract.Data;
using App.Core.Contract.Logic;
using Microsoft.Extensions.Logging;

namespace App.Application.Logic;

public class ItemProcessor(IItemStore store, ILogger<ItemProcessor> logger) : IItemProcessor
{
    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        var pending = await store.GetPendingAsync(cancellationToken);
        logger.LogInformation("Processing {Count} pending item(s)", pending.Count);

        foreach (var item in pending)
        {
            logger.LogDebug("Processed item {ItemId}: {Name}", item.Id, item.Name);
            await store.MarkProcessedAsync(item.Id, cancellationToken);
        }
    }
}
