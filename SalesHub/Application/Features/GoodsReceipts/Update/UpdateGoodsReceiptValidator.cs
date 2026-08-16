using Application.Models.Documents;
using FluentValidation;

namespace Application.Features.GoodsReceipts.Update;

public class UpdateGoodsReceiptValidator : UpdateDocumentValidator<UpdateGoodsReceiptCommand>
{
    public UpdateGoodsReceiptValidator()
    {
        RuleFor(p => p.ShipperName)
            .MaximumLength(50);

        RuleFor(p => p.Lines)
            .NotEmpty();

        RuleForEach(p => p.Lines)
            .SetValidator(new GoodsReceiptLineDtoValidator());
    }   
}
