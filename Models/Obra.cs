using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VigiaTrujillo.Models;

public class Obra
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
    [Display(Name = "CUI")]
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

    [Display(Name = "Estado")]
    [StringLength(50)]
    public string Estado { get; set; } = ObraEstados.Programada;

    [Display(Name = "Fuente del dato")]
    [StringLength(200)]
    public string Fuente { get; set; } = string.Empty;

    [Display(Name = "Plazo declarado")]
    [StringLength(120)]
    public string Plazo { get; set; } = string.Empty;

    [Display(Name = "Presupuesto (S/)")]
    [Range(0, double.MaxValue, ErrorMessage = "El presupuesto debe ser un valor positivo.")]
    [Column(TypeName = "decimal(18,2)")] 
    public decimal? Presupuesto { get; set; }

    [Display(Name = "Contratista")]
    [StringLength(200)]
    public string? Contratista { get; set; }

    [Display(Name = "Avance físico (%)")]
    [Range(0, 100, ErrorMessage = "El avance físico debe estar entre 0 y 100%.")]
    public decimal? AvanceFisico { get; set; }

    [Display(Name = "Zona")]
    [StringLength(100)]
    public string? Zona { get; set; }

    [Display(Name = "Categoría")]
    [StringLength(100)]
    public string? Categoria { get; set; }

    [Display(Name = "Motivo de retroceso")]
    [StringLength(500)]
    public string? MotivoRetroceso { get; set; }

    [Display(Name = "Motivo de cambio de estado")]
    [StringLength(500)]
    public string? MotivoCambioEstado { get; set; }

    [Display(Name = "Activo")]
    [Column(TypeName = "bit")]
    public bool Activo { get; set; } = true;

    [Display(Name = "Fecha de baja")]
    [DataType(DataType.Date)]
    public DateTime? FechaBaja { get; set; }

    [Display(Name = "Motivo de baja")]
    [StringLength(500)]
    public string? MotivoBaja { get; set; }

    [Display(Name = "Registrado por")]
    [StringLength(100)]
    public string? UsuarioBaja { get; set; }

    public ICollection<Incidencia> Incidencias { get; set; } = new List<Incidencia>();

    public ICollection<ObraArchivo> Archivos { get; set; } = new List<ObraArchivo>();

    public static readonly string[] EstadosDisponibles = ObraEstados.Todos;
}