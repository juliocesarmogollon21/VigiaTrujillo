using VigiaTrujillo.Models;

namespace VigiaTrujillo.Repositories;

public interface ISolicitudInformacionRepository
{
    SolicitudInformacion? GetById(int id);
    IReadOnlyList<SolicitudInformacion> GetPendientes(int? obraId = null);
    IReadOnlyList<SolicitudInformacion> GetRespondidas(int cantidad = 20);
    IReadOnlyList<SolicitudInformacion> GetByIncidencia(int incidenciaId);
    int ContarPendientes();
    void Agregar(SolicitudInformacion solicitud);
    void Actualizar(SolicitudInformacion solicitud);
    void CerrarPendientesSinRespuesta(int incidenciaId);
    void MarcarRespuestasVistas(int incidenciaId);
}
