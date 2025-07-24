using FluentValidation;
using ReviewMetricsProcessor.Endpoints.Reviews;

namespace ReviewMetricsProcessor.Validators;

public class ReviewEventValidator : AbstractValidator<ReviewEvent>
{
    public const string ReviewStartedType = "ReviewStarted";
    public const string ReviewCompletedType = "ReviewCompleted";
    
    private static readonly string[] ValidTypes = { ReviewStartedType, ReviewCompletedType };

    public ReviewEventValidator()
    {
        RuleFor(x => x.Type)
            .NotNull()
            .NotEmpty()
            .WithMessage("Type is required and cannot be null or empty.")
            .Must(type => ValidTypes.Contains(type))
            .WithMessage($"Type must be one of: {string.Join(", ", ValidTypes)}.");

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
            .When(x => x.Type == ReviewCompletedType)
            .WithMessage("LinesOfCodeReviewed must be positive for ReviewCompleted events.");
    }
}