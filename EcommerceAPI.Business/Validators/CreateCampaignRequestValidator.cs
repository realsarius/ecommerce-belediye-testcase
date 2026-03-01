using EcommerceAPI.Entities.DTOs;
using FluentValidation;

namespace EcommerceAPI.Business.Validators;

public class CreateCampaignRequestValidator : AbstractValidator<CreateCampaignRequest>
{
    public CreateCampaignRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.StartsAt)
            .LessThan(x => x.EndsAt);

        RuleFor(x => x.Products)
            .NotEmpty();

        RuleForEach(x => x.Products).ChildRules(product =>
        {
            product.RuleFor(x => x.ProductId).GreaterThan(0);
            product.RuleFor(x => x.CampaignPrice).GreaterThan(0);
        });
    }
}
