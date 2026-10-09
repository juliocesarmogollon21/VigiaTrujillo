using VigiaTrujillo.Models;

namespace VigiaTrujillo.Services.Interfaces;

public interface IIncidenciaService
{
    string Registrar(Incidencia incidencia);
    Incidencia? GetByCodigoSeguimiento(string codigo);
    Incidencia? GetByIdWithDetalle(int id);
    IReadOnlyList<Incidencia> GetByObraId(int obraId);
    IReadOnlyList<Incidencia> Filtrar(string? estado = null, int? obraId = null, DateTime? desde = null, DateTime? hasta = null);
    void RegistrarEvidencia(int incidenciaId, string rutaArchivo, string nombreArchivo);
    bool EliminarEvidencia(int evidenciaId);
    string? AgregarObservacion(int incidenciaId, string texto, string autor);
    string? CambiarEstado(int incidenciaId, string estadoDestino, string? resultadoRevision = null, string? autor = null);
    IReadOnlyList<string> ObtenerEstadosPermitidos(string? estadoActual);
    IReadOnlyList<string> ObtenerEstadosPermitidos(Incidencia incidencia);

    // Solicitudes de información (Supervisor -> Personal Municipal)
    string? SolicitarInformacion(int incidenciaId, string? pregunta, string autor);
    string? ValidarRespuesta(int solicitudId, string? respuesta);
    string? ResponderSolicitud(int solicitudId, string? respuesta, string autor, string? archivoRuta = null, string? archivoNombre = null);
    SolicitudInformacion? GetSolicitud(int solicitudId);
    IReadOnlyList<SolicitudInformacion> GetSolicitudesPendientes(int? obraId = null);
    IReadOnlyList<SolicitudInformacion> GetSolicitudesRespondidas(int cantidad = 20);
    int ContarSolicitudesPendientes();
    void MarcarRespuestasVistas(int incidenciaId);
}
