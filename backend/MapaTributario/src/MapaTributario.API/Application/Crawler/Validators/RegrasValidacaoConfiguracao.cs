using FluentValidation;

namespace MapaTributario.API.Application.Crawler.Validators;

/// <summary>
/// Regras de validação compartilhadas entre PUT e PATCH de configuração do crawler.
/// Centraliza limites e mensagens para evitar duplicação entre os dois validators.
/// </summary>
public static class RegrasValidacaoConfiguracao
{
    public const string PadraoCronValido = @"^(\S+\s+){4}\S+$";
    public const string PadraoCodigoSondagem = @"^\d{2}\.\d{2}\.\d{2}$";

    public static IRuleBuilderOptions<T, string> ValidarCronSchedule<T>(
        this IRuleBuilder<T, string> regra)
    {
        return regra
            .NotEmpty().WithMessage("'{PropertyName}' não pode ser vazio")
            .Matches(PadraoCronValido).WithMessage("'{PropertyName}' deve ser uma expressão cron válida com 5 partes");
    }

    public static IRuleBuilderOptions<T, int> ValidarLimiteRequisicoesPorSegundo<T>(
        this IRuleBuilder<T, int> regra)
    {
        return regra
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(100).WithMessage("'{PropertyName}' deve ser no máximo 100");
    }

    public static IRuleBuilderOptions<T, int> ValidarOrcamentoDiario<T>(
        this IRuleBuilder<T, int> regra)
    {
        return regra
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(500000).WithMessage("'{PropertyName}' deve ser no máximo 500000");
    }

    public static IRuleBuilderOptions<T, int> ValidarTamanhoLoteCertificado<T>(
        this IRuleBuilder<T, int> regra)
    {
        return regra
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(1000).WithMessage("'{PropertyName}' deve ser no máximo 1000");
    }

    public static IRuleBuilderOptions<T, int> ValidarPausaLoteSegundos<T>(
        this IRuleBuilder<T, int> regra)
    {
        return regra
            .GreaterThanOrEqualTo(0).WithMessage("'{PropertyName}' deve ser maior ou igual a 0")
            .LessThanOrEqualTo(300).WithMessage("'{PropertyName}' deve ser no máximo 300");
    }

    public static IRuleBuilderOptions<T, int> ValidarTamanhoLoteMongo<T>(
        this IRuleBuilder<T, int> regra)
    {
        return regra
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(500).WithMessage("'{PropertyName}' deve ser no máximo 500");
    }

    public static IRuleBuilderOptions<T, int> ValidarMaxTentativas<T>(
        this IRuleBuilder<T, int> regra)
    {
        return regra
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(10).WithMessage("'{PropertyName}' deve ser no máximo 10");
    }

    public static IRuleBuilderOptions<T, int> ValidarLimiteParadaAntecipada<T>(
        this IRuleBuilder<T, int> regra)
    {
        return regra
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(100).WithMessage("'{PropertyName}' deve ser no máximo 100");
    }

    public static IRuleBuilderOptions<T, int> ValidarMaxDesdobramento<T>(
        this IRuleBuilder<T, int> regra)
    {
        return regra
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(100).WithMessage("'{PropertyName}' deve ser no máximo 100");
    }

    public static IRuleBuilderOptions<T, int> ValidarMaxDetalhamento<T>(
        this IRuleBuilder<T, int> regra)
    {
        return regra
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(999).WithMessage("'{PropertyName}' deve ser no máximo 999");
    }

    public static IRuleBuilderOptions<T, int> ValidarMaxFalhasConsecutivas<T>(
        this IRuleBuilder<T, int> regra)
    {
        return regra
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(20).WithMessage("'{PropertyName}' deve ser no máximo 20");
    }

    public static IRuleBuilderOptions<T, int> ValidarMaxItensParalelos<T>(
        this IRuleBuilder<T, int> regra)
    {
        return regra
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(50).WithMessage("'{PropertyName}' deve ser no máximo 50");
    }

    public static IRuleBuilderOptions<T, int> ValidarMaxUfsParalelas<T>(
        this IRuleBuilder<T, int> regra)
    {
        return regra
            .InclusiveBetween(1, 27).WithMessage("'{PropertyName}' deve estar entre 1 e 27");
    }

    public static IRuleBuilderOptions<T, int> ValidarValidadeDiasProcessamento<T>(
        this IRuleBuilder<T, int> regra)
    {
        return regra
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(365).WithMessage("'{PropertyName}' deve ser no máximo 365");
    }

    public static IRuleBuilderOptions<T, int> ValidarCircuitBreakerLimiarErroPercent<T>(
        this IRuleBuilder<T, int> regra)
    {
        return regra
            .InclusiveBetween(1, 100).WithMessage("'{PropertyName}' deve estar entre 1 e 100");
    }

    public static IRuleBuilderOptions<T, int> ValidarCircuitBreakerSegundos<T>(
        this IRuleBuilder<T, int> regra)
    {
        return regra
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(3600).WithMessage("'{PropertyName}' deve ser no máximo 3600");
    }

    public static IRuleBuilderOptions<T, int> ValidarCircuitBreakerAmostraMinima<T>(
        this IRuleBuilder<T, int> regra)
    {
        return regra
            .GreaterThan(0).WithMessage("'{PropertyName}' deve ser maior que 0")
            .LessThanOrEqualTo(1000).WithMessage("'{PropertyName}' deve ser no máximo 1000");
    }
}
