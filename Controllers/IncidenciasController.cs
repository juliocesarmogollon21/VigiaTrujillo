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
    private readonly IFiltroContenidoService _filtro;

    private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png", ".pdf" };
    private const long TamanoMaximoBytes = 5 * 1024 * 1024;
    private const int MaximoArchivos = 3;

    public IncidenciasController(
        IIncidenciaService incidenciaService,
        IObraService obraService,
        IWebHostEnvironment env,
        ILogger<IncidenciasController> logger,
        IHubContext<VigiaHub> hub,
        IFiltroContenidoService filtro)
    {
        _incidenciaService = incidenciaService;
        _obraService = obraService;
        _env = env;
        _logger = logger;
        _hub = hub;
        _filtro = filtro;
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Registrar(int? obraId)
    {
        var vm = new IncidenciaFormViewModel
        {
            ObraId = obraId ?? 0,
            ObrasDisponibles = ObrasActivas()
        };
        return View(vm);
    }

    private List<Obra> ObrasActivas() => _obraService.GetAll().Where(o => o.Activo).ToList();

    [HttpPost, ValidateAntiForgeryToken, AllowAnonymous]
    public async Task<IActionResult> Registrar(IncidenciaFormViewModel vm)
    {
        // La obra debe existir y seguir activa (no dada de baja).
        var obraSeleccionada = vm.ObraId > 0 ? _obraService.GetById(vm.ObraId) : null;
        if (vm.ObraId > 0 && (obraSeleccionada == null || !obraSeleccionada.Activo))
            ModelState.AddModelError(nameof(vm.ObraId), "La obra seleccionada no existe o ya no está disponible.");

        // Filtro de contenido: palabras no permitidas, palabras repetidas y relleno de caracteres.
        foreach (var problema in _filtro.Validar(vm.Descripcion, "La descripción"))
            ModelState.AddModelError(nameof(vm.Descripcion), problema);

        var archivos = vm.Archivos?.Where(a => a != null && a.Length > 0).ToList() ?? new List<IFormFile>();
        if (archivos.Count > MaximoArchivos)
            ModelState.AddModelError(nameof(vm.Archivos), $"Solo puedes adjuntar hasta {MaximoArchivos} archivos.");

        foreach (var archivo in archivos)
        {
            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (!ExtensionesPermitidas.Contains(extension))
                ModelState.AddModelError(nameof(vm.Archivos), $"El archivo '{archivo.FileName}' tiene un formato no permitido. Use JPG, PNG o PDF.");
            else if (archivo.Length > TamanoMaximoBytes)
                ModelState.AddModelError(nameof(vm.Archivos), $"El archivo '{archivo.FileName}' excede el tamaño máximo de 5 MB.");
        }

        if (!ModelState.IsValid)
        {
            vm.ObrasDisponibles = ObrasActivas();
            return View(vm);
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
            vm.ObrasDisponibles = ObrasActivas();
            return View(vm);
        }

        if (archivos.Count > 0)
        {
            GuardarEvidencias(incidencia.Id, archivos);
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

        foreach (var archivo in archivos.Take(MaximoArchivos))
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