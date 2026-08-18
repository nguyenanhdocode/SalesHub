using FluentValidation;

namespace Application.Models.Documents;

public class CreateDocumentValidator<T> : AbstractValidator<T> where T: CreateDocumentCommand
{
    public CreateDocumentValidator()
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
