using FluentValidation;
using MapaTributario.API.Application.Crawler.Contracts;

namespace MapaTributario.API.Application.Crawler.Validators;

public class AtualizarParcialConfiguracaoCrawlerRequestValidator : AbstractValidator<AtualizarParcialConfiguracaoCrawlerRequest>
{
    public AtualizarParcialConfiguracaoCrawlerRequestValidator()
    {
        RuleFor(x => x)
            .Must(PossuiAoMenosUmCampoPreenchido)
            .WithMessage("É necessário informar ao menos um campo para atualização parcial");

        RuleFor(x => x.CronSchedule)
            .ValidarCronSchedule()
            .When(x => x.CronSchedule is not null);

        RuleFor(x => x.LimiteRequisicoesPorSegundo!.Value)
            .ValidarLimiteRequisicoesPorSegundo()
            .When(x => x.LimiteRequisicoesPorSegundo.HasValue);

        RuleFor(x => x.OrcamentoDiario!.Value)
            .ValidarOrcamentoDiario()
            .When(x => x.OrcamentoDiario.HasValue);

        RuleFor(x => x.TamanhoLoteCertificado!.Value)
            .ValidarTamanhoLoteCertificado()
            .When(x => x.TamanhoLoteCertificado.HasValue);

        RuleFor(x => x.PausaLoteSegundos!.Value)
            .ValidarPausaLoteSegundos()
            .When(x => x.PausaLoteSegundos.HasValue);

        RuleFor(x => x.TamanhoLoteMongo!.Value)
            .ValidarTamanhoLoteMongo()
            .When(x => x.TamanhoLoteMongo.HasValue);

        RuleFor(x => x.MaxTentativas!.Value)
            .ValidarMaxTentativas()
            .When(x => x.MaxTentativas.HasValue);

        RuleFor(x => x.LimiteParadaAntecipada!.Value)
            .ValidarLimiteParadaAntecipada()
            .When(x => x.LimiteParadaAntecipada.HasValue);

        RuleFor(x => x.MaxDesdobramento!.Value)
            .ValidarMaxDesdobramento()
            .When(x => x.MaxDesdobramento.HasValue);

        RuleFor(x => x.MaxDetalhamento!.Value)
            .ValidarMaxDetalhamento()
            .When(x => x.MaxDetalhamento.HasValue);

        RuleFor(x => x.MaxFalhasConsecutivasDetalhamento!.Value)
            .ValidarMaxFalhasConsecutivas()
            .When(x => x.MaxFalhasConsecutivasDetalhamento.HasValue);

        RuleFor(x => x.MaxFalhasConsecutivasDesdobramento!.Value)
            .ValidarMaxFalhasConsecutivas()
            .When(x => x.MaxFalhasConsecutivasDesdobramento.HasValue);

        RuleFor(x => x.MaxItensParalelos!.Value)
            .ValidarMaxItensParalelos()
            .When(x => x.MaxItensParalelos.HasValue);

        RuleFor(x => x.MaxUfsParalelas!.Value)
            .ValidarMaxUfsParalelas()
            .When(x => x.MaxUfsParalelas.HasValue);

        RuleFor(x => x.CodigosSondagem!)
            .NotEmpty().WithMessage("'{PropertyName}' não pode ser vazio")
            .When(x => x.CodigosSondagem is not null);

        RuleForEach(x => x.CodigosSondagem)
            .Matches(RegrasValidacaoConfiguracao.PadraoCodigoSondagem)
            .WithMessage("Cada código de sondagem deve seguir o formato XX.XX.XX (ex: 01.01.01)")
            .When(x => x.CodigosSondagem is not null);

        RuleFor(x => x.ValidadeDiasProcessamento!.Value)
            .ValidarValidadeDiasProcessamento()
            .When(x => x.ValidadeDiasProcessamento.HasValue);

        RuleFor(x => x.CircuitBreakerLimiarErroPercent!.Value)
            .ValidarCircuitBreakerLimiarErroPercent()
            .When(x => x.CircuitBreakerLimiarErroPercent.HasValue);

        RuleFor(x => x.CircuitBreakerJanelaAvaliacaoSegundos!.Value)
            .ValidarCircuitBreakerSegundos()
            .When(x => x.CircuitBreakerJanelaAvaliacaoSegundos.HasValue);

        RuleFor(x => x.CircuitBreakerPausaSegundos!.Value)
            .ValidarCircuitBreakerSegundos()
            .When(x => x.CircuitBreakerPausaSegundos.HasValue);

        RuleFor(x => x.CircuitBreakerAmostraMinima!.Value)
            .ValidarCircuitBreakerAmostraMinima()
            .When(x => x.CircuitBreakerAmostraMinima.HasValue);
    }

    private static bool PossuiAoMenosUmCampoPreenchido(AtualizarParcialConfiguracaoCrawlerRequest request)
    {
        return request.CronSchedule is not null
            || request.LimiteRequisicoesPorSegundo.HasValue
            || request.OrcamentoDiario.HasValue
            || request.TamanhoLoteCertificado.HasValue
            || request.PausaLoteSegundos.HasValue
            || request.TamanhoLoteMongo.HasValue
            || request.MaxTentativas.HasValue
            || request.LimiteParadaAntecipada.HasValue
            || request.MaxDesdobramento.HasValue
            || request.MaxDetalhamento.HasValue
            || request.MaxFalhasConsecutivasDetalhamento.HasValue
            || request.MaxFalhasConsecutivasDesdobramento.HasValue
            || request.MaxItensParalelos.HasValue
            || request.MaxUfsParalelas.HasValue
            || request.CodigosSondagem is not null
            || request.ValidadeDiasProcessamento.HasValue
            || request.CircuitBreakerLimiarErroPercent.HasValue
            || request.CircuitBreakerJanelaAvaliacaoSegundos.HasValue
            || request.CircuitBreakerPausaSegundos.HasValue
            || request.CircuitBreakerAmostraMinima.HasValue
            || request.Ativo.HasValue;
    }
}
