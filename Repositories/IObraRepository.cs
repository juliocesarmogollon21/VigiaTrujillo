using VigiaTrujillo.Models;

namespace VigiaTrujillo.Repositories;
public interface IObraRepository
{
    IReadOnlyList<Obra> GetAll(string? busqueda = null, string? estado = null, string? contratista = null, string? zona = null, string? categoria = null, bool soloActivas = true);
    IReadOnlyList<Obra> GetDadosDeBaja();
    Obra? GetById(int id);
    Obra? GetByIdWithArchivos(int id);
    Obra Create(Obra obra);
    Obra? Update(Obra obra);
    bool DarDeBaja(int id, string? motivo, string? usuario);
    bool Reactivar(int id);
    bool ExistsCui(string cui, int? excludeId = null);
    void AddArchivo(ObraArchivo archivo);
    bool DeleteArchivo(int archivoId);
}
