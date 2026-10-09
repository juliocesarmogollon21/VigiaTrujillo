using VigiaTrujillo.Models;

namespace VigiaTrujillo.Strategies;

// "Pendiente de revisión" es el estado inicial: lo recibe la incidencia cuando el ciudadano la registra.
// Una vez que el supervisor empezó a revisarla ya no se puede regresar a este estado,
// por eso esta estrategia rechaza cualquier cambio que tenga a "Pendiente de revisión" como destino.
public class PendienteDeRevisionStrategy : IIncidenciaEstadoStrategy
{
    public string EstadoDestino => IncidenciaEstados.PendienteDeRevision;

    public string? Validar(Incidencia incidencia, string? resultadoRevision = null)
    {
        if (IncidenciaEstados.EsCerrada(incidencia.Estado))
            return "La incidencia ya está cerrada (Resuelta o Derivada); no se permiten más cambios de estado.";
        if (IncidenciaEstados.EsIgual(incidencia.Estado, EstadoDestino))
            return "La incidencia ya está en 'Pendiente de revisión'.";

        return "Una incidencia no puede volver a 'Pendiente de revisión'. Ese estado solo se usa cuando el ciudadano registra el reporte; "
             + "si necesita seguir analizándola, elija 'En revisión'.";
    }

    // Nunca se llega a aplicar desde otro estado porque Validar siempre lo rechaza.
    public void Aplicar(Incidencia incidencia, string? resultadoRevision = null)
        => incidencia.Estado = EstadoDestino;
}
