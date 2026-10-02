using Microsoft.EntityFrameworkCore;
using VigiaTrujillo.Data;
using VigiaTrujillo.Models;

namespace VigiaTrujillo.Repositories;

public class ObraRepository : IObraRepository
{
    private readonly ApplicationDbContext _db;
    public ObraRepository(ApplicationDbContext db) => _db = db;

    public IReadOnlyList<Obra> GetAll(string? busqueda = null, string? estado = null, string? contratista = null, string? zona = null, string? categoria = null, bool soloActivas = true)
    {
        IQueryable<Obra> q = _db.Obras.AsNoTracking();

        if (soloActivas) q = q.Where(o => o.Activo);

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var t = busqueda.Trim();
            q = q.Where(o => o.Nombre.Contains(t) || o.Cui.Contains(t) || o.Ubicacion.Contains(t)
                || (o.Contratista != null && o.Contratista.Contains(t))
                || (o.Zona != null && o.Zona.Contains(t))
                || (o.Categoria != null && o.Categoria.Contains(t)));
        }
        if (!string.IsNullOrWhiteSpace(estado)) q = q.Where(o => o.Estado == estado);
        if (!string.IsNullOrWhiteSpace(contratista)) q = q.Where(o => o.Contratista != null && o.Contratista.Contains(contratista.Trim()));
        if (!string.IsNullOrWhiteSpace(zona)) q = q.Where(o => o.Zona != null && o.Zona.Contains(zona.Trim()));
        if (!string.IsNullOrWhiteSpace(categoria)) q = q.Where(o => o.Categoria != null && o.Categoria.Contains(categoria.Trim()));
        
        return q.OrderBy(o => o.Id).ToList();
    }

    public IReadOnlyList<Obra> GetDadosDeBaja() =>
        _db.Obras.AsNoTracking()
            .Where(o => !o.Activo)
            .OrderByDescending(o => o.FechaBaja ?? DateTime.MinValue)
            .ToList();

    public Obra? GetById(int id) => _db.Obras.AsNoTracking().FirstOrDefault(o => o.Id == id);

    public Obra? GetByIdWithArchivos(int id) =>
        _db.Obras.AsNoTracking()
            .Include(o => o.Archivos)
            .Include(o => o.Incidencias)
                .ThenInclude(i => i.Observaciones)
            .FirstOrDefault(o => o.Id == id);

    public Obra Create(Obra obra)
    {
        if (ExistsCui(obra.Cui)) throw new InvalidOperationException($"Ya existe una obra con el CUI '{obra.Cui}'.");
        if (string.IsNullOrWhiteSpace(obra.Estado)) obra.Estado = ObraEstados.Programada;
        _db.Obras.Add(obra);
        _db.SaveChanges();
        return obra;
    }

    public Obra? Update(Obra obra)
    {
        var e = _db.Obras.FirstOrDefault(o => o.Id == obra.Id);
        if (e == null) return null;
        
        if (ExistsCui(obra.Cui, obra.Id)) throw new InvalidOperationException($"Ya existe una obra con el CUI '{obra.Cui}'.");
        
        e.Nombre = obra.Nombre; 
        e.Ubicacion = obra.Ubicacion; 
        e.Cui = obra.Cui;
        e.FechaInicio = obra.FechaInicio; 
        e.FechaFin = obra.FechaFin;
        e.Estado = ObraEstados.Canonico(obra.Estado) ?? ObraEstados.Programada;
        e.Presupuesto = obra.Presupuesto; 
        e.Contratista = obra.Contratista;
        e.AvanceFisico = obra.AvanceFisico; 
        e.Zona = obra.Zona; 
        e.Categoria = obra.Categoria;
        e.MotivoRetroceso = obra.MotivoRetroceso; 
        e.MotivoCambioEstado = obra.MotivoCambioEstado;
        
        _db.SaveChanges();
        return e;
    }

        public bool DarDeBaja(int id, string? motivo, string? usuario)
        {
            var e = _db.Obras.FirstOrDefault(o => o.Id == id);
            if (e == null) return false;

            e.Activo = false;
            e.FechaBaja = DateTime.Now;
            e.MotivoBaja = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
            e.UsuarioBaja = string.IsNullOrWhiteSpace(usuario) ? null : usuario.Trim();

            _db.SaveChanges();
            return true;
        }

        public bool Reactivar(int id)
        {
            var e = _db.Obras.FirstOrDefault(o => o.Id == id);
            if (e == null) return false;

            e.Activo = true;
            e.FechaBaja = null;
            e.MotivoBaja = null;
            e.UsuarioBaja = null;

            _db.SaveChanges();
            return true;
        }

    public bool ExistsCui(string cui, int? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(cui)) return false;
        var t = cui.Trim();
        return _db.Obras.Any(o => o.Cui == t && (!excludeId.HasValue || o.Id != excludeId.Value));
    }

    public void AddArchivo(ObraArchivo archivo) 
    { 
        _db.ObraArchivos.Add(archivo); 
        _db.SaveChanges(); 
    }

    public bool DeleteArchivo(int archivoId)
    {
        var a = _db.ObraArchivos.FirstOrDefault(x => x.Id == archivoId);
        if (a == null) return false;
        _db.ObraArchivos.Remove(a); 
        _db.SaveChanges(); 
        return true;
    }
}