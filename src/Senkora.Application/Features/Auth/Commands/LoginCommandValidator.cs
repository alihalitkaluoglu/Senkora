using FluentValidation;

namespace Senkora.Application.Features.Auth.Commands;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email bos olamaz.")
            .EmailAddress().WithMessage("Gecerli bir email girin.")
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Sifre bos olamaz.")
            .MinimumLength(6).WithMessage("Sifre en az 6 karakter olmalidir.");
    }
}
