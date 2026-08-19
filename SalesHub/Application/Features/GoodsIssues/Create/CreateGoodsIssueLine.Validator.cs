using FluentValidation;

namespace Application.Features.GoodsIssues.Create;

public class CreateGoodsIssuesLineValidator : AbstractValidator<CreateGoodsIssueLineInput>
{
    public CreateGoodsIssuesLineValidator()
    {
        RuleFor(p => p.ProductId).NotNull();
        RuleFor(p => p.UnitId).NotNull();
        RuleFor(p => p.DocumentQuantity).NotNull().GreaterThan(0);
        RuleFor(p => p.ActualQuantity).NotNull().GreaterThan(0);
        RuleFor(p => p.Amount).NotNull().GreaterThan(0);
        RuleFor(p => p.UnitPrice).NotNull().GreaterThan(0);
    }
}
