using Mvp24Hours.WebAPI.Extensions;
using CustomerAPI.Core.Resources;
using CustomerAPI.Entities;
using CustomerAPI.Extensions;
using CustomerAPI.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.DTOs.Models;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.Helpers;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using Mvp24Hours.WebAPI.Binders;
using System.Linq.Expressions;
using System.Net;



#region [ Configure Services ]
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTimeProvider();
builder.Services.Configure(builder.Configuration);
builder.Services.AddNativeProblemDetailsAll(builder.Environment);
#endregion



#region [ Configure Application ]
var app = builder.Build();
app.Configure();
if (!app.Environment.IsProduction())
{
    app.MapMvp24HoursNativeOpenApi();
}
await app.MigrateDatabaseAsync();
#endregion



#region [ Routes ]



#region [ Customers ]



app.MapGet("/customer", async Task<IResult> (
    CustomerQuery model,
    ModelBinder<PagingCriteriaRequest> pagingCriteriaBinder,
    [FromServices] IUnitOfWorkAsync uoW,
    CancellationToken cancellationToken) =>
{
    if (pagingCriteriaBinder.Error != null)
        return TypedResults.BadRequest(pagingCriteriaBinder.Error.ToMessageResult());



    var pagingCriteria = pagingCriteriaBinder.Data;



    // construct expression to apply filter on database
    Expression<Func<Customer, bool>> clause =
        x => (string.IsNullOrEmpty(model.Name) || x.Name.Contains(model.Name))
            && (model.Active == null || x.Active == model.Active);



    // apply filter with pagination
    var repository = uoW.GetRepository<Customer>();
    var criteria = pagingCriteria.ToPagingCriteria();
    int limit = criteria.Limit > 0 ? criteria.Limit : ContantsHelper.Data.MaxQtyByQueryPage;
    int offset = criteria.Offset;
    int totalCount = await repository.GetByCountAsync(clause, cancellationToken);
    int totalPages = (int)Math.Ceiling((double)totalCount / limit);
    var items = await repository.GetByAsync(clause, criteria, cancellationToken);
    var result = items.ToBusinessPaging(
        new PageResult(limit, offset, items.Count),
        new SummaryResult(totalCount, totalPages));



    // checks if there are any records in the database from the filter
    if (!result.HasData())
    {
        // reply with standard message for record not found
        return TypedResults.NotFound(Messages.RECORD_NOT_FOUND
            .ToMessageResult(MessageType.Error)
                .ToBusiness<IList<Customer>>());
    }



    return TypedResults.Ok(result);
})
.Produces(StatusCodes.Status200OK, typeof(ActionResult<IPagingResult<IList<Customer>>>))
.Produces(StatusCodes.Status404NotFound, typeof(ActionResult<IBusinessResult<IList<Customer>>>))
.WithName("CustomerGetBy");



app.MapGet("/customer/{id}", async Task<IResult> (int id, [FromServices] IUnitOfWorkAsync uoW, CancellationToken cancellationToken) =>
{
    var model = await uoW.GetRepository<Customer>().GetByIdAsync(id, cancellationToken);
    if (model == null)
    {
        // reply with standard message for record not found
        return TypedResults.NotFound(Messages.RECORD_NOT_FOUND_FOR_ID
            .ToMessageResult(MessageType.Error)
                .ToBusiness<Customer>());
    }
    return TypedResults.Ok(model.ToBusiness());
})
.Produces(StatusCodes.Status200OK, typeof(ActionResult<IBusinessResult<Customer>>))
.Produces(StatusCodes.Status404NotFound, typeof(ActionResult<IBusinessResult<Customer>>))
.WithName("CustomerGetById");



