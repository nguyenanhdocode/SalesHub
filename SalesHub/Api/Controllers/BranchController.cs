using Application.Features.Branchs.Create;
using Application.Features.Branchs.Delete;
using Application.Features.Branchs.Get;
using Application.Features.Branchs.List;
using Application.Features.Branchs.Update;
using Application.Features.Units.Create;
using Application.Features.User.Create;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("/api/branchs")]
public class BranchController : ControllerBase
{
    private readonly ISender _sender;

    public BranchController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize]
    public async Task<IResult> Create(CreateBranchCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        return Results.Ok(result);
    }

    [HttpDelete]
    [Route("{branchId}")]
    [Authorize]
    public async Task<IResult> Delete(int branchId, CancellationToken cancellationToken)
    {
        await _sender.Send(new  DeleteBranchCommand { BranchId = branchId }, cancellationToken);

        return Results.Ok();
    }

    [HttpPut]
    [Route("{branchId}")]
    [Authorize]
    public async Task<IResult> Update(int branchId, [FromBody] UpdateBranchCommand command
        , CancellationToken cancellationToken)
    {
        command.BranchId = branchId;
        await _sender.Send(command, cancellationToken);

        return Results.Ok();
    }

    [HttpGet]
    [Authorize]
    public async Task<IResult> List([FromQuery]ListBranchQuery command, CancellationToken cancellationToken)
    {
        var data = await _sender.Send(command, cancellationToken);

        return Results.Ok(data);
    }

    [HttpGet]
    [Route("{branchId}")]
    [Authorize]
    public async Task<IResult> Get(int branchId, CancellationToken cancellationToken)
    {
        var row = await _sender.Send(new GetBranchQuery { BranchId = branchId }, cancellationToken);

        return Results.Ok(row);
    }
}
