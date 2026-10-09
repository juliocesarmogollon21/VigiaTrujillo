using System.Globalization;
using System.Text;

namespace VigiaTrujillo.Models;

public static class IncidenciaEstados
{
    public const string PendienteDeRevision = "Pendiente de revisión";
    public const string EnRevision = "En revisión";
    public const string InformacionSolicitada = "Información solicitada";
    public const string EnVerificacion = "En verificación";
    public const string Resuelta = "Resuelta";
    public const string Derivada = "Derivada";

    public static readonly string[] Todos =
    {
        PendienteDeRevision, EnRevision, InformacionSolicitada,
        EnVerificacion, Resuelta, Derivada
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

    public static bool EsCerrada(string? estado) =>
        EsIgual(estado, Resuelta) || EsIgual(estado, Derivada);

    public static bool EsValido(string? estado) => Canonico(estado) != null;

    // Matriz de transiciones de la incidencia. Solo el supervisor cambia el estado y el flujo solo avanza:
    //   Pendiente de revisión -> En revisión -> Información solicitada -> En verificación -> Resuelta / Derivada
    // - "Pendiente de revisión" es solo el estado inicial (cuando el ciudadano registra el reporte).
    // - El Personal Municipal solo responde la solicitud; su respuesta no cambia el estado.
    //   La incidencia sigue en "Información solicitada" (con el aviso «Respuesta nueva») hasta que el
    //   supervisor la pasa a "En verificación". Ese paso solo se permite cuando la solicitud ya tiene respuesta.
    // - Desde "En verificación" el supervisor puede volver a pedir información todas las veces que necesite;
    //   cada pedido crea una nueva SolicitudInformacion.
    // - Desde "En revisión" se puede pasar directo a "En verificación" (verificar en campo sin pedir
    //   información) o a "Derivada" (irregularidad grave ya confirmada con la revisión documental).
    // - "Resuelta" solo se emite desde "En verificación": hay que comprobar que el problema se corrigió.
    // - "Resuelta" y "Derivada" cierran el caso y ya no permiten cambios.
    public static IReadOnlyList<string> TransicionesPermitidas(string? estadoActual)
    {
        var actual = Canonico(estadoActual);
        if (actual == null) return new[] { EnRevision };

        return actual switch
        {
            PendienteDeRevision => new[] { EnRevision },

            EnRevision => new[] { InformacionSolicitada, EnVerificacion, Derivada },

            InformacionSolicitada => new[] { EnVerificacion, Derivada },

            EnVerificacion => new[] { InformacionSolicitada, Resuelta, Derivada },

            _ => Array.Empty<string>()
        };
    }

    public static bool TransicionPermitida(string? desde, string? hacia)
    {
        var dest = Canonico(hacia);
        if (dest == null) return false;
        if (EsIgual(desde, hacia)) return false;
        return TransicionesPermitidas(desde).Any(t => EsIgual(t, dest));
    }
}