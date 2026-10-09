using VigiaTrujillo.Models;

namespace VigiaTrujillo.Strategies;

// Pedir información al Personal Municipal. Se puede hacer desde "En revisión" o "En verificación",
// tantas veces como el supervisor lo necesite (cada pedido crea una nueva solicitud).
// La respuesta del Personal Municipal no cambia el estado: el supervisor decide pasarla a "En verificación".
public class InformacionSolicitadaStrategy : IIncidenciaEstadoStrategy
{
    public string EstadoDestino => IncidenciaEstados.InformacionSolicitada;

    public string? Validar(Incidencia incidencia, string? resultadoRevision = null)
    {
        if (IncidenciaEstados.EsCerrada(incidencia.Estado))
            return "No se puede solicitar información sobre una incidencia cerrada.";
        if (IncidenciaEstados.EsIgual(incidencia.Estado, EstadoDestino))
            return "Ya hay una solicitud de información esperando respuesta del Personal Municipal.";
        if (IncidenciaEstados.EsIgual(incidencia.Estado, IncidenciaEstados.PendienteDeRevision))
            return "Primero pase la incidencia a 'En revisión'; después podrá solicitar información al Personal Municipal.";
        if (!IncidenciaEstados.TransicionPermitida(incidencia.Estado, EstadoDestino))
            return $"No se puede pasar de '{incidencia.Estado}' a '{EstadoDestino}'.";
        if (string.IsNullOrWhiteSpace(resultadoRevision) || resultadoRevision.Trim().Length < 10)
            return "Indique qué información necesita del Personal Municipal (mínimo 10 caracteres).";
        return null;
    }

    public void Aplicar(Incidencia incidencia, string? resultadoRevision = null)
        => incidencia.Estado = EstadoDestino;
}
