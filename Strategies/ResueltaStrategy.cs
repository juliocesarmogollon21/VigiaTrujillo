using VigiaTrujillo.Models;

namespace VigiaTrujillo.Strategies;

public class ResueltaStrategy : IIncidenciaEstadoStrategy
{
    public string EstadoDestino => IncidenciaEstados.Resuelta;

    public string? Validar(Incidencia incidencia, string? resultadoRevision = null)
    {
        if (IncidenciaEstados.EsCerrada(incidencia.Estado))
            return "La incidencia ya está cerrada.";
        if (!IncidenciaEstados.TransicionPermitida(incidencia.Estado, EstadoDestino))
            return $"No se puede pasar de '{incidencia.Estado}' a '{EstadoDestino}'.";
        if (string.IsNullOrWhiteSpace(resultadoRevision) && string.IsNullOrWhiteSpace(incidencia.ResultadoRevision))
            return "Debe indicar el resultado de la revisión antes de marcar como Resuelta.";
        if (incidencia.Observaciones == null || !incidencia.Observaciones.Any())
            return "Debe registrar al menos una observación antes de resolver.";
        
        if (IncidenciaEstados.EsIgual(incidencia.Estado, IncidenciaEstados.PendienteDeRevision))
        {
            var textoCompleto = resultadoRevision ?? incidencia.ResultadoRevision ?? "";
            if (textoCompleto.Trim().Length < 100)
                return "Al resolver directamente desde 'Pendiente de revisión', debe proporcionar una justificación detallada (mínimo 100 caracteres) explicando por qué no se requirió revisión intermedia.";
        }
        
        return null;
    }

    public void Aplicar(Incidencia incidencia, string? resultadoRevision = null)
    {
        if (!string.IsNullOrWhiteSpace(resultadoRevision))
            incidencia.ResultadoRevision = resultadoRevision.Trim();
        incidencia.Estado = EstadoDestino;
    }
}