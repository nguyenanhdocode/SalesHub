using Application.Features.Periods.Close;
using Application.Features.Periods.Create;
using Application.Features.Periods.Delete;
using Application.Features.Periods.Get;
using Application.Features.Periods.List;
using Application.Features.Periods.Update;
using Application.Features.Warehouses.Create;
using Application.Features.Warehouses.Delete;
using Application.Features.Warehouses.Get;
using Application.Features.Warehouses.List;
using Application.Features.Warehouses.Update;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("/api/warehouses")]
public class WarehousesController : ControllerBase
{
    private readonly ISender _sender;

    public WarehousesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize]
    public async Task<IResult> Create(CreateWarehouseCommand command, CancellationToken cancellationToken)
    {
        var result =  await _sender.Send(command, cancellationToken);

        return Results.Ok(result);
    }

    [HttpPut]
    [Authorize]
    [Route("{warehouseId}")]
    public async Task<IResult> Update(int warehouseId
    , [FromBody]UpdateWarehouseCommand command, CancellationToken cancellationToken)
    {
        command.WarehouseId = warehouseId;
        await _sender.Send(command, cancellationToken);

        return Results.Ok();
    }

    [HttpGet]
    [Authorize]
    [Route("{warehouseId}")]
    public async Task<IResult> Get(int warehouseId, CancellationToken cancellationToken)
    {
        var row = await _sender.Send(new GetWarehouseQuery { WarehouseId = warehouseId }, cancellationToken);

        return Results.Ok(row);
    }

    [HttpGet]
    public async Task<IResult> List([FromQuery]ListWarehouseQuery command, CancellationToken cancellationToken)
    {
        var data = await _sender.Send(command, cancellationToken);

        return Results.Ok(data);
    }

    [HttpDelete]
    [Authorize]
    [Route("{warehouseId}")]
    public async Task<IResult> Delete(int warehouseId, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteWarehouseCommand { WarehouseId = warehouseId }, cancellationToken);

        return Results.Ok();
    }
}