using FluentValidation;

namespace Application.Features.GoodsReceipts.Update;

public class GoodsReceiptLineInputValidator : AbstractValidator<GoodsReceiptLineInput>
{
    public GoodsReceiptLineInputValidator()
    {
        RuleFor(p => p.ProductId).NotNull();
        RuleFor(p => p.UnitId).NotNull();
        RuleFor(p => p.DocumentQuantity).NotNull().GreaterThan(0);
        RuleFor(p => p.ActualQuantity).NotNull().GreaterThan(0);
        RuleFor(p => p.Amount).NotNull().GreaterThan(0);
        RuleFor(p => p.UnitPrice).NotNull().GreaterThan(0);
    }
}
