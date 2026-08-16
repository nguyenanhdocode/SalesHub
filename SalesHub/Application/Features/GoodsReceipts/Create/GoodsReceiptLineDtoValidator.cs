using FluentValidation;

namespace Application.Features.GoodsReceipts.Create;

public class GoodsReceiptLineDtoValidator : AbstractValidator<GoodsReceiptLineDto>
{
    public GoodsReceiptLineDtoValidator()
    {
        RuleFor(p => p.ProductId).NotNull();
        RuleFor(p => p.UnitId).NotNull();
        RuleFor(p => p.DocumentQuantity).NotNull().GreaterThan(0);
        RuleFor(p => p.ActualQuantity).NotNull().GreaterThan(0);
        RuleFor(p => p.Amount).NotNull().GreaterThan(0);
        RuleFor(p => p.UnitPrice).NotNull().GreaterThan(0);
    }
}
