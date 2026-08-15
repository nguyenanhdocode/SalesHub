using Application.Features.Products.Create;
using Application.Features.Products.Delete;
using Application.Features.Products.Get;
using Application.Features.Products.List;
using Application.Features.Products.UnitConversions.List;
using Application.Features.Products.UnitConversions.Update;
using Application.Features.Products.Update;
using Application.Features.Units.Create;
using Application.Features.Units.Delete;
using Application.Features.Units.Get;
using Application.Features.Units.List;
using Application.Features.User.Create;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("/api/products")]
public class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize]
    public async Task<IResult> Create(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var result =  await _sender.Send(command, cancellationToken);

        return Results.Ok(result);
    }

    [HttpPut]
    [Authorize]
    [Route("{productId}")]
    public async Task<IResult> Update(int productId
    , [FromBody]UpdateProductCommand command, CancellationToken cancellationToken)
    {
        command.ProductId = productId;
        await _sender.Send(command, cancellationToken);

        return Results.Ok();
    }

    [HttpGet]
    public async Task<IResult> List([FromQuery]ListProductQuery command, CancellationToken cancellationToken)
    {
        var data = await _sender.Send(command, cancellationToken);

        return Results.Ok(data);
    }

    [HttpDelete]
    [Authorize]
    [Route("{productId}")]
    public async Task<IResult> Delete(int productId, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteProductCommand { ProductId = productId }, cancellationToken);

        return Results.Ok();
    }

    [HttpGet]
    [Authorize]
    [Route("{productId}")]
    public async Task<IResult> Get(int productId, CancellationToken cancellationToken)
    {
        var row = await _sender.Send(new GetProductQuery { ProductId = productId }, cancellationToken);

        return Results.Ok(row);
    }

    [HttpPut]
    [Authorize]
    [Route("{productId}/unit-conversions")]
    public async Task<IResult> UpdateUnitConversions(int productId
    , [FromBody]UpdateUnitConversionsCommand command, CancellationToken cancellationToken)
    {
        command.ProductId = productId;
        await _sender.Send(command, cancellationToken);

        return Results.Ok();
    }

    [HttpGet]
    [Authorize]
    [Route("{productId}/unit-conversions")]
    public async Task<IResult> ListUnitConversions(int productId, CancellationToken cancellationToken)
    {
        var units = await _sender.Send(new ListUnitConversionsQuery { ProductId = productId }, cancellationToken);

        return Results.Ok(units);
    }
}
