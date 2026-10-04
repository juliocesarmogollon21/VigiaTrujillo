using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using VigiaTrujillo.Hubs;
using VigiaTrujillo.Models;
using VigiaTrujillo.Services.Interfaces;
using VigiaTrujillo.ViewModels;

namespace VigiaTrujillo.Controllers;

[Authorize(Roles = "Supervisor,Administrador")]
public class SupervisorController : Controller
{
    private readonly IIncidenciaService _incidencias;
    private readonly IObraService _obras;
    private readonly IWebHostEnvironment _env;
    private readonly IHubContext<VigiaHub> _hub;
    private readonly ILogger<SupervisorController> _logger;
    private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png", ".pdf" };
    private const long TamanoMaximoBytes = 5 * 1024 * 1024;

    public SupervisorController(
        IIncidenciaService incidencias,
        IObraService obras,
        IWebHostEnvironment env,
        IHubContext<VigiaHub> hub,
        ILogger<SupervisorController> logger)
    {
        _incidencias = incidencias;
        _obras = obras;
        _env = env;
        _hub = hub;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index(string? estado, int? obraId, DateTime? desde, DateTime? hasta)
    {
        var lista = _incidencias.Filtrar(estado, obraId, desde, hasta);
        ViewBag.Estado = estado;
        ViewBag.ObraId = obraId;
        ViewBag.Desde = desde?.ToString("yyyy-MM-dd");
        ViewBag.Hasta = hasta?.ToString("yyyy-MM-dd");
        ViewBag.Estados = Incidencia.EstadosDisponibles.Select(e => new SelectListItem(e, e, e == estado));
        ViewBag.Obras = _obras.GetAll().Select(o => new SelectListItem(o.Nombre, o.Id.ToString(), obraId == o.Id));
        return View(lista);
    }

    [HttpGet]
    public IActionResult Revisar(int id)
    {
        var incidencia = _incidencias.GetByIdWithDetalle(id);
        if (incidencia == null)
        {
            TempData["Error"] = "La incidencia no existe.";
            return RedirectToAction(nameof(Index));
        }

        var permitidos = _incidencias.ObtenerEstadosPermitidos(incidencia.Estado);
        ViewBag.EstadosPermitidos = permitidos;
        ViewBag.IncidenciaCerrada = IncidenciaEstados.EsCerrada(incidencia.Estado);

        return View(new SupervisorRevisarViewModel
        {
            Incidencia = incidencia,
            NuevoEstado = incidencia.Estado,
            ResultadoRevision = incidencia.ResultadoRevision
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarObservacion(int id, string texto)
    {
        var autor = User.Identity?.Name ?? "Supervisor";
        var error = _incidencias.AgregarObservacion(id, texto ?? string.Empty, autor);
        if (error != null)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Revisar), new { id });
        }

        await NotificarSeguro(async () =>
        {
            var inc = _incidencias.GetByIdWithDetalle(id);
            await _hub.Clients.Group(VigiaHub.GrupoTablero).SendAsync(
                "ObservacionAgregada",
                new { id, codigo = inc?.CodigoSeguimiento, autor, mensaje = "Observación agregada a la incidencia" });
        });

        TempData["Success"] = "Observación registrada correctamente en el historial.";
        return RedirectToAction(nameof(Revisar), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, string? nuevoEstado, string? resultadoRevision)
    {
        if (string.IsNullOrWhiteSpace(nuevoEstado))
        {
            TempData["Error"] = "Debe seleccionar un estado destino.";
            return RedirectToAction(nameof(Revisar), new { id });
        }

        var autor = User.Identity?.Name ?? "Supervisor";
        var error = _incidencias.CambiarEstado(id, nuevoEstado, resultadoRevision, autor);

        if (error != null)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Revisar), new { id });
        }

        await NotificarSeguro(async () =>
        {
            var inc = _incidencias.GetByIdWithDetalle(id);
            await _hub.Clients.Group(VigiaHub.GrupoTablero).SendAsync(
                "IncidenciaActualizada",
                new { id, codigo = inc?.CodigoSeguimiento, estado = IncidenciaEstados.Canonico(nuevoEstado) ?? nuevoEstado, obraNombre = inc?.Obra?.Nombre, mensaje = "Estado actualizado" });
        });

