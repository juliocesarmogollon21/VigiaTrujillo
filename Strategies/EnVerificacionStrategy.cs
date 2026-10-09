using VigiaTrujillo.Models;

namespace VigiaTrujillo.Strategies;

// El supervisor pasa la incidencia a verificación para comprobar la información recibida o verificar en campo.
// Desde "Información solicitada" solo se permite cuando el Personal Municipal ya respondió la última solicitud.
public class EnVerificacionStrategy : IIncidenciaEstadoStrategy
{
    public string EstadoDestino => IncidenciaEstados.EnVerificacion;

    public string? Validar(Incidencia incidencia, string? resultadoRevision = null)
    {
        if (IncidenciaEstados.EsCerrada(incidencia.Estado))
            return "No se puede verificar una incidencia ya cerrada.";
        if (IncidenciaEstados.EsIgual(incidencia.Estado, EstadoDestino))
            return "La incidencia ya está en 'En verificación'.";
        if (IncidenciaEstados.EsIgual(incidencia.Estado, IncidenciaEstados.PendienteDeRevision))
            return "Primero pase la incidencia a 'En revisión'; después podrá enviarla a verificación.";
        if (IncidenciaEstados.EsIgual(incidencia.Estado, IncidenciaEstados.InformacionSolicitada) && !RespuestaRecibida(incidencia))
            return "Todavía no llega la respuesta del Personal Municipal. Podrá pasar la incidencia a 'En verificación' cuando respondan la solicitud de información.";
        if (!IncidenciaEstados.TransicionPermitida(incidencia.Estado, EstadoDestino))
            return $"No se puede pasar de '{incidencia.Estado}' a '{EstadoDestino}'.";
        return null;
    }

    public void Aplicar(Incidencia incidencia, string? resultadoRevision = null)
        => incidencia.Estado = EstadoDestino;

    // La última solicitud de información de la incidencia ya fue respondida por el Personal Municipal.
    public static bool RespuestaRecibida(Incidencia incidencia)
    {
        var ultima = incidencia.Solicitudes?
            .OrderByDescending(s => s.FechaSolicitud)
            .ThenByDescending(s => s.Id)
            .FirstOrDefault();
        return ultima != null && ultima.Estado == SolicitudInformacion.EstadoRespondida;
    }
}
