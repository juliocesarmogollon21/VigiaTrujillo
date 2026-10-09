using VigiaTrujillo.Models;

namespace VigiaTrujillo.Strategies;

public class ResueltaStrategy : IIncidenciaEstadoStrategy
{
    public string EstadoDestino => IncidenciaEstados.Resuelta;

    public string? Validar(Incidencia incidencia, string? resultadoRevision = null)
    {
        if (IncidenciaEstados.EsCerrada(incidencia.Estado))
            return "La incidencia ya está cerrada.";
        if (!IncidenciaEstados.EsIgual(incidencia.Estado, IncidenciaEstados.EnVerificacion))
            return "Para marcar la incidencia como 'Resuelta' primero debe pasar por 'En verificación' (comprobar en campo que el problema se corrigió).";
        if (!IncidenciaEstados.TransicionPermitida(incidencia.Estado, EstadoDestino))
            return $"No se puede pasar de '{incidencia.Estado}' a '{EstadoDestino}'.";
        if (string.IsNullOrWhiteSpace(resultadoRevision) && string.IsNullOrWhiteSpace(incidencia.ResultadoRevision))
            return "Debe indicar el resultado de la revisión antes de marcar como Resuelta.";
        // El sustento que se escribe al cerrar también cuenta como observación del historial.
        if ((incidencia.Observaciones == null || !incidencia.Observaciones.Any()) && string.IsNullOrWhiteSpace(resultadoRevision))
            return "Debe registrar al menos una observación antes de resolver.";

        return null;
    }

    public void Aplicar(Incidencia incidencia, string? resultadoRevision = null)
    {
        if (!string.IsNullOrWhiteSpace(resultadoRevision))
            incidencia.ResultadoRevision = resultadoRevision.Trim();
        incidencia.Estado = EstadoDestino;
    }
}