using Application.Features.GoodsReceipts.Create;
using Application.Features.GoodsReceipts.List;
using Application.Features.GoodsReceipts.Update;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MyApp.Namespace
{
    [Route("api/goods-receipts")]
    [ApiController]
    public class GoodsReceiptsController : ControllerBase
    {
        private readonly ISender _sender;

        public GoodsReceiptsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        [Authorize]
        public async Task<IResult> Create(CreateGoodsReceiptCommand command, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(command, cancellationToken);

            return Results.Ok(result);
        }

        [HttpPut]
        [Route("{documentId}")]
        [Authorize]
        public async Task<IResult> Update(string documentId, [FromBody] UpdateGoodsReceiptCommand command
        , CancellationToken cancellationToken)
        {
            command.DocumentId = Guid.Parse(documentId);
            await _sender.Send(command, cancellationToken);

            return Results.Ok();
        }

        [HttpGet]
        [Authorize]
        public async Task<IResult> List([FromQuery] ListGoodsReceiptsQuery command, CancellationToken cancellationToken)
        {
            var data = await _sender.Send(command, cancellationToken);

            return Results.Ok(data);
        }
    }
}
