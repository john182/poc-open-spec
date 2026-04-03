using FluentValidation;
using MapaTributario.API.Application.Crawler.Contracts;

namespace MapaTributario.API.Application.Crawler.Validators;

public class AtualizarConfiguracaoCrawlerRequestValidator : AbstractValidator<AtualizarConfiguracaoCrawlerRequest>
{
    public AtualizarConfiguracaoCrawlerRequestValidator()
    {
        RuleFor(x => x.CronSchedule).ValidarCronSchedule();
        RuleFor(x => x.LimiteRequisicoesPorSegundo).ValidarLimiteRequisicoesPorSegundo();
        RuleFor(x => x.LimiteDiarioRequisicoes).ValidarLimiteDiarioRequisicoes();
        RuleFor(x => x.TamanhoLoteCertificado).ValidarTamanhoLoteCertificado();
        RuleFor(x => x.PausaLoteSegundos).ValidarPausaLoteSegundos();
        RuleFor(x => x.TamanhoLoteMongo).ValidarTamanhoLoteMongo();
        RuleFor(x => x.MaxTentativas).ValidarMaxTentativas();
        RuleFor(x => x.LimiteParadaAntecipada).ValidarLimiteParadaAntecipada();
        RuleFor(x => x.MaxDesdobramento).ValidarMaxDesdobramento();
        RuleFor(x => x.MaxDetalhamento).ValidarMaxDetalhamento();
        RuleFor(x => x.MaxFalhasConsecutivasDetalhamento).ValidarMaxFalhasConsecutivas();
        RuleFor(x => x.MaxFalhasConsecutivasDesdobramento).ValidarMaxFalhasConsecutivas();
        RuleFor(x => x.MaxItensParalelos).ValidarMaxItensParalelos();
        RuleFor(x => x.MaxUfsParalelas).ValidarMaxUfsParalelas();

        RuleFor(x => x.CodigosSondagem)
            .NotEmpty().WithMessage("'{PropertyName}' não pode ser vazio");

        RuleForEach(x => x.CodigosSondagem)
            .Matches(RegrasValidacaoConfiguracao.PadraoCodigoSondagem)
            .WithMessage("Cada código de sondagem deve seguir o formato XX.XX.XX (ex: 01.01.01)");

        RuleFor(x => x.ValidadeDiasProcessamento).ValidarValidadeDiasProcessamento();
        RuleFor(x => x.CircuitBreakerLimiarErroPercent).ValidarCircuitBreakerLimiarErroPercent();
        RuleFor(x => x.CircuitBreakerJanelaAvaliacaoSegundos).ValidarCircuitBreakerSegundos();
        RuleFor(x => x.CircuitBreakerPausaSegundos).ValidarCircuitBreakerSegundos();
        RuleFor(x => x.CircuitBreakerAmostraMinima).ValidarCircuitBreakerAmostraMinima();
    }
}
