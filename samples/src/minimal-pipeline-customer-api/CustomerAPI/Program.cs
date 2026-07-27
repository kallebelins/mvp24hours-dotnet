using CustomerAPI.Core.Resources;
using CustomerAPI.Extensions;
using CustomerAPI.Operations;
using CustomerAPI.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;

#region [ Configure Services ]
var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure(builder.Configuration);
#endregion

#region [ Configure Application ]
var app = builder.Build();
app.Configure(app.Environment);
#endregion

#region [ Routes ]

#region [ Customers ]

app.MapGet("/customer", async (CustomerQueryRequest model, [FromServices] IPipelineAsync pipeline) =>
{
    // add operations/steps
    pipeline.Add<GetCustomerClientStep>();
    pipeline.Add<GetByCustomerMapperResponseStep>();

    // run pipeline with package with content (int -> id)
    await pipeline.ExecuteAsync(model.ToMessage());

    // try to get response content
    var message = pipeline.GetMessage();

    // checks for failure in the notification context
    if (message.IsFaulty)
    {
        return Results.BadRequest(message.Messages.ToBusiness<IList<CustomerResponse>>());
    }

    // get message content
    var result = message.GetContent<List<CustomerResponse>>();

    // checks if there are any records
    if (!result.AnySafe())
    {
        // reply with standard message for record not found
        return Results.NotFound(Messages.RECORD_NOT_FOUND
            .ToMessageResult(MessageType.Error)
                .ToBusiness<IList<CustomerResponse>>());
    }

    return Results.Ok(result.ToBusiness());
})
.Produces(StatusCodes.Status200OK, typeof(ActionResult<IBusinessResult<IList<CustomerResponse>>>))
.Produces(StatusCodes.Status404NotFound, typeof(ActionResult<IBusinessResult<IList<CustomerResponse>>>))
.WithName("CustomerGetBy")
.WithOpenApi();

app.MapGet("/customer/{id}", async (int id, [FromServices] IPipelineAsync pipeline) =>
{
    // add operations/steps
    pipeline.Add<GetCustomerClientStep>();
    pipeline.Add<GetByIdCustomerMapperResponseStep>();

    // run pipeline with package with content (int -> id)
    await pipeline.ExecuteAsync(id.ToMessage("id"));

    // try to get response content
    var message = pipeline.GetMessage();

    // checks for failure in the notification context
    if (message.IsFaulty)
    {
        return Results.BadRequest(message.Messages.ToBusiness<CustomerIdResponse>());
    }

    // get message content
    var result = message.GetContent<CustomerIdResponse>();

    if (result == null)
    {
        // reply with standard message for record not found
        return Results.NotFound(Messages.RECORD_NOT_FOUND_FOR_ID
            .ToMessageResult(MessageType.Error)
                .ToBusiness<CustomerIdResponse>());
    }

    return Results.Ok(result.ToBusiness());
})
.Produces(StatusCodes.Status200OK, typeof(ActionResult<IBusinessResult<CustomerIdResponse>>))
.Produces(StatusCodes.Status404NotFound, typeof(ActionResult<IBusinessResult<CustomerIdResponse>>))
.Produces(StatusCodes.Status400BadRequest, typeof(ActionResult<IBusinessResult<CustomerIdResponse>>))
.WithName("CustomerGetById")
.WithOpenApi();

#endregion

#endregion

app.Run();

