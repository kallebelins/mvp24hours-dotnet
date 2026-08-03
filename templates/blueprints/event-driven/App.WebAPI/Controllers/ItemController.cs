using App.Application.Items.Commands.CreateItem;
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace App.WebAPI.Controllers;

[Produces("application/json")]
[Route("api/[controller]")]
[ApiController]
public class ItemController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<IBusinessResult<int>>> Create([FromBody] ItemCreate model, CancellationToken cancellationToken)
    {
        var result = await mediator.SendAsync(new CreateItemCommand { Model = model }, cancellationToken);
        return result.HasErrors ? BadRequest(result) : Created(nameof(Create), result);
    }
}
