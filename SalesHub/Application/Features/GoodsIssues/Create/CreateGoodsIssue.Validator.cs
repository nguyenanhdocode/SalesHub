using Application.Models.Documents;
using FluentValidation;

namespace Application.Features.GoodsIssues.Create;

public class CreateGoodsIssuesValidator : CreateDocumentValidator<CreateGoodsIssueCommand>
{
    public CreateGoodsIssuesValidator()
    {
        RuleFor(p => p.WarehouseId).NotNull();
        RuleFor(p => p.Reason).NotNull().NotEmpty().MaximumLength(1000);
    }
}
