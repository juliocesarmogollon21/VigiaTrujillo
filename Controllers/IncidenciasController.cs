using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using VigiaTrujillo.Hubs;
using VigiaTrujillo.Models;
using VigiaTrujillo.Services.Interfaces;
using VigiaTrujillo.ViewModels;

namespace VigiaTrujillo.Controllers;

public class IncidenciasController : Controller
{
    private readonly IIncidenciaService _incidenciaService;
    private readonly IObraService _obraService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<IncidenciasController> _logger;
    private readonly IHubContext<VigiaHub> _hub;

    private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png", ".pdf" };
    private const long TamanoMaximoBytes = 5 * 1024 * 1024;

    public IncidenciasController(
        IIncidenciaService incidenciaService,
        IObraService obraService,
        IWebHostEnvironment env,
        ILogger<IncidenciasController> logger,
        IHubContext<VigiaHub> hub)
    {
        _incidenciaService = incidenciaService;
        _obraService = obraService;
        _env = env;
        _logger = logger;
        _hub = hub;
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Registrar(int? obraId)
    {
        var vm = new IncidenciaFormViewModel
        {
            ObraId = obraId ?? 0,
            ObrasDisponibles = _obraService.GetAll().Where(o => o.Activo).ToList()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken, AllowAnonymous]
    public async Task<IActionResult> Registrar(IncidenciaFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.ObrasDisponibles = _obraService.GetAll().ToList();
            return View(vm);
        }

        if (vm.Archivos != null && vm.Archivos.Count > 0)
        {
            foreach (var archivo in vm.Archivos)
            {
                if (archivo.Length == 0) continue;

                var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
                
                if (!ExtensionesPermitidas.Contains(extension))
                {
                    ModelState.AddModelError("Archivos", $"El archivo '{archivo.FileName}' tiene un formato no permitido. Use JPG, PNG o PDF.");
                    vm.ObrasDisponibles = _obraService.GetAll().ToList();
                    return View(vm);
                }

                if (archivo.Length > TamanoMaximoBytes)
                {
                    ModelState.AddModelError("Archivos", $"El archivo '{archivo.FileName}' excede el tamaño máximo de 5 MB.");
                    vm.ObrasDisponibles = _obraService.GetAll().ToList();
                    return View(vm); 
                }
            }
        }

        var incidencia = new Incidencia
        {
            ObraId = vm.ObraId,
            Descripcion = vm.Descripcion.Trim()
        };

        string codigo;
        try 
        { 
            codigo = _incidenciaService.Registrar(incidencia); 
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            vm.ObrasDisponibles = _obraService.GetAll().ToList();
            return View(vm);
        }

        if (vm.Archivos != null && vm.Archivos.Count > 0)
        {
            GuardarEvidencias(incidencia.Id, vm.Archivos);
        }

        var obra = _obraService.GetById(incidencia.ObraId);
        try
        {
            await _hub.Clients.Group(VigiaHub.GrupoSupervisores).SendAsync(
                "NuevaIncidencia",
                new
                {
                    id = incidencia.Id,
                    codigo,
                    obraId = incidencia.ObraId,
                    obraNombre = obra?.Nombre ?? "Obra",
                    estado = incidencia.Estado,
                    fecha = incidencia.FechaRegistro.ToString("dd/MM/yyyy HH:mm"),
                    mensaje = "Nueva incidencia registrada"
                });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR falló al notificar nueva incidencia {Codigo}", codigo);
        }

        return RedirectToAction(nameof(Confirmacion), new { codigo });
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Confirmacion(string codigo)
    {
        ViewBag.Codigo = codigo;
        return View(_incidenciaService.GetByCodigoSeguimiento(codigo));
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Consultar() => View(new ConsultaEstadoViewModel());

    [HttpPost, ValidateAntiForgeryToken, AllowAnonymous]
    public IActionResult Consultar(ConsultaEstadoViewModel vm)
    {
        vm.Buscado = true;
        if (!string.IsNullOrWhiteSpace(vm.Codigo))
            vm.Resultado = _incidenciaService.GetByCodigoSeguimiento(vm.Codigo);
        return View(vm);
    }

    private void GuardarEvidencias(int incidenciaId, List<IFormFile> archivos)
    {
        var carpeta = Path.Combine(_env.WebRootPath, "uploads", "incidencias", incidenciaId.ToString());
        Directory.CreateDirectory(carpeta);

        foreach (var archivo in archivos.Take(3)) 
        {
            if (archivo.Length == 0) continue;

            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            var nombreUnico = $"{Guid.NewGuid()}{extension}";
            var rutaFisica = Path.Combine(carpeta, nombreUnico);
            
            using (var stream = new FileStream(rutaFisica, FileMode.Create))
            {
                archivo.CopyTo(stream);
            }

            var rutaWeb = $"/uploads/incidencias/{incidenciaId}/{nombreUnico}";
            _incidenciaService.RegistrarEvidencia(incidenciaId, rutaWeb, archivo.FileName);
        }
    }
}