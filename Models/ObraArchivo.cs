using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VigiaTrujillo.Models;

public class ObraArchivo
{
    public int Id { get; set; }
    public int ObraId { get; set; }
    [ForeignKey(nameof(ObraId))]
    public Obra? Obra { get; set; }

    [Required, StringLength(300)]
    public string Ruta { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(50)]
    public string Tipo { get; set; } = "documento";

    public DateTime FechaCarga { get; set; } = DateTime.Now;
}
