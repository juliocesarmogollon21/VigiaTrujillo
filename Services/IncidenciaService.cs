using VigiaTrujillo.Models;
using VigiaTrujillo.Repositories;
using VigiaTrujillo.Services.Interfaces;
using VigiaTrujillo.Strategies;

namespace VigiaTrujillo.Services;

public class IncidenciaService : IIncidenciaService
{
    private readonly IIncidenciaRepository _repo;
    private readonly ISolicitudInformacionRepository _solicitudes;
    private readonly IncidenciaEstadoContext _estadoContext;

    public IncidenciaService(
        IIncidenciaRepository repo,
        ISolicitudInformacionRepository solicitudes,
        IncidenciaEstadoContext estadoContext)
    {
        _repo = repo;
        _solicitudes = solicitudes;
        _estadoContext = estadoContext;
    }

    public string Registrar(Incidencia incidencia) => _repo.Registrar(incidencia);
    
    public Incidencia? GetByCodigoSeguimiento(string codigo) => _repo.GetByCodigoSeguimiento(codigo);
    
    public Incidencia? GetByIdWithDetalle(int id) => _repo.GetByIdWithDetalle(id);
    
    public IReadOnlyList<Incidencia> GetByObraId(int obraId) => _repo.GetByObraId(obraId);
    
    public IReadOnlyList<Incidencia> Filtrar(string? estado = null, int? obraId = null, DateTime? desde = null, DateTime? hasta = null)
        => _repo.Filtrar(estado, obraId, desde, hasta);

    public void RegistrarEvidencia(int incidenciaId, string rutaArchivo, string nombreArchivo)
        => _repo.RegistrarEvidencia(incidenciaId, rutaArchivo, nombreArchivo);

    public bool EliminarEvidencia(int evidenciaId) => _repo.EliminarEvidencia(evidenciaId);

    public string? AgregarObservacion(int incidenciaId, string texto, string autor)
    {
        var incidencia = _repo.GetById(incidenciaId);
        if (incidencia == null) return "La incidencia no existe.";
    
        if (string.IsNullOrWhiteSpace(texto) || texto.Trim().Length < 5)
            return "La observación debe tener al menos 5 caracteres.";
        
        if (IncidenciaEstados.EsCerrada(incidencia.Estado))
            return "No se pueden agregar observaciones a una incidencia cerrada.";

        _repo.AgregarObservacion(new ObservacionIncidencia
        {
            IncidenciaId = incidenciaId,
            Texto = texto.Trim(),
            Autor = autor,
            Fecha = DateTime.Now
        });
        return null;
    }

    public string? CambiarEstado(int incidenciaId, string estadoDestino, string? resultadoRevision = null, string? autor = null)
    {
        // Pedir información no es solo cambiar el estado: también se crea la solicitud
        // para que el Personal Municipal la vea y la pueda responder.
        if (IncidenciaEstados.EsIgual(estadoDestino, IncidenciaEstados.InformacionSolicitada))
            return SolicitarInformacion(incidenciaId, resultadoRevision, string.IsNullOrWhiteSpace(autor) ? "Supervisor" : autor);

        var incidencia = _repo.GetByIdWithDetalle(incidenciaId);
        if (incidencia == null) return "La incidencia no existe.";

        var estadoAnterior = incidencia.Estado;
        var error = _estadoContext.CambiarEstado(incidencia, estadoDestino, resultadoRevision);
        if (error != null) return error;

        if (!string.IsNullOrWhiteSpace(resultadoRevision))
        {
            var canonico = IncidenciaEstados.Canonico(estadoDestino) ?? estadoDestino.Trim();
            _repo.AgregarObservacion(new ObservacionIncidencia
            {
                IncidenciaId = incidenciaId,
                Texto = $"[Cambio de estado a '{canonico}']\n{resultadoRevision.Trim()}",
                Autor = string.IsNullOrWhiteSpace(autor) ? "Supervisor" : autor.Trim(),
                Fecha = DateTime.Now
            });
        }

        _repo.Actualizar(incidencia);

        // Si el supervisor saca la incidencia de "Información solicitada" sin esperar la respuesta,
        // la solicitud que quedó abierta se cierra para que no siga apareciendo al Personal Municipal.
        if (IncidenciaEstados.EsIgual(estadoAnterior, IncidenciaEstados.InformacionSolicitada))
            _solicitudes.CerrarPendientesSinRespuesta(incidenciaId);

        return null;
    }

    public IReadOnlyList<string> ObtenerEstadosPermitidos(Incidencia incidencia) =>
        _estadoContext.ObtenerEstadosPermitidos(incidencia);

