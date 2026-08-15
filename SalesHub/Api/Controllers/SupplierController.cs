using Application.Features.Branchs.Create;
using Application.Features.Suppliers.Create;
using Application.Features.Suppliers.Delete;
using Application.Features.Suppliers.Get;
using Application.Features.Suppliers.List;
using Application.Features.Suppliers.Update;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("/api/suppliers")]
public class SupplierController : ControllerBase
{
    private readonly ISender _sender;

    public SupplierController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize]
    public async Task<IResult> Create(CreateSupplierCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        return Results.Ok(result);
    }

    [HttpPut]
    [Authorize]
    [Route("{supplierId}")]
    public async Task<IResult> Update(int supplierId, [FromBody]UpdateSupplierCommand command
        , CancellationToken cancellationToken)
    {
        command.SupplierId = supplierId;
        await _sender.Send(command, cancellationToken);

        return Results.Ok();
    }

    [HttpGet]
    [Authorize]
    public async Task<IResult> List([FromQuery]ListSupplierQuery command, CancellationToken cancellationToken)
    {
        var data = await _sender.Send(command, cancellationToken);

        return Results.Ok(data);
    }

    [HttpGet]
    [Route("{supplierId}")]
    [Authorize]
    public async Task<IResult> Get(int supplierId, CancellationToken cancellationToken)
    {
        var row = await _sender.Send(new GetSupplierQuery { SupplierId = supplierId }, cancellationToken);

        return Results.Ok(row);
    }

    [HttpDelete]
    [Route("{supplierId}")]
    [Authorize]
    public async Task<IResult> Delete(int supplierId, CancellationToken cancellationToken)
    {
        await _sender.Send(new  DeleteSupplierCommand { SupplierId = supplierId }, cancellationToken);

        return Results.Ok();
    }
}