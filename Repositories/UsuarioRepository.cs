using Microsoft.EntityFrameworkCore;
using VigiaTrujillo.Data;
using VigiaTrujillo.Models;

namespace VigiaTrujillo.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly ApplicationDbContext _db;
    public UsuarioRepository(ApplicationDbContext db) => _db = db;

    public IReadOnlyList<Usuario> GetAll() =>
        _db.Usuarios.AsNoTracking().OrderBy(u => u.NombreUsuario).ToList();

    public Usuario? GetById(int id) => _db.Usuarios.AsNoTracking().FirstOrDefault(u => u.Id == id); 

    public Usuario? GetByNombre(string nombreUsuario) =>
        _db.Usuarios.AsNoTracking().FirstOrDefault(u => u.NombreUsuario == nombreUsuario);

    public Usuario Create(Usuario usuario)
    {
        _db.Usuarios.Add(usuario);
        _db.SaveChanges();
        return usuario;
    }

    public Usuario? Update(Usuario usuario)
    {
        var e = _db.Usuarios.FirstOrDefault(u => u.Id == usuario.Id);
        if (e == null) return null;
        
        e.NombreUsuario = usuario.NombreUsuario;
        e.Rol = usuario.Rol;
        e.Activo = usuario.Activo;
        
        if (!string.IsNullOrEmpty(usuario.PasswordHash))
            e.PasswordHash = usuario.PasswordHash;
            
        _db.SaveChanges();
        return e;
    }

    public int ContarAdministradoresActivos(int? excludeId = null) =>
        _db.Usuarios.Count(u => u.Rol == "Administrador" && u.Activo
            && (!excludeId.HasValue || u.Id != excludeId.Value));
}