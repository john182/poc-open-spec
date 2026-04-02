using FluentValidation;
using MapaTributario.API.Application.Consulta.Contracts;

namespace MapaTributario.API.Validators;

public class AliquotaQueryParamsValidator : AbstractValidator<AliquotaQueryParams>
{
    public AliquotaQueryParamsValidator()
    {
        RuleFor(x => x.Pagina)
            .GreaterThanOrEqualTo(1).WithMessage("Página deve ser maior ou igual a 1");

        RuleFor(x => x.TamanhoPagina)
            .InclusiveBetween(1, 100).WithMessage("Tamanho da página deve ser entre 1 e 100");

        RuleFor(x => x.AliquotaMin)
            .GreaterThanOrEqualTo(0).When(x => x.AliquotaMin.HasValue)
            .WithMessage("Alíquota mínima deve ser maior ou igual a 0");

        RuleFor(x => x.AliquotaMax)
            .GreaterThanOrEqualTo(0).When(x => x.AliquotaMax.HasValue)
            .WithMessage("Alíquota máxima deve ser maior ou igual a 0");

        RuleFor(x => x)
            .Must(x => !x.AliquotaMin.HasValue || !x.AliquotaMax.HasValue || x.AliquotaMin <= x.AliquotaMax)
            .WithMessage("Alíquota mínima não pode ser maior que a alíquota máxima");
    }
}