app.MapPost("/customer", async Task<IResult> (Customer model, [FromServices] IUnitOfWorkAsync uoW, [FromServices] IValidator<Customer> validator, CancellationToken cancellationToken) =>
{
    // apply data validation to the model/entity with FluentValidation or DataAnnotation
    var errors = model.TryValidate(validator);



    if (!errors.AnySafe())
    {
        await uoW.GetRepository<Customer>().AddAsync(model, cancellationToken);
        if (await uoW.SaveChangesAsync(cancellationToken) > 0)
        {
            return TypedResults.Created($"/customer/{model.Id}", model.ToBusiness());
        }
    }
    // get message in request context, if not, use default message
    return TypedResults.BadRequest(errors
        .ToBusiness<Customer>(
            defaultMessage: Messages.OPERATION_FAIL
                .ToMessageResult(MessageType.Error))
    );
})
.Produces(StatusCodes.Status201Created, typeof(ActionResult<IBusinessResult<Customer>>))
.Produces(StatusCodes.Status400BadRequest, typeof(ActionResult<IBusinessResult<Customer>>))
.WithName("CustomerCreate");



app.MapPut("/customer/{id}", async Task<IResult> (int id, Customer model, [FromServices] IUnitOfWorkAsync uoW, [FromServices] IValidator<Customer> validator, CancellationToken cancellationToken) =>
{
    // apply data validation to the model/entity with FluentValidation or DataAnnotation
    var errors = model.TryValidate(validator);



    if (!errors.AnySafe())
    {
        // gets entity through the identifier informed in the resource
        var modelDb = await uoW.GetRepository<Customer>().GetByIdAsync(id, cancellationToken);
        if (modelDb == null)
        {
            return TypedResults.NotFound(Messages.RECORD_NOT_FOUND_FOR_ID
                .ToMessageResult(MessageType.Error)
                    .ToBusiness<Customer>());
        }



        // properties that cannot be overridden
        model.Id = modelDb.Id;
        model.Created = modelDb.Created;



        // apply changes to database
        await uoW.GetRepository<Customer>().ModifyAsync(model, cancellationToken);
        if (await uoW.SaveChangesAsync(cancellationToken) == 0)
        {
            return TypedResults.StatusCode((int)HttpStatusCode.NotModified);
        }
        else
        {
            return TypedResults.Ok(model.ToBusiness(Messages.OPERATION_SUCCESS.ToMessageResult(MessageType.Success)));
        }
    }
    // get message in request context, if not, use default message
    return TypedResults.BadRequest(errors
        .ToBusiness<Customer>(
            defaultMessage: Messages.OPERATION_FAIL
                .ToMessageResult(MessageType.Error))
    );
})
.Produces(StatusCodes.Status200OK, typeof(ActionResult<IBusinessResult<Customer>>))
.Produces(StatusCodes.Status404NotFound, typeof(ActionResult<IBusinessResult<Customer>>))
.Produces(StatusCodes.Status400BadRequest, typeof(ActionResult<IBusinessResult<Customer>>))
.Produces(StatusCodes.Status304NotModified)
.WithName("CustomerUpdate");





app.MapDelete("/customer/{id}", async Task<IResult> (int id, [FromServices] IUnitOfWorkAsync uoW, CancellationToken cancellationToken) =>
{
    // try to retrieve entity by identifier
    var model = await uoW.GetRepository<Customer>().GetByIdAsync(id, cancellationToken);
    if (model == null)
    {
        return TypedResults.NotFound(Messages.RECORD_NOT_FOUND_FOR_ID
            .ToMessageResult(MessageType.Error)
                .ToBusiness<Customer>());
    }



    // perform delete action
    await uoW.GetRepository<Customer>().RemoveAsync(model, cancellationToken);
    if (await uoW.SaveChangesAsync(cancellationToken) > 0)
    {
        return TypedResults.Ok(Messages.OPERATION_SUCCESS
            .ToMessageResult(MessageType.Success)
                .ToBusiness<Customer>());
    }
    // get message in request context, if not, use default message
    return TypedResults.BadRequest(Messages.OPERATION_FAIL
        .ToMessageResult(MessageType.Error)
        .ToBusiness<Customer>()
    );
})
.Produces(StatusCodes.Status200OK, typeof(ActionResult<IBusinessResult<Customer>>))
.Produces(StatusCodes.Status404NotFound, typeof(ActionResult<IBusinessResult<Customer>>))
.Produces(StatusCodes.Status400BadRequest, typeof(ActionResult<IBusinessResult<Customer>>))
.WithName("CustomerDelete");



#endregion



#endregion



app.Run();

public partial class Program { }
