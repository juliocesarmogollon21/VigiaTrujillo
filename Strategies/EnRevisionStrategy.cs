using VigiaTrujillo.Models;

namespace VigiaTrujillo.Strategies;
public class EnRevisionStrategy : IIncidenciaEstadoStrategy
{
    public string EstadoDestino => IncidenciaEstados.EnRevision;

    public string? Validar(Incidencia incidencia, string? resultadoRevision = null)
    {
        if (IncidenciaEstados.EsCerrada(incidencia.Estado))
            return "No se puede reabrir una incidencia ya cerrada.";
        if (IncidenciaEstados.EsIgual(incidencia.Estado, EstadoDestino))
            return "La incidencia ya está en 'En revisión'.";
        if (IncidenciaEstados.EsIgual(incidencia.Estado, IncidenciaEstados.InformacionSolicitada))
            return "La incidencia no puede regresar a 'En revisión'. Cuando el Personal Municipal responda, pásela a 'En verificación'.";
        if (IncidenciaEstados.EsIgual(incidencia.Estado, IncidenciaEstados.EnVerificacion))
            return "La incidencia ya está en 'En verificación' y no puede regresar a 'En revisión'. Si necesita más datos, use «Solicitar información».";
        if (!IncidenciaEstados.TransicionPermitida(incidencia.Estado, EstadoDestino))
            return $"No se puede pasar de '{incidencia.Estado}' a '{EstadoDestino}'.";
        return null;
    }

    public void Aplicar(Incidencia incidencia, string? resultadoRevision = null)
        => incidencia.Estado = EstadoDestino;
}
