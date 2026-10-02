using VigiaTrujillo.Models;

namespace VigiaTrujillo.Strategies;

public class DerivadaStrategy : IIncidenciaEstadoStrategy
{
    public string EstadoDestino => IncidenciaEstados.Derivada;

    public string? Validar(Incidencia incidencia, string? resultadoRevision = null)
    {
        if (IncidenciaEstados.EsCerrada(incidencia.Estado))
            return "La incidencia ya está cerrada.";
        if (!IncidenciaEstados.TransicionPermitida(incidencia.Estado, EstadoDestino))
            return $"No se puede pasar de '{incidencia.Estado}' a '{EstadoDestino}'.";
        
        if (string.IsNullOrWhiteSpace(resultadoRevision) && string.IsNullOrWhiteSpace(incidencia.ResultadoRevision))
            return "Para derivar una incidencia (irregularidad confirmada), es obligatorio redactar el sustento documental.";
        
        if (incidencia.Observaciones == null || !incidencia.Observaciones.Any())
            return "Debe registrar al menos una observación antes de derivar.";
        
        if (IncidenciaEstados.EsIgual(incidencia.Estado, IncidenciaEstados.PendienteDeRevision))
        {
            var textoCompleto = resultadoRevision ?? incidencia.ResultadoRevision ?? "";
            if (textoCompleto.Trim().Length < 100)
                return "Al derivar directamente desde 'Pendiente de revisión', debe proporcionar una justificación detallada (mínimo 100 caracteres) explicando la gravedad de la irregularidad y por qué se requiere escalamiento inmediato.";
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