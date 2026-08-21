using Application.Models.Documents;
using FluentValidation;

namespace Application.Features.GoodsIssues.Update;

public class UpdateGoodsIssuesValidator : UpdateDocumentValidator<UpdateGoodsIssueCommand>
{
    public UpdateGoodsIssuesValidator()
    {
        RuleFor(p => p.Reason).NotNull().NotEmpty().MaximumLength(1000);
        RuleFor(p => p.Lines)
            .NotEmpty()
            .Must(lines => !lines.GroupBy(p => new {p.ProductId, p.UnitId}).Any(g => g.Count() > 1));
        RuleForEach(p => p.Lines).SetValidator(new UpdateGoodsIssuesLineValidator());
    }
}
