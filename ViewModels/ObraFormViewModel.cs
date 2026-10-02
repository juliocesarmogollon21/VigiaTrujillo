using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VigiaTrujillo.ViewModels;

public class ObraFormViewModel : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [Display(Name = "Nombre / Componente")]
    [StringLength(200)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La ubicación es obligatoria.")]
    [Display(Name = "Ubicación")]
    [StringLength(300)]
    public string Ubicacion { get; set; } = string.Empty;

    [Required(ErrorMessage = "El CUI es obligatorio.")]
    [Display(Name = "CUI (Código Único de Inversión)")]
    [StringLength(50)]
    public string Cui { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
    [Display(Name = "Fecha de inicio")]
    [DataType(DataType.Date)]
    public DateTime? FechaInicio { get; set; }

    [Required(ErrorMessage = "La fecha de fin es obligatoria.")]
    [Display(Name = "Fecha de fin")]
    [DataType(DataType.Date)]
    public DateTime? FechaFin { get; set; }

    [Required, Display(Name = "Estado")]
    public string Estado { get; set; } = "Programada";

    [Display(Name = "Presupuesto (S/)")]
    [Range(1, double.MaxValue, ErrorMessage = "El presupuesto debe ser mayor a cero.")]
    public decimal? Presupuesto { get; set; }

    [Display(Name = "Contratista"), StringLength(200)]
    public string? Contratista { get; set; }

    [Display(Name = "Avance físico (%)")]
    [Range(0, 100)]
    public decimal? AvanceFisico { get; set; }
    public bool TieneSolicitudPendiente { get; set; }

    [Display(Name = "Zona"), StringLength(100)]
    public string? Zona { get; set; }

    [Display(Name = "Categoría"), StringLength(100)]
    public string? Categoria { get; set; }

    public IEnumerable<SelectListItem> Estados { get; set; } = Array.Empty<SelectListItem>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FechaInicio.HasValue && FechaFin.HasValue && FechaFin.Value.Date < FechaInicio.Value.Date)
            yield return new ValidationResult("La fecha de fin debe ser mayor o igual a la fecha de inicio.", new[] { nameof(FechaFin) });
    }
}
