using Application.Models.Documents;
using FluentValidation;

namespace Application.Features.GoodsIssues.Update;

public class UpdateGoodsIssuesValidator : UpdateDocumentValidator<UpdateGoodsIssueCommand>
{
    public UpdateGoodsIssuesValidator()
    {
        RuleFor(p => p.Reason).NotNull().NotEmpty().MaximumLength(1000);
    }
}
