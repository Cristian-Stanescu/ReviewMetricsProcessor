using FluentValidation;
using ReviewMetricsProcessor.Endpoints.Reviews;

namespace ReviewMetricsProcessor.Validators;

public class ReviewEventValidator : AbstractValidator<ReviewEvent>
{
    public ReviewEventValidator()
    {
        RuleFor(x => x.Type)
            .NotNull()
            .NotEmpty()
            .WithMessage("Type is required and cannot be null or empty.");

        RuleFor(x => x.ReviewId)
            .NotNull()
            .NotEmpty()
            .WithMessage("ReviewId is required and cannot be null or empty.");

        RuleFor(x => x.AuthorId)
            .NotNull()
            .NotEmpty()
            .WithMessage("AuthorId is required and cannot be null or empty.");

        RuleFor(x => x.LinesOfCodeReviewed)
            .NotNull()
            .WithMessage("LinesOfCodeReviewed is required for ReviewCompleted events.")
            .GreaterThan(0)
            .When(x => x.Type == "ReviewCompleted")
            .WithMessage("LinesOfCodeReviewed must be positive for ReviewCompleted events.");
    }
}