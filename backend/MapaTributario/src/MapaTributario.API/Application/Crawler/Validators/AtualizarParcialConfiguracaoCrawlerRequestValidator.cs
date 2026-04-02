using FluentValidation;
using MapaTributario.API.Application.Crawler.Contracts;

namespace MapaTributario.API.Application.Crawler.Validators;

public class AtualizarParcialConfiguracaoCrawlerRequestValidator : AbstractValidator<AtualizarParcialConfiguracaoCrawlerRequest>
{
    private const string PadraoCronValido = @"^(\S+\s+){4}\S+$";
    private const string PadraoCodigoSondagem = @"^\d{2}\.\d{2}\.\d{2}$";

    public AtualizarParcialConfiguracaoCrawlerRequestValidator()
    {
        RuleFor(x => x.CronSchedule)
            .NotEmpty().WithMessage("'{PropertyName}' não pode ser vazio")
            .Matches(PadraoCronValido).WithMessage("'{PropertyName}' deve ser uma expressão cron válida com 5 partes")
            .When(x => x.CronSchedule is not null);

        RuleFor(x => x.LimiteRequisicoesPorSegundo)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(100).WithMessage("'{PropertyName}' deve ser no máximo 100")
            .When(x => x.LimiteRequisicoesPorSegundo.HasValue);

        RuleFor(x => x.OrcamentoDiario)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(500000).WithMessage("'{PropertyName}' deve ser no máximo 500000")
            .When(x => x.OrcamentoDiario.HasValue);

        RuleFor(x => x.TamanheLoteCertificado)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(1000).WithMessage("'{PropertyName}' deve ser no máximo 1000")
            .When(x => x.TamanheLoteCertificado.HasValue);

        RuleFor(x => x.PausaLoteSegundos)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(300).WithMessage("'{PropertyName}' deve ser no máximo 300")
            .When(x => x.PausaLoteSegundos.HasValue);

        RuleFor(x => x.TamanheLoteMongo)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(500).WithMessage("'{PropertyName}' deve ser no máximo 500")
            .When(x => x.TamanheLoteMongo.HasValue);

        RuleFor(x => x.MaxTentativas)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(10).WithMessage("'{PropertyName}' deve ser no máximo 10")
            .When(x => x.MaxTentativas.HasValue);

        RuleFor(x => x.LimiteParadaAntecipada)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(50).WithMessage("'{PropertyName}' deve ser no máximo 50")
            .When(x => x.LimiteParadaAntecipada.HasValue);

        RuleFor(x => x.MaxDesdobramento)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(100).WithMessage("'{PropertyName}' deve ser no máximo 100")
            .When(x => x.MaxDesdobramento.HasValue);

        RuleFor(x => x.MaxDetalhamento)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(999).WithMessage("'{PropertyName}' deve ser no máximo 999")
            .When(x => x.MaxDetalhamento.HasValue);

        RuleFor(x => x.MaxFalhasConsecutivasDetalhamento)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(20).WithMessage("'{PropertyName}' deve ser no máximo 20")
            .When(x => x.MaxFalhasConsecutivasDetalhamento.HasValue);

        RuleFor(x => x.MaxFalhasConsecutivasDesdobramento)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(20).WithMessage("'{PropertyName}' deve ser no máximo 20")
            .When(x => x.MaxFalhasConsecutivasDesdobramento.HasValue);

        RuleFor(x => x.MaxItensParalelos)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(50).WithMessage("'{PropertyName}' deve ser no máximo 50")
            .When(x => x.MaxItensParalelos.HasValue);

        RuleFor(x => x.CodigosSondagem)
            .NotEmpty().WithMessage("'{PropertyName}' não pode ser vazio")
            .When(x => x.CodigosSondagem is not null);

        RuleForEach(x => x.CodigosSondagem)
            .Matches(PadraoCodigoSondagem).WithMessage("Cada código de sondagem deve seguir o formato XX.XX.XX (ex: 01.01.01)")
            .When(x => x.CodigosSondagem is not null);

        RuleFor(x => x.ValidadeDiasProcessamento)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(365).WithMessage("'{PropertyName}' deve ser no máximo 365")
            .When(x => x.ValidadeDiasProcessamento.HasValue);

        RuleFor(x => x.CircuitBreakerLimiarErroPercent)
            .InclusiveBetween(1, 100).WithMessage("'{PropertyName}' deve estar entre 1 e 100")
            .When(x => x.CircuitBreakerLimiarErroPercent.HasValue);

        RuleFor(x => x.CircuitBreakerJanelaAvaliacaoSegundos)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(3600).WithMessage("'{PropertyName}' deve ser no máximo 3600")
            .When(x => x.CircuitBreakerJanelaAvaliacaoSegundos.HasValue);

        RuleFor(x => x.CircuitBreakerPausaSegundos)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(3600).WithMessage("'{PropertyName}' deve ser no máximo 3600")
            .When(x => x.CircuitBreakerPausaSegundos.HasValue);

        RuleFor(x => x.CircuitBreakerAmostraMinima)
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(1000).WithMessage("'{PropertyName}' deve ser no máximo 1000")
            .When(x => x.CircuitBreakerAmostraMinima.HasValue);
    }
}
