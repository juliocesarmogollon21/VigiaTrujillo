using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using VigiaTrujillo.Hubs;
using VigiaTrujillo.Models;
using VigiaTrujillo.Services.Interfaces;
using VigiaTrujillo.ViewModels;

namespace VigiaTrujillo.Controllers;

[Authorize(Roles = "PersonalMunicipal,Administrador")]
public class ObrasController : Controller
{
    private readonly IObraService _obraService;
    private readonly IIncidenciaService _incidenciaService;
    private readonly IWebHostEnvironment _env;
    private readonly IHubContext<VigiaHub> _hub;
    private readonly ILogger<ObrasController> _logger;

    public ObrasController(
        IObraService obraService, 
        IIncidenciaService incidenciaService, 
        IWebHostEnvironment env, 
        IHubContext<VigiaHub> hub, 
        ILogger<ObrasController> logger)
    {
        _obraService = obraService;
        _incidenciaService = incidenciaService;
        _env = env;
        _hub = hub;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Dashboard()
    {
        var obras = _obraService.GetAll();
        
        ViewBag.Total = obras.Count;
        ViewBag.Programadas = obras.Count(o => o.Estado == "Programada");
        ViewBag.EnEjecucion = obras.Count(o => o.Estado == "En ejecución");
        ViewBag.Concluidas = obras.Count(o => o.Estado == "Concluida");
        ViewBag.Paralizadas = obras.Count(o => o.Estado == "Paralizada");
        ViewBag.Sobrecosto = obras.Count(o => o.Estado == "Con Sobrecosto");
 
        ViewBag.SolicitudesPendientes = _incidenciaService.ContarSolicitudesPendientes();
        
        ViewBag.Categorias = obras
            .Where(o => !string.IsNullOrEmpty(o.Categoria))
            .GroupBy(o => o.Categoria)
            .Select(g => g.Key)
            .Take(6)
            .ToList();

        ViewBag.Presupuestos = obras
            .Where(o => !string.IsNullOrEmpty(o.Categoria))
            .GroupBy(o => o.Categoria)
            .Select(g => g.Sum(o => o.Presupuesto ?? 0))
            .Take(6)
            .ToList();

        return View();
    }

    [HttpGet]
    public IActionResult Index(string? busqueda, string? estado, bool conSolicitudes = false)
    {
        var obras = _obraService.GetAll(busqueda, estado);
        
        // Solicitudes del Supervisor que siguen sin respuesta, agrupadas por obra.
        var dictPendientes = _incidenciaService.GetSolicitudesPendientes()
            .Where(sol => sol.Incidencia != null)
            .GroupBy(sol => sol.Incidencia!.ObraId)
            .ToDictionary(g => g.Key, g => g.ToList());

        if (conSolicitudes)
        {
            var obrasIds = dictPendientes.Keys.ToHashSet();
            obras = obras.Where(o => obrasIds.Contains(o.Id)).ToList();
        }
        
        var vm = new ObraIndexViewModel
        {
            Busqueda = busqueda,
            Estado = estado,
            ConSolicitudes = conSolicitudes,
            Obras = obras.Select(o => 
            {
                var pendientesObra = dictPendientes.TryGetValue(o.Id, out var sp) ? sp : new List<SolicitudInformacion>();
                var solPendiente = pendientesObra.FirstOrDefault();

                return new ObraListItemViewModel
                {
                    Id = o.Id, 
                    Nombre = o.Nombre, 
                    Ubicacion = o.Ubicacion, 
                    Cui = o.Cui,
                    Estado = o.Estado, 
                    FechaInicio = o.FechaInicio, 
                    FechaFin = o.FechaFin,
                    AvanceFisico = o.AvanceFisico,
                    
                    TieneSolicitudPendiente = solPendiente != null,
                    SolicitudIdPendiente = solPendiente?.Id,
                    CantidadSolicitudesPendientes = pendientesObra.Count,
                    IncidenciaIdPendiente = solPendiente?.IncidenciaId,
                    CodigoIncidenciaPendiente = solPendiente?.Incidencia?.CodigoSeguimiento,
                    TextoSolicitudPendiente = solPendiente?.Pregunta
                };
            }).ToList()
        };
        return View(vm);
    }

    public IActionResult Details(int id)
    {
        var obra = _obraService.GetByIdWithArchivos(id);
        if (obra == null) 
        { 
            TempData["Error"] = "La obra solicitada no existe."; 
            return RedirectToAction(nameof(Index)); 
        }

        ViewBag.IncidenciasAsociadas = obra.Incidencias.ToList();
        return View(obra);
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Publico(string? busqueda, string? estado, string? contratista, string? zona, string? categoria)
    {
        var obras = _obraService.GetAll(busqueda, estado, contratista, zona, categoria);
        var todas = _obraService.GetAll();

        ViewBag.Total = obras.Count;
        ViewBag.TotalGeneral = todas.Count;
        ViewBag.Busqueda = busqueda; 
        ViewBag.Estado = estado;
        ViewBag.Contratista = contratista; 
        ViewBag.Zona = zona; 
        ViewBag.Categoria = categoria;
        ViewBag.Estados = Obra.EstadosDisponibles.Select(e => new SelectListItem(e, e, e == estado));

        ViewBag.Categorias = todas
            .Where(o => !string.IsNullOrWhiteSpace(o.Categoria))
            .Select(o => o.Categoria!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .Select(c => new SelectListItem(c, c, string.Equals(c, categoria, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        ViewBag.Zonas = todas
            .Where(o => !string.IsNullOrWhiteSpace(o.Zona))
            .Select(o => o.Zona!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(z => z)
            .Select(z => new SelectListItem(z, z, string.Equals(z, zona, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        ViewBag.Contratistas = todas
            .Where(o => !string.IsNullOrWhiteSpace(o.Contratista))
            .Select(o => o.Contratista!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .Select(c => new SelectListItem(c, c, string.Equals(c, contratista, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        ViewBag.HayFiltros = !string.IsNullOrWhiteSpace(busqueda)
                         || !string.IsNullOrWhiteSpace(estado)
                         || !string.IsNullOrWhiteSpace(categoria)
                         || !string.IsNullOrWhiteSpace(zona)
                         || !string.IsNullOrWhiteSpace(contratista);

        return View(obras);
    }

    [HttpGet, AllowAnonymous]
    public IActionResult DetallePublico(int id)
    {
        var obra = _obraService.GetByIdWithArchivos(id);

        if (obra == null || !obra.Activo)
        { 
            TempData["Error"] = "La obra no está disponible."; 
            return RedirectToAction(nameof(Publico)); 
        } 
        return View(obra);
    }

    public IActionResult Create() => View(new ObraFormViewModel { Estado = "Programada", Estados = BuildEstados("Programada") });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ObraFormViewModel vm)
    {
        if (_obraService.ExistsCui(vm.Cui))
        {
            ModelState.AddModelError(nameof(vm.Cui), "Ya existe una obra con este CUI. Debe ser único.");
        }

        if (!ModelState.IsValid) 
        { 
            vm.Estados = BuildEstados(vm.Estado); 
            return View(vm); 
        }
        
        try
        {
            var obra = Map(vm);
            _obraService.Create(obra);
            TempData["Success"] = $"Obra \"{obra.Nombre}\" registrada y publicada correctamente.";
            
            await NotificarSeguro(() => _hub.Clients.Group(VigiaHub.GrupoTablero).SendAsync("EstadoObraActualizado", new 
            { 
                id = obra.Id, 
                nombre = obra.Nombre, 
                estado = obra.Estado, 
                avance = obra.AvanceFisico, 
                mensaje = "Nueva obra registrada" 
            }));
            
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            vm.Estados = BuildEstados(vm.Estado); 
            return View(vm);
        }
    }

        [HttpGet]
    public IActionResult Edit(int id)
    {
        var obra = _obraService.GetById(id);
        if (obra == null) 
        { 
            TempData["Error"] = "La obra solicitada no existe."; 
            return RedirectToAction(nameof(Index)); 
        }

        if (obra.Estado == "Concluida")
        {
            TempData["Error"] = "No es posible editar una obra que ya se encuentra en estado 'Concluida'. Los registros finales son inmutables.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var vm = MapForm(obra); 
        vm.Estados = BuildEstados(obra.Estado); 
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ObraFormViewModel vm)
    {
        if (id != vm.Id) 
        { 
            TempData["Error"] = "Identificador inconsistente."; 
            return RedirectToAction(nameof(Index)); 
        }

        var obraExistente = _obraService.GetById(id);
        if (obraExistente != null && obraExistente.Estado == "Concluida")
        {
            TempData["Error"] = "No es posible editar una obra que ya se encuentra en estado 'Concluida'.";
            return RedirectToAction(nameof(Details), new { id });
        }
        
        if (obraExistente == null)
        {
            TempData["Error"] = "La obra solicitada no existe.";
            return RedirectToAction(nameof(Index));
        }

        if (_obraService.ExistsCui(vm.Cui, vm.Id))
        {
            ModelState.AddModelError(nameof(vm.Cui), "Ya existe una obra con este CUI. Debe ser único.");
        }

        // Los cambios de estado o de avance desde el formulario de edición también necesitan su motivo.
        var cambiaEstado = !ObraEstados.EsIgual(obraExistente.Estado, vm.Estado);
        var avanceAnterior = obraExistente.AvanceFisico ?? 0;
        var avanceNuevo = vm.AvanceFisico ?? 0;
        if (cambiaEstado && string.IsNullOrWhiteSpace(vm.MotivoCambioEstado))
            ModelState.AddModelError(nameof(vm.MotivoCambioEstado), "Debe indicar el motivo del cambio de estado.");
        if (avanceNuevo != avanceAnterior && string.IsNullOrWhiteSpace(vm.MotivoCambioAvance))
            ModelState.AddModelError(nameof(vm.MotivoCambioAvance), "Debe justificar el cambio del avance físico.");

        if (!ModelState.IsValid) 
        { 
            vm.Estados = BuildEstados(vm.Estado); 
            ViewBag.EstadoOriginal = obraExistente.Estado;
            ViewBag.AvanceOriginal = obraExistente.AvanceFisico;
            return View(vm); 
        }
        
        try
        {
            var obraEditada = Map(vm);
            obraEditada.MotivoCambioEstado = cambiaEstado ? vm.MotivoCambioEstado!.Trim() : obraExistente.MotivoCambioEstado;
            obraEditada.MotivoRetroceso = avanceNuevo < avanceAnterior ? vm.MotivoCambioAvance!.Trim() : obraExistente.MotivoRetroceso;
            var updated = _obraService.Update(obraEditada);
            if (updated == null) 
            { 
                TempData["Error"] = "La obra solicitada no existe."; 
                return RedirectToAction(nameof(Index)); 
            }
            
            TempData["Success"] = $"Obra \"{updated.Nombre}\" actualizada correctamente.";
            
            await NotificarSeguro(() => _hub.Clients.Group(VigiaHub.GrupoTablero).SendAsync("EstadoObraActualizado", new 
            { 
                id = updated.Id, 
                nombre = updated.Nombre, 
                estado = updated.Estado, 
                avance = updated.AvanceFisico, 
                mensaje = "Información de obra actualizada" 
            }));
            
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            vm.Estados = BuildEstados(vm.Estado); 
            return View(vm);
        }
    }

    public IActionResult Delete(int id)
    {
        var obra = _obraService.GetById(id);
        if (obra == null) 
        { 
            TempData["Error"] = "La obra solicitada no existe."; 
            return RedirectToAction(nameof(Index)); 
        }

        ViewBag.IncidenciasAsociadas = _incidenciaService.GetByObraId(id);
        return View(obra);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, string? motivo)
    {
        var obra = _obraService.GetById(id);
        if (obra == null)
        {
            TempData["Error"] = "La obra solicitada no existe.";
            return RedirectToAction(nameof(Index));
        }

        var error = _obraService.DarDeBaja(id, motivo);
        if (error != null)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Delete), new { id });
        }

        TempData["Success"] = $"La obra \"{obra.Nombre}\" fue dada de baja. "
                            + "El registro se conserva en la base de datos para su auditoria.";

        await NotificarSeguro(() => _hub.Clients.Group(VigiaHub.GrupoTablero).SendAsync("EstadoObraActualizado", new
        {
            id = obra.Id,
            nombre = obra.Nombre,
            estado = "Dada de baja",
            avance = obra.AvanceFisico,
            mensaje = "Obra dada de baja (registro conservado)"
        }));

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivar(int id)
    {
        var error = _obraService.Reactivar(id);
        if (error != null)
        {
            TempData["Error"] = error;
        }
        else
        {
            var obra = _obraService.GetById(id);
            TempData["Success"] = $"La obra \"{obra?.Nombre}\" fue reactivada correctamente.";
            await NotificarSeguro(() => _hub.Clients.Group(VigiaHub.GrupoTablero).SendAsync("EstadoObraActualizado", new
            {
                id,
                nombre = obra?.Nombre,
                estado = obra?.Estado,
                avance = obra?.AvanceFisico,
                mensaje = "Obra reactivada"
            }));
        }

        return RedirectToAction(nameof(DadosDeBaja));
    }

    [HttpGet]
    public IActionResult DadosDeBaja() => View(_obraService.GetDadosDeBaja());

    [HttpGet]
    public IActionResult ActualizarAvance(int id)
    {
        var obra = _obraService.GetById(id);
        if (obra == null) 
        { 
            TempData["Error"] = "La obra no existe."; 
            return RedirectToAction(nameof(Index)); 
        }
        return View(obra);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarAvance(int id, decimal nuevoAvance, string? motivoRetroceso)
    {
        var error = _obraService.ActualizarAvance(id, nuevoAvance, motivoRetroceso);
        if (error != null) 
        { 
            TempData["Error"] = error; 
            var oErr = _obraService.GetById(id); 
            return oErr == null ? RedirectToAction(nameof(Index)) : View(oErr); 
        }

        var obra = _obraService.GetById(id);
        if (obra == null)
        {
            TempData["Error"] = "La obra no existe.";
            return RedirectToAction(nameof(Index));
        }

        await NotificarSeguro(() => _hub.Clients.Group(VigiaHub.GrupoTablero).SendAsync("EstadoObraActualizado", new 
        { 
            id, 
            nombre = obra.Nombre, 
            estado = obra.Estado, 
            avance = obra.AvanceFisico, 
            mensaje = "Avance de obra actualizado" 
        }));

        TempData["Success"] = "Avance físico actualizado correctamente.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public IActionResult CambiarEstado(int id)
    {
        var obra = _obraService.GetById(id);
        if (obra == null) 
        { 
            TempData["Error"] = "La obra no existe."; 
            return RedirectToAction(nameof(Index)); 
        }
        return View(obra);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, string estado, string? motivo)
    {
        var error = _obraService.CambiarEstado(id, estado, motivo);
        if (error != null) 
        { 
            TempData["Error"] = error; 
            var oErr = _obraService.GetById(id); 
            return oErr == null ? RedirectToAction(nameof(Index)) : View(oErr); 
        }

        var obra = _obraService.GetById(id);
        if (obra == null)
        {
            TempData["Error"] = "La obra no existe.";
            return RedirectToAction(nameof(Index));
        }

        await NotificarSeguro(() => _hub.Clients.Group(VigiaHub.GrupoTablero).SendAsync("EstadoObraActualizado", new 
        { 
            id, 
            nombre = obra.Nombre, 
            estado = obra.Estado, 
            avance = obra.AvanceFisico, 
            mensaje = "Estado de obra actualizado" 
        }));

        TempData["Success"] = "Estado de obra actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SubirArchivo(int id, IFormFile? archivo, string tipo = "documento")
    {
        var obraExiste = _obraService.GetById(id);
        if (obraExiste == null) 
        { 
            TempData["Error"] = "La obra no existe."; 
            return RedirectToAction(nameof(Index)); 
        }
        
        if (archivo == null || archivo.Length == 0)
        { 
            TempData["Error"] = "Debe seleccionar un archivo válido."; 
            return RedirectToAction(nameof(Details), new { id }); 
        }

        var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        var permitidas = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx" };
        
        if (!permitidas.Contains(ext))
        { 
            TempData["Error"] = "Formato no permitido. Use JPG, PNG, PDF o DOC."; 
            return RedirectToAction(nameof(Details), new { id }); 
        }
        
        if (archivo.Length > 10 * 1024 * 1024)
        { 
            TempData["Error"] = "El archivo excede el tamaño máximo permitido (10 MB). Por favor, comprímalo e intente nuevamente."; 
            return RedirectToAction(nameof(Details), new { id }); 
        }

        var carpeta = Path.Combine(_env.WebRootPath, "uploads", "obras", id.ToString());
        Directory.CreateDirectory(carpeta);
        var nombreUnico = $"{Guid.NewGuid()}{ext}";
        
        using (var stream = new FileStream(Path.Combine(carpeta, nombreUnico), FileMode.Create))
            archivo.CopyTo(stream);

        _obraService.AgregarArchivo(id, $"/uploads/obras/{id}/{nombreUnico}", archivo.FileName, tipo);
        TempData["Success"] = "Archivo cargado y publicado correctamente en el detalle de la obra.";
        
        await NotificarSeguro(() => _hub.Clients.Group(VigiaHub.GrupoTablero).SendAsync("ArchivoObraActualizado", new 
        { 
            obraId = id,
            obraNombre = obraExiste.Nombre,
            archivoNombre = archivo.FileName,
            archivoTipo = tipo,
            archivoRuta = $"/uploads/obras/{id}/{nombreUnico}",
            mensaje = $"Se ha publicado nueva evidencia en: {obraExiste.Nombre}"
        }));
        
        return RedirectToAction(nameof(Details), new { id });
    }

    private static Obra Map(ObraFormViewModel vm) => new()
    {
        Id = vm.Id, 
        Nombre = vm.Nombre?.Trim() ?? string.Empty, 
        Ubicacion = vm.Ubicacion?.Trim() ?? string.Empty,
        Cui = vm.Cui?.Trim() ?? string.Empty, 
        FechaInicio = vm.FechaInicio, 
        FechaFin = vm.FechaFin,
        Estado = ObraEstados.Canonico(vm.Estado) ?? ObraEstados.Programada, 
        Presupuesto = vm.Presupuesto,
        Contratista = string.IsNullOrWhiteSpace(vm.Contratista) ? null : vm.Contratista.Trim(),
        AvanceFisico = vm.AvanceFisico, 
        Zona = string.IsNullOrWhiteSpace(vm.Zona) ? null : vm.Zona.Trim(),
        Categoria = string.IsNullOrWhiteSpace(vm.Categoria) ? null : vm.Categoria.Trim()
    };

    private static ObraFormViewModel MapForm(Obra o) => new()
    {
        Id = o.Id, 
        Nombre = o.Nombre, 
        Ubicacion = o.Ubicacion, 
        Cui = o.Cui, 
        FechaInicio = o.FechaInicio,
        FechaFin = o.FechaFin, 
        Estado = o.Estado, 
        Presupuesto = o.Presupuesto, 
        Contratista = o.Contratista,
        AvanceFisico = o.AvanceFisico, 
        Zona = o.Zona, 
        Categoria = o.Categoria
    };

    private static IEnumerable<SelectListItem> BuildEstados(string? selected) =>
        Obra.EstadosDisponibles.Select(e => new SelectListItem 
        { 
            Value = e, 
            Text = e, 
            Selected = string.Equals(e, selected, StringComparison.OrdinalIgnoreCase) 
        });

    private async Task NotificarSeguro(Func<Task> publicar)
    {
        try 
        { 
            await publicar(); 
        }
        catch (Exception ex) 
        { 
            _logger.LogWarning(ex, "SignalR falló al notificar; la operación principal se conservó."); 
        }
    }
}