using FluentValidation;
using MapaTributario.API.Application.Perfil.Contracts;

namespace MapaTributario.API.Validators;

public class AtualizarPerfilRequestValidator : AbstractValidator<AtualizarPerfilRequest>
{
    public AtualizarPerfilRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .MinimumLength(2).WithMessage("Nome deve ter no mínimo 2 caracteres");

        RuleFor(x => x.SenhaAtual)
            .NotEmpty().WithMessage("Senha atual é obrigatória para alterar a senha")
            .When(x => !string.IsNullOrEmpty(x.NovaSenha));

        RuleFor(x => x.NovaSenha)
            .MinimumLength(8).WithMessage("Nova senha deve ter no mínimo 8 caracteres")
            .When(x => !string.IsNullOrEmpty(x.NovaSenha));
    }
}
