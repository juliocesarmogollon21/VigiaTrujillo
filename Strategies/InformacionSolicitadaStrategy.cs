using VigiaTrujillo.Models;

namespace VigiaTrujillo.Strategies;
public class InformacionSolicitadaStrategy : IIncidenciaEstadoStrategy
{
    public string EstadoDestino => IncidenciaEstados.InformacionSolicitada;

    public string? Validar(Incidencia incidencia, string? resultadoRevision = null)
    {
        if (IncidenciaEstados.EsCerrada(incidencia.Estado))
            return "No se puede solicitar información sobre una incidencia cerrada.";
        if (IncidenciaEstados.EsIgual(incidencia.Estado, EstadoDestino))
            return "La incidencia ya está en 'Información solicitada'.";
        if (!IncidenciaEstados.TransicionPermitida(incidencia.Estado, EstadoDestino))
            return $"No se puede pasar de '{incidencia.Estado}' a '{EstadoDestino}'.";
        if (incidencia.Observaciones == null || !incidencia.Observaciones.Any())
            return "Debe registrar al menos una observación antes de solicitar información adicional.";
        return null;
    }

    public void Aplicar(Incidencia incidencia, string? resultadoRevision = null)
        => incidencia.Estado = EstadoDestino;
}