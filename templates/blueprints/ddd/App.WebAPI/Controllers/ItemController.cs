using App.Application.Items.Commands.CreateItem;
using App.Application.Items.Queries.GetItems;
using App.Core.ValueObjects.Items;
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.DTOs.Models;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace App.WebAPI.Controllers;

[Produces("application/json")]
[Route("api/[controller]")]
[ApiController]
public class ItemController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IPagingResult<List<ItemResult>>>> GetBy(
        [FromQuery] ItemQuery filter,
        [FromQuery] PagingCriteriaRequest pagingCriteria,
        CancellationToken cancellationToken)
    {
        var result = await mediator.SendAsync(new GetItemsQuery
        {
            Filter = filter,
            Criteria = pagingCriteria.ToPagingCriteria()
        }, cancellationToken);

        return result.HasData() ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<ActionResult<IBusinessResult<int>>> Create([FromBody] ItemCreate model, CancellationToken cancellationToken)
    {
        var result = await mediator.SendAsync(new CreateItemCommand { Model = model }, cancellationToken);
        return result.HasErrors ? BadRequest(result) : Created(nameof(Create), result);
    }
}
