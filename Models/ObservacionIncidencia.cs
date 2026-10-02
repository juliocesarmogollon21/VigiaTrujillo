using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VigiaTrujillo.Models;

public class ObservacionIncidencia
{
    public int Id { get; set; }
    public int IncidenciaId { get; set; }
    [ForeignKey(nameof(IncidenciaId))]
    public Incidencia? Incidencia { get; set; }

    [Required, StringLength(2000, MinimumLength = 5)]
    public string Texto { get; set; } = string.Empty;

    public DateTime Fecha { get; set; } = DateTime.Now;

    [Required, StringLength(50)]
    public string Autor { get; set; } = string.Empty;
}
