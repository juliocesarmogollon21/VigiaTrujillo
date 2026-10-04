namespace VigiaTrujillo.ViewModels;

public class ObraListItemViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Ubicacion { get; set; } = string.Empty;
    public string Cui { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public decimal? AvanceFisico { get; set; }
    
    public bool TieneSolicitudPendiente { get; set; }
    public int? IncidenciaIdPendiente { get; set; }
    public string? CodigoIncidenciaPendiente { get; set; }
    
    public string? TextoSolicitudPendiente { get; set; }
}

public class ObraIndexViewModel
{
    public string? Busqueda { get; set; }
    public string? Estado { get; set; }
    public bool ConSolicitudes { get; set; }
    
    public IReadOnlyList<ObraListItemViewModel> Obras { get; set; } = Array.Empty<ObraListItemViewModel>();
}