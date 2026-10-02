using System.Globalization;
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
using VigiaTrujillo.Utils;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IObraRepository, ObraRepository>();
builder.Services.AddScoped<IIncidenciaRepository, IncidenciaRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();

builder.Services.AddScoped<IObraService, ObraEfService>();
builder.Services.AddScoped<IIncidenciaService, IncidenciaService>();

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
    
    db.Database.Migrate();
    
    SeedUsuarios(db);
    SeedObras(db);
    SeedIncidencias(db);
}

app.Run();

static void SeedUsuarios(ApplicationDbContext db)
{
    var seeds = new (string User, string Pass, string Rol)[]
    {
        ("personal.municipal", "Municipal2026", "PersonalMunicipal"),
        ("supervisor", "Supervisor2026", "Supervisor"),
        ("admin", "Admin2026", "Administrador")
    };

    int creados = 0;
    foreach (var (user, pass, rol) in seeds)
    {
        var existing = db.Usuarios.FirstOrDefault(u => u.NombreUsuario == user);
        if (existing == null)
        {
            db.Usuarios.Add(new Usuario
            {
                NombreUsuario = user,
                PasswordHash = PasswordHelper.Hash(pass),
                Rol = rol,
                Activo = true
            });
            creados++;
        }
        else
        {
            existing.Rol = rol;
            existing.Activo = true;
            existing.PasswordHash = PasswordHelper.Hash(pass);
        }
    }
    db.SaveChanges();
    Console.WriteLine($"Usuarios de prueba creados correctamente. ({creados} nuevos)");
}

