# Vigía Trujillo — Plataforma de Transparencia y Control Ciudadano

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![Entity Framework](https://img.shields.io/badge/EF%20Core-8.0-green.svg)](https://learn.microsoft.com/ef/core/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-Express-red.svg)](https://www.microsoft.com/sql-server)

**Vigía Trujillo** es una plataforma web para el monitoreo, la fiscalización y la rendición de
cuentas de las obras públicas de la Municipalidad Distrital de Trujillo. Conecta a la ciudadanía
(consulta pública y reporte anónimo), al Personal Municipal (gestión de obras) y al Supervisor de
Transparencia (revisión y cierre de incidencias), con notificaciones en tiempo real vía SignalR.

**Curso:** Diseño y Arquitectura de Software — Ciclo 2026-2
**Universidad Privada del Norte — Facultad de Ingeniería de Sistemas Computacionales**
**Docente:** Vilchez Valdez Edgard William

---

## Índice

- [Características](#características)
- [Stack tecnológico](#stack-tecnológico)
- [Arquitectura](#arquitectura)
- [Patrones de diseño](#patrones-de-diseño)
- [Requisitos](#requisitos)
- [Instalación y ejecución](#instalación-y-ejecución)
- [Cuentas de acceso](#cuentas-de-acceso)
- [Mapa de casos de uso a pantallas](#mapa-de-casos-de-uso-a-pantallas)
- [Máquina de estados de la incidencia](#máquina-de-estados-de-la-incidencia)
- [Documentación de apoyo](#documentación-de-apoyo)
- [Estructura del proyecto](#estructura-del-proyecto)
- [Base de datos y datos semilla](#base-de-datos-y-datos-semilla)
- [Notificaciones en tiempo real (SignalR)](#notificaciones-en-tiempo-real-signalr)
- [Validaciones y manejo de errores](#validaciones-y-manejo-de-errores)
- [Decisiones de diseño](#decisiones-de-diseño)
- [Autores](#autores)

---

## Características

### Ciudadano (acceso público, sin registro)

| Funcionalidad | Descripción |
|---|---|
| **Consulta pública de obras** | Listado con búsqueda y filtros por **estado, categoría, zona y contratista**. Los filtros se generan desde los datos reales registrados, no de listas fijas. |
| **Detalle de obra** | Presupuesto, contratista, fechas, avance físico, estado, ubicación, zona y evidencia publicada (fotos y documentos). |
| **Reporte anónimo** | Registro de una incidencia con descripción (mínimo 20 caracteres) y hasta 3 evidencias (JPG, PNG o PDF; máx. 5 MB c/u). |
| **Seguimiento del reporte** | Consulta del estado por código de seguimiento `INC-AAAA-######`, con línea de tiempo del proceso y el historial de observaciones del Supervisor. |

> El sistema genera un **código de seguimiento único** para que el ciudadano pueda consultar su
> reporte sin necesidad de crear una cuenta.

### Personal Municipal

- **Dashboard ejecutivo** con KPIs, distribución por estado y alertas de obras paralizadas o con sobrecosto.
- **CRUD de obras** con validación de unicidad del **CUI**.
- **Actualización de avance físico** con motivo obligatorio cuando el avance retrocede.
- **Cambio de estado** con motivo obligatorio al marcar una obra como **Paralizada**.
- **Carga de evidencia** (fotos y documentos) publicada en el detalle público de la obra.
- **Respuesta a solicitudes de información** del Supervisor, con archivo de sustento.

### Supervisor de Transparencia

- **Bandeja de incidencias** con filtros por estado, obra y rango de fechas.
- **Comparación de información** (CU-14): datos oficiales de la obra frente al reporte ciudadano,
 con avance físico, presupuesto oficial y los archivos publicados de la obra (fotos y PDF) en
 paralelo con las evidencias del ciudadano.
- **Solicitud de información adicional** (CU-13) dirigida al Personal Municipal.
- **Registro de observaciones** (CU-15) que alimentan el historial cronológico.
- **Cambio de estado** (CU-16) y **emisión de resultado formal** (CU-17) mediante el patrón Strategy,
 con el desplegable alimentado **solo con las transiciones permitidas**.
- **Maestro-detalle de evidencias**: alta y baja de archivos desde la pantalla de revisión.
- **Bloqueo preventivo**: si la incidencia está **Resuelta** o **Derivada**, la interfaz deshabilita
 observaciones, cambios de estado y carga de evidencias.

###  Administrador

- Alta de usuarios con **contraseña temporal** generada automáticamente.
- Edición de usuario, cambio de rol y activación/desactivación.
- **Protección del último Administrador**: el sistema bloquea la acción si dejaría la plataforma
 sin ningún Administrador activo.

### Interfaz

- **Navegación lateral por rol** con iconos, visible solo dentro del sistema. El portal público y
 la pantalla de acceso se muestran a ancho completo.
- **Cabecera superior** con el escudo municipal sobre disco blanco y el menú de usuario.
- **Tema claro y oscuro** con conmutador en la cabecera. La preferencia se guarda en el navegador y
 se respeta la preferencia del sistema en la primera visita.
- **Diseño adaptable** verificado en escritorio, portátil, tablet y móvil; en pantallas pequeñas la
 barra lateral se convierte en cajón deslizable y en imágenes muy angostas colapsa a iconos.

---

## Stack tecnológico

- **Backend:** ASP.NET Core 8.0 MVC (C# 12)
- **ORM / Persistencia:** Entity Framework Core 8.0 — *Code First* sobre **SQL Server Express**
- **Autenticación:** Cookies de ASP.NET Core Identity + Claims por rol
- **Tiempo real:** Microsoft **SignalR** (`/hubs/vigia`)
- **Frontend:** HTML5, CSS3, **Bootstrap 5**, Bootstrap Icons, JavaScript (vanilla), Chart.js
- **Patrones:** Repository, Strategy, Inyección de Dependencias, arquitectura N-capas

---

## Arquitectura

Arquitectura **monolítica en N-capas con patrón MVC**, según la comparación de arquitecturas de la PC4:

```
┌─────────────────────────────────────────────────────────────┐
│ CAPA DE PRESENTACIÓN (Views Razor + ViewModels) │
│ Publico · DetallePublico · Registrar · Consultar │
│ Supervisor/Index · Supervisor/Revisar · Obras/* · Usuarios/*│
└───────────────────────────┬─────────────────────────────────┘
 │ HTTP (Model Binding / TempData)
┌───────────────────────────▼─────────────────────────────────┐
│ CAPA DE CONTROL (Controllers) │
│ ObrasController · IncidenciasController │
│ SupervisorController · UsuariosController · AccountController│
└───────────────────────────┬─────────────────────────────────┘
 │ inyección de dependencias
┌───────────────────────────▼─────────────────────────────────┐
│ CAPA DE LÓGICA DE NEGOCIO (Services + Strategies) │
│ IObraService / ObraEfService │
│ IIncidenciaService / IncidenciaService │
│ IncidenciaEstadoContext + 6 Strategies (GoF Strategy) │
└───────────────────────────┬─────────────────────────────────┘
 │ interfaces de repositorio
┌───────────────────────────▼─────────────────────────────────┐
│ CAPA DE ACCESO A DATOS (Repositories + DbContext) │
│ IObraRepository · IIncidenciaRepository · IUsuarioRepository│
│ ApplicationDbContext (Code First → SQL Server) │
└─────────────────────────────────────────────────────────────┘

Transversal: Hubs/VigiaHub (SignalR) · Utils/PasswordHelper · Modelos de estado
```

Las publicaciones de eventos SignalR se ejecutan bajo bloques `try/catch`: si el hub falla, la
operación principal **ya quedó persistida** y solo se registra un `warning` en el log.

---

## Patrones de diseño

### Repository (GoF)

Interfaces e implementaciones en `Repositories/`, registradas con `AddScoped` en `Program.cs`.
Aíslan las consultas de EF Core para que la capa de negocio no dependa del proveedor de datos.

```
Repositories/
├── IObraRepository.cs → ObraRepository.cs
├── IIncidenciaRepository.cs → IncidenciaRepository.cs (Include de evidencias y observaciones)
└── IUsuarioRepository.cs → UsuarioRepository.cs
```

### Strategy (GoF) — máquina de estados de la incidencia

`IncidenciaEstadoContext` recibe las seis estrategias por inyección de dependencias, selecciona la
correspondiente al estado destino y ejecuta `Validar` y `Aplicar`.

| Interfaz / clase | Rol |
|---|---|
| `IIncidenciaEstadoStrategy` | Contrato: `EstadoDestino`, `Validar(...)`, `Aplicar(...)` |
| `IncidenciaEstadoContext` | Selecciona la estrategia y ejecuta la transición |
| `PendienteDeRevisionStrategy` | Estado inicial; rechaza cualquier intento de volver a «Pendiente de revisión» |
| `EnRevisionStrategy` | Pasa a análisis en curso |
| `InformacionSolicitadaStrategy` | Espera documentación del Personal Municipal |
| `EnVerificacionStrategy` | Verificación de la información recibida o en campo (desde «Información solicitada» exige que la solicitud ya tenga respuesta) |
| `ResueltaStrategy` | Cierre por observación subsanada (CU-17) |
| `DerivadaStrategy` | Cierre por irregularidad confirmada → instancia superior |

**Decisión de diseño:** `IncidenciaEstadoContext.CambiarEstado` **devuelve un mensaje en español**
o `null` si la operación es exitosa, en lugar de lanzar excepciones. Se evaluó el uso de
excepciones, pero producía páginas de error genéricas que deterioraban la experiencia del
Supervisor. El mensaje se guarda en `TempData` y se muestra en la misma vista de revisión.

Las constantes de estado y la matriz de transiciones están centralizadas en
`Models/IncidenciaEstados.cs` (y `Models/ObraEstados.cs`), evitando duplicación de literales y
discrepancias de acentuación.

---

## Requisitos

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) o superior
- [SQL Server Express](https://www.microsoft.com/sql-server) (instancia por defecto: `.\SQLEXPRESS`)

---

## Instalación y ejecución

### 1. Configurar la cadena de conexión

Edita `appsettings.json`:

```json
{
 "ConnectionStrings": {
 "DefaultConnection": "Server=.\\SQLEXPRESS;Database=VigiaTrujilloDb;Trusted_Connection=True;TrustServerCertificate=True;"
 }
}
```

> Si usas otra instancia, cambia `Server=`. Con `Trusted_Connection=True` se usa
> **autenticación de Windows** (sin usuario ni contraseña).

### 2. Aplicar migraciones y ejecutar

```bash
dotnet restore
dotnet ef database update
dotnet run
```

> El esquema se crea con **migraciones de EF Core**, no con `EnsureCreated()`. Al arrancar,
> `Program.cs` ejecuta `Database.Migrate()`, que aplica las migraciones pendientes y registra cada
> una en `__EFMigrationsHistory`. A continuación carga los datos iniciales; esa carga solo se
> ejecuta si las tablas están vacías, de modo que los datos ya existentes no se sobrescriben.

### 3. Abrir la aplicación

| Perfil | URL |
|---|---|
| `http` | <http://localhost:5237> |
| `https` | <https://localhost:7235> |

### 4. Detener la aplicación

`Ctrl + C` en la terminal donde corre `dotnet run`.

---

## Cuentas de acceso

El Seed crea tres cuentas para el personal de la municipalidad y **restaura su contraseña en cada
arranque**, de modo que siempre puedan usarse aunque se hayan modificado:

| Rol | Usuario | Contraseña | Acceso principal |
|---|---|---|---|
| Supervisor de Transparencia | `supervisor` | `Supervisor2026` | `/Supervisor` — bandeja y cierre de incidencias |
| Personal Municipal | `personal.municipal` | `Municipal2026` | `/Obras/Dashboard` — gestión de obras |
| Administrador | `admin` | `Admin2026` | `/Usuarios` — gestión de usuarios y roles |

> **El ciudadano no tiene cuenta.** Usa el portal público sin iniciar sesión: consulta las obras
> (`/Obras/Publico`), registra su reporte de forma anónima (`/Incidencias/Registrar`) y sigue su
> estado con el código de seguimiento (`/Incidencias/Consultar`). Por eso el Administrador solo puede
> asignar los roles Personal Municipal, Supervisor de Transparencia y Administrador; el sistema
> rechaza cualquier intento de crear o editar un usuario con el rol «Ciudadano».

---

## Mapa de casos de uso a pantallas

Cada caso de uso se documenta con el controlador y la acción que lo atienden, la vista Razor exacta
que se renderiza y la redirección que ocurre al completar la operación.

### Módulo Ciudadano · Sin autenticación

| Caso de uso | Controlador y accion | Vista exacta | Redireccion / siguiente paso |
|---|---|---|---|
| CU-01: Buscar y filtrar obras públicas | `ObrasController → Publico (GET)` | `Views/Obras/Publico.cshtml` | Muestra el listado con filtros por categoría, zona, contratista y estado. El botón de detalle lleva al CU-02. |
| CU-02: Ver detalle de obra | `ObrasController → DetallePublico (GET)` | `Views/Obras/DetallePublico.cshtml` | Muestra la ficha oficial con presupuesto, contratista, avance y evidencias. El botón "Reportar irregularidad" lleva al CU-03. |
| CU-03: Registrar incidencia | `IncidenciasController → Registrar (GET/POST)` | `Views/Incidencias/Registrar.cshtml` | GET: formulario con el selector de obras. POST exitoso: RedirectToAction("Confirmacion", new { codigo }). |
| CU-04: Adjuntar evidencia | `IncidenciasController → Registrar (POST)` | `Views/Incidencias/Registrar.cshtml` | Subproceso del CU-03. Valida formato (jpg, jpeg, png, pdf) y tamaño máximo de 5 MB antes de guardar. |
| CU-03 (cont.): Confirmación de registro | `IncidenciasController → Confirmacion (GET)` | `Views/Incidencias/Confirmacion.cshtml` | Muestra el código de seguimiento generado con formato INC-2026-XXXXXX. |
| CU-05: Consultar estado de reporte | `IncidenciasController → Consultar (GET/POST)` | `Views/Incidencias/Consultar.cshtml` | POST: recarga la misma vista con el ViewModel y el historial de observaciones del Supervisor. |

### Módulo Personal Municipal · Rol: `PersonalMunicipal`

| Caso de uso | Controlador y accion | Vista exacta | Redireccion / siguiente paso |
|---|---|---|---|
| CU-06: Registrar obra | `ObrasController → Create (GET/POST)` | `Views/Obras/Create.cshtml` | POST exitoso: RedirectToAction("Index") con mensaje en TempData. |
| CU-07: Editar información de obra | `ObrasController → Edit (GET/POST)` | `Views/Obras/Edit.cshtml` | POST exitoso: RedirectToAction("Index"). Si la obra no existe, vuelve al Index con mensaje de error. |
| CU-08: Actualizar avance físico | `ObrasController → ActualizarAvance (GET/POST)` | `Views/Obras/ActualizarAvance.cshtml` | POST exitoso: RedirectToAction("Details", new { id }). Exige motivo si el avance retrocede. |
| CU-09: Subir documentos y fotografías | `ObrasController → Details (GET) + SubirArchivo (POST)` | `Views/Obras/Details.cshtml` | POST exitoso: RedirectToAction("Details", new { id }) con la evidencia listada. Máximo 10 MB. |
| CU-10: Actualizar estado de obra | `ObrasController → CambiarEstado (GET/POST)` | `Views/Obras/CambiarEstado.cshtml` | POST exitoso: RedirectToAction("Index"). Exige motivo al marcar la obra como Paralizada. |
| CU-06 (cont.): Dar de baja una obra | `ObrasController → Delete (GET) + DeleteConfirmed (POST)` | `Views/Obras/Delete.cshtml` | Baja lógica, no borrado físico. Exige motivo. POST exitoso: RedirectToAction("Index"). |
| CU-06 (cont.): Reactivar obra dada de baja | `ObrasController → Reactivar (POST)` | `Views/Obras/DadosDeBaja.cshtml` | POST exitoso: RedirectToAction("DadosDeBaja"). Recupera el registro con su historial intacto. |
| CU-13 (cont.): Responder solicitud de información | `ObrasController → ResponderSolicitud (POST)` | `Views/Obras/Details.cshtml` | POST exitoso: RedirectToAction("Details") con la respuesta y su archivo de sustento. |

### Módulo Supervisor de Transparencia · Rol: `Supervisor`

| Caso de uso | Controlador y accion | Vista exacta | Redireccion / siguiente paso |
|---|---|---|---|
| CU-11: Ver incidencias registradas | `SupervisorController → Index (GET)` | `Views/Supervisor/Index.cshtml` | Muestra la bandeja con KPIs y filtros (estado, obra y rango de fechas). SignalR agrega filas en vivo. |
| CU-12: Revisar evidencia | `SupervisorController → Revisar (GET)` | `Views/Supervisor/Revisar.cshtml` | Maestro-detalle: incidencia, obra, evidencias y observaciones en la misma vista. |
| CU-12 (cont.): Agregar evidencia a la incidencia | `SupervisorController → AgregarEvidencia (POST)` | `Views/Supervisor/Revisar.cshtml` | POST: recarga Revisar. Bloqueado si la incidencia está Resuelta o Derivada. Máximo 5 MB. |
| CU-12 (cont.): Eliminar evidencia | `SupervisorController → EliminarEvidencia (POST)` | `Views/Supervisor/Revisar.cshtml` | POST: recarga Revisar con el mensaje de éxito o de error según el resultado. |
| CU-13: Solicitar información adicional | `SupervisorController → SolicitarInformacion (POST)` | `Views/Supervisor/Revisar.cshtml` | POST: RedirectToAction("Revisar", new { id }). La incidencia pasa a "Información solicitada". |
| CU-14: Comparar información | `SupervisorController → Revisar (GET)` | `Views/Supervisor/Revisar.cshtml` | Paneles lado a lado: datos oficiales de la obra frente al reporte y la evidencia ciudadana. |
| CU-15: Registrar observaciones | `SupervisorController → AgregarObservacion (POST)` | `Views/Supervisor/Revisar.cshtml` | POST: recarga Revisar y agrega la observación al historial visible. |
| CU-16: Cambiar estado de incidencia | `SupervisorController → CambiarEstado (POST)` | `Views/Supervisor/Revisar.cshtml` | POST: valida la transición con el patrón Strategy. Si es inválida muestra el error sin escribir observación. |
| CU-17: Emitir resultado de revisión | `SupervisorController → EmitirResultado (POST)` | `Views/Supervisor/Revisar.cshtml` | POST: exige resultado final Resuelta o Derivada; si es Derivada, sustento documental obligatorio. |

### Módulo Administrador · Rol: `Administrador`

| Caso de uso | Controlador y accion | Vista exacta | Redireccion / siguiente paso |
|---|---|---|---|
| CU-18: Crear usuario | `UsuariosController → Create (GET/POST)` | `Views/Usuarios/Create.cshtml` | POST exitoso: RedirectToAction("Index"). Si no se ingresa contraseña, genera una temporal. |
| CU-19: Desactivar / reactivar usuario | `UsuariosController → Edit (GET/POST) o Desactivar (POST)` | `Views/Usuarios/Edit.cshtml` | POST exitoso: RedirectToAction("Index"). Protege al último administrador activo. |
| CU-20: Asignar o modificar roles | `UsuariosController → Edit (GET/POST)` | `Views/Usuarios/Edit.cshtml` | POST exitoso: RedirectToAction("Index"). Bloquea el cambio si deja el sistema sin administrador. |

## Máquina de estados de la incidencia

Solo el **supervisor** cambia el estado y el flujo **solo avanza**, nunca retrocede:

`Pendiente de revisión → En revisión → Información solicitada → En verificación → Resuelta / Derivada`

| Estado actual | Puede pasar a |
|---|---|
| Pendiente de revisión | En revisión (es el estado inicial al registrar el reporte) |
| En revisión | Información solicitada · En verificación · Derivada |
| Información solicitada | En verificación (solo cuando la solicitud ya tiene respuesta) · Derivada |
| En verificación | Información solicitada (pedir más datos) · Resuelta · Derivada |
| **Resuelta** | Cerrada — sin cambios |
| **Derivada** | Cerrada — sin cambios |

- Ninguna incidencia vuelve a «Pendiente de revisión» ni a «En revisión».
- El Personal Municipal **solo responde** la solicitud: su respuesta no cambia el estado. La
  incidencia sigue en «Información solicitada» con el aviso «Respuesta nueva» hasta que el
  supervisor la pasa a «En verificación».
- Si la respuesta no es suficiente, desde «En verificación» se puede **volver a solicitar
  información** todas las veces que haga falta; cada pedido crea una nueva `SolicitudInformacion`.
- Desde «En revisión» se puede ir directo a «En verificación» (verificar en campo sin pedir
  información) o a «Derivada» (irregularidad grave ya confirmada en la revisión documental).
- «Resuelta» solo se emite desde «En verificación» (hay que comprobar que el problema se corrigió).

En `Supervisor/Revisar` la lista de estados solo muestra los pasos válidos para la incidencia y el
pedido de información tiene su propio formulario («Solicitar información al Personal Municipal»).
Aunque alguien envíe un POST manipulado, `IncidenciaEstadoContext` y las estrategias lo rechazan con
un mensaje en español.

---|---|---|
| Pendiente de revisión | En revisión | — (es el estado inicial al registrar el reporte) |
| En revisión | Información solicitada · En verificación · Derivada | — |
| Información solicitada | Derivada | Cuando el Personal Municipal responde → **En verificación** |
| En verificación | Información solicitada (pedir más datos) · Resuelta · Derivada | — |
| **Resuelta** | Cerrada — sin cambios | — |
| **Derivada** | Cerrada — sin cambios | — |

- Ninguna incidencia vuelve a «Pendiente de revisión» ni a «En revisión».
- Si la respuesta del municipio no es suficiente, desde «En verificación» se puede **volver a
  solicitar información** todas las veces que haga falta; cada pedido crea una nueva
  `SolicitudInformacion` y, al responderse, la incidencia regresa a «En verificación».
- «Resuelta» solo se emite desde «En verificación» (hay que comprobar que el problema se corrigió).
- «Derivada» también se permite desde «En revisión» o mientras se espera la respuesta, porque una
  irregularidad grave confirmada en la revisión documental no necesita verificación en campo.

En `Supervisor/Revisar` la lista de estados se construye con `IncidenciaEstados.TransicionesPermitidas(...)`
y el pedido de información tiene su propio formulario («Solicitar información al Personal Municipal»).
Aunque alguien envíe un POST manipulado, `IncidenciaEstadoContext` y las estrategias lo rechazan con
un mensaje en español.

---|---|
| Pendiente de revisión | En revisión · Información solicitada · En verificación · Resuelta · Derivada |
| En revisión | Pendiente de revisión · Información solicitada · En verificación · Resuelta · Derivada |
| Información solicitada | En revisión · En verificación · Resuelta · Derivada |
| En verificación | Información solicitada · Resuelta · Derivada |
| **Resuelta** | Cerrada — sin cambios |
| **Derivada** | Cerrada — sin cambios |

El desplegable de la pantalla `Supervisor/Revisar` se construye a partir de
`IncidenciaEstados.TransicionesPermitidas(...)`, por lo que **solo ofrece opciones válidas** y no
expone transiciones que luego serían rechazadas.

---

## Documentación de apoyo

La carpeta `documentacion/` contiene dos planillas que respaldan el desarrollo:

| Archivo | Contenido |
|---|---|
| `Flujo CU.xlsx` | Mapa de flujo de los 20 casos de uso: módulo, controlador y acción, vista Razor exacta y redirección |
| `ObrasTrujillo.xlsx` | Relación de obras con componente, tramo de intervención, CUI y plazos declarados |

## Estructura del proyecto

├── documentacion/        # Flujo CU.xlsx y ObrasTrujillo.xlsx (fuente de los casos de uso y de las obras)
```
VigiaTrujillo/
├── Controllers/          # Obras, Incidencias, Supervisor, Usuarios, Account, Home
├── Data/                 # ApplicationDbContext (Code First)
├── Hubs/                 # VigiaHub.cs — grupos de SignalR por rol
├── Migrations/           # Migraciones de EF Core (versionado del esquema)
├── Models/               # Obra, Incidencia, Evidencia, ObservacionIncidencia,
│                         # ObraArchivo, Usuario + constantes de estado
├── Repositories/         # Patrón Repository (GoF)
├── Services/             # Lógica de negocio + Interfaces
├── Strategies/           # Patrón Strategy (GoF) para estados de incidencia
├── Utils/                # PasswordHelper (hash PBKDF2)
├── ViewModels/           # DTOs para las vistas
├── Views/                # Plantillas Razor
├── wwwroot/
│   ├── css/site.css      # Estilos con temas claro y oscuro
│   ├── img/              # Escudo municipal e imágenes institucionales
│   ├── js/               # Scripts del sitio y cliente de SignalR
│   ├── lib/              # Bibliotecas de terceros
│   └── uploads/          # Evidencias: incidencias/{id} y obras/{id}
├── Properties/ # launchSettings.json
├── Program.cs # DI, SignalR, middlewares y Seed
└── README.md
```

---

## Base de datos y datos semilla

El esquema se versiona con **migraciones de EF Core** (Code First). La migración `InitialCreate`
crea las seis tablas con sus claves foráneas, índices únicos y restricciones:

| Relación | Comportamiento |
|---|---|
| `Incidencias.ObraId` → `Obras.Id` | `RESTRICT` |
| `Evidencias.IncidenciaId` → `Incidencias.Id` | `CASCADE` |
| `ObservacionesIncidencia.IncidenciaId` → `Incidencias.Id` | `CASCADE` |
| `ObraArchivos.ObraId` → `Obras.Id` | `CASCADE` |
| `SolicitudesInformacion.IncidenciaId` → `Incidencias.Id` | `CASCADE` |

Índices únicos: `Obras.Cui` y `Incidencias.CodigoSeguimiento`.

Base: **`VigiaTrujilloDb`** en SQL Server Express.

| Tabla | PK | FK / Índices |
|---|---|---|
| `Obras` | `Id` | Índice único `Cui`; columna `Activo` (baja lógica) |
| `Incidencias` | `Id` | FK `ObraId` **RESTRICT**; índice único `CodigoSeguimiento` |
| `Evidencias` | `Id` | FK `IncidenciaId` **CASCADE** (detalle del maestro-detalle) |
| `ObservacionesIncidencia` | `Id` | FK `IncidenciaId` **CASCADE** |
| `ObraArchivos` | `Id` | FK `ObraId` **CASCADE** |
| `Usuarios` | `Id` | Índice único `NombreUsuario` |
| `SolicitudesInformacion` | `Id` | FK `IncidenciaId` **CASCADE**; índices `IncidenciaId` y `Estado` (migración `AgregarSolicitudesInformacion`) |

### Baja lógica: nada se elimina

**El sistema no ejecuta ningún `DELETE` sobre las obras.** La eliminación es una *baja lógica*:
la obra se marca como inactiva y deja de mostrarse, pero **el registro se conserva íntegro** en la
base de datos, junto con sus incidencias y evidencias, para eventuales auditorías.

| Campo | Función |
|---|---|
| `Activo` | `true` = visible en listados y portal público · `false` = dada de baja |
| `FechaBaja` | Fecha y hora en que se registró la baja |
| `MotivoBaja` | Justificación registrada por el Personal Municipal |
| `UsuarioBaja` | Responsable de la baja |

**Comportamiento verificado:**

- La baja **exige un motivo**; sin él no se registra.
- La obra desaparece del listado municipal, del portal público y del selector de reportar
 incidencias, y su URL pública directa deja de funcionar.
- Sigue existiendo en la base de datos con su historial completo.
- Puede **reactivarse** desde *Obras dadas de baja*, recuperando el registro tal como estaba.
- La vista **Obras dadas de baja** (`/Obras/DadosDeBaja`) funciona como archivo histórico.

> El `RESTRICT` en `Incidencias.ObraId` se mantiene como red de seguridad de integridad: si algún
> día se intentara un borrado accidental, la base lo impediría en lugar de destruir el historial.

**Datos iniciales**

Los datos iniciales se cargan desde `Data/DbInitializer.cs` cada vez que la aplicación arranca
con la base de datos vacía (si ya hay obras o incidencias, no se vuelven a cargar). Las fotos y
documentos (actas, cronogramas e informes de la Municipalidad Distrital de Trujillo) están en
`Data/SeedArchivos/` y se copian a `wwwroot/uploads/` al cargar los datos, por eso después de un
reinicio todas las imágenes vuelven a aparecer.

- **3 usuarios**: `personal.municipal`, `supervisor` y `admin` (el ciudadano no tiene cuenta).
- **12 obras**: 11 activas (1 programada, 5 en ejecución, 2 paralizadas con motivo, 2 concluidas
  y 1 con sobrecosto) y 1 dada de baja para la vista *Obras dadas de baja*. Cada obra tiene CUI,
  tramo, contratista, monto, plazo y fechas. El campo `Fuente` indica de dónde sale la cifra:
  la Consulta Amigable del MEF o el informe de avance de la Gerencia de Obras Públicas.
- **17 archivos de obra** (fotos y actas en PDF).
- **12 incidencias** en los seis estados (4 pendientes de revisión, 1 en revisión, 3 con
  información solicitada, 1 en verificación, 2 resueltas y 1 derivada), con **13 evidencias**,
  **26 observaciones** y **6 solicitudes de información** (2 pendientes, 3 respondidas con archivo
  adjunto —una todavía marcada como «Respuesta nueva»— y 1 cerrada sin respuesta). Todos los
  historiales siguen el flujo de estados (solo avanzan).

| Código | Obra | Estado | Qué se puede revisar |
|---|---|---|---|
| INC-2026-819601 | Av. Manuel Vera Enríquez | Pendiente de revisión | Revisar un caso nuevo con foto |
| INC-2026-819602 | Jr. San Martín / Jr. Independencia | Pendiente de revisión | Caso nuevo con foto |
| INC-2026-819603 | Urb. Ingeniería I (paralizada) | Pendiente de revisión | Caso nuevo con foto |
| INC-2026-819604 | Av. Federico Villarreal | Información solicitada | Respuesta del municipio sin ver («Respuesta nueva») con foto adjunta; el supervisor la puede pasar a «En verificación» |
| INC-2026-819605 | Av. Víctor Larco | Información solicitada | Solicitud pendiente para el personal municipal |
| INC-2026-819606 | Av. Costa Rica (paralizada) | Información solicitada | Solicitud pendiente para el personal municipal |
| INC-2026-819607 | Av. Perú (con sobrecosto) | En verificación | Respuesta con informe técnico en PDF adjunto |
| INC-2026-819608 | Av. América Sur | Resuelta | Caso cerrado con respuesta y foto |
| INC-2026-819609 | Av. Víctor Larco | Derivada | Caso derivado; evidencia en PDF; solicitud cerrada sin respuesta |
| INC-2026-819610 | Pasaje San Agustín | Resuelta | Caso verificado en campo sin pedir información; sin evidencias |
| INC-2026-819611 | Av. Federico Villarreal | En revisión | Caso con observaciones y sin evidencias |
| INC-2026-819612 | Pueblo Joven El Bosque | Pendiente de revisión | Caso nuevo sin evidencias |

### Reiniciar datos iniciales

Desde la carpeta del proyecto (PowerShell), con la aplicación detenida:

```powershell
# 1. Borrar la base de datos
dotnet ef database drop --force

# 2. (Opcional) Borrar los archivos subidos desde la aplicación.
#    Ojo: también borra lo que se haya subido a mano desde la aplicación.
Remove-Item -Recurse -Force wwwroot\uploads\incidencias, wwwroot\uploads\obras, wwwroot\uploads\respuestas-solicitud -ErrorAction SilentlyContinue

# 3. Arrancar: se aplican las migraciones y se cargan los datos iniciales con sus imágenes
dotnet run
```

En la consola debe aparecer algo como:
`12 obras y 17 archivos de obra cargados.` y
`12 incidencias, 13 evidencias, 26 observaciones y 6 solicitudes de información cargadas.`

> Al abrir una incidencia en la pantalla *Revisar*, el supervisor marca como vista la respuesta
> del municipio. Si se quiere volver a mostrar la «Respuesta nueva» de INC-2026-819604, basta con
> reiniciar los datos.

### Agregar una migración tras modificar el modelo

```bash
dotnet ef migrations add NombreDelCambio
dotnet ef database update
```

---

## Notificaciones en tiempo real (SignalR)

- **Servidor:** `builder.Services.AddSignalR()` y `app.MapHub<VigiaHub>("/hubs/vigia")` en `Program.cs`.
- **Grupos por rol** asignados en `VigiaHub.OnConnectedAsync()`:

| Grupo | Integrantes |
|---|---|
| `Tablero` | todas las conexiones activas |
| `PortalPublico` | todas las conexiones activas |
| `Supervisores` | rol **Supervisor** y **Administrador** |
| `Municipal` | rol **PersonalMunicipal** y **Administrador** |

- **Eventos publicados por los controladores:**

| Evento | Grupo | Cuándo |
|---|---|---|
| `NuevaIncidencia` | Supervisores | Un ciudadano registra una incidencia |
| `IncidenciaActualizada` | Tablero | Cambio de estado o solicitud de información |
| `ObservacionAgregada` | Tablero / Supervisores | Se registra una observación o respuesta |
| `EstadoObraActualizado` | Tablero | Alta, edición, avance, estado o borrado de obra |
| `ArchivoObraActualizado` | Tablero | Se publica nueva evidencia de una obra |

- **Cliente** (`wwwroot/js/vigia-signalr.js`): muestra *toasts* de Bootstrap y, en la bandeja del
 Supervisor, **añade la fila de la incidencia nueva en tiempo real** sin recargar la página.

---

## Validaciones y manejo de errores

| Escenario controlado | Comportamiento |
|---|---|
| `Id` de obra o incidencia inexistente | Mensaje amigable y redirección al listado |
| Resultado sobre incidencia ya cerrada | Transición rechazada con mensaje explicativo |
| Retroceso del avance de una obra | Exige **motivo de retroceso** |
| Marcar obra como **Paralizada** | Exige **motivo** del cambio de estado |
| Archivos vacíos o formato no permitido | Rechazo con los formatos y el tamaño admitidos |
| Evidencia sobre incidencia cerrada | Bloqueado en la interfaz y en el controlador |
| `NombreUsuario` duplicado | Error de validación en el formulario |
| Último Administrador activo | Acción bloqueada en desactivación y cambio de rol |
| Eliminar obra con incidencias | Informativo: el historial debe conservarse |
| Deshabilitación de transiciones | El dropdown solo lista lo permitido (Strategy) |

**Manejo global de excepciones:** `app.UseExceptionHandler("/Home/Error")` se configura
**también en Development**, y `Views/Shared/Error.cshtml` muestra un mensaje amigable con código
de referencia — **sin exponer trazas (stack traces)** al usuario final.

**Formato de archivos y tamaños:**

| Contexto | Formatos | Máximo |
|---|---|---|
| Evidencia de incidencia | `.jpg` `.jpeg` `.png` `.pdf` | 5 MB |
| Evidencia de obra / sustento | `.jpg` `.jpeg` `.png` `.pdf` `.doc` `.docx` | 10 MB |

---

## Decisiones de diseño

**Nada se elimina físicamente.** Toda baja es lógica y queda registrada con su motivo y responsable.
Es un requisito de auditoría: las evidencias ciudadanas deben conservar su trazabilidad aunque la
obra se cierre.

**Transición e historial coordinados.** El cambio de estado, el registro de la observación y la
aplicación del nuevo estado se resuelven en una sola operación de servicio. Validar por separado
producía observaciones fantasma en los rechazos y pérdida del registro de cierre.

**Mensajes de validación en lugar de excepciones.** Las transiciones inválidas son un flujo
esperado del negocio, no un error de programación; por eso el servicio devuelve el mensaje y la
interfaz lo muestra tal como corresponde al usuario.

**Filtros derivados de la base de datos.** Las categorías, zonas y contratistas disponibles se
obtienen de las obras registradas. Una lista fija producía resultados vacíos cuando no coincidía
con los datos reales.

**Los datos declaran su origen.** Cada obra registra en el campo `Fuente` de dónde salen sus
cifras: la Consulta Amigable del MEF o el informe de avance de la Gerencia de Obras Públicas de la
Municipalidad Distrital de Trujillo. Así el supervisor sabe qué documento respalda cada monto y
cada porcentaje de avance.

**Estilos por tokens CSS.** Toda la interfaz se apoya en variables definidas en `:root`
(`--vt-surface`, `--vt-ink`, `--vt-muted`, `--vt-border`, `--vt-primary` y sus derivados). El tema
oscuro se declara en el bloque `[data-theme="dark"]`, que redefine esas mismas variables; por eso no
hay estilos duplicados por componente. Las clases reutilizables usan el prefijo `vt-`:
`vt-page-header`, `vt-kpi`, `vt-dl`, `vt-timeline`, `vt-empty`, `vt-evidence`, `vt-badge-*`,
`vt-sideitem`, `vt-portada` y `vt-chart`.

**Gráficos con contenedor de altura fija.** Chart.js con `maintainAspectRatio: false` necesita un
contenedor de altura definida; sin ella se produce un bucle de crecimiento que deforma la página.

---

## Autores

| Autor | |
|---|---|
| DELGADO GUTIERREZ, Edilson Daniel | 100% |
| MOGOLLÓN CARRANZA, Julio César | 100% |
| QUEZADA LÓPEZ, Jonathan Alfredo | 100% |
| SUCLUPE VELA, Steven Edson Francesscoly | 100% |
| VÁSQUEZ SILVA, Kevin Jair | 100% |

**Repositorio:** <https://github.com/juliocesarmogollon21/VigiaTrujillo>

---

© 2026 Vigía Trujillo — Municipalidad Distrital de Trujillo. Todos los derechos reservados.
