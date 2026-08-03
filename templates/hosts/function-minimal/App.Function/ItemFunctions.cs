using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace App.Function;

public class ItemFunctions(ILogger<ItemFunctions> logger)
{
    private static readonly ConcurrentDictionary<int, ItemRecord> Store = new();
    private static int _nextId = 1;

    [Function("GetItems")]
    public IActionResult GetItems([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "items")] HttpRequest req)
    {
        logger.LogInformation("GET /items — {Count} item(s) in store", Store.Count);
        return new OkObjectResult(Store.Values.OrderBy(x => x.Id).ToList());
    }

    [Function("CreateItem")]
    public async Task<IActionResult> CreateItem(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "items")] HttpRequest req)
    {
        var body = await req.ReadFromJsonAsync<ItemCreateRequest>();
        if (body is null || string.IsNullOrWhiteSpace(body.Name))
        {
            return new BadRequestObjectResult("Name is required.");
        }

        var record = new ItemRecord
        {
            Id = Interlocked.Increment(ref _nextId),
            Name = body.Name,
            Note = body.Note,
            Created = DateTime.UtcNow
        };

        Store[record.Id] = record;
        logger.LogInformation("Created item {ItemId}", record.Id);
        return new CreatedResult($"/api/items/{record.Id}", record);
    }

    private sealed record ItemCreateRequest(string Name, string? Note);

    private sealed record ItemRecord
    {
        public int Id { get; init; }
        public required string Name { get; init; }
        public string? Note { get; init; }
        public DateTime Created { get; init; }
    }
}
