using EcommerceAPI.Entities.DTOs;
using FluentValidation;

namespace EcommerceAPI.Business.Validators;

public class ReorderCartRequestValidator : AbstractValidator<ReorderCartRequest>
{
    public ReorderCartRequestValidator()
    {
        RuleFor(x => x.OrderId)
            .GreaterThan(0)
            .WithMessage("Geçerli bir sipariş ID giriniz");
    }
}
