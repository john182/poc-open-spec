using FluentValidation;
using MapaTributario.API.Application.Crawler.Contracts;

namespace MapaTributario.API.Application.Crawler.Validators;

public class AtualizarConfiguracaoCrawlerRequestValidator : AbstractValidator<AtualizarConfiguracaoCrawlerRequest>
{
    private const string PadraoCronValido = @"^(\S+\s+){4}\S+$";
    private const string PadraoCodigoSondagem = @"^\d{2}\.\d{2}\.\d{2}$";

    public AtualizarConfiguracaoCrawlerRequestValidator()
    {
        RuleFor(x => x.CronSchedule)
            .NotEmpty().WithMessage("'{PropertyName}' não pode ser vazio")
            .Matches(PadraoCronValido).WithMessage("'{PropertyName}' deve ser uma expressão cron válida com 5 partes");

        RuleFor(x => x.LimiteRequisicoesPorSegundo)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(100).WithMessage("'{PropertyName}' deve ser no máximo 100");

        RuleFor(x => x.OrcamentoDiario)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(500000).WithMessage("'{PropertyName}' deve ser no máximo 500000");

        RuleFor(x => x.TamanheLoteCertificado)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(1000).WithMessage("'{PropertyName}' deve ser no máximo 1000");

        RuleFor(x => x.PausaLoteSegundos)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(300).WithMessage("'{PropertyName}' deve ser no máximo 300");

        RuleFor(x => x.TamanheLoteMongo)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(500).WithMessage("'{PropertyName}' deve ser no máximo 500");

        RuleFor(x => x.MaxTentativas)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(10).WithMessage("'{PropertyName}' deve ser no máximo 10");

        RuleFor(x => x.LimiteParadaAntecipada)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(50).WithMessage("'{PropertyName}' deve ser no máximo 50");

        RuleFor(x => x.MaxDesdobramento)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(100).WithMessage("'{PropertyName}' deve ser no máximo 100");

        RuleFor(x => x.MaxDetalhamento)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(999).WithMessage("'{PropertyName}' deve ser no máximo 999");

        RuleFor(x => x.MaxFalhasConsecutivasDetalhamento)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(20).WithMessage("'{PropertyName}' deve ser no máximo 20");

        RuleFor(x => x.MaxFalhasConsecutivasDesdobramento)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(20).WithMessage("'{PropertyName}' deve ser no máximo 20");

        RuleFor(x => x.MaxItensParalelos)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(50).WithMessage("'{PropertyName}' deve ser no máximo 50");

        RuleFor(x => x.CodigosSondagem)
            .NotEmpty().WithMessage("'{PropertyName}' não pode ser vazio");

        RuleForEach(x => x.CodigosSondagem)
            .Matches(PadraoCodigoSondagem).WithMessage("Cada código de sondagem deve seguir o formato XX.XX.XX (ex: 01.01.01)");

        RuleFor(x => x.ValidadeDiasProcessamento)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(365).WithMessage("'{PropertyName}' deve ser no máximo 365");

        RuleFor(x => x.CircuitBreakerLimiarErroPercent)
            .InclusiveBetween(1, 100).WithMessage("'{PropertyName}' deve estar entre 1 e 100");

        RuleFor(x => x.CircuitBreakerJanelaAvaliacaoSegundos)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(3600).WithMessage("'{PropertyName}' deve ser no máximo 3600");

        RuleFor(x => x.CircuitBreakerPausaSegundos)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(3600).WithMessage("'{PropertyName}' deve ser no máximo 3600");

        RuleFor(x => x.CircuitBreakerAmostraMinima)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(1000).WithMessage("'{PropertyName}' deve ser no máximo 1000");
    }
}
