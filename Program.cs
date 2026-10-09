using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using VigiaTrujillo.Data;
using VigiaTrujillo.Models;
using VigiaTrujillo.Repositories;
using VigiaTrujillo.Services;
using VigiaTrujillo.Services.Interfaces;
using VigiaTrujillo.Strategies;
using VigiaTrujillo.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IObraRepository, ObraRepository>();
builder.Services.AddScoped<IIncidenciaRepository, IncidenciaRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<ISolicitudInformacionRepository, SolicitudInformacionRepository>();

builder.Services.AddScoped<IObraService, ObraEfService>();
builder.Services.AddScoped<IIncidenciaService, IncidenciaService>();

// Filtro de contenido para los textos del ciudadano (lista y límites en appsettings.json)
builder.Services.Configure<FiltroContenidoOptions>(builder.Configuration.GetSection("FiltroContenido"));
builder.Services.AddSingleton<IFiltroContenidoService, FiltroContenidoService>();

builder.Services.AddScoped<IIncidenciaEstadoStrategy, PendienteDeRevisionStrategy>();
builder.Services.AddScoped<IIncidenciaEstadoStrategy, EnRevisionStrategy>();
builder.Services.AddScoped<IIncidenciaEstadoStrategy, InformacionSolicitadaStrategy>();
builder.Services.AddScoped<IIncidenciaEstadoStrategy, EnVerificacionStrategy>();
builder.Services.AddScoped<IIncidenciaEstadoStrategy, ResueltaStrategy>();
builder.Services.AddScoped<IIncidenciaEstadoStrategy, DerivadaStrategy>();
builder.Services.AddScoped<IncidenciaEstadoContext>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        // Una sesión con un rol que ya no existe (por ejemplo una cuenta antigua de ciudadano) se cierra.
        options.Events.OnValidatePrincipal = async context =>
        {
            var rol = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (!Usuario.EsRolValido(rol))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

var app = builder.Build();

var supportedCultures = new[] { new CultureInfo("es-PE") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("es-PE"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

app.UseExceptionHandler("/Home/Error");
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHub<VigiaHub>("/hubs/vigia");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    DbInitializer.Inicializar(db, app.Environment.ContentRootPath, app.Environment.WebRootPath);
}

app.Run();
