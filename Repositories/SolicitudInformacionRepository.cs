using Microsoft.EntityFrameworkCore;
using VigiaTrujillo.Data;
using VigiaTrujillo.Models;

namespace VigiaTrujillo.Repositories;

public class SolicitudInformacionRepository : ISolicitudInformacionRepository
{
    private readonly ApplicationDbContext _db;
    public SolicitudInformacionRepository(ApplicationDbContext db) => _db = db;

    public SolicitudInformacion? GetById(int id) =>
        _db.SolicitudesInformacion
            .Include(s => s.Incidencia).ThenInclude(i => i!.Obra)
            .FirstOrDefault(s => s.Id == id);

    public IReadOnlyList<SolicitudInformacion> GetPendientes(int? obraId = null)
    {
        var q = _db.SolicitudesInformacion.AsNoTracking()
            .Include(s => s.Incidencia).ThenInclude(i => i!.Obra)
            .Include(s => s.Incidencia).ThenInclude(i => i!.Evidencias)
            .Where(s => s.Estado == SolicitudInformacion.EstadoPendiente);

        if (obraId.HasValue) q = q.Where(s => s.Incidencia!.ObraId == obraId.Value);

        return q.OrderBy(s => s.FechaSolicitud).ToList();
    }

    public IReadOnlyList<SolicitudInformacion> GetRespondidas(int cantidad = 20) =>
        _db.SolicitudesInformacion.AsNoTracking()
            .Include(s => s.Incidencia).ThenInclude(i => i!.Obra)
            .Where(s => s.Estado == SolicitudInformacion.EstadoRespondida)
            .OrderByDescending(s => s.FechaRespuesta)
            .Take(cantidad)
            .ToList();

    public IReadOnlyList<SolicitudInformacion> GetByIncidencia(int incidenciaId) =>
        _db.SolicitudesInformacion.AsNoTracking()
            .Where(s => s.IncidenciaId == incidenciaId)
            .OrderByDescending(s => s.FechaSolicitud)
            .ToList();

    public int ContarPendientes() =>
        _db.SolicitudesInformacion.Count(s => s.Estado == SolicitudInformacion.EstadoPendiente);

    public void Agregar(SolicitudInformacion solicitud)
    {
        _db.SolicitudesInformacion.Add(solicitud);
        _db.SaveChanges();
    }

    public void Actualizar(SolicitudInformacion solicitud)
    {
        if (_db.Entry(solicitud).State == EntityState.Detached)
            _db.SolicitudesInformacion.Update(solicitud);
        _db.SaveChanges();
    }

    public void CerrarPendientesSinRespuesta(int incidenciaId)
    {
        var pendientes = _db.SolicitudesInformacion
            .Where(s => s.IncidenciaId == incidenciaId && s.Estado == SolicitudInformacion.EstadoPendiente)
            .ToList();
        if (pendientes.Count == 0) return;

        foreach (var s in pendientes) s.Estado = SolicitudInformacion.EstadoSinRespuesta;
        _db.SaveChanges();
    }

    public void MarcarRespuestasVistas(int incidenciaId)
    {
        var noVistas = _db.SolicitudesInformacion
            .Where(s => s.IncidenciaId == incidenciaId && s.Estado == SolicitudInformacion.EstadoRespondida && !s.RespuestaVista)
            .ToList();
        if (noVistas.Count == 0) return;

        foreach (var s in noVistas) s.RespuestaVista = true;
        _db.SaveChanges();
    }
}
