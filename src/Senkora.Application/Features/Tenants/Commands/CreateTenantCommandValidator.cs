using FluentValidation;

namespace Senkora.Application.Features.Tenants.Commands;

public sealed class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Firma adi bos olamaz.")
            .MaximumLength(200);

        RuleFor(x => x.Subdomain)
            .NotEmpty().WithMessage("Subdomain bos olamaz.")
            .MaximumLength(100)
            .Matches("^[a-z0-9-]+$")
            .WithMessage("Subdomain sadece kucuk harf, rakam ve tire icermelidir.");

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("Email bos olamaz.")
            .EmailAddress().WithMessage("Gecerli bir email girin.");
    }
}
