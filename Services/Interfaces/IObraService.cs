using VigiaTrujillo.Models;

namespace VigiaTrujillo.Services.Interfaces;

public interface IObraService
{
    IReadOnlyList<Obra> GetAll(string? busqueda = null, string? estado = null, string? contratista = null, string? zona = null, string? categoria = null, bool soloActivas = true);
    IReadOnlyList<Obra> GetDadosDeBaja();
    Obra? GetById(int id);
    Obra? GetByIdWithArchivos(int id);
    Obra Create(Obra obra);
    Obra? Update(Obra obra);
    bool ExistsCui(string cui, int? excludeId = null);
    string? ActualizarAvance(int id, decimal nuevoAvance, string? motivoRetroceso);
    string? CambiarEstado(int id, string? nuevoEstado, string? motivo);
    void AgregarArchivo(int obraId, string ruta, string nombre, string tipo);
    bool EliminarArchivo(int archivoId);

    string? DarDeBaja(int id, string? motivo);

    string? Reactivar(int id);
}
