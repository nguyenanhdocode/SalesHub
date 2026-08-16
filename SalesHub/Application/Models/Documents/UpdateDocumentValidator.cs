using FluentValidation;

namespace Application.Models.Documents;

public class UpdateDocumentValidator<T> : AbstractValidator<T> where T: UpdateDocumentCommand
{
    public UpdateDocumentValidator()
    {
        RuleFor(p => p.PostingDate)
            .NotNull()
            .NotEmpty();

        RuleFor(p => p.DocumentDate)
            .NotNull()
            .NotEmpty();

        RuleFor(p => p.PeriodId)
            .NotNull();

        RuleFor(p => p.Note)
            .MaximumLength(1000);

        RuleFor(p => p.Status)
            .NotNull();
    }
}