static void SeedObras(ApplicationDbContext db)
{
    if (db.Obras.Any())
    {
        Console.WriteLine("Las obras ya existen en la base de datos. Omitiendo carga.");
        return;
    }

    const string fuenteBase = "Identidad y plazo: ObrasTrujillo.xlsx. ";
    const string fuenteSimulada = fuenteBase + "Presupuesto y avance simulados para la demostracion; no corresponden a cifras oficiales.";
    const string fuenteMef = "MEF - Consulta Amigable, corte 01/10/2026.";

    var obras = new List<Obra>
    {
        new Obra
        {
            Nombre = "Avenida Manuel Vera Enriquez",
            Cui = "2687013",
            Ubicacion = "Desde Av. Espana hasta Av. Teodoro Valcarcel",
            Zona = "Trujillo",
            Categoria = "Pavimentacion",
            Contratista = "Consorcio vial del norte",
            FechaInicio = new DateTime(2026, 7, 1),
            FechaFin = new DateTime(2026, 10, 1),
            Presupuesto = 4850000.00m,
            AvanceFisico = 72.50m,
            Estado = ObraEstados.EnEjecucion,
            Plazo = "Del 01/07/2026 al 01/10/2026",
            Fuente = fuenteSimulada
        },
        new Obra
        {
            Nombre = "Avenida Federico Villarreal",
            Cui = "2671169",
            Ubicacion = "Desde Av. America Norte hasta Av. Cesar Vallejo",
            Zona = "Trujillo",
            Categoria = "Pavimentacion",
            Contratista = "No informado en la fuente oficial",
            FechaInicio = new DateTime(2025, 8, 1),
            FechaFin = new DateTime(2026, 3, 31),
            Presupuesto = 9000000.00m,
            AvanceFisico = 96.90m,
            Estado = ObraEstados.EnEjecucion,
            Plazo = "Desde el 01/08/2025. Primer Trimestre 2026",
            Fuente = fuenteBase + fuenteMef + " PIM S/ 9.0 M; devengado S/ 8.8 M"
        },
        new Obra
        {
            Nombre = "Urb. Ingenieria I",
            Cui = "2709817",
            Ubicacion = "Servicio de movilidad urbana integral",
            Zona = "Trujillo",
            Categoria = "Movilidad Urbana",
            Contratista = "Constructora Andina S.A.C.",
            FechaInicio = new DateTime(2026, 5, 1),
            FechaFin = new DateTime(2026, 12, 31),
            Presupuesto = 2340000.00m,
            AvanceFisico = 15.00m,
            Estado = ObraEstados.EnEjecucion,
            Plazo = "Desde el 01/05/2026. Fines de 2026",
            Fuente = fuenteSimulada
        },
        new Obra
        {
            Nombre = "Pueblo Joven El Bosque",
            Cui = "2664241",
            Ubicacion = "Servicio de movilidad urbana integral",
            Zona = "El Porvenir",
            Categoria = "Movilidad Urbana",
            Contratista = "Obras por Impuestos",
            FechaInicio = new DateTime(2026, 7, 1),
            FechaFin = new DateTime(2026, 12, 1),
            Presupuesto = 3120000.00m,
            AvanceFisico = 0.00m,
            Estado = ObraEstados.Programada,
            Plazo = "Julio / Agosto 2026 al 01/12/2026",
            Fuente = fuenteSimulada
        },
        new Obra
        {
            Nombre = "Avenida Costa Rica",
            Cui = "2721460",
            Ubicacion = "Desde Av. 28 de Julio hasta la Av. Moche",
            Zona = "La Esperanza",
            Categoria = "Pavimentacion",
            Contratista = "Consorcio Costa Norte",
            FechaInicio = new DateTime(2026, 7, 1),
            FechaFin = new DateTime(2026, 11, 30),
            Presupuesto = 6780000.00m,
            AvanceFisico = 45.00m,
            Estado = ObraEstados.Paralizada,
            MotivoCambioEstado = "Interrupcion por indisponibilidad de material de base granular",
            Plazo = "Del 01/07/2026. Octubre / Noviembre 2026",
            Fuente = fuenteSimulada
        },
        new Obra
        {
            Nombre = "Pasaje San Agustin y Pasaje Armas",
            Cui = "2721620",
            Ubicacion = "Reparacion de calzadas, veredas y senalizacion",
            Zona = "Centro Historico",
            Categoria = "Pavimentacion",
            Contratista = "Consorcio del Centro",
            FechaInicio = new DateTime(2026, 1, 1),
            FechaFin = new DateTime(2026, 12, 31),
            Presupuesto = 1950000.00m,
            AvanceFisico = 100.00m,
            Estado = ObraEstados.Concluida,
            Plazo = "Primer Semestre 2026 al Segundo Semestre 2026",
            Fuente = fuenteSimulada
        },
        new Obra
        {
            Nombre = "Avenida Victor Larco",
            Cui = "2671991",
            Ubicacion = "Desde Av. Espana hasta Av. Los Paujiles",
            Zona = "Trujillo",
            Categoria = "Pavimentacion",
            Contratista = "No informado en la fuente oficial",
            FechaInicio = new DateTime(2026, 3, 1),
            FechaFin = new DateTime(2026, 9, 30),
            Presupuesto = 5800000.00m,
            AvanceFisico = 96.00m,
            Estado = ObraEstados.EnEjecucion,
            Plazo = "Del 01/03/2026. Agosto / Setiembre 2026",
            Fuente = fuenteBase + fuenteMef + " PIM S/ 5.8 M; devengado S/ 5.6 M"
        },
        new Obra
        {
            Nombre = "Avenida America Sur",
            Cui = "2687102",
            Ubicacion = "Desde Ovalo Larco hasta Prolongacion Cesar Vallejo",
            Zona = "Trujillo",
            Categoria = "Pavimentacion",
            Contratista = "Consorcio Vial Trujillo",
            FechaInicio = new DateTime(2026, 7, 1),
            FechaFin = new DateTime(2026, 11, 1),
            Presupuesto = 7450000.00m,
            AvanceFisico = 38.00m,
            Estado = ObraEstados.EnEjecucion,
            Plazo = "Del 01/07/2026 al 01/11/2026",
            Fuente = fuenteSimulada
        },
        new Obra
        {
            Nombre = "Jiron San Martin / Jiron Independencia",
            Cui = "2687057",
            Ubicacion = "Todo el trayecto del Jiron",
            Zona = "Centro Historico",
            Categoria = "Pavimentacion",
            Contratista = "Consorcio del Centro",
            FechaInicio = new DateTime(2026, 7, 1),
            FechaFin = new DateTime(2026, 10, 1),
            Presupuesto = 2260000.00m,
            AvanceFisico = 0.00m,
            Estado = ObraEstados.Programada,
            Plazo = "Del 01/07/2026 al 01/10/2026",
            Fuente = fuenteSimulada
        },
        new Obra
        {
            Nombre = "Urb. La Merced (Vias)",
            Cui = "2698144",
            Ubicacion = "Calles internas y conectores principales",
            Zona = "La Merced",
            Categoria = "Pavimentacion",
            Contratista = "Inversion privada",
            FechaInicio = new DateTime(2026, 4, 1),
            FechaFin = new DateTime(2026, 9, 1),
            Presupuesto = 3480000.00m,
            AvanceFisico = 100.00m,
            Estado = ObraEstados.Concluida,
            Plazo = "Del 01/04/2026 al 01/09/2026",
            Fuente = fuenteSimulada
        },
        new Obra
        {
            Nombre = "Avenida Peru",
            Cui = "2710036",
            Ubicacion = "Intersecciones con Av. Villarreal, Av. America Norte y Av. Espana",
            Zona = "Trujillo",
            Categoria = "Pavimentacion",
            Contratista = "Consorcio Pavimentos Norte",
            FechaInicio = new DateTime(2026, 4, 1),
            FechaFin = new DateTime(2026, 9, 1),
            Presupuesto = 2477454.00m,
            AvanceFisico = 50.00m,
            Estado = ObraEstados.ConSobrecosto,
            MotivoCambioEstado = "Sobrecosto del 18 por ciento por mayores metrados de carpeta de rodadura",
            Plazo = "Del 01/04/2026 al 01/09/2026",
            Fuente = fuenteSimulada
        }
    };

    db.Obras.AddRange(obras);
    db.SaveChanges();
    Console.WriteLine($"{obras.Count} obras cargadas desde ObrasTrujillo.xlsx (2 con cifras oficiales del MEF).");
}

