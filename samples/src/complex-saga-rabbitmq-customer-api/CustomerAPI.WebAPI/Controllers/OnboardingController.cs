using CustomerAPI.Application.Sagas;
using CustomerAPI.Domain.Repositories;
using CustomerAPI.Domain.Sagas;
using CustomerAPI.WebAPI.Validations;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Saga;

namespace CustomerAPI.WebAPI.Controllers;

/// <summary>
/// Orchestrates the customer onboarding saga.
/// </summary>
[ApiController]
[Route("api/onboarding")]
public class OnboardingController(
    ISagaOrchestrator orchestrator,
    ICustomerRepository customerRepository,
    IValidator<OnboardCustomerRequest> onboardValidator) : ControllerBase
{
    /// <summary>
    /// Starts the customer onboarding saga.
    /// </summary>
    /// <remarks>
    /// Set <c>simulateGiftFailure = true</c> to trigger a failure at Step 2
    /// and observe the saga compensating Step 1 (customer deletion).
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> StartOnboarding(
        [FromBody] OnboardCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.TryValidate(onboardValidator);
        if (validationErrors.AnySafe())
            return BadRequest(validationErrors);

        var data = new OnboardCustomerData
        {
            Name = request.Name,
            Email = request.Email,
            SimulateGiftFailure = request.SimulateGiftFailure
        };

        SagaResult<OnboardCustomerData> result = await orchestrator
            .ExecuteAsync<OnboardCustomerSaga, OnboardCustomerData>(data, cancellationToken: cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(new
            {
                result.SagaId,
                result.Data?.CustomerId,
                result.Data?.WelcomeGiftCode,
                result.Data?.WelcomeEmailSent,
                Status = result.Status.ToString()
            });
        }

        if (result.WasCompensated)
        {
            return UnprocessableEntity(new
            {
                result.SagaId,
                Error = result.ErrorMessage,
                Status = result.Status.ToString(),
                Message = "Saga failed and all eligible steps were compensated."
            });
        }

        return StatusCode(StatusCodes.Status500InternalServerError, new
        {
            result.SagaId,
            Error = result.ErrorMessage,
            Status = result.Status.ToString()
        });
    }

    /// <summary>
    /// Returns all customers currently stored in the in-memory repository.
    /// Useful to verify compensation (customer should be absent after a compensated run).
    /// </summary>
    [HttpGet("customers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomers(CancellationToken cancellationToken)
    {
        var customers = await customerRepository.GetAllAsync(cancellationToken);
        return Ok(customers);
    }

    /// <summary>
    /// Returns the current status of a saga by ID.
    /// </summary>
    [HttpGet("{sagaId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(Guid sagaId, CancellationToken cancellationToken)
    {
        var state = await orchestrator.GetStatusAsync(sagaId, cancellationToken);
        if (state is null)
            return NotFound(new { sagaId, Message = "Saga not found." });

        return Ok(new
        {
            state.SagaId,
            state.Status,
            state.CurrentStepName,
            state.CurrentStepIndex,
            state.StartedAt,
            state.CompletedAt
        });
    }
}

public record OnboardCustomerRequest(string Name, string Email, bool SimulateGiftFailure = false);
