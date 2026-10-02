using VigiaTrujillo.Models;

namespace VigiaTrujillo.Repositories;
public interface IIncidenciaRepository
{
    string Registrar(Incidencia incidencia);
    Incidencia? GetById(int id);
    Incidencia? GetByIdWithDetalle(int id);
    Incidencia? GetByCodigoSeguimiento(string codigo);
    IReadOnlyList<Incidencia> GetByObraId(int obraId);
    IReadOnlyList<Incidencia> Filtrar(string? estado = null, int? obraId = null, DateTime? desde = null, DateTime? hasta = null);
    void RegistrarEvidencia(int incidenciaId, string rutaArchivo, string nombreArchivo);
    bool EliminarEvidencia(int evidenciaId);
    void AgregarObservacion(ObservacionIncidencia observacion);
    void Actualizar(Incidencia incidencia);
}
