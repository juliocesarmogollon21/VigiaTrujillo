using VigiaTrujillo.Models;

namespace VigiaTrujillo.Strategies;
public interface IIncidenciaEstadoStrategy
{
    string EstadoDestino { get; }
    string? Validar(Incidencia incidencia, string? resultadoRevision = null);
    void Aplicar(Incidencia incidencia, string? resultadoRevision = null);
}
