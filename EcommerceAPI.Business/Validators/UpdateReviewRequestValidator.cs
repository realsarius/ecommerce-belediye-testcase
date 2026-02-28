using EcommerceAPI.Business.Constants;
using EcommerceAPI.Entities.DTOs;
using FluentValidation;

namespace EcommerceAPI.Business.Validators;

public class UpdateReviewRequestValidator : AbstractValidator<UpdateReviewRequest>
{
    public UpdateReviewRequestValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage(Messages.ReviewRatingInvalid);

        RuleFor(x => x.Comment)
            .MaximumLength(1000)
            .WithMessage(Messages.ReviewCommentTooLong);
    }
}
