using Application.Features.InventoryOpenings.CarryForward;
using Application.Features.InventoryOpenings.Create;
using Application.Features.InventoryOpenings.Delete;
using Application.Features.InventoryOpenings.Get;
using Application.Features.InventoryOpenings.List;
using Application.Features.InventoryOpenings.Update;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MyApp.Namespace
{
    [Route("api/inventory-openings")]
    [ApiController]
    public class InventoryOpeningsController : ControllerBase
    {
        private readonly ISender _sender;

        public InventoryOpeningsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        [Authorize]
        public async Task<IResult> Create(CreateInventoryOpeningCommand command, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(command, cancellationToken);

            return Results.Ok(result);
        }

        [HttpGet]
        [Authorize]
        public async Task<IResult> List([FromQuery] ListInventoryOpeningQuery command, CancellationToken cancellationToken)
        {
            var data = await _sender.Send(command, cancellationToken);

            return Results.Ok(data);
        }

        [HttpPut]
        [Route("{documentId}")]
        [Authorize]
        public async Task<IResult> Update(string documentId, [FromBody] UpdateInventoryOpeningCommand command
        , CancellationToken cancellationToken)
        {
            command.DocumentId = Guid.Parse(documentId);
            await _sender.Send(command, cancellationToken);

            return Results.Ok();
        }

        [HttpGet]
        [Authorize]
        [Route("{documentId}")]
        public async Task<IResult> Get(Guid documentId, CancellationToken cancellationToken)
        {
            var row = await _sender.Send(new GetInventoryOpeningQuery { DocumentId = documentId }, cancellationToken);

            return Results.Ok(row);
        }

        [HttpDelete]
        [Authorize]
        [Route("{documentId}")]
        public async Task<IResult> Delete(Guid documentId, CancellationToken cancellationToken)
        {
            await _sender.Send(new DeleteInventoryOpeningCommand { DocumentId = documentId }, cancellationToken);

            return Results.Ok();
        }

        [HttpPost]
        [Authorize]
        [Route("{periodId}/carry-forward")]
        public async Task<IResult> CarryForward(int periodId, [FromBody]CarryForwardCommand command, CancellationToken cancellationToken)
        {
            command.SrcPeriodId = periodId;
            await _sender.Send(command, cancellationToken);

            return Results.Ok();
        }
    }
}
