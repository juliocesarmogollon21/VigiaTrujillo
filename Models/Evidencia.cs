using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VigiaTrujillo.Models;

public class Evidencia
{
    public int Id { get; set; }
    public int IncidenciaId { get; set; }
    [ForeignKey(nameof(IncidenciaId))]
    public Incidencia? Incidencia { get; set; }

    [Required, StringLength(300)]
    public string RutaArchivo { get; set; } = string.Empty;

    [StringLength(100)]
    public string NombreArchivo { get; set; } = string.Empty;

    public DateTime FechaCarga { get; set; } = DateTime.Now;
}
