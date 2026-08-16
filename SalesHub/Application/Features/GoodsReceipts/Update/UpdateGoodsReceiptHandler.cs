using System.ComponentModel;
using Application.Database;
using Application.Services;
using Application.Shared;
using Application.Shared.Documents;
using Dapper;
using Infrastructure.Security;
using MediatR;

namespace Application.Features.GoodsReceipts.Update;

public class UpdateGoodsReceiptHandler : IRequestHandler<UpdateGoodsReceiptCommand>
{
    private readonly DbSession _dbSession;
    private readonly CurrentUser _currentUser;
    private readonly DocumentNoService _docNoService;

    public UpdateGoodsReceiptHandler(DbSession dbSession
        , CurrentUser currentUser
        , DocumentNoService documentNoService)
    {
        _dbSession = dbSession;
        _currentUser = currentUser;
        _docNoService = documentNoService;
    }

    public async Task Handle(UpdateGoodsReceiptCommand request, CancellationToken cancellationToken)
    {
        
    }
}
