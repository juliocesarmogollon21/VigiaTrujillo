using VigiaTrujillo.Models;

namespace VigiaTrujillo.Repositories;
public interface IUsuarioRepository
{
    IReadOnlyList<Usuario> GetAll();
    Usuario? GetById(int id);
    Usuario? GetByNombre(string nombreUsuario);
    Usuario Create(Usuario usuario);
    Usuario? Update(Usuario usuario);
    int ContarAdministradoresActivos(int? excludeId = null);
}
