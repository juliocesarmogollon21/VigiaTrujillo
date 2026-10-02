using System.ComponentModel.DataAnnotations;

namespace VigiaTrujillo.ViewModels;

public class ConsultaEstadoViewModel
{
    [Required(ErrorMessage = "Ingresa tu código de seguimiento.")]
    [Display(Name = "Código de seguimiento")]
    public string Codigo { get; set; } = string.Empty;

    public Models.Incidencia? Resultado { get; set; }
    public bool Buscado { get; set; }
}
