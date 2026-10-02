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

    public string? CambiarEstado(Incidencia incidencia, string? estadoDestino, string? resultadoRevision = null)
    {
        if (string.IsNullOrWhiteSpace(estadoDestino))
            return "Debe seleccionar un estado destino.";

        var canonico = IncidenciaEstados.Canonico(estadoDestino);
        if (canonico == null)
            return $"El estado '{estadoDestino.Trim()}' no es válido.";

        if (IncidenciaEstados.EsIgual(incidencia.Estado, canonico))
            return $"La incidencia ya se encuentra en el estado '{canonico}'.";

        if (!IncidenciaEstados.TransicionPermitida(incidencia.Estado, canonico))
        {
            if (IncidenciaEstados.EsCerrada(incidencia.Estado))
                return "La incidencia ya está cerrada; no se permiten más cambios de estado.";
            return $"No está permitido cambiar de '{incidencia.Estado}' a '{canonico}'.";
        }

        var strategy = BuscarEstrategia(canonico);
        if (strategy == null)
            return $"No hay estrategia registrada para el estado '{canonico}'.";

        var error = strategy.Validar(incidencia, resultadoRevision);
        if (error != null) return error;

        strategy.Aplicar(incidencia, resultadoRevision);
        incidencia.Estado = canonico;
        return null;
    }
}