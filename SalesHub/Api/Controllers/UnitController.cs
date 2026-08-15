using Application.Features.Units.Create;
using Application.Features.Units.Delete;
using Application.Features.Units.Get;
using Application.Features.Units.List;
using Application.Features.User.Create;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("/api/units")]
public class UnitController : ControllerBase
{
    private readonly ISender _sender;

    public UnitController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize]
    public async Task<IResult> Create(CreateUnitCommand command, CancellationToken cancellationToken)
    {
        var result =  await _sender.Send(command, cancellationToken);

        return Results.Ok(result);
    }

    [HttpPut]
    [Authorize]
    [Route("{unitId}")]
    public async Task<IResult> Update(int unitId
    , [FromBody]UpdateUnitCommand command, CancellationToken cancellationToken)
    {
        command.UnitId = unitId;
        await _sender.Send(command, cancellationToken);

        return Results.Ok();
    }

    [HttpGet]
    public async Task<IResult> List([FromQuery]ListUnitQuery command, CancellationToken cancellationToken)
    {
        var data = await _sender.Send(command, cancellationToken);

        return Results.Ok(data);
    }

    [HttpDelete]
    [Authorize]
    [Route("{unitId}")]
    public async Task<IResult> Delete(int unitId, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteUnitCommand { UnitId = unitId }, cancellationToken);

        return Results.Ok();
    }

    [HttpGet]
    [Authorize]
    [Route("{unitId}")]
    public async Task<IResult> Get(int unitId, CancellationToken cancellationToken)
    {
        var row = await _sender.Send(new GetUnitQuery { UnitId = unitId }, cancellationToken);

        return Results.Ok(row);
    }
}
