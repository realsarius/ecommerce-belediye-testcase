using EcommerceAPI.Entities.DTOs;
using FluentValidation;

namespace EcommerceAPI.Business.Validators;

public class UpdateNotificationTemplateRequestValidator : AbstractValidator<UpdateNotificationTemplateRequest>
{
    public UpdateNotificationTemplateRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(512);

        RuleFor(x => x.TitleExample)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.BodyExample)
            .NotEmpty()
            .MaximumLength(1024);
    }
}
