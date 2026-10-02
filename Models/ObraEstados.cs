using System.Globalization;
using System.Text;

namespace VigiaTrujillo.Models;
public static class ObraEstados
{
    public const string Programada = "Programada";
    public const string EnEjecucion = "En ejecución";
    public const string Paralizada = "Paralizada";
    public const string Concluida = "Concluida";
    public const string ConSobrecosto = "Con Sobrecosto";

    public static readonly string[] Todos =
    {
        Programada, EnEjecucion, Paralizada, Concluida, ConSobrecosto
    };

    public static string Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return string.Empty;
        var formD = valor.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var c in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsWhiteSpace(c)) { if (sb.Length == 0 || sb[^1] != ' ') sb.Append(' '); continue; }
            sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    public static bool EsIgual(string? a, string? b) =>
        string.Equals(Normalizar(a), Normalizar(b), StringComparison.Ordinal);

    public static string? Canonico(string? estado)
    {
        if (string.IsNullOrWhiteSpace(estado)) return null;
        return Todos.FirstOrDefault(t => EsIgual(t, estado));
    }

    public static bool EsValido(string? estado) => Canonico(estado) != null;
}