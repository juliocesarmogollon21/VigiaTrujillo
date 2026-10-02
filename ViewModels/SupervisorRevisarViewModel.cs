using VigiaTrujillo.Models;

namespace VigiaTrujillo.ViewModels;
public class SupervisorRevisarViewModel
{
    public Incidencia Incidencia { get; set; } = null!;
    public string NuevaObservacion { get; set; } = string.Empty;
    public string NuevoEstado { get; set; } = string.Empty;
    public string? ResultadoRevision { get; set; }
}
