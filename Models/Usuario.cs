using System.ComponentModel.DataAnnotations;

namespace VigiaTrujillo.Models;

public class Usuario
{
    public int Id { get; set; }

    [Required, StringLength(50), Display(Name = "Nombre de usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, StringLength(30), Display(Name = "Rol")]
    public string Rol { get; set; } = "PersonalMunicipal";

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;

    public static readonly string[] RolesDisponibles =
        { "Ciudadano", "PersonalMunicipal", "Supervisor", "Administrador" };
}
