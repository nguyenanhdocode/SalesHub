using Application.Models.Documents;
using FluentValidation;

namespace Application.Features.GoodsReceipts.Create;

public class CreateGoodsReceiptValidator : CreateDocumentValidator<CreateGoodsReceiptCommand>
{
    public CreateGoodsReceiptValidator()
    {
        RuleFor(p => p.ShipperName)
            .MaximumLength(50);

        RuleFor(p => p.WarehouseId)
            .NotNull();

        RuleFor(p => p.Lines)
            .NotEmpty()
            .Must(lines => !lines.GroupBy(p => new {p.ProductId, p.UnitId}).Any(g => g.Count() > 1));

        RuleForEach(p => p.Lines)
            .SetValidator(new GoodsReceiptLineInputValidator());
    }   
}
