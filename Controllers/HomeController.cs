using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VigiaTrujillo.ViewModels;
using VigiaTrujillo.Services.Interfaces;

namespace VigiaTrujillo.Controllers;

public class HomeController : Controller
{
    private readonly IObraService _obraService;
    public HomeController(IObraService obraService) => _obraService = obraService;

    public IActionResult Index()
    {
        var obras = _obraService.GetAll();
        ViewBag.TotalObras = obras.Count;
        ViewBag.Programadas = obras.Count(o => o.Estado == "Programada");
        ViewBag.EnEjecucion = obras.Count(o => o.Estado == "En ejecución");
        ViewBag.Concluidas = obras.Count(o => o.Estado == "Concluida");
        return View();
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
