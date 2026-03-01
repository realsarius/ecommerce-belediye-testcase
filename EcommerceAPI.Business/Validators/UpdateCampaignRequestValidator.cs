using EcommerceAPI.Entities.DTOs;
using FluentValidation;

namespace EcommerceAPI.Business.Validators;

public class UpdateCampaignRequestValidator : AbstractValidator<UpdateCampaignRequest>
{
    public UpdateCampaignRequestValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(200);

        When(x => x.StartsAt.HasValue && x.EndsAt.HasValue, () =>
        {
            RuleFor(x => x.StartsAt!.Value)
                .LessThan(x => x.EndsAt!.Value);
        });

        RuleForEach(x => x.Products!).ChildRules(product =>
        {
            product.RuleFor(x => x.ProductId).GreaterThan(0);
            product.RuleFor(x => x.CampaignPrice).GreaterThan(0);
        }).When(x => x.Products != null);
    }
}
