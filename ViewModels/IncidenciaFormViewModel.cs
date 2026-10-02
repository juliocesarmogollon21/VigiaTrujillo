using System.ComponentModel.DataAnnotations;

namespace VigiaTrujillo.ViewModels;

public class IncidenciaFormViewModel
{
    [Required(ErrorMessage = "Debes seleccionar una obra.")]
    [Display(Name = "Obra")]
    public int ObraId { get; set; }

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [Display(Name = "Descripción de la incidencia")]
    [StringLength(1000, MinimumLength = 20, ErrorMessage = "La descripción debe tener al menos 20 caracteres.")]
    public string Descripcion { get; set; } = string.Empty;

    [Display(Name = "Fotos de evidencia")]
    public List<IFormFile>? Archivos { get; set; }

    public List<Models.Obra> ObrasDisponibles { get; set; } = new();
}