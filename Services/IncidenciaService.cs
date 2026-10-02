using VigiaTrujillo.Models;
using VigiaTrujillo.Repositories;
using VigiaTrujillo.Services.Interfaces;
using VigiaTrujillo.Strategies;

namespace VigiaTrujillo.Services;

public class IncidenciaService : IIncidenciaService
{
    private readonly IIncidenciaRepository _repo;
    private readonly IncidenciaEstadoContext _estadoContext;

    public IncidenciaService(IIncidenciaRepository repo, IncidenciaEstadoContext estadoContext)
    {
        _repo = repo;
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
        var incidencia = _repo.GetByIdWithDetalle(incidenciaId);
        if (incidencia == null) return "La incidencia no existe.";

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
        return null;
    }

    public IReadOnlyList<string> ObtenerEstadosPermitidos(string? estadoActual) =>
        _estadoContext.ObtenerEstadosPermitidos(estadoActual);
}