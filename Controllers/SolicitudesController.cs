using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using VigiaTrujillo.Hubs;
using VigiaTrujillo.Services.Interfaces;

namespace VigiaTrujillo.Controllers;

// Pantalla del Personal Municipal para ver y responder las solicitudes de información del Supervisor.
[Authorize(Roles = "PersonalMunicipal,Administrador")]
public class SolicitudesController : Controller
{
    private readonly IIncidenciaService _incidencias;
    private readonly IWebHostEnvironment _env;
    private readonly IHubContext<VigiaHub> _hub;
    private readonly ILogger<SolicitudesController> _logger;

    private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx" };
    private const long TamanoMaximoBytes = 10 * 1024 * 1024;

    public SolicitudesController(
        IIncidenciaService incidencias,
        IWebHostEnvironment env,
        IHubContext<VigiaHub> hub,
        ILogger<SolicitudesController> logger)
    {
        _incidencias = incidencias;
        _env = env;
        _hub = hub;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index(int? obraId)
    {
        ViewBag.ObraId = obraId;
        ViewBag.Pendientes = _incidencias.GetSolicitudesPendientes(obraId);
        ViewBag.Respondidas = _incidencias.GetSolicitudesRespondidas(15);
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Responder(int solicitudId, string? respuesta, IFormFile? archivoSustento, string? origen)
    {
        // Primero se valida la solicitud y el texto; así no se guarda un archivo si la respuesta no procede.
        var error = _incidencias.ValidarRespuesta(solicitudId, respuesta);
        if (error == null && archivoSustento != null && archivoSustento.Length > 0)
        {
            var ext = Path.GetExtension(archivoSustento.FileName).ToLowerInvariant();
            if (!ExtensionesPermitidas.Contains(ext))
                error = "Formato de archivo no permitido. Use JPG, PNG, PDF o DOC.";
            else if (archivoSustento.Length > TamanoMaximoBytes)
                error = "El archivo excede el tamaño máximo (10 MB).";
        }

        if (error != null)
        {
            TempData["Error"] = error;
            return Volver(origen);
        }

        string? ruta = null, nombre = null;
        if (archivoSustento != null && archivoSustento.Length > 0)
        {
            var ext = Path.GetExtension(archivoSustento.FileName).ToLowerInvariant();
            var carpeta = Path.Combine(_env.WebRootPath, "uploads", "respuestas-solicitud");
            Directory.CreateDirectory(carpeta);
            var nombreUnico = $"resp-{solicitudId}-{Guid.NewGuid()}{ext}";
            using (var stream = new FileStream(Path.Combine(carpeta, nombreUnico), FileMode.Create))
                await archivoSustento.CopyToAsync(stream);
            ruta = $"/uploads/respuestas-solicitud/{nombreUnico}";
            nombre = Path.GetFileName(archivoSustento.FileName);
            if (nombre.Length > 200) nombre = nombre[^200..];
        }

        var autor = User.Identity?.Name ?? "Personal Municipal";
        error = _incidencias.ResponderSolicitud(solicitudId, respuesta, autor, ruta, nombre);
        if (error != null)
        {
            TempData["Error"] = error;
            return Volver(origen);
        }

        var solicitud = _incidencias.GetSolicitud(solicitudId);
        try
        {
            await _hub.Clients.Group(VigiaHub.GrupoSupervisores).SendAsync("RespuestaSolicitud", new
            {
                id = solicitud?.IncidenciaId,
                codigo = solicitud?.Incidencia?.CodigoSeguimiento,
                obraNombre = solicitud?.Incidencia?.Obra?.Nombre,
                autor,
                mensaje = "El Personal Municipal respondió una solicitud de información"
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR falló al notificar la respuesta de la solicitud {Id}", solicitudId);
        }

        TempData["Success"] = $"Respuesta enviada al Supervisor (incidencia {solicitud?.Incidencia?.CodigoSeguimiento}). El Supervisor la verá marcada como «Respuesta nueva».";
        return Volver(origen);
    }

    private IActionResult Volver(string? origen) =>
        origen == "obras" ? RedirectToAction("Index", "Obras") : RedirectToAction(nameof(Index));
}