        var estadoMostrar = IncidenciaEstados.Canonico(nuevoEstado) ?? nuevoEstado.Trim();
        TempData["Success"] = $"Estado actualizado a «{estadoMostrar}».";
        return RedirectToAction(nameof(Revisar), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EmitirResultado(int id, string? resultadoFinal, string? resultadoRevision)
    {
        if (!IncidenciaEstados.EsIgual(resultadoFinal, IncidenciaEstados.Resuelta)
            && !IncidenciaEstados.EsIgual(resultadoFinal, IncidenciaEstados.Derivada))
        {
            TempData["Error"] = "El resultado final debe ser Resuelta o Derivada.";
            return RedirectToAction(nameof(Revisar), new { id });
        }

        if (IncidenciaEstados.EsIgual(resultadoFinal, IncidenciaEstados.Derivada) && string.IsNullOrWhiteSpace(resultadoRevision))
        {
            TempData["Error"] = "Para derivar una incidencia (irregularidad confirmada), es obligatorio redactar el sustento documental.";
            return RedirectToAction(nameof(Revisar), new { id });
        }

        var destino = IncidenciaEstados.Canonico(resultadoFinal)!;
        return await CambiarEstado(id, destino, resultadoRevision);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SolicitarInformacion(int id, string? nota)
    {
        if (string.IsNullOrWhiteSpace(nota))
        {
            TempData["Error"] = "Debe especificar qué documentación o información se requiere.";
            return RedirectToAction(nameof(Revisar), new { id });
        }

        var obsError = _incidencias.AgregarObservacion(id, $"[Solicitud de información] {nota.Trim()}", User.Identity?.Name ?? "Supervisor");
        if (obsError != null)
        {
            TempData["Error"] = obsError;
            return RedirectToAction(nameof(Revisar), new { id });
        }

        var error = _incidencias.CambiarEstado(id, IncidenciaEstados.InformacionSolicitada);
        if (error == null)
        {
            await NotificarSeguro(async () =>
            {
                var inc = _incidencias.GetByIdWithDetalle(id);
                await _hub.Clients.Group(VigiaHub.GrupoTablero).SendAsync(
                    "IncidenciaActualizada",
                    new { id, codigo = inc?.CodigoSeguimiento, estado = IncidenciaEstados.InformacionSolicitada, obraNombre = inc?.Obra?.Nombre, mensaje = "Se solicitó información adicional" });
            });
        }

        TempData[error == null ? "Success" : "Error"] = error ?? "Se solicitó información adicional al Personal Municipal.";
        return RedirectToAction(nameof(Revisar), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult AgregarEvidencia(int id, IFormFile? archivo)
    {
        var incidencia = _incidencias.GetByIdWithDetalle(id);
        if (incidencia == null) return RedirectToAction(nameof(Index));
        
        if (IncidenciaEstados.EsCerrada(incidencia.Estado))
        {
            TempData["Error"] = "No se pueden agregar evidencias a una incidencia cerrada.";
            return RedirectToAction(nameof(Revisar), new { id });
        }
        if (archivo == null || archivo.Length == 0)
        {
            TempData["Error"] = "Seleccione un archivo válido.";
            return RedirectToAction(nameof(Revisar), new { id });
        }
        
        var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!ExtensionesPermitidas.Contains(ext) || archivo.Length > TamanoMaximoBytes)
        {
            TempData["Error"] = "Archivo no permitido. Formatos: jpg, png, pdf. Máx. 5 MB.";
            return RedirectToAction(nameof(Revisar), new { id });
        }

        var carpeta = Path.Combine(_env.WebRootPath, "uploads", "incidencias", id.ToString());
        Directory.CreateDirectory(carpeta);
        var nombreUnico = $"{Guid.NewGuid()}{ext}";
        using (var stream = new FileStream(Path.Combine(carpeta, nombreUnico), FileMode.Create))
            archivo.CopyTo(stream);

        _incidencias.RegistrarEvidencia(id, $"/uploads/incidencias/{id}/{nombreUnico}", archivo.FileName);
        TempData["Success"] = "Evidencia agregada al detalle correctamente.";
        return RedirectToAction(nameof(Revisar), new { id });
    }

    private async Task NotificarSeguro(Func<Task> publicar)
    {
        try { await publicar(); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo publicar notificación SignalR; la operación principal se conservó.");
        }
    }
}