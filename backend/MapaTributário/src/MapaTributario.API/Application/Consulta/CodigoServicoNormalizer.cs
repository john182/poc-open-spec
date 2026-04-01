using System.Text.RegularExpressions;

namespace MapaTributario.API.Application.Consulta;

public static partial class CodigoServicoNormalizer
{
    private static readonly Regex _formatoComPontos = new(@"^\d{2}\.\d{2}\.\d{2}$", RegexOptions.Compiled);
    private static readonly Regex _formatoSemPontos = new(@"^\d{6}$", RegexOptions.Compiled);

    /// <summary>
    /// Remove pontos do codigo de servico, retornando apenas digitos.
    /// Ex: "01.02.00" -> "010200"
    /// </summary>
    public static string RemoverPontos(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return string.Empty;
        }

        return codigo.Replace(".", "");
    }

    /// <summary>
    /// Formata codigo de servico com pontos no formato ii.ss.dd.
    /// Ex: "010200" -> "01.02.00"
    /// </summary>
    public static string Formatar(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return string.Empty;
        }

        var limpo = codigo.Replace(".", "");
        if (limpo.Length != 6)
        {
            return codigo;
        }

        return $"{limpo[..2]}.{limpo[2..4]}.{limpo[4..6]}";
    }

    /// <summary>
    /// Normaliza o codigo para formato sem pontos (armazenamento).
    /// Aceita "01.02.00" ou "010200".
    /// </summary>
    public static string Normalizar(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return string.Empty;
        }

        var limpo = codigo.Replace(".", "");
        if (limpo.Length != 6 || !limpo.All(char.IsDigit))
        {
            return string.Empty;
        }

        return limpo;
    }

    /// <summary>
    /// Valida se o codigo esta em formato aceito (com ou sem pontos, 6 digitos).
    /// </summary>
    public static bool EhValido(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return false;
        }

        if (_formatoComPontos.IsMatch(codigo))
        {
            return true;
        }

        if (_formatoSemPontos.IsMatch(codigo))
        {
            return true;
        }

        return false;
    }
}
