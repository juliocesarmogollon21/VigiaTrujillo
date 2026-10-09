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

    // Roles que tienen cuenta en el sistema. El ciudadano no tiene cuenta: usa el portal público
    // (obras, reportar y consultar con su código) de forma anónima.
    public static readonly string[] RolesDisponibles =
        { "PersonalMunicipal", "Supervisor", "Administrador" };

    public static bool EsRolValido(string? rol) =>
        !string.IsNullOrWhiteSpace(rol) && RolesDisponibles.Contains(rol);

    // Nombre del rol para mostrarlo en pantalla.
    public static string NombreRol(string? rol) => rol switch
    {
        "PersonalMunicipal" => "Personal Municipal",
        "Supervisor" => "Supervisor de Transparencia",
        "Administrador" => "Administrador",
        _ => rol ?? string.Empty
    };
}