static void SeedIncidencias(ApplicationDbContext db)
{
    if (db.Incidencias.Any())
    {
        Console.WriteLine("Las incidencias ya existen en la base de datos. Omitiendo carga.");
        return;
    }

    var porCui = db.Obras.ToDictionary(o => o.Cui, o => o);
    var villyarreal = porCui["2671169"];
    var americaSur = porCui["2687102"];
    var larco = porCui["2671991"];
    var liverpool = porCui["2721620"];

    var incidencias = new List<Incidencia>
    {
        new Incidencia
        {
            ObraId = liverpool.Id,
            CodigoSeguimiento = GenerarCodigoSeguimiento(1),
            Descripcion = "La obra aun no empieza pese a que el municipio la anuncio como programada. En el tramo intervenido no se observa ninguna maquinaria ni personal laborando, y el TRANSITO por la calle Liverpool se mantiene igual de deteriorado.",
            FechaRegistro = new DateTime(2026, 9, 30, 8, 12, 0),
            Estado = "Pendiente de revision",
            ResultadoRevision = ""
        },
        new Incidencia
        {
            ObraId = americaSur.Id,
            CodigoSeguimiento = GenerarCodigoSeguimiento(2),
            Descripcion = "La obra de la Av. America Sur aun no se inicia pese a la fecha prevista en el cronograma de obra. Entre el Ovalo Larco y la Av. Cesar Vallejo se ven tramos sin carpeta de rodadura nueva mientras la maquina trabaja en el sector opuesto.",
            FechaRegistro = new DateTime(2026, 9, 28, 17, 45, 0),
            Estado = "En revision",
            ResultadoRevision = ""
        },
        new Incidencia
        {
            ObraId = larco.Id,
            CodigoSeguimiento = GenerarCodigoSeguimiento(3),
            Descripcion = "Se requiere el cronograma actualizado y el acta de entrega de la Av. Larco. El portal indica 96 por ciento de avance pero el tramo entre la Av. Espana y la Av. Los Paujiles sigue sin senalizacion horizontal definitiva.",
            FechaRegistro = new DateTime(2026, 9, 27, 10, 5, 0),
            Estado = "Informacion solicitada",
            ResultadoRevision = ""
        },
        new Incidencia
        {
            ObraId = villyarreal.Id,
            CodigoSeguimiento = GenerarCodigoSeguimiento(4),
            Descripcion = "La Av. Federico Villarreal presenta baches profundos en la intersection con la Av. America Norte pese a que la obra registra 96.9 por ciento de avance. Adjunto fotografias del estado del pavimento tomada el dia de hoy.",
            FechaRegistro = new DateTime(2026, 9, 26, 9, 30, 0),
            Estado = "En verificacion",
            ResultadoRevision = ""
        },
        new Incidencia
        {
            ObraId = americaSur.Id,
            CodigoSeguimiento = GenerarCodigoSeguimiento(5),
            Descripcion = "El sardinel recien construido en la Av. America Sur se encuentra fracturado en el tramo frente al Ovalo Larco. El material usado no parece corresponder al presupuesto reportado para la obra.",
            FechaRegistro = new DateTime(2026, 9, 22, 11, 20, 0),
            Estado = "Resuelta",
            ResultadoRevision = "Tras la verificacion en campo y el contraste con el reporte del contratista, se confirma que el sardinel fue reemplazado por garanta contractual el 25/09/2026 segun acta N 2026-089. La subsanacion queda documentada y el caso se cierra."
        },
        new Incidencia
        {
            ObraId = larco.Id,
            CodigoSeguimiento = GenerarCodigoSeguimiento(6),
            Descripcion = "Se observa maquinaria de la Av. Larco trabajando tambien en obras privadas sin cobertura de la unidad de supervision municipal. El personal no porta credencial visible.",
            FechaRegistro = new DateTime(2026, 9, 15, 15, 40, 0),
            Estado = "Derivada",
            ResultadoRevision = "La revision documental revela un sobrecosto del 30 por ciento sobre el valor de mercado y posible conflicto de intereses no declarado. Se deriva a la Contraloria General de la Republica mediante Oficio N 2026-045-CG para investigacion administrativa y fiscal."
        },
        new Incidencia
        {
            ObraId = liverpool.Id,
            CodigoSeguimiento = GenerarCodigoSeguimiento(7),
            Descripcion = "El transito de vehiculos pesados aumento de forma notable desde el inicio de los trabajos en los jirones San Martin e Independencia. Los vecinos solicitan señalizacion provisoria mientras dura la intervencion.",
            FechaRegistro = new DateTime(2026, 9, 12, 8, 50, 0),
            Estado = "Pendiente de revision",
            ResultadoRevision = ""
        },
        new Incidencia
        {
            ObraId = villyarreal.Id,
            CodigoSeguimiento = GenerarCodigoSeguimiento(8),
            Descripcion = "El area verde prevista en el proyecto de la Av. Federico Villarreal no se observa en el tramo intervenido. El portal menciona ejecucion de area verde pero no hay siembra ni mantenimiento alguno.",
            FechaRegistro = new DateTime(2026, 9, 8, 19, 15, 0),
            Estado = "En revision",
            ResultadoRevision = ""
        }
    };

    db.Incidencias.AddRange(incidencias);
    db.SaveChanges();

    var porId = incidencias.ToDictionary(i => i.CodigoSeguimiento, i => i.Id);

    db.Evidencias.AddRange(new[]
    {
        new Evidencia { IncidenciaId = porId[GenerarCodigoSeguimiento(4)], RutaArchivo = "/uploads/incidencias/1/bache1.jpg", NombreArchivo = "bache_calle_principal.jpg" },
        new Evidencia { IncidenciaId = porId[GenerarCodigoSeguimiento(4)], RutaArchivo = "/uploads/incidencias/1/bache2.jpg", NombreArchivo = "bache_interseccion.jpg" },
        new Evidencia { IncidenciaId = porId[GenerarCodigoSeguimiento(5)], RutaArchivo = "/uploads/incidencias/2/obra_lenta1.jpg", NombreArchivo = "sardinel_fracturado.jpg" },
        new Evidencia { IncidenciaId = porId[GenerarCodigoSeguimiento(5)], RutaArchivo = "/uploads/incidencias/2/obra_lenta2.jpg", NombreArchivo = "sardinel_detalle.jpg" },
        new Evidencia { IncidenciaId = porId[GenerarCodigoSeguimiento(6)], RutaArchivo = "/uploads/incidencias/3/material_defectuoso1.jpeg", NombreArchivo = "material_no_correspondiente.jpg" },
        new Evidencia { IncidenciaId = porId[GenerarCodigoSeguimiento(6)], RutaArchivo = "/uploads/incidencias/3/material_defectuoso2.jpg", NombreArchivo = "detalle_material.jpg" },
        new Evidencia { IncidenciaId = porId[GenerarCodigoSeguimiento(6)], RutaArchivo = "/uploads/incidencias/3/material_defectuoso3.jpg", NombreArchivo = "maquinaria_sin_supervision.jpg" },
        new Evidencia { IncidenciaId = porId[GenerarCodigoSeguimiento(6)], RutaArchivo = "/uploads/incidencias/5/presupuesto_inflado.pdf", NombreArchivo = "presupuesto_inflado.pdf" },
        new Evidencia { IncidenciaId = porId[GenerarCodigoSeguimiento(6)], RutaArchivo = "/uploads/incidencias/5/contrato_contratista.pdf", NombreArchivo = "contrato_contratista.pdf" },
        new Evidencia { IncidenciaId = porId[GenerarCodigoSeguimiento(1)], RutaArchivo = "/uploads/incidencias/4/senalizacion_borrada.jpg", NombreArchivo = "senalizacion_provisoria.jpg" },
        new Evidencia { IncidenciaId = porId[GenerarCodigoSeguimiento(2)], RutaArchivo = "/uploads/incidencias/6/5f4bf245-77ee-4283-830d-42c69ca6964d.jpg", NombreArchivo = "tramo_sin_pavimento.jpg" }
    });

    db.ObservacionesIncidencia.AddRange(new[]
    {
        new ObservacionIncidencia { IncidenciaId = porId[GenerarCodigoSeguimiento(4)], Texto = "[Cambio de estado a 'En revision']\nSe procede a validar la informacion brindada por el ciudadano", Autor = "supervisor", Fecha = new DateTime(2026, 9, 26, 9, 35, 0) },
        new ObservacionIncidencia { IncidenciaId = porId[GenerarCodigoSeguimiento(4)], Texto = "[Cambio de estado a 'Informacion solicitada']\nSe solicita informacion del contratista de la obra", Autor = "supervisor", Fecha = new DateTime(2026, 9, 26, 9, 40, 0) },
        new ObservacionIncidencia { IncidenciaId = porId[GenerarCodigoSeguimiento(4)], Texto = "Se comparte la informacion solicitada por el supervisor para que lo valide", Autor = "personal.municipal", Fecha = new DateTime(2026, 9, 26, 12, 10, 0) },
        new ObservacionIncidencia { IncidenciaId = porId[GenerarCodigoSeguimiento(4)], Texto = "[Cambio de estado a 'En verificacion']\nSe verifica la informacion brindada por el personal de la municipalidad", Autor = "supervisor", Fecha = new DateTime(2026, 9, 27, 8, 55, 0) },

        new ObservacionIncidencia { IncidenciaId = porId[GenerarCodigoSeguimiento(5)], Texto = "[Cambio de estado a 'En revision']\nSe revisa la evidencia fotografica remitida por el ciudadano", Autor = "supervisor", Fecha = new DateTime(2026, 9, 22, 12, 0, 0) },
        new ObservacionIncidencia { IncidenciaId = porId[GenerarCodigoSeguimiento(5)], Texto = "[Cambio de estado a 'Resuelta']\nTras verificar en campo y revisar el acta de garantia, se confirma la subsanacion del sardinel", Autor = "supervisor", Fecha = new DateTime(2026, 9, 28, 16, 30, 0) },

        new ObservacionIncidencia { IncidenciaId = porId[GenerarCodigoSeguimiento(6)], Texto = "[Cambio de estado a 'En revision']\nCaso de alta prioridad. Se inicia verificacion documental exhaustiva.", Autor = "supervisor", Fecha = new DateTime(2026, 9, 15, 17, 10, 0) },
        new ObservacionIncidencia { IncidenciaId = porId[GenerarCodigoSeguimiento(6)], Texto = "[Cambio de estado a 'Derivada']\nSe deriva a la Contraloria General de la Republica por indicios de sobrecosto injustificado", Autor = "supervisor", Fecha = new DateTime(2026, 9, 18, 10, 0, 0) },

        new ObservacionIncidencia { IncidenciaId = porId[GenerarCodigoSeguimiento(3)], Texto = "[Cambio de estado a 'Informacion solicitada']\nSe requiere cronograma actualizado y acta de entrega de la obra", Autor = "supervisor", Fecha = new DateTime(2026, 9, 27, 10, 20, 0) },

        new ObservacionIncidencia { IncidenciaId = porId[GenerarCodigoSeguimiento(2)], Texto = "[Cambio de estado a 'En revision']\nSe verifica el contraste entre el avance publicado y el avance en campo", Autor = "supervisor", Fecha = new DateTime(2026, 9, 28, 18, 0, 0) },

        new ObservacionIncidencia { IncidenciaId = porId[GenerarCodigoSeguimiento(8)], Texto = "[Cambio de estado a 'En revision']\nSe solicita el soporte documental del componente de areas verdes", Autor = "supervisor", Fecha = new DateTime(2026, 9, 9, 9, 0, 0) }
    });

    db.SaveChanges();
    Console.WriteLine($"{incidencias.Count} incidencias, {db.Evidencias.Count()} evidencias y {db.ObservacionesIncidencia.Count()} observaciones cargadas.");
}

static string GenerarCodigoSeguimiento(int numero)
{
    return $"INC-2026-{819600 + numero}";
}