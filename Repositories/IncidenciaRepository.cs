using Microsoft.EntityFrameworkCore;
using VigiaTrujillo.Data;
using VigiaTrujillo.Models;

namespace VigiaTrujillo.Repositories;

public class IncidenciaRepository : IIncidenciaRepository
{
    private readonly ApplicationDbContext _db;
    public IncidenciaRepository(ApplicationDbContext db) => _db = db;

    public string Registrar(Incidencia incidencia)
    {
        if (!_db.Obras.Any(o => o.Id == incidencia.ObraId))
            throw new InvalidOperationException("La obra seleccionada no existe.");

        incidencia.CodigoSeguimiento = GenerarCodigo();
        incidencia.Estado = IncidenciaEstados.PendienteDeRevision;
        incidencia.FechaRegistro = DateTime.Now;
        _db.Incidencias.Add(incidencia);
        _db.SaveChanges();
        return incidencia.CodigoSeguimiento;
    }

    public Incidencia? GetById(int id) => _db.Incidencias.AsNoTracking().FirstOrDefault(i => i.Id == id);

    public Incidencia? GetByIdWithDetalle(int id) =>
        _db.Incidencias.AsSplitQuery()
            .Include(i => i.Obra)
            .Include(i => i.Evidencias)
            .Include(i => i.Observaciones)
            .Include(i => i.Solicitudes)
            .FirstOrDefault(i => i.Id == id);

    public Incidencia? GetByCodigoSeguimiento(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return null;
        var t = codigo.Trim().ToUpperInvariant();
        return _db.Incidencias.AsNoTracking().AsSplitQuery()
            .Include(i => i.Obra)
            .Include(i => i.Evidencias)
            .Include(i => i.Observaciones)
            .FirstOrDefault(i => i.CodigoSeguimiento == t);
    }

    public IReadOnlyList<Incidencia> GetByObraId(int obraId) =>
        _db.Incidencias.AsNoTracking()
            .Where(i => i.ObraId == obraId)
            .OrderByDescending(i => i.FechaRegistro)
            .ToList();

    public IReadOnlyList<Incidencia> Filtrar(string? estado = null, int? obraId = null, DateTime? desde = null, DateTime? hasta = null)
    {
        IQueryable<Incidencia> q = _db.Incidencias.AsNoTracking().AsSplitQuery()
            .Include(i => i.Obra)
            .Include(i => i.Evidencias)
            .Include(i => i.Observaciones)
            .Include(i => i.Solicitudes);

        if (obraId.HasValue) q = q.Where(i => i.ObraId == obraId.Value);
        if (desde.HasValue) q = q.Where(i => i.FechaRegistro >= desde.Value.Date);
        if (hasta.HasValue) q = q.Where(i => i.FechaRegistro < hasta.Value.Date.AddDays(1));
        
        var lista = q.OrderByDescending(i => i.FechaRegistro).ToList();

        if (!string.IsNullOrWhiteSpace(estado))
        {
            lista = lista.Where(i => IncidenciaEstados.EsIgual(i.Estado, estado)).ToList();
        }

        return lista;
    }

    public void RegistrarEvidencia(int incidenciaId, string rutaArchivo, string nombreArchivo)
    {
        _db.Evidencias.Add(new Evidencia
        {
            IncidenciaId = incidenciaId,
            RutaArchivo = rutaArchivo,
            NombreArchivo = nombreArchivo,
            FechaCarga = DateTime.Now
        });
        _db.SaveChanges();
    }

    public bool EliminarEvidencia(int evidenciaId)
    {
        var e = _db.Evidencias.FirstOrDefault(x => x.Id == evidenciaId);
        if (e == null) return false;
        _db.Evidencias.Remove(e);
        _db.SaveChanges();
        return true;
    }

    public void AgregarObservacion(ObservacionIncidencia observacion)
    {
        _db.ObservacionesIncidencia.Add(observacion);
        _db.SaveChanges();
    }

    public void Actualizar(Incidencia incidencia)
    {
        _db.Incidencias.Update(incidencia);
        _db.SaveChanges();
    }

    private string GenerarCodigo()
    {
        string codigo;
        do
        {
            codigo = $"INC-{DateTime.Now:yyyy}-{Random.Shared.Next(100000, 999999)}";
        }
        while (_db.Incidencias.Any(i => i.CodigoSeguimiento == codigo));
        return codigo;
    }
}