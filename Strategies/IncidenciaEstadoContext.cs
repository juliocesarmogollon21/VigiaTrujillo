using VigiaTrujillo.Models;

namespace VigiaTrujillo.Strategies;
public class IncidenciaEstadoContext
{
    private readonly IEnumerable<IIncidenciaEstadoStrategy> _strategies;

    public IncidenciaEstadoContext(IEnumerable<IIncidenciaEstadoStrategy> strategies)
        => _strategies = strategies;

    public IIncidenciaEstadoStrategy? BuscarEstrategia(string? estadoDestino)
    {
        var canonico = IncidenciaEstados.Canonico(estadoDestino);
        if (canonico == null) return null;
        return _strategies.FirstOrDefault(s => IncidenciaEstados.EsIgual(s.EstadoDestino, canonico));
    }

    public IIncidenciaEstadoStrategy? ObtenerEstrategiaONulo(string? estadoDestino) => BuscarEstrategia(estadoDestino);

    public IReadOnlyList<string> ObtenerEstadosPermitidos(string? estadoActual) =>
        IncidenciaEstados.TransicionesPermitidas(estadoActual);

    public IReadOnlyList<string> ObtenerEstadosPermitidos(Incidencia incidencia)
    {
        var permitidos = IncidenciaEstados.TransicionesPermitidas(incidencia.Estado);
        if (IncidenciaEstados.EsIgual(incidencia.Estado, IncidenciaEstados.InformacionSolicitada)
            && !EnVerificacionStrategy.RespuestaRecibida(incidencia))
        {
            permitidos = permitidos.Where(e => e != IncidenciaEstados.EnVerificacion).ToList();
        }
        return permitidos;
    }

    public string? CambiarEstado(Incidencia incidencia, string? estadoDestino, string? resultadoRevision = null)
    {
        if (string.IsNullOrWhiteSpace(estadoDestino))
            return "Debe seleccionar un estado destino.";

        var canonico = IncidenciaEstados.Canonico(estadoDestino);
        if (canonico == null)
            return $"El estado '{estadoDestino.Trim()}' no es válido.";

        if (IncidenciaEstados.EsIgual(incidencia.Estado, canonico))
            return $"La incidencia ya se encuentra en el estado '{canonico}'.";

        if (IncidenciaEstados.EsCerrada(incidencia.Estado))
            return "La incidencia ya está cerrada; no se permiten más cambios de estado.";

        var strategy = BuscarEstrategia(canonico);
        if (strategy == null)
            return $"No hay estrategia registrada para el estado '{canonico}'.";

        // Primero valida la estrategia del estado destino (da el mensaje más claro para el usuario).
        var error = strategy.Validar(incidencia, resultadoRevision);
        if (error != null) return error;

        // Control final con la matriz de transiciones, por si una estrategia no lo revisó.
        if (!IncidenciaEstados.TransicionPermitida(incidencia.Estado, canonico))
            return $"No está permitido cambiar de '{incidencia.Estado}' a '{canonico}'.";

        strategy.Aplicar(incidencia, resultadoRevision);
        incidencia.Estado = canonico;
        return null;
    }
}
