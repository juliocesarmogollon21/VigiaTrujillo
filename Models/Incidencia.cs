using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VigiaTrujillo.Models;

public class Incidencia
{
    public int Id { get; set; }

    [Required, Display(Name = "Código de seguimiento"), StringLength(20)]
    public string CodigoSeguimiento { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debes seleccionar una obra."), Display(Name = "Obra")]
    public int ObraId { get; set; }

    [ForeignKey(nameof(ObraId))]
    public Obra? Obra { get; set; }

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [Display(Name = "Descripción")]
    [StringLength(1000, MinimumLength = 20)]
    public string Descripcion { get; set; } = string.Empty;

    [Display(Name = "Estado"), StringLength(50)]
    public string Estado { get; set; } = IncidenciaEstados.PendienteDeRevision;

    [Display(Name = "Fecha de registro")]
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    [Display(Name = "Resultado de revisión"), StringLength(1000)]
    public string? ResultadoRevision { get; set; }

    public ICollection<Evidencia> Evidencias { get; set; } = new List<Evidencia>();
    public ICollection<ObservacionIncidencia> Observaciones { get; set; } = new List<ObservacionIncidencia>();

    public static readonly string[] EstadosDisponibles = IncidenciaEstados.Todos;
}