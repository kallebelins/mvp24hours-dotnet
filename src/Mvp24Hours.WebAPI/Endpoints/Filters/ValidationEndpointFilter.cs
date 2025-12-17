//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.Endpoints.Filters;

/// <summary>
/// Endpoint filter that automatically validates requests using FluentValidation.
/// </summary>
/// <typeparam name="TRequest">The request type to validate.</typeparam>
/// <remarks>
/// <para>
/// This filter:
/// <list type="bullet">
/// <item>Resolves a FluentValidation validator for the request type from DI container</item>
/// <item>Validates the request before it reaches the endpoint handler</item>
/// <item>Returns 400 Bad Request with validation errors if validation fails</item>
/// <item>Allows the request to proceed if validation passes or no validator is found</item>
/// </list>
/// </para>
/// <para>
/// <strong>Prerequisites:</strong>
/// Register FluentValidation validators via <c>services.AddValidatorsFromAssembly()</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register validators
/// builder.Services.AddValidatorsFromAssemblyContaining&lt;CreateOrderCommandValidator&gt;();
/// 
/// // Use filter automatically via MapCommand/MapQuery
/// app.MapCommand&lt;CreateOrderCommand, OrderDto&gt;("/api/orders");
/// </code>
/// </example>
public class ValidationEndpointFilter<TRequest> : IEndpointFilter
    where TRequest : class
{
    /// <summary>
    /// Validates the request and returns validation errors if validation fails.
    /// </summary>
    /// <param name="context">The endpoint filter context.</param>
    /// <param name="next">The next filter in the pipeline.</param>
    /// <returns>The result of validation or the next filter.</returns>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // Find the request parameter
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();

        if (request is null)
        {
            // If no request found, proceed to next filter
            return await next(context);
        }

        // Try to resolve validator from DI container
        var validator = context.HttpContext.RequestServices.GetService<IValidator<TRequest>>();

        // If no validator is registered, proceed without validation
        if (validator is null)
        {
            return await next(context);
        }

        // Validate the request
        var validationResult = await validator.ValidateAsync(request, CancellationToken.None);

        if (!validationResult.IsValid)
        {
            // Convert FluentValidation errors to ValidationProblem
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return Results.ValidationProblem(errors);
        }

        // Validation passed, proceed to next filter
        return await next(context);
    }
}

