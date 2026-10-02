using VigiaTrujillo.Models;
using VigiaTrujillo.Repositories;
using VigiaTrujillo.Services.Interfaces;

namespace VigiaTrujillo.Services;

public class ObraEfService : IObraService
{
    private readonly IObraRepository _repo;
    public ObraEfService(IObraRepository repo) => _repo = repo;

    public IReadOnlyList<Obra> GetAll(string? busqueda = null, string? estado = null, string? contratista = null, string? zona = null, string? categoria = null, bool soloActivas = true)
        => _repo.GetAll(busqueda, estado, contratista, zona, categoria, soloActivas);

    public IReadOnlyList<Obra> GetDadosDeBaja() => _repo.GetDadosDeBaja();

    public Obra? GetById(int id) => _repo.GetById(id);
    public Obra? GetByIdWithArchivos(int id) => _repo.GetByIdWithArchivos(id);
    
    public Obra Create(Obra obra) => _repo.Create(obra);
    
    public Obra? Update(Obra obra) => _repo.Update(obra);
    
    public bool ExistsCui(string cui, int? excludeId = null) => _repo.ExistsCui(cui, excludeId);

    public string? ActualizarAvance(int id, decimal nuevoAvance, string? motivoRetroceso)
    {
        var obra = _repo.GetById(id);
        if (obra == null) return "La obra no existe.";
        if (nuevoAvance < 0 || nuevoAvance > 100) return "El avance debe estar entre 0 y 100.";
        
        var anterior = obra.AvanceFisico ?? 0;
        
        if (nuevoAvance < anterior && string.IsNullOrWhiteSpace(motivoRetroceso))
            return "Si el avance disminuye, debe indicar el motivo de retroceso.";
        
        obra.AvanceFisico = nuevoAvance;
        if (nuevoAvance < anterior) obra.MotivoRetroceso = motivoRetroceso!.Trim();
        
        _repo.Update(obra);
        return null;
    }

    public string? CambiarEstado(int id, string? nuevoEstado, string? motivo)
    {
        var obra = _repo.GetById(id);
        if (obra == null) return "La obra no existe.";
        if (string.IsNullOrWhiteSpace(nuevoEstado))
            return "Debe seleccionar un estado.";

        var canonico = ObraEstados.Canonico(nuevoEstado);
        if (canonico == null)
            return $"El estado '{nuevoEstado.Trim()}' no es válido.";

        if (ObraEstados.EsIgual(obra.Estado, canonico))
            return $"La obra ya se encuentra en el estado '{canonico}'.";

        if (ObraEstados.EsIgual(canonico, ObraEstados.Paralizada) && string.IsNullOrWhiteSpace(motivo))
            return "Debe indicar el motivo al paralizar la obra.";

        obra.Estado = canonico;
        if (!string.IsNullOrWhiteSpace(motivo)) obra.MotivoCambioEstado = motivo.Trim();
        
        _repo.Update(obra);
        return null;
    }

    public void AgregarArchivo(int obraId, string ruta, string nombre, string tipo)
    {
        _repo.AddArchivo(new ObraArchivo
        {
            ObraId = obraId, Ruta = ruta, Nombre = nombre, Tipo = tipo, FechaCarga = DateTime.Now
        });
    }

    public bool EliminarArchivo(int archivoId) => _repo.DeleteArchivo(archivoId);

    public string? DarDeBaja(int id, string? motivo)
    {
        var obra = _repo.GetById(id);
        if (obra == null) return "La obra no existe.";
        if (!obra.Activo) return $"La obra «{obra.Nombre}» ya se encuentra dada de baja.";

        if (string.IsNullOrWhiteSpace(motivo))
            return "Debe indicar el motivo de la baja. El registro se conserva para auditoría.";

        return _repo.DarDeBaja(id, motivo, "Personal Municipal") ? null : "No se pudo registrar la baja.";
    }

    public string? Reactivar(int id)
    {
        var obra = _repo.GetById(id);
        if (obra == null) return "La obra no existe.";
        if (obra.Activo) return $"La obra «{obra.Nombre}» ya se encuentra activa.";

        return _repo.Reactivar(id) ? null : "No se pudo reactivar la obra.";
    }
}