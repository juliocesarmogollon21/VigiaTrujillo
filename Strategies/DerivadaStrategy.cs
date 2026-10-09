using VigiaTrujillo.Models;

namespace VigiaTrujillo.Strategies;

public class DerivadaStrategy : IIncidenciaEstadoStrategy
{
    public string EstadoDestino => IncidenciaEstados.Derivada;

    public string? Validar(Incidencia incidencia, string? resultadoRevision = null)
    {
        if (IncidenciaEstados.EsCerrada(incidencia.Estado))
            return "La incidencia ya está cerrada.";
        if (IncidenciaEstados.EsIgual(incidencia.Estado, IncidenciaEstados.PendienteDeRevision))
            return "Primero pase la incidencia a 'En revisión'; la derivación se decide después de revisar la documentación.";
        if (!IncidenciaEstados.TransicionPermitida(incidencia.Estado, EstadoDestino))
            return $"No se puede pasar de '{incidencia.Estado}' a '{EstadoDestino}'.";
        
        if (string.IsNullOrWhiteSpace(resultadoRevision) && string.IsNullOrWhiteSpace(incidencia.ResultadoRevision))
            return "Para derivar una incidencia (irregularidad confirmada), es obligatorio redactar el sustento documental.";
        
        // El sustento que se escribe al cerrar también cuenta como observación del historial.
        if ((incidencia.Observaciones == null || !incidencia.Observaciones.Any()) && string.IsNullOrWhiteSpace(resultadoRevision))
            return "Debe registrar al menos una observación antes de derivar.";
        
        return null;
    }

    public void Aplicar(Incidencia incidencia, string? resultadoRevision = null)
    {
        if (!string.IsNullOrWhiteSpace(resultadoRevision))
            incidencia.ResultadoRevision = resultadoRevision.Trim();
        incidencia.Estado = EstadoDestino;
    }
}