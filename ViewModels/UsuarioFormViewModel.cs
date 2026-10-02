using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VigiaTrujillo.ViewModels;

public class UsuarioFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [Display(Name = "Usuario"), StringLength(50)]
    public string NombreUsuario { get; set; } = string.Empty;

    [Display(Name = "Contraseña"), DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mínimo 6 caracteres.")]
    public string? Contrasena { get; set; }

    [Required, Display(Name = "Rol")]
    public string Rol { get; set; } = "Ciudadano";

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;

    public IEnumerable<SelectListItem> Roles { get; set; } = Array.Empty<SelectListItem>();
}
