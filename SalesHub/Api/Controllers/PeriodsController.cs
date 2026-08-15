using Application.Features.Periods.Close;
using Application.Features.Periods.Create;
using Application.Features.Periods.Delete;
using Application.Features.Periods.Get;
using Application.Features.Periods.List;
using Application.Features.Periods.Update;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("/api/periods")]
public class PeriodController : ControllerBase
{
    private readonly ISender _sender;

    public PeriodController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize]
    public async Task<IResult> Create(CreatePeriodCommand command, CancellationToken cancellationToken)
    {
        var result =  await _sender.Send(command, cancellationToken);

        return Results.Ok(result);
    }

    [HttpGet]
    [Authorize]
    public async Task<IResult> List([FromQuery]ListPeriodQuery command, CancellationToken cancellationToken)
    {
        var data = await _sender.Send(command, cancellationToken);

        return Results.Ok(data);
    }

    [HttpPut]
    [Authorize]
    [Route("{periodId}")]
    public async Task<IResult> Update(int periodId
    , [FromBody]UpdatePeriodCommand command, CancellationToken cancellationToken)
    {
        command.PeriodId = periodId;
        await _sender.Send(command, cancellationToken);

        return Results.Ok();
    }

    [HttpGet]
    [Authorize]
    [Route("{periodId}")]
    public async Task<IResult> Get(int periodId, CancellationToken cancellationToken)
    {
        var row = await _sender.Send(new GetPeriodQuery { PeriodId = periodId }, cancellationToken);

        return Results.Ok(row);
    }

    [HttpDelete]
    [Authorize]
    [Route("{periodId}")]
    public async Task<IResult> Delete(int periodId, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeletePeriodCommand { PeriodId = periodId }, cancellationToken);

        return Results.Ok();
    }

    [HttpPost]
    [Authorize]
    [Route("{periodId}/close")]
    public async Task<IResult> Close(int periodId, CancellationToken cancellationToken)
    {
        await _sender.Send(new ClosePeriodCommand { PeriodId = periodId }, cancellationToken);

        return Results.Ok();
    }
}
