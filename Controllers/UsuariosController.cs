using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VigiaTrujillo.Models;
using VigiaTrujillo.Repositories;
using VigiaTrujillo.Utils;
using VigiaTrujillo.ViewModels;

namespace VigiaTrujillo.Controllers;

[Authorize(Roles = "Administrador")]
public class UsuariosController : Controller
{
    private readonly IUsuarioRepository _repo;
    public UsuariosController(IUsuarioRepository repo) => _repo = repo;

    public IActionResult Index() => View(_repo.GetAll());

    [HttpGet]
    public IActionResult Create() =>
        View(new UsuarioFormViewModel { Roles = BuildRoles() });

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(UsuarioFormViewModel vm)
    {
        vm.Roles = BuildRoles(vm.Rol);
        if (!ModelState.IsValid) return View(vm);
        if (_repo.GetByNombre(vm.NombreUsuario.Trim()) != null)
        {
            ModelState.AddModelError(nameof(vm.NombreUsuario), "Ya existe un usuario con ese nombre.");
            return View(vm);
        }

        var tempPassword = string.IsNullOrWhiteSpace(vm.Contrasena)
            ? $"Temp{Random.Shared.Next(1000, 9999)}!"
            : vm.Contrasena;

        _repo.Create(new Usuario
        {
            NombreUsuario = vm.NombreUsuario.Trim(),
            PasswordHash = PasswordHelper.Hash(tempPassword),
            Rol = vm.Rol,
            Activo = true
        });

        TempData["Success"] = $"Usuario «{vm.NombreUsuario}» creado. Contraseña temporal: {tempPassword}";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var u = _repo.GetById(id);
        if (u == null)
        {
            TempData["Error"] = "El usuario no existe.";
            return RedirectToAction(nameof(Index));
        }
        return View(new UsuarioFormViewModel
        {
            Id = u.Id, NombreUsuario = u.NombreUsuario, Rol = u.Rol, Activo = u.Activo,
            Roles = BuildRoles(u.Rol)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Edit(int id, UsuarioFormViewModel vm)
    {
        if (id != vm.Id)
        {
            TempData["Error"] = "Identificador inconsistente.";
            return RedirectToAction(nameof(Index));
        }
        vm.Roles = BuildRoles(vm.Rol);
        if (!ModelState.IsValid) return View(vm);

        var existing = _repo.GetById(id);
        if (existing == null)
        {
            TempData["Error"] = "El usuario no existe.";
            return RedirectToAction(nameof(Index));
        }

        var otro = _repo.GetByNombre(vm.NombreUsuario.Trim());
        if (otro != null && otro.Id != id)
        {
            ModelState.AddModelError(nameof(vm.NombreUsuario), "Ya existe un usuario con ese nombre.");
            return View(vm);
        }

        var pierdeAdmin = existing.Rol == "Administrador" && existing.Activo
                          && (vm.Rol != "Administrador" || !vm.Activo);
        if (pierdeAdmin && _repo.ContarAdministradoresActivos(excludeId: id) == 0)
        {
            ModelState.AddModelError(string.Empty, "No se puede dejar el sistema sin Administradores activos.");
            return View(vm);
        }

        existing.NombreUsuario = vm.NombreUsuario.Trim();
        existing.Rol = vm.Rol;
        existing.Activo = vm.Activo;
        if (!string.IsNullOrWhiteSpace(vm.Contrasena))
            existing.PasswordHash = PasswordHelper.Hash(vm.Contrasena);

        _repo.Update(existing);
        TempData["Success"] = "Usuario actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Desactivar(int id)
    {
        var u = _repo.GetById(id);
        if (u == null)
        {
            TempData["Error"] = "El usuario no existe.";
            return RedirectToAction(nameof(Index));
        }
        if (u.Rol == "Administrador" && u.Activo && _repo.ContarAdministradoresActivos(excludeId: id) == 0)
        {
            TempData["Error"] = "No se puede desactivar el último Administrador.";
            return RedirectToAction(nameof(Index));
        }
        u.Activo = false;
        _repo.Update(u);
        TempData["Success"] = $"Usuario «{u.NombreUsuario}» desactivado.";
        return RedirectToAction(nameof(Index));
    }

    private static IEnumerable<SelectListItem> BuildRoles(string? selected = null) =>
        Usuario.RolesDisponibles.Select(r => new SelectListItem(r, r, r == selected));
}