    public IReadOnlyList<string> ObtenerEstadosPermitidos(string? estadoActual) =>
        _estadoContext.ObtenerEstadosPermitidos(estadoActual);

    public string? SolicitarInformacion(int incidenciaId, string? pregunta, string autor)
    {
        var incidencia = _repo.GetByIdWithDetalle(incidenciaId);
        if (incidencia == null) return "La incidencia no existe.";

        if (string.IsNullOrWhiteSpace(pregunta) || pregunta.Trim().Length < 10)
            return "Indique qué información necesita del Personal Municipal (mínimo 10 caracteres).";

        var texto = pregunta.Trim();
        if (texto.Length > 1000)
            return "La solicitud no puede superar los 1000 caracteres.";

        // La regla de si se puede pasar a "Información solicitada" la decide la estrategia.
        var error = _estadoContext.CambiarEstado(incidencia, IncidenciaEstados.InformacionSolicitada, texto);
        if (error != null) return error;

        _repo.AgregarObservacion(new ObservacionIncidencia
        {
            IncidenciaId = incidenciaId,
            Texto = $"[Solicitud de información]\n{texto}",
            Autor = autor,
            Fecha = DateTime.Now
        });
        _repo.Actualizar(incidencia);

        _solicitudes.Agregar(new SolicitudInformacion
        {
            IncidenciaId = incidenciaId,
            Pregunta = texto,
            SolicitadoPor = autor,
            FechaSolicitud = DateTime.Now,
            Estado = SolicitudInformacion.EstadoPendiente
        });
        return null;
    }

    public string? ValidarRespuesta(int solicitudId, string? respuesta)
    {
        var solicitud = _solicitudes.GetById(solicitudId);
        if (solicitud == null || solicitud.Incidencia == null) return "La solicitud de información no existe.";
        if (!solicitud.EstaPendiente) return "Esta solicitud ya fue atendida o cerrada.";
        if (!IncidenciaEstados.EsIgual(solicitud.Incidencia.Estado, IncidenciaEstados.InformacionSolicitada))
            return "La incidencia ya no está esperando información del Personal Municipal.";
        if (string.IsNullOrWhiteSpace(respuesta) || respuesta.Trim().Length < 10)
            return "La respuesta debe tener al menos 10 caracteres.";
        if (respuesta.Trim().Length > 2000)
            return "La respuesta no puede superar los 2000 caracteres.";
        return null;
    }

    public string? ResponderSolicitud(int solicitudId, string? respuesta, string autor, string? archivoRuta = null, string? archivoNombre = null)
    {
        var error = ValidarRespuesta(solicitudId, respuesta);
        if (error != null) return error;

        var solicitud = _solicitudes.GetById(solicitudId)!;
        var incidencia = _repo.GetByIdWithDetalle(solicitud.IncidenciaId);
        if (incidencia == null) return "La incidencia no existe.";

        // La respuesta no cambia el estado: la incidencia sigue en "Información solicitada" con el aviso
        // «Respuesta nueva» y es el supervisor quien decide pasarla a "En verificación".

        solicitud.Respuesta = respuesta!.Trim();
        solicitud.RespondidoPor = autor;
        solicitud.FechaRespuesta = DateTime.Now;
        solicitud.ArchivoRuta = archivoRuta;
        solicitud.ArchivoNombre = archivoNombre;
        solicitud.Estado = SolicitudInformacion.EstadoRespondida;
        solicitud.RespuestaVista = false;
        _solicitudes.Actualizar(solicitud);

        _repo.AgregarObservacion(new ObservacionIncidencia
        {
            IncidenciaId = incidencia.Id,
            Texto = "[Respuesta a solicitud de información]\nEl Personal Municipal respondió la solicitud de información.",
            Autor = autor,
            Fecha = DateTime.Now
        });
        _repo.Actualizar(incidencia);
        return null;
    }

    public SolicitudInformacion? GetSolicitud(int solicitudId) => _solicitudes.GetById(solicitudId);

    public IReadOnlyList<SolicitudInformacion> GetSolicitudesPendientes(int? obraId = null) => _solicitudes.GetPendientes(obraId);

    public IReadOnlyList<SolicitudInformacion> GetSolicitudesRespondidas(int cantidad = 20) => _solicitudes.GetRespondidas(cantidad);

    public int ContarSolicitudesPendientes() => _solicitudes.ContarPendientes();

    public void MarcarRespuestasVistas(int incidenciaId) => _solicitudes.MarcarRespuestasVistas(incidenciaId);
}
