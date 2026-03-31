using FluentValidation;
using MapaTributario.API.Application.Auth.Contracts;

namespace MapaTributario.API.Validators;

public class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("RefreshToken é obrigatório");
    }
}
