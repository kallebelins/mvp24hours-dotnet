using CustomerAPI.Application.DTOs.ExternalProfiles;
using CustomerAPI.Application.Ports;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerAPI.WebAPI.Controllers;

/// <summary>
/// Inbound HTTP adapter — exposes external profile enrichment data.
/// Demonstrates the outbound HTTP adapter (Typicode JSONPlaceholder) through ports.
/// </summary>
[Produces("application/json")]
[Route("api/[controller]")]
[ApiController]
public class ExternalProfileController(IExternalProfileUseCase externalProfileUseCase) : ControllerBase
{
    /// <summary>
    /// Get all external profiles from Typicode JSONPlaceholder (via outbound HTTP adapter).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IBusinessResult<IList<ExternalProfileResult>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IBusinessResult<IList<ExternalProfileResult>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(IBusinessResult<IList<ExternalProfileResult>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IBusinessResult<IList<ExternalProfileResult>>>> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await externalProfileUseCase.GetProfilesAsync(cancellationToken);
        if (result.HasErrors) return BadRequest(result);
        if (result.HasData()) return Ok(result);
        return NotFound(result);
    }

    /// <summary>
    /// Get a single external profile by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(IBusinessResult<ExternalProfileResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IBusinessResult<ExternalProfileResult>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(IBusinessResult<ExternalProfileResult>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IBusinessResult<ExternalProfileResult>>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await externalProfileUseCase.GetProfileByIdAsync(id, cancellationToken);
        if (result.HasErrors) return BadRequest(result);
        if (result.HasData()) return Ok(result);
        return NotFound(result);
    }
}
