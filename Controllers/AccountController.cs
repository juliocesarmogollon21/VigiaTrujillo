using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using VigiaTrujillo.Models;
using VigiaTrujillo.Repositories;
using VigiaTrujillo.Utils;
using VigiaTrujillo.ViewModels;

namespace VigiaTrujillo.Controllers;

public class AccountController : Controller
{
    private readonly IUsuarioRepository _usuarios;
    public AccountController(IUsuarioRepository usuarios) => _usuarios = usuarios;

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(vm);

        var usuario = _usuarios.GetByNombre(vm.Usuario);
        // Solo inician sesión el Personal Municipal, el Supervisor y el Administrador.
        // El ciudadano no tiene cuenta: usa el portal público sin iniciar sesión.
        if (usuario == null || !usuario.Activo || !Usuario.EsRolValido(usuario.Rol)
            || !PasswordHelper.Verify(vm.Contrasena, usuario.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
            return View(vm);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, usuario.NombreUsuario),
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Role, usuario.Rol)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectByRole(usuario.Rol);
    }

    private IActionResult RedirectByRole(string rol) => rol switch
    {
        "PersonalMunicipal" => RedirectToAction("Dashboard", "Obras"),
        "Supervisor" => RedirectToAction("Index", "Supervisor"),
        "Administrador" => RedirectToAction("Index", "Usuarios"),
        _ => RedirectToAction("Index", "Home")
    };

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}
