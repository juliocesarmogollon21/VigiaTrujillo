using Microsoft.EntityFrameworkCore;
using VigiaTrujillo.Models;
using VigiaTrujillo.Utils;

namespace VigiaTrujillo.Data;

// Carga los datos iniciales cuando la base de datos está vacía.
// Las fotos y documentos iniciales están en Data/SeedArchivos y se copian a wwwroot/uploads,
// así que después de borrar la base de datos todo vuelve a quedar igual (usuarios, obras,
// incidencias con sus evidencias, observaciones y solicitudes de información).
public static class DbInitializer
{
    private const string SupervisorUsuario = "supervisor";
    private const string MunicipalUsuario = "personal.municipal";

    public static void Inicializar(ApplicationDbContext db, string contentRoot, string webRoot)
    {
        db.Database.Migrate();

        var archivos = new ArchivosSeed(contentRoot, webRoot);

        SeedUsuarios(db);
        SeedObras(db, archivos);
        SeedIncidencias(db, archivos);
        CargarSolicitudesExistentes(db);
    }

    // ---------------------------------------------------------------------
    // Usuarios
    // ---------------------------------------------------------------------
    // Solo se crean cuentas para el personal de la municipalidad. El ciudadano no tiene cuenta:
    // reporta y consulta sus incidencias de forma anónima desde el portal público.
    private static void SeedUsuarios(ApplicationDbContext db)
    {
        var seeds = new (string User, string Pass, string Rol)[]
        {
            (MunicipalUsuario, "Municipal2026", "PersonalMunicipal"),
            (SupervisorUsuario, "Supervisor2026", "Supervisor"),
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
        Console.WriteLine($"Usuarios iniciales listos. ({creados} nuevos)");
    }

    // ---------------------------------------------------------------------
    // Obras y sus archivos
    // ---------------------------------------------------------------------
    private static void SeedObras(ApplicationDbContext db, ArchivosSeed archivos)
    {
        if (db.Obras.Any())
        {
            Console.WriteLine("Las obras ya existen en la base de datos. Omitiendo carga.");
            return;
        }

        const string fuenteGop = "Gerencia de Obras Públicas - MDT, informe de avance al 30/09/2026.";
        const string fuenteMef = "MEF - Consulta Amigable, corte 01/10/2026.";

        var obras = new List<Obra>
        {
            new Obra
            {
                Nombre = "Avenida Manuel Vera Enríquez",
                Cui = "2687013",
                Ubicacion = "Desde Av. España hasta Av. Teodoro Valcárcel",
                Zona = "Trujillo",
                Categoria = "Pavimentación",
                Contratista = "Consorcio Vial Vera Enríquez",
                FechaInicio = new DateTime(2026, 7, 1),
                FechaFin = new DateTime(2026, 10, 1),
                Presupuesto = 4850000.00m,
                AvanceFisico = 72.50m,
                Estado = ObraEstados.EnEjecucion,
                Plazo = "Del 01/07/2026 al 01/10/2026",
                Fuente = fuenteGop
            },
            new Obra
            {
                Nombre = "Avenida Federico Villarreal",
                Cui = "2671169",
                Ubicacion = "Desde Av. América Norte hasta Av. César Vallejo",
                Zona = "Trujillo",
                Categoria = "Pavimentación",
                Contratista = "Consorcio Villarreal Norte",
                FechaInicio = new DateTime(2025, 8, 1),
                FechaFin = new DateTime(2026, 3, 31),
                Presupuesto = 9000000.00m,
                AvanceFisico = 96.90m,
                Estado = ObraEstados.EnEjecucion,
                Plazo = "Del 01/08/2025 al 31/03/2026 (ampliación en trámite)",
                Fuente = fuenteMef + " PIM S/ 9.0 M; devengado S/ 8.8 M"
            },
            new Obra
            {
                Nombre = "Urb. Ingeniería I",
                Cui = "2709817",
                Ubicacion = "Calles internas de la Urb. Ingeniería I y conexión con la Av. Húsares de Junín",
                Zona = "Trujillo",
                Categoria = "Movilidad urbana",
                Contratista = "Constructora Valle Santa Catalina S.A.C.",
                FechaInicio = new DateTime(2026, 5, 1),
                FechaFin = new DateTime(2026, 12, 31),
                Presupuesto = 2340000.00m,
                AvanceFisico = 15.00m,
                Estado = ObraEstados.Paralizada,
                MotivoCambioEstado = "Paralizada desde el 20/09/2026: en la excavación aparecieron tuberías de agua que no estaban en el expediente. Se espera que SEDALIB las reubique.",
                Plazo = "Del 01/05/2026 al 31/12/2026",
                Fuente = fuenteGop
            },
            new Obra
            {
                Nombre = "Pueblo Joven El Bosque",
                Cui = "2664241",
                Ubicacion = "Calles principales del Pueblo Joven El Bosque",
                Zona = "El Bosque",
                Categoria = "Movilidad urbana",
                Contratista = "Inversiones y Construcciones Rinconada E.I.R.L.",
                FechaInicio = new DateTime(2026, 7, 1),
                FechaFin = new DateTime(2026, 12, 1),
                Presupuesto = 3120000.00m,
                AvanceFisico = 0.00m,
                Estado = ObraEstados.Programada,
                Plazo = "Del 01/07/2026 al 01/12/2026",
                Fuente = fuenteGop
            },
            new Obra
            {
                Nombre = "Avenida Costa Rica",
                Cui = "2721460",
                Ubicacion = "Desde Av. 28 de Julio hasta la Av. Moche",
                Zona = "La Esperanza",
                Categoria = "Pavimentación",
                Contratista = "Consorcio Costa Rica Trujillo",
                FechaInicio = new DateTime(2026, 7, 1),
                FechaFin = new DateTime(2026, 11, 30),
                Presupuesto = 6780000.00m,
                AvanceFisico = 45.00m,
                Estado = ObraEstados.Paralizada,
                MotivoCambioEstado = "Paralizada desde el 12/09/2026 por falta de material de base granular de la cantera autorizada.",
                Plazo = "Del 01/07/2026 al 30/11/2026",
                Fuente = fuenteGop
            },
            new Obra
            {
                Nombre = "Pasaje San Agustín y Pasaje Armas",
                Cui = "2721620",
                Ubicacion = "Reparación de calzadas, veredas y señalización",
                Zona = "Centro Histórico",
                Categoria = "Pavimentación",
                Contratista = "Consorcio Centro Histórico San Agustín",
                FechaInicio = new DateTime(2026, 1, 1),
                FechaFin = new DateTime(2026, 8, 15),
                Presupuesto = 1950000.00m,
                AvanceFisico = 100.00m,
                Estado = ObraEstados.Concluida,
                Plazo = "Del 01/01/2026 al 15/08/2026",
                Fuente = fuenteGop
            },
            new Obra
            {
                Nombre = "Avenida Víctor Larco",
                Cui = "2671991",
                Ubicacion = "Desde Av. España hasta Av. Los Paujiles",
                Zona = "Trujillo",
                Categoria = "Pavimentación",
                Contratista = "Sánchez Arroyo Ingenieros Contratistas E.I.R.L.",
                FechaInicio = new DateTime(2026, 3, 1),
                FechaFin = new DateTime(2026, 9, 30),
                Presupuesto = 5800000.00m,
                AvanceFisico = 96.00m,
                Estado = ObraEstados.EnEjecucion,
                Plazo = "Del 01/03/2026 al 30/09/2026",
                Fuente = fuenteMef + " PIM S/ 5.8 M; devengado S/ 5.6 M"
            },
            new Obra
            {
                Nombre = "Avenida América Sur",
                Cui = "2687102",
                Ubicacion = "Desde Óvalo Larco hasta Prolongación César Vallejo",
                Zona = "Trujillo",
                Categoria = "Pavimentación",
                Contratista = "Consorcio Vial América Sur",
                FechaInicio = new DateTime(2026, 7, 1),
                FechaFin = new DateTime(2026, 11, 1),
                Presupuesto = 7450000.00m,
                AvanceFisico = 38.00m,
                Estado = ObraEstados.EnEjecucion,
                MotivoRetroceso = "Se bajó el avance de 42 % a 38 % porque la supervisión observó un tramo de carpeta asfáltica que el contratista debe reponer.",
                Plazo = "Del 01/07/2026 al 01/11/2026",
                Fuente = fuenteGop
            },
            new Obra
            {
                Nombre = "Jirón San Martín / Jirón Independencia",
                Cui = "2687057",
                Ubicacion = "Todo el trayecto de ambos jirones",
                Zona = "Centro Histórico",
                Categoria = "Pavimentación",
                Contratista = "Constructora Mochica Pavimentos S.A.C.",
                FechaInicio = new DateTime(2026, 9, 1),
                FechaFin = new DateTime(2026, 12, 15),
                Presupuesto = 2260000.00m,
                AvanceFisico = 22.00m,
                Estado = ObraEstados.EnEjecucion,
                MotivoCambioEstado = "Inicio de obra el 01/09/2026 según acta de entrega de terreno.",
                Plazo = "Del 01/09/2026 al 15/12/2026",
                Fuente = fuenteGop
            },
            new Obra
            {
                Nombre = "Urb. La Merced (Vías)",
                Cui = "2698144",
                Ubicacion = "Calles internas y conectores principales",
                Zona = "La Merced",
                Categoria = "Pavimentación",
                Contratista = "Edificaciones y Pavimentos La Merced S.A.C.",
                FechaInicio = new DateTime(2026, 4, 1),
                FechaFin = new DateTime(2026, 9, 1),
                Presupuesto = 3480000.00m,
                AvanceFisico = 100.00m,
                Estado = ObraEstados.Concluida,
                Plazo = "Del 01/04/2026 al 01/09/2026",
                Fuente = fuenteGop
            },
            new Obra
            {
                Nombre = "Avenida Perú",
                Cui = "2710036",
                Ubicacion = "Intersecciones con Av. Villarreal, Av. América Norte y Av. España",
                Zona = "Trujillo",
                Categoria = "Pavimentación",
                Contratista = "Consorcio Pavimentos Avenida Perú",
                FechaInicio = new DateTime(2026, 4, 1),
                FechaFin = new DateTime(2026, 9, 1),
                Presupuesto = 2477454.00m,
                AvanceFisico = 50.00m,
                Estado = ObraEstados.ConSobrecosto,
                MotivoCambioEstado = "Sobrecosto de 18 % por mayores metrados de adoquinado y base granular en dos intersecciones.",
                Plazo = "Del 01/04/2026 al 01/09/2026",
                Fuente = fuenteGop
            },
            // Obra dada de baja (baja lógica) para que la pantalla "Dados de baja" tenga datos.
            new Obra
            {
                Nombre = "Losa deportiva Urb. Santa María",
                Cui = "2698870",
                Ubicacion = "Parque principal de la Urb. Santa María",
                Zona = "Trujillo",
                Categoria = "Recreación",
                Contratista = "Deportes y Obras Santa María E.I.R.L.",
                FechaInicio = new DateTime(2026, 8, 1),
                FechaFin = new DateTime(2026, 11, 30),
                Presupuesto = 380000.00m,
                AvanceFisico = 0.00m,
                Estado = ObraEstados.Programada,
                Plazo = "Del 01/08/2026 al 30/11/2026",
                Fuente = fuenteGop,
                Activo = false,
                FechaBaja = new DateTime(2026, 9, 15),
                MotivoBaja = "Registro duplicado: la losa deportiva se ejecutará dentro del proyecto de la Urb. Ingeniería I.",
                UsuarioBaja = MunicipalUsuario
            }
        };

        db.Obras.AddRange(obras);
        db.SaveChanges();

        // Fotos y documentos de cada obra (se ven en el detalle de la obra y en la vista pública).
        var porCui = obras.ToDictionary(o => o.Cui);
        var archivosObra = new (string Cui, string Archivo, string Nombre, string Tipo, DateTime Fecha)[]
        {
            ("2687013", "cronograma_obra_vera_enriquez.pdf", "Cronograma de ejecución.pdf", "documento", new DateTime(2026, 7, 2, 9, 0, 0)),
            ("2687013", "via_sin_asfaltar.jpg", "Avance de base granular.jpg", "foto", new DateTime(2026, 9, 20, 16, 0, 0)),
            ("2671169", "acta_inicio_obra_villarreal.pdf", "Acta de inicio de obra.pdf", "documento", new DateTime(2025, 8, 2, 10, 0, 0)),
            ("2671169", "asfaltado_tramo.jpg", "Colocación de carpeta asfáltica.jpg", "foto", new DateTime(2026, 2, 10, 11, 0, 0)),
            ("2709817", "acta_paralizacion_ingenieria.pdf", "Acta de paralización.pdf", "documento", new DateTime(2026, 9, 20, 15, 0, 0)),
            ("2709817", "tramo_sin_carpeta.jpg", "Estado de la vía al paralizar.jpg", "foto", new DateTime(2026, 9, 20, 15, 5, 0)),
            ("2721460", "acta_paralizacion_costa_rica.pdf", "Acta de paralización.pdf", "documento", new DateTime(2026, 9, 12, 12, 0, 0)),
            ("2721460", "excavacion_buzon.jpg", "Buzones y zanjas abiertas.jpg", "foto", new DateTime(2026, 9, 12, 12, 10, 0)),
            ("2721620", "acta_recepcion_pasaje_san_agustin.pdf", "Acta de recepción de obra.pdf", "documento", new DateTime(2026, 8, 15, 10, 0, 0)),
            ("2721620", "colocacion_adoquines.jpg", "Colocación de adoquines.jpg", "foto", new DateTime(2026, 6, 5, 9, 30, 0)),
            ("2671991", "vista_aerea_tramo.jpg", "Vista aérea del tramo.jpg", "foto", new DateTime(2026, 9, 25, 8, 0, 0)),
            ("2687102", "informe_avance_america_sur.pdf", "Informe de avance - setiembre.pdf", "documento", new DateTime(2026, 9, 30, 18, 0, 0)),
            ("2687102", "encofrado_sardinel.jpg", "Encofrado de sardineles.jpg", "foto", new DateTime(2026, 9, 18, 10, 0, 0)),
            ("2687057", "acopio_adoquines.jpg", "Acopio de adoquines en el jirón.jpg", "foto", new DateTime(2026, 9, 10, 9, 0, 0)),
            ("2698144", "acta_recepcion_la_merced.pdf", "Acta de recepción de obra.pdf", "documento", new DateTime(2026, 9, 5, 11, 0, 0)),
            ("2710036", "informe_sobrecosto_av_peru.pdf", "Informe de mayores metrados.pdf", "documento", new DateTime(2026, 9, 28, 17, 0, 0)),
            ("2710036", "acopio_adoquines.jpg", "Reemplazo de adoquines.jpg", "foto", new DateTime(2026, 9, 28, 17, 5, 0))
        };

        foreach (var a in archivosObra)
        {
            var obra = porCui[a.Cui];
            db.ObraArchivos.Add(new ObraArchivo
            {
                ObraId = obra.Id,
                Ruta = archivos.Copiar(a.Archivo, "obras", obra.Id.ToString()),
                Nombre = a.Nombre,
                Tipo = a.Tipo,
                FechaCarga = a.Fecha
            });
        }
        db.SaveChanges();

        Console.WriteLine($"{obras.Count} obras y {archivosObra.Length} archivos de obra cargados.");
    }

    // ---------------------------------------------------------------------
    // Incidencias, evidencias, observaciones y solicitudes de información
    // ---------------------------------------------------------------------
    private static void SeedIncidencias(ApplicationDbContext db, ArchivosSeed archivos)
    {
        if (db.Incidencias.Any())
        {
            Console.WriteLine("Las incidencias ya existen en la base de datos. Omitiendo carga.");
            return;
        }

        var obra = db.Obras.ToDictionary(o => o.Cui, o => o.Id);
        var veraEnriquez = obra["2687013"];
        var villarreal = obra["2671169"];
        var ingenieria = obra["2709817"];
        var elBosque = obra["2664241"];
        var costaRica = obra["2721460"];
        var pasajeSanAgustin = obra["2721620"];
        var larco = obra["2671991"];
        var americaSur = obra["2687102"];
        var sanMartin = obra["2687057"];
        var peru = obra["2710036"];

        var incidencias = new List<Incidencia>
        {
            // 1. Pendiente de revisión
            Nueva(1, veraEnriquez, new DateTime(2026, 10, 6, 19, 20, 0), IncidenciaEstados.PendienteDeRevision,
                "Frente al mercado de la Av. Manuel Vera Enríquez quedó abierta una zanja de casi dos metros de profundidad. No tiene cinta de seguridad ni conos y en la noche no se ve. Ayer un mototaxi estuvo a punto de caer. Pido que se señalice cuanto antes."),
            // 2. Pendiente de revisión
            Nueva(2, sanMartin, new DateTime(2026, 10, 5, 8, 40, 0), IncidenciaEstados.PendienteDeRevision,
                "Desde que empezaron los trabajos en el Jr. San Martín los camiones y volquetes pasan por el Jr. Independencia sin ningún control. No hay señales de desvío ni personal que ordene el tránsito y las veredas se llenan de polvo. Los vecinos pedimos un plan de desvío visible."),
            // 3. Pendiente de revisión (obra paralizada)
            Nueva(3, ingenieria, new DateTime(2026, 10, 7, 11, 5, 0), IncidenciaEstados.PendienteDeRevision,
                "La obra de la Urb. Ingeniería I está detenida desde hace casi un mes. Dejaron las calles levantadas, con montículos de tierra y sin cerco, y los niños del colegio cruzan por encima del desmonte. Queremos saber cuándo se retoman los trabajos."),
            // 4. Información solicitada: el municipio ya respondió y el supervisor todavía no ha visto la respuesta («Respuesta nueva»)
            Nueva(4, villarreal, new DateTime(2026, 9, 26, 9, 30, 0), IncidenciaEstados.InformacionSolicitada,
                "En la Av. Federico Villarreal, en el cruce con la Av. América Norte, hay baches profundos en el pavimento nuevo. El portal de la municipalidad indica que la obra tiene 96.9 % de avance, pero ese tramo ya se está rompiendo. Adjunto fotos tomadas hoy en la mañana."),
            // 5. Información solicitada (pendiente de respuesta)
            Nueva(5, larco, new DateTime(2026, 9, 27, 10, 5, 0), IncidenciaEstados.InformacionSolicitada,
                "En la Av. Víctor Larco, entre la Av. España y la Av. Los Paujiles, todavía hay sectores sin carpeta asfáltica y no se ha pintado la señalización en el piso. Según el portal la obra va en 96 %, pero lo que se ve en campo no coincide con ese avance."),
            // 6. Información solicitada (pendiente de respuesta)
            Nueva(6, costaRica, new DateTime(2026, 10, 2, 18, 10, 0), IncidenciaEstados.InformacionSolicitada,
                "La obra de la Av. Costa Rica está paralizada, pero dejaron buzones abiertos y la malla de seguridad está caída en varios puntos. Por la noche la zona queda oscura y es peligroso para los peatones. Quisiera saber quién se hace responsable mientras la obra está detenida."),
            // 7. En verificación (el municipio respondió con un informe en PDF)
            Nueva(7, peru, new DateTime(2026, 9, 20, 12, 30, 0), IncidenciaEstados.EnVerificacion,
                "En la intersección de la Av. Perú con la Av. España están levantando adoquines que se colocaron hace pocas semanas para volver a ponerlos. Me preocupa que se esté pagando dos veces el mismo trabajo, ya que la obra figura con sobrecosto."),
            // 8. Resuelta (después de verificar la respuesta del municipio)
            Nueva(8, americaSur, new DateTime(2026, 9, 22, 11, 20, 0), IncidenciaEstados.Resuelta,
                "El sardinel recién construido en la Av. América Sur, frente al Óvalo Larco, ya está fracturado en varios tramos. Parece que el concreto no fue bien curado o que el material no es el indicado en el expediente.",
                "Se verificó en campo que el contratista reemplazó el sardinel dañado el 25/09/2026 por garantía (acta N.° 2026-089). El tramo quedó conforme y el caso se cierra."),
            // 9. Derivada (la solicitud quedó cerrada sin respuesta)
            Nueva(9, larco, new DateTime(2026, 9, 14, 15, 40, 0), IncidenciaEstados.Derivada,
                "La retroexcavadora de la obra de la Av. Víctor Larco sale del tramo en las mañanas para trabajar en un terreno privado frente al parque de la cuadra 12. El operador no lleva credencial y no hay nadie de la supervisión municipal. Adjunto mis apuntes con las horas.",
                "La revisión documental encontró que el parte diario no registra la salida de la maquinaria y hay indicios de uso indebido de bienes de la obra. Se deriva a la Contraloría General de la República mediante Oficio N.° 2026-045-MDT/GOP."),
            // 10. Resuelta (el supervisor verificó en campo sin pedir información; sin evidencias adjuntas)
            Nueva(10, pasajeSanAgustin, new DateTime(2026, 9, 2, 17, 15, 0), IncidenciaEstados.Resuelta,
                "En el Pasaje San Agustín, cerca de la Plaza de Armas, algunas losas de la vereda nueva quedaron desniveladas y una señora adulta mayor se tropezó. Pido que revisen el acabado antes de que ocurra un accidente más grave.",
                "El contratista niveló las losas observadas el 09/09/2026 dentro del periodo de garantía. Se verificó en campo y la vereda quedó conforme."),
            // 11. En revisión (sin solicitudes)
            Nueva(11, villarreal, new DateTime(2026, 9, 8, 19, 15, 0), IncidenciaEstados.EnRevision,
                "En el proyecto de la Av. Federico Villarreal se anunció un área verde en la berma central, pero en el tramo terminado solo hay tierra suelta. No se ve siembra de grass ni riego, y la gente ya la usa para dejar basura."),
            // 12. Pendiente de revisión (obra programada que no empieza)
            Nueva(12, elBosque, new DateTime(2026, 10, 8, 7, 50, 0), IncidenciaEstados.PendienteDeRevision,
                "La obra de pistas en el Pueblo Joven El Bosque figura como programada desde julio, pero hasta ahora no hay cartel de obra ni trabajos. Queremos saber la fecha real de inicio, porque las calles están en muy mal estado.")
        };

        db.Incidencias.AddRange(incidencias);
        db.SaveChanges();

        var inc = incidencias.ToDictionary(i => i.CodigoSeguimiento, i => i.Id);
        int Id(int numero) => inc[Codigo(numero)];
        ObservacionIncidencia Obs(int numero, string autor, DateTime fecha, string texto) => new()
        {
            IncidenciaId = Id(numero),
            Autor = autor,
            Fecha = fecha,
            Texto = texto
        };

        // Evidencias del ciudadano (fotos y un PDF), copiadas a wwwroot/uploads/incidencias/{id}
        var evidencias = new (int Inc, string Archivo, string Nombre)[]
        {
            (1, "zanja_sin_senalizar.jpg", "zanja_frente_mercado.jpg"),
            (2, "asfaltado_tramo.jpg", "transito_pesado_jr_independencia.jpg"),
            (3, "tramo_sin_carpeta.jpg", "calles_levantadas.jpg"),
            (4, "bache_pavimento.jpg", "bache_cruce_america_norte.jpg"),
            (4, "via_deteriorada.jpg", "pavimento_deteriorado.jpg"),
            (5, "vista_aerea_tramo.jpg", "tramo_sin_carpeta_larco.jpg"),
            (5, "via_sin_asfaltar.jpg", "sin_senalizacion_horizontal.jpg"),
            (6, "excavacion_buzon.jpg", "buzon_abierto_costa_rica.jpg"),
            (7, "colocacion_adoquines.jpg", "levantando_adoquines.jpg"),
            (7, "acopio_adoquines.jpg", "adoquines_retirados.jpg"),
            (8, "encofrado_sardinel.jpg", "sardinel_fracturado.jpg"),
            (9, "registro_maquinaria_larco.pdf", "apuntes_horas_maquinaria.pdf"),
            (9, "via_sin_asfaltar.jpg", "maquinaria_fuera_del_tramo.jpg")
        };

        foreach (var e in evidencias)
        {
            var incidencia = incidencias.First(i => i.Id == Id(e.Inc));
            db.Evidencias.Add(new Evidencia
            {
                IncidenciaId = incidencia.Id,
                RutaArchivo = archivos.Copiar(e.Archivo, "incidencias", incidencia.Id.ToString()),
                NombreArchivo = e.Nombre,
                FechaCarga = incidencia.FechaRegistro
            });
        }

        // Historial de cada incidencia, con el mismo formato que usa el sistema al trabajar.
        var observaciones = new List<ObservacionIncidencia>
        {
            Obs(4, SupervisorUsuario, new DateTime(2026, 9, 26, 10, 15, 0), Cambio(IncidenciaEstados.EnRevision, "Se revisan las fotos enviadas por el ciudadano y el último informe de avance de la obra.")),
            Obs(4, SupervisorUsuario, new DateTime(2026, 9, 27, 9, 0, 0), "[Solicitud de información]\n" + PreguntaVillarreal),
            Obs(4, MunicipalUsuario, new DateTime(2026, 10, 7, 16, 40, 0), RespondioMunicipal),

            Obs(5, SupervisorUsuario, new DateTime(2026, 9, 28, 8, 30, 0), Cambio(IncidenciaEstados.EnRevision, "Se compara el avance publicado en el portal con las fotos del ciudadano.")),
            Obs(5, SupervisorUsuario, new DateTime(2026, 9, 29, 10, 20, 0), "[Solicitud de información]\n" + PreguntaLarco),

            Obs(6, SupervisorUsuario, new DateTime(2026, 10, 3, 9, 10, 0), Cambio(IncidenciaEstados.EnRevision, "Se revisa el estado de la obra paralizada y las medidas de seguridad en la vía.")),
            Obs(6, SupervisorUsuario, new DateTime(2026, 10, 3, 9, 25, 0), "[Solicitud de información]\n" + PreguntaCostaRica),

            Obs(7, SupervisorUsuario, new DateTime(2026, 9, 21, 9, 0, 0), Cambio(IncidenciaEstados.EnRevision, "Se revisa el expediente de la obra y el adicional aprobado por mayores metrados.")),
            Obs(7, SupervisorUsuario, new DateTime(2026, 9, 21, 9, 30, 0), "[Solicitud de información]\n" + PreguntaPeru),
            Obs(7, MunicipalUsuario, new DateTime(2026, 9, 24, 15, 0, 0), RespondioMunicipal),
            Obs(7, SupervisorUsuario, new DateTime(2026, 9, 25, 10, 0, 0), Cambio(IncidenciaEstados.EnVerificacion, "Se programa una visita a campo para comparar los metrados del informe con lo ejecutado.")),
            Obs(7, SupervisorUsuario, new DateTime(2026, 9, 25, 10, 5, 0), "La visita a campo queda programada para la primera semana de octubre junto con el residente de obra."),

            Obs(8, SupervisorUsuario, new DateTime(2026, 9, 22, 14, 0, 0), Cambio(IncidenciaEstados.EnRevision, "Se revisa la foto del sardinel y el expediente técnico de la obra.")),
            Obs(8, SupervisorUsuario, new DateTime(2026, 9, 23, 9, 0, 0), "[Solicitud de información]\n" + PreguntaAmericaSur),
            Obs(8, MunicipalUsuario, new DateTime(2026, 9, 25, 17, 30, 0), RespondioMunicipal),
            Obs(8, SupervisorUsuario, new DateTime(2026, 9, 26, 9, 0, 0), Cambio(IncidenciaEstados.EnVerificacion, "Se programa una visita a campo para comprobar el reemplazo del sardinel.")),
            Obs(8, SupervisorUsuario, new DateTime(2026, 9, 28, 16, 30, 0), Cambio(IncidenciaEstados.Resuelta, "Se verificó en campo que el contratista reemplazó el sardinel dañado el 25/09/2026 por garantía (acta N.° 2026-089). El tramo quedó conforme y el caso se cierra.")),

            Obs(9, SupervisorUsuario, new DateTime(2026, 9, 15, 8, 30, 0), Cambio(IncidenciaEstados.EnRevision, "Caso de prioridad alta. Se inicia la revisión documental de la obra.")),
            Obs(9, SupervisorUsuario, new DateTime(2026, 9, 15, 9, 0, 0), "[Solicitud de información]\n" + PreguntaMaquinaria),
            Obs(9, SupervisorUsuario, new DateTime(2026, 9, 18, 10, 0, 0), Cambio(IncidenciaEstados.Derivada, "La revisión documental encontró que el parte diario no registra la salida de la maquinaria y hay indicios de uso indebido de bienes de la obra. Se deriva a la Contraloría General de la República mediante Oficio N.° 2026-045-MDT/GOP.")),

            Obs(10, SupervisorUsuario, new DateTime(2026, 9, 3, 9, 0, 0), Cambio(IncidenciaEstados.EnRevision, "Se revisa el acta de recepción de la obra y el periodo de garantía.")),
            Obs(10, SupervisorUsuario, new DateTime(2026, 9, 5, 11, 0, 0), "Se coordinó con el contratista la nivelación de las losas dentro del periodo de garantía."),
            Obs(10, SupervisorUsuario, new DateTime(2026, 9, 9, 16, 0, 0), Cambio(IncidenciaEstados.EnVerificacion, "El contratista informó que niveló las losas. Se programa la verificación en campo.")),
            Obs(10, SupervisorUsuario, new DateTime(2026, 9, 10, 12, 0, 0), Cambio(IncidenciaEstados.Resuelta, "El contratista niveló las losas observadas el 09/09/2026 dentro del periodo de garantía. Se verificó en campo y la vereda quedó conforme.")),

            Obs(11, SupervisorUsuario, new DateTime(2026, 9, 9, 9, 0, 0), Cambio(IncidenciaEstados.EnRevision, "Se revisa si el componente de áreas verdes está en el expediente de la obra.")),
            Obs(11, SupervisorUsuario, new DateTime(2026, 9, 9, 9, 20, 0), "El componente de áreas verdes sí figura en el expediente. Falta confirmar la fecha de siembra con la Subgerencia de Parques y Jardines.")
        };
        db.ObservacionesIncidencia.AddRange(observaciones);
        db.SaveChanges();

        // Solicitudes de información del supervisor al personal municipal.
        var solicitudes = new List<SolicitudInformacion>
        {
            // Respondida y todavía no vista por el supervisor: aparece como "Respuesta nueva".
            new SolicitudInformacion
            {
                IncidenciaId = Id(4),
                Pregunta = PreguntaVillarreal,
                SolicitadoPor = SupervisorUsuario,
                FechaSolicitud = new DateTime(2026, 9, 27, 9, 0, 0),
                Respuesta = "La supervisión de obra confirmó fallas en la carpeta asfáltica por una filtración de la red de desagüe. El contratista hizo el parchado el 06/10/2026 dentro del periodo de garantía. Adjuntamos la foto del tramo reparado.",
                RespondidoPor = MunicipalUsuario,
                FechaRespuesta = new DateTime(2026, 10, 7, 16, 40, 0),
                Estado = SolicitudInformacion.EstadoRespondida,
                RespuestaVista = false
            },
            // Pendientes: aparecen en la pantalla "Solicitudes" del personal municipal.
            new SolicitudInformacion
            {
                IncidenciaId = Id(5),
                Pregunta = PreguntaLarco,
                SolicitadoPor = SupervisorUsuario,
                FechaSolicitud = new DateTime(2026, 9, 29, 10, 20, 0),
                Estado = SolicitudInformacion.EstadoPendiente
            },
            new SolicitudInformacion
            {
                IncidenciaId = Id(6),
                Pregunta = PreguntaCostaRica,
                SolicitadoPor = SupervisorUsuario,
                FechaSolicitud = new DateTime(2026, 10, 3, 9, 25, 0),
                Estado = SolicitudInformacion.EstadoPendiente
            },
            // Respondidas y ya revisadas por el supervisor.
            new SolicitudInformacion
            {
                IncidenciaId = Id(7),
                Pregunta = PreguntaPeru,
                SolicitadoPor = SupervisorUsuario,
                FechaSolicitud = new DateTime(2026, 9, 21, 9, 30, 0),
                Respuesta = "Se adjunta el informe técnico de mayores metrados. El reemplazo de adoquines se hizo porque la base estaba saturada por una filtración del desagüe; el costo está incluido en el adicional aprobado.",
                RespondidoPor = MunicipalUsuario,
                FechaRespuesta = new DateTime(2026, 9, 24, 15, 0, 0),
                Estado = SolicitudInformacion.EstadoRespondida,
                RespuestaVista = true
            },
            new SolicitudInformacion
            {
                IncidenciaId = Id(8),
                Pregunta = PreguntaAmericaSur,
                SolicitadoPor = SupervisorUsuario,
                FechaSolicitud = new DateTime(2026, 9, 23, 9, 0, 0),
                Respuesta = "El contratista reemplazó el sardinel dañado el 25/09/2026 por garantía, según acta N.° 2026-089. Se adjunta la foto del nuevo encofrado.",
                RespondidoPor = MunicipalUsuario,
                FechaRespuesta = new DateTime(2026, 9, 25, 17, 30, 0),
                Estado = SolicitudInformacion.EstadoRespondida,
                RespuestaVista = true
            },
            // El supervisor derivó el caso sin esperar la respuesta.
            new SolicitudInformacion
            {
                IncidenciaId = Id(9),
                Pregunta = PreguntaMaquinaria,
                SolicitadoPor = SupervisorUsuario,
                FechaSolicitud = new DateTime(2026, 9, 15, 9, 0, 0),
                Estado = SolicitudInformacion.EstadoSinRespuesta
            }
        };
        db.SolicitudesInformacion.AddRange(solicitudes);
        db.SaveChanges();

        // Adjuntos de las respuestas (necesitan el Id de la solicitud para el nombre del archivo).
        AdjuntarRespuesta(archivos, solicitudes[0], "asfaltado_tramo.jpg", "parchado_villarreal.jpg");
        AdjuntarRespuesta(archivos, solicitudes[3], "informe_sobrecosto_av_peru.pdf", "informe_mayores_metrados.pdf");
        AdjuntarRespuesta(archivos, solicitudes[4], "encofrado_sardinel.jpg", "sardinel_reemplazado.jpg");
        db.SaveChanges();

        Console.WriteLine($"{incidencias.Count} incidencias, {evidencias.Length} evidencias, {observaciones.Count} observaciones y {solicitudes.Count} solicitudes de información cargadas.");
    }

    private const string PreguntaVillarreal = "Por favor, enviar el informe de la supervisión de obra sobre el estado del pavimento en el cruce con la Av. América Norte y la fecha en que se reparará.";
    private const string PreguntaLarco = "Enviar el cronograma actualizado de la obra y el último informe de valorización para comparar el avance publicado con lo que se ve en campo.";
    private const string PreguntaCostaRica = "Indicar si existe acta de paralización y qué medidas de seguridad debe mantener el contratista en la vía mientras la obra está detenida.";
    private const string PreguntaPeru = "Remitir el sustento técnico del cambio de adoquines en la intersección con la Av. España y el informe de mayores metrados aprobado.";
    private const string PreguntaAmericaSur = "Solicitar al contratista el acta de garantía y la fecha programada para el cambio del sardinel fracturado.";
    private const string PreguntaMaquinaria = "Enviar el parte diario de maquinaria de la obra de los días 12 al 14 de setiembre.";
    // La respuesta del Personal Municipal queda en el historial, pero no cambia el estado de la incidencia.
    private const string RespondioMunicipal = "[Respuesta a solicitud de información]\nEl Personal Municipal respondió la solicitud de información.";

    private static Incidencia Nueva(int numero, int obraId, DateTime fecha, string estado, string descripcion, string resultado = "") => new()
    {
        ObraId = obraId,
        CodigoSeguimiento = Codigo(numero),
        Descripcion = descripcion,
        FechaRegistro = fecha,
        Estado = estado,
        ResultadoRevision = resultado
    };

    private static string Cambio(string estado, string detalle) => $"[Cambio de estado a '{estado}']\n{detalle}";

    private static void AdjuntarRespuesta(ArchivosSeed archivos, SolicitudInformacion solicitud, string archivo, string nombre)
    {
        solicitud.ArchivoRuta = archivos.Copiar(archivo, "respuestas-solicitud", null, $"resp-{solicitud.Id}-{archivo}");
        solicitud.ArchivoNombre = nombre;
    }

    private static string Codigo(int numero) => $"INC-2026-{819600 + numero}";

    // ---------------------------------------------------------------------
    // Bases de datos antiguas: las solicitudes se guardaban solo como observaciones.
    // La primera vez se pasan a la tabla SolicitudesInformacion, emparejando cada pedido
    // del supervisor con la respuesta del personal municipal que vino después.
    // Si la tabla ya tiene datos (como pasa después de cargar los datos iniciales) no hace nada.
    // ---------------------------------------------------------------------
    private static void CargarSolicitudesExistentes(ApplicationDbContext db)
    {
        if (db.SolicitudesInformacion.Any()) return;

        var municipales = db.Usuarios
            .Where(u => u.Rol == "PersonalMunicipal")
            .Select(u => u.NombreUsuario)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var incidencias = db.Incidencias.Include(i => i.Observaciones).ToList();
        var nuevas = new List<SolicitudInformacion>();
        const string separadorArchivo = "\n\n\U0001F4CE Archivo adjunto: ";

        foreach (var inc in incidencias)
        {
            SolicitudInformacion? abierta = null;
            foreach (var obs in inc.Observaciones.OrderBy(o => o.Fecha))
            {
                var texto = obs.Texto ?? string.Empty;
                var normal = IncidenciaEstados.Normalizar(texto);
                var esPedido = normal.StartsWith("[cambio de estado a 'informacion solicitada']") || normal.StartsWith("[solicitud de informacion]");

                if (esPedido)
                {
                    if (abierta != null) abierta.Estado = SolicitudInformacion.EstadoSinRespuesta;
                    var salto = texto.IndexOf('\n');
                    var pregunta = salto >= 0 ? texto[(salto + 1)..].Trim() : texto.Trim();
                    abierta = new SolicitudInformacion
                    {
                        IncidenciaId = inc.Id,
                        Pregunta = string.IsNullOrWhiteSpace(pregunta) ? "Información adicional sobre la incidencia." : pregunta,
                        SolicitadoPor = obs.Autor,
                        FechaSolicitud = obs.Fecha,
                        Estado = SolicitudInformacion.EstadoPendiente
                    };
                    nuevas.Add(abierta);
                }
                else if (abierta != null && municipales.Contains(obs.Autor) && !texto.StartsWith("["))
                {
                    var partes = texto.Split(separadorArchivo, StringSplitOptions.None);
                    abierta.Respuesta = partes[0].Trim();
                    if (partes.Length > 1)
                    {
                        var info = partes[1].Split(" (", 2, StringSplitOptions.None);
                        abierta.ArchivoNombre = info[0].Trim();
                        abierta.ArchivoRuta = info.Length > 1 ? info[1].TrimEnd(')', ' ') : null;
                    }
                    abierta.RespondidoPor = obs.Autor;
                    abierta.FechaRespuesta = obs.Fecha;
                    abierta.Estado = SolicitudInformacion.EstadoRespondida;
                    abierta.RespuestaVista = true;
                    abierta = null;
                }
            }

            // Si la incidencia ya no espera información, el pedido que quedó abierto se cierra.
            if (abierta != null && !IncidenciaEstados.EsIgual(inc.Estado, IncidenciaEstados.InformacionSolicitada))
                abierta.Estado = SolicitudInformacion.EstadoSinRespuesta;
        }

        if (nuevas.Count == 0) return;
        db.SolicitudesInformacion.AddRange(nuevas);
        db.SaveChanges();
        Console.WriteLine($"{nuevas.Count} solicitudes de información pasadas a la nueva tabla.");
    }
}

// Copia un archivo de Data/SeedArchivos a wwwroot/uploads y devuelve la ruta web que se guarda en la BD.
public class ArchivosSeed
{
    private readonly string _origen;
    private readonly string _uploads;

    public ArchivosSeed(string contentRoot, string webRoot)
    {
        _origen = Path.Combine(contentRoot, "Data", "SeedArchivos");
        _uploads = Path.Combine(webRoot, "uploads");
    }

    public string Copiar(string archivo, string carpeta, string? subcarpeta, string? nombreDestino = null)
    {
        var destinoCarpeta = subcarpeta == null
            ? Path.Combine(_uploads, carpeta)
            : Path.Combine(_uploads, carpeta, subcarpeta);
        Directory.CreateDirectory(destinoCarpeta);

        var nombre = nombreDestino ?? archivo;
        var origen = Path.Combine(_origen, archivo);
        if (File.Exists(origen))
            File.Copy(origen, Path.Combine(destinoCarpeta, nombre), overwrite: true);
        else
            Console.WriteLine($"Aviso: no se encontró el archivo inicial {origen}");

        return subcarpeta == null
            ? $"/uploads/{carpeta}/{nombre}"
            : $"/uploads/{carpeta}/{subcarpeta}/{nombre}";
    }
}
