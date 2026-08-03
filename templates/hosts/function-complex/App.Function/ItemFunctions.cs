using App.Core.Contract.Logic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace App.Function;

public class ItemFunctions(IItemService itemService, ILogger<ItemFunctions> logger)
{
    [Function("GetItems")]
    public async Task<IActionResult> GetItems(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "items")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        var items = await itemService.GetAllAsync(cancellationToken);
        return new OkObjectResult(items);
    }

    [Function("CreateItem")]
    public async Task<IActionResult> CreateItem(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "items")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        var body = await req.ReadFromJsonAsync<ItemCreateRequest>(cancellationToken);
        if (body is null || string.IsNullOrWhiteSpace(body.Name))
        {
            return new BadRequestObjectResult("Name is required.");
        }

        var item = await itemService.CreateAsync(body.Name, body.Note, cancellationToken);
        logger.LogInformation("Created item {ItemId}", item.Id);
        return new CreatedResult($"/api/items/{item.Id}", item);
    }

    private sealed record ItemCreateRequest(string Name, string? Note);
}
