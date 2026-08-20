using Application.Features.GoodsIssues.Create;
using Application.Features.GoodsIssues.Delete;
using Application.Features.GoodsIssues.Get;
using Application.Features.GoodsIssues.List;
using Application.Features.GoodsIssues.Update;
using Application.Features.GoodsReceipts.Create;
using Application.Features.GoodsReceipts.Get;
using Application.Features.GoodsReceipts.List;
using Application.Features.GoodsReceipts.Update;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MyApp.Namespace
{
    [Route("api/goods-issues")]
    [ApiController]
    public class GoodsIssuesController : ControllerBase
    {
        private readonly ISender _sender;

        public GoodsIssuesController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        [Authorize]
        public async Task<IResult> Create(CreateGoodsIssueCommand command, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(command, cancellationToken);

            return Results.Ok(result);
        }

        [HttpPut]
        [Route("{documentId}")]
        [Authorize]
        public async Task<IResult> Update(string documentId, [FromBody] UpdateGoodsIssueCommand command
        , CancellationToken cancellationToken)
        {
            command.DocumentId = Guid.Parse(documentId);
            await _sender.Send(command, cancellationToken);

            return Results.Ok();
        }

        [HttpGet]
        [Authorize]
        public async Task<IResult> List([FromQuery] ListGoodsIssueQuery command, CancellationToken cancellationToken)
        {
            var data = await _sender.Send(command, cancellationToken);

            return Results.Ok(data);
        }

        [HttpGet]
        [Authorize]
        [Route("{documentId}")]
        public async Task<IResult> Get(Guid documentId, CancellationToken cancellationToken)
        {
            var row = await _sender.Send(new GetGoodsIssueQuery { DocumentId = documentId }, cancellationToken);

            return Results.Ok(row);
        }

        [HttpDelete]
        [Route("{documentId}")]
        [Authorize]
        public async Task<IResult> Delete(Guid documentId, CancellationToken cancellationToken)
        {
            await _sender.Send(new DeleteGoodsIssueCommand {DocumentId = documentId}, cancellationToken);

            return Results.Ok();
        }
    }
}
