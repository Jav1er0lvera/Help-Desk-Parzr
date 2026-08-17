# Conceptos, terminología y mejores prácticas — HelpDesk

Guía de referencia de los conceptos que aparecen en el proyecto. Para cada uno: **qué es**, **por qué se hace así** y **cómo aplica aquí**. Complementa a [HALLAZGOS.md](HALLAZGOS.md) (que registra el estado real y las mejoras).

---

## Índice

1. [Arquitectura y capas](#1-arquitectura-y-capas)
2. [Clean Architecture](#2-clean-architecture)
3. [CQRS y MediatR](#3-cqrs-y-mediatr)
4. [Tipos de objeto: Entidad, DTO, ViewModel, Request](#4-tipos-de-objeto)
5. [SDK y Refit](#5-sdk-y-refit)
6. [Capa de Servicio y ServiceResult](#6-capa-de-servicio-y-serviceresult)
7. [Mapper / Mapeo](#7-mapper--mapeo)
8. [Inyección de dependencias (DI)](#8-inyección-de-dependencias-di)
9. [Autenticación: Cookies vs JWT](#9-autenticación-cookies-vs-jwt)
10. [Vistas: Razor, Layout, server-render vs JS fetch](#10-vistas-razor-layout-y-render)
11. [.NET Aspire](#11-net-aspire)
12. [Entity Framework Core](#12-entity-framework-core)
13. [Mejores prácticas — resumen](#13-mejores-prácticas--resumen)

---

## 1. Arquitectura y capas

**Qué es:** organizar el código en proyectos con una responsabilidad fija cada uno, en lugar de mezclar todo en un solo lugar.

**Los 9 proyectos del sistema:**

| Proyecto | Rol |
|---|---|
| `HelpDesk.Domain` | Entidades del negocio + interfaces de repositorio. No depende de nada. |
| `HelpDesk.Application` | Casos de uso (comandos/handlers de MediatR). |
| `HelpDesk.Infrastructure` | EF Core, `DbContext`, repositorios, migraciones. |
| `HelpDesk.API` | Backend HTTP (controladores REST `api/v1/*`). |
| `HelpDesk.SDK` | Clientes Refit tipados + DTOs para consumir la API. |
| `HelpDesk.Web` | UI MVC (Razor), autenticación por cookies. |
| `HelpDesk.AppHost` | Aspire: orquesta Postgres + API + Web. |
| `HelpDesk.ServiceDefaults` | Telemetría, health checks, service discovery. |
| `HelpDesk.Domain.Tests` | Pruebas unitarias del Domain (xUnit). |

**Por qué:** separar responsabilidades hace el código más fácil de entender, probar y cambiar. Si cambia la base de datos, solo tocas Infrastructure; si cambia la UI, solo tocas Web.

---

## 2. Clean Architecture

**Qué es:** un estilo de arquitectura donde las dependencias apuntan **hacia adentro**. Las capas externas (UI, API) conocen a las internas (Application, Domain), pero **nunca al revés**.

```
Web / API  →  Application  →  Domain
Infrastructure  →  Domain
```

**Regla de oro:** el **Domain** (el núcleo, la lógica de negocio) no sabe que existe HTTP, ni EF Core, ni ASP.NET. Es C# puro.

**Por qué:**
- El negocio queda **aislado** de los detalles técnicos (base de datos, framework web).
- Puedes cambiar la infraestructura (ej. de PostgreSQL a SQL Server) sin tocar la lógica.
- Es **testeable**: pruebas el Domain sin levantar base de datos ni servidor.

**Cómo aplica aquí:** el Web no referencia a la API/Application/Infrastructure directamente — habla con la API por HTTP (vía el SDK). *(Ver excepciones reales en HALLAZGOS.md #4: el login rompe esta regla.)*

---

## 3. CQRS y MediatR

**CQRS = Command Query Responsibility Segregation.** Separa las operaciones que **escriben** (Commands) de las que **leen** (Queries).

| | Command | Query |
|---|---|---|
| Hace | Modifica estado (crear/actualizar/borrar) | Lee datos |
| Ejemplo | `CreateTicketCommand` | "dame todos los tickets" |

**Regla mental:** un método o **cambia** algo, o te **responde** algo — no las dos cosas.

**MediatR:** librería que implementa CQRS en .NET. Cada caso de uso es un `Command` + su `Handler`. El controlador solo **envía** el comando (`_mediator.Send(...)`) sin saber cómo se ejecuta.

```csharp
public class CreateTicketCommand : IRequest<Ticket> { ... }          // la intención
public class CreateTicketHandler : IRequestHandler<...> { Handle() } // la ejecución
```

**Por qué:** cada operación es una clase pequeña, aislada, fácil de leer y probar, sin un "servicio gigante" que hace todo.

**Cómo aplica aquí:** la API usa MediatR en algunos endpoints (`CreateTicket`, `AssignTicket`) pero **no en todos** — es CQRS parcial (ver HALLAZGOS / CLAUDE.md).

---

## 4. Tipos de objeto

Clases parecidas pero con roles distintos. **Confundirlas es un error común.**

| Tipo | Vive en | Para qué | Contiene |
|---|---|---|---|
| **Entidad** (`User`, `Ticket`) | Domain | Representa el negocio y se guarda en la BD | TODO (incluido `PasswordHash`) |
| **DTO** (`UserDto`, `TicketDto`) | SDK / API | Viajar por la red (HTTP/JSON) | Solo lo público (sin secretos) |
| **ViewModel** (`CategoryViewModel`) | Web | Lo que la vista muestra | Datos listos para pintar |
| **Request** (`CreateTicketRequest`) | API / Web | Datos que **entran** en una petición | Solo los campos de entrada |

**Ejemplo clave — por qué el DTO NO es la entidad:**

```csharp
public class User      { ... string PasswordHash; UserRole Role; }  // entidad: TODO
public class UserDto   { ... /* SIN PasswordHash, SIN Role */ }      // DTO: solo lo seguro
```

**Por qué existen los DTOs y ViewModels:**
- **Seguridad:** el DTO filtra datos sensibles (nunca expones el `PasswordHash` por la red).
- **Desacople:** la vista/red puede cambiar sin tocar el negocio ni la BD.
- **Regla de dependencias:** el Domain no debe conocer detalles de HTTP ni de la UI.

**Regla:** nunca metas una **entidad de Domain** directo en una vista o en una respuesta HTTP. Tradúcela a DTO/ViewModel.

---

## 5. SDK y Refit

**SDK:** proyecto con **clientes tipados** para consumir la API sin escribir `HttpClient` a mano.

**Refit:** librería que convierte una **interfaz de C#** en llamadas HTTP automáticamente. Tú describes el endpoint con atributos; Refit genera el código.

```csharp
public interface ICategoriesApi
{
    [Get("/api/v1/categories")]                     // describe: "hay un GET aquí"
    Task<List<CategoryDto>> GetAllAsync();
}
```

**Analogía:** la **API es la cocina** (ejecuta), el **SDK es el menú** (describe qué pedir), el **Web es el cliente** (pide).

**El SDK NO define endpoints** — los describe para consumirlos. Los endpoints reales viven en la API.

**Por qué usar el SDK en vez de `HttpClient` crudo:**

| | `HttpClient` manual | SDK (Refit) |
|---|---|---|
| Código | serializar/deserializar a mano | una línea tipada |
| URLs | strings sueltos | dentro de la interfaz |
| Errores | revisar `StatusCode` | lanza `Refit.ApiException` (capturable) |
| Tipos | JSON crudo | trabaja con DTOs |

*(Estado real: el SDK existe pero el Web aún no lo usa — HALLAZGOS.md #1.)*

### DelegatingHandler (interceptor del HttpClient)

**Qué es:** una clase que se coloca **en medio** de cada petición HTTP que sale de un `HttpClient`, antes de que llegue a la red. Es un *interceptor* (o "middleware" del cliente HTTP): puede leer o modificar la petición saliente y la respuesta entrante.

```csharp
public class JwtHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        // ... modifica la petición aquí (ej. añadir una cabecera) ...
        return await base.SendAsync(request, ct);   // deja seguir la petición
    }
}
```

**Cómo se conecta:** se "engancha" a un `HttpClient` con nombre en `Program.cs`. A partir de ahí, **toda** petición hecha con ese cliente pasa por el handler:

```csharp
builder.Services.AddHttpClient("api", ...)
    .AddHttpMessageHandler<JwtHandler>();   // ← engancha el interceptor
```

```
Tu código  →  DelegatingHandler  →  Red / API
(GetAsync)     (modifica la          (recibe la petición
               petición)              ya modificada)
```

**Por qué usarlo:** centraliza en **un solo lugar** algo que aplica a **todas** las peticiones, en vez de repetirlo en cada llamada. Usos típicos:
- **Autenticación** — añadir `Authorization: Bearer <token>` a cada petición (ver [JWT.md](JWT.md) Pieza 5).
- **Logging** — registrar cada petición/respuesta.
- **Reintentos / resiliencia** — reintentar si falla.
- **Cabeceras comunes** — correlación, idioma, etc.

**Regla:** si algo debe pasar en *todas* las llamadas de un `HttpClient`, va en un `DelegatingHandler`, no copiado en cada método. Funciona igual con `HttpClient` crudo o con el SDK (Refit).

---

## 6. Capa de Servicio y ServiceResult

**Capa de Servicio:** una capa entre el controlador y el SDK que centraliza `try/catch`, logging, mapeo y mensajes al usuario.

```
Controlador → Servicio (try/catch + mapper) → SDK → API
```

**ServiceResult\<T\>:** un **envoltorio de resultado**. En vez de devolver `T` a secas, devuelve `T` + metadata de cómo salió la operación.

```csharp
public class ServiceResult<T>
{
    public T? Data { get; set; }        // los datos
    public bool Success { get; set; }    // ¿salió bien?
    public bool Warning { get; set; }    // ¿bien pero sin datos?
    public string Message { get; set; }  // mensaje para el usuario
}
```

**Los tres estados que distingue** (que un `StatusCode(500)` mudo no puede):

| Estado | Significa |
|---|---|
| `Success` | OK con datos |
| `Warning` | OK pero vacío ("aún no hay elementos") |
| `Fail` | Error real |

**Por qué:**
- **Contrato consistente:** todo servicio devuelve lo mismo; el controlador siempre sabe qué esperar.
- **El controlador nunca ve una excepción** — el servicio la atrapa y la traduce a un mensaje.
- **Separa el detalle técnico (log) del mensaje al usuario** (no filtras stack traces).
- **Distingue "vacío" de "error"** — mejor experiencia de usuario.

**Mejores prácticas del patrón:**
- Centralizar el `try/catch` en un `BaseService.ExecuteAsync` (no repetirlo en cada método).
- Usar `[CallerMemberName]` para el nombre del método en los logs (evita bugs de copy/paste).
- Capturar `OperationCanceledException` aparte (una cancelación **no es un error**).
- Capturar `Refit.ApiException` (error de la API) separado de `Exception` (error del Web).
- Métodos de fábrica: `ServiceResult.Ok(data)` / `.Warn(msg)` / `.Fail(msg)`.

*(Implementación completa en HALLAZGOS.md #5.)*

### Patrones de diseño formales que combina

El `BaseService` + `ExecuteAsync` + `ServiceResult` no es un invento suelto: combina **tres patrones con nombre propio**.

**1. `ExecuteAsync` (el envoltorio try/catch) → "Execute Around Method" (Execute-Around).**
Es el nombre preciso del patrón "rodea este bloque de código con un comportamiento común (aquí: try/catch + logging)". Le pasas el "qué hacer" (`Func<...>`) y él pone el "envoltorio" alrededor. Es el corazón de tu `ExecuteAsync`.

**2. La clase base abstracta que comparte estructura → "Template Method".**
Cuando una clase base define el esqueleto (el manejo de errores) y obliga a los hijos a rellenar huecos (`ServiceName`), eso es el patrón *Template Method* clásico (de los "Gang of Four").

**3. El `ServiceResult<T>` → "Result Pattern" (u Operation Result / Result Object).**
Devolver un objeto que dice "éxito/error + datos + mensaje" en vez de lanzar excepciones o devolver el dato pelón, es el *Result Pattern*.

> En una frase: tu `BaseService` usa **Execute-Around + Template Method** para producir un **Result** (`ServiceResult`).

---

## 7. Mapper / Mapeo

**Qué es:** convertir un objeto de un tipo a otro (ej. `CategoryDto` → `CategoryViewModel`).

```csharp
public static List<CategoryViewModel> MapToViewModel(this List<CategoryDto> dtos) =>
    dtos.Select(d => new CategoryViewModel { Id = d.Id, Name = d.Name, ... }).ToList();
```

**Por qué:** cada capa tiene su propio tipo (DTO para la red, ViewModel para la vista). El mapper es la "frontera de traducción" entre ellos. Mantiene las capas desacopladas.

**Dónde hacerlo:** en la **capa de servicio** (Web), justo después de recibir el DTO del SDK.

**Nota:** puede hacerse a mano (métodos de extensión, como aquí) o con librerías (AutoMapper, Mapster). A mano es explícito y sin magia; conviene para proyectos que valoran claridad.

---

## 8. Inyección de dependencias (DI)

**Qué es:** en vez de que una clase **cree** sus dependencias (`new Servicio()`), las **recibe** por el constructor. Un contenedor las provee.

```csharp
public CategoriesController(CategoriesService service)  // la recibe, no la crea
{
    _service = service;
}
```

**Registro** (en `Program.cs`):
```csharp
builder.Services.AddScoped<CategoriesService>();          // "cuando alguien pida esto, dáselo"
builder.Services.AddScoped<ITicketRepository, TicketRepository>();  // interfaz → implementación
```

**Ciclos de vida:** `AddScoped` (uno por petición HTTP), `AddSingleton` (uno para toda la app), `AddTransient` (uno nuevo cada vez).

**Por qué:**
- **Desacople:** dependes de interfaces, no de clases concretas → fácil de cambiar o simular en tests.
- **Testeable:** en pruebas inyectas una versión falsa (mock).
- **Centralizado:** toda la configuración de dependencias vive en un lugar.

**Convención del proyecto:** cada capa expone un método de extensión (`AddApplication()`, `AddInfrastructure()`, `AddHelpDeskSdk()`) que registra sus propias dependencias.

---

## 9. Autenticación: Cookies vs JWT

Dos mecanismos que **coexisten** en capas distintas (no se contradicen):

| | Cookie | JWT (JSON Web Token) |
|---|---|---|
| Entre quién | Navegador ↔ Web | Web ↔ API |
| Protege | Páginas MVC del Web | Endpoints de la API |
| Estado | Con estado (sesión en el servidor) | Sin estado (el token se auto-valida) |
| Cómo viaja | Cookie automática del navegador | Cabecera `Authorization: Bearer <token>` |

**Cookie:** tras el login, el servidor emite una cookie cifrada con los *claims* del usuario (id, nombre, rol). El navegador la reenvía en cada petición.

**JWT:** una cadena firmada criptográficamente que contiene los claims. La API **verifica la firma** sin consultar la BD. Si alguien altera el token, la firma no coincide y se rechaza.

**Claims:** "datos que el sistema afirma sobre el usuario" (su id, rol, email). Se leen así:
```csharp
var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
```

**`[Authorize]` / `[AllowAnonymous]`:**
- `[Authorize]` exige estar autenticado para entrar a un controlador o acción.
- `[AllowAnonymous]` abre una excepción pública (ej. login, landing).
- **Mejor práctica "seguro por defecto":** `[Authorize]` en la clase + `[AllowAnonymous]` solo en lo público → las acciones nuevas nacen protegidas.

**BCrypt:** algoritmo para hashear contraseñas. Nunca se guarda la contraseña en texto plano; se guarda su hash. Es lento a propósito (dificulta fuerza bruta) y cada hash lleva su propia "sal".

*(Estado real: la API no tiene autorización — HALLAZGOS.md #4.)*

---

## 10. Vistas: Razor, Layout y render

**Razor (`.cshtml`):** motor de plantillas que mezcla HTML con C# (`@Model`, `@foreach`).

**`@model`:** declara el tipo de datos que la vista recibe → tipado fuerte, autocompletado, validación en compilación. **Preferir sobre `ViewBag`** (que es dinámico y falla en runtime).

**`_Layout.cshtml`:** plantilla común (head, navbar, scripts) que envuelve a todas las vistas con `@RenderBody()`. Evita duplicar el HTML en cada página.

**`_ViewStart.cshtml`:** define el layout por defecto para todas las vistas (`Layout = "_Layout";`).

**Por qué usar Layout:** cambiar el menú o un CSS se hace en **1 archivo**, no en 7. Evita duplicación e inconsistencias.

**Server-render vs JS fetch — dos formas de mostrar datos:**

| | Server-render (`@model` + `@foreach`) | JS fetch |
|---|---|---|
| Quién pinta | El servidor (Razor) | El navegador (JavaScript) |
| Datos | El controlador pasa el ViewModel | JS hace `fetch()` a un endpoint JSON |
| Ventaja | Menos JS, más simple, SEO | Refresca sin recargar la página |
| Recomendado para | Paneles administrativos internos | UIs muy interactivas |

*(Estado real: las vistas no usan `_Layout` y usan JS fetch — HALLAZGOS.md #2.)*

---

## 11. .NET Aspire

**Qué es:** herramienta para **orquestar** aplicaciones distribuidas en local. Levanta con un comando todos los servicios (base de datos, API, Web) y los conecta.

**`AppHost.cs`:** define los recursos y sus relaciones.
```csharp
var postgres = builder.AddPostgres("postgres").WithDataVolume().AddDatabase("helpdesk");
var api = builder.AddProject<Projects.HelpDesk_API>("api").WithReference(postgres);
```

**Service discovery:** Aspire resuelve las direcciones entre servicios por nombre. Por eso el Web usa `https+http://api` en vez de una URL fija — Aspire la resuelve en runtime.

**Por qué:** en vez de arrancar 3 proyectos y una base de datos a mano y configurar connection strings, `aspire run` lo hace todo y da un dashboard para ver los servicios.

---

## 12. Entity Framework Core

**Qué es:** ORM (Object-Relational Mapper). Traduce entre objetos C# y tablas de base de datos, para no escribir SQL a mano.

**`DbContext` (`HelpDeskDbContext`):** la sesión con la base de datos. Cada `DbSet<T>` es una tabla.
```csharp
public DbSet<Ticket> Tickets => Set<Ticket>();
```

**Migraciones:** versiones del esquema de la BD generadas desde el modelo C#. Cada cambio (agregar una columna) genera una migración.
```bash
dotnet ef migrations add <Nombre> --project src/HelpDesk.Infrastructure --startup-project src/HelpDesk.API
```

**Repositorio:** clase que encapsula el acceso a datos de una entidad (`TicketRepository`), detrás de una interfaz (`ITicketRepository`) que vive en el Domain.

**Por qué el patrón repositorio:** el Domain define **qué** operaciones existen (interfaz) sin saber **cómo** se implementan (EF Core) → respeta la regla de dependencias.

---

## 13. Mejores prácticas — resumen

**Arquitectura**
- Respeta la regla de dependencias: el Domain no conoce HTTP, EF ni la UI.
- El Web habla con la API por HTTP (vía SDK), no accede a la BD directo.
- Cada capa expone su `Add...()` para registrar sus dependencias.

**Tipos**
- Nunca expongas una entidad de Domain por la red o en una vista → usa DTO/ViewModel.
- Los DTOs no llevan datos sensibles (ej. `PasswordHash`).

**Consumo de la API (Web)**
- Usa el SDK (Refit) en vez de `HttpClient` crudo.
- Mete la lógica en una **capa de servicio**, no en el controlador.
- Devuelve `ServiceResult<T>` para un contrato consistente y manejo de errores centralizado.
- Centraliza `try/catch`, logging y mensajes; no los repitas en cada método.

**Vistas**
- Usa `@model` (ViewModel), no `ViewBag`, para datos importantes.
- Usa `_Layout` para no duplicar HTML.
- La vista solo muestra; no calcula ni busca datos.

**Seguridad**
- "Seguro por defecto": `[Authorize]` en la clase, `[AllowAnonymous]` en excepciones.
- Protege **la API**, no solo el Web (una puerta trasera abierta anula el candado del frente).
- Nunca guardes contraseñas en texto plano (BCrypt).
- No filtres detalles técnicos al usuario (loguéalos, muestra un mensaje genérico).

**General**
- Inyección de dependencias por interfaz → desacople y testeabilidad.
- Un solo lugar para cada responsabilidad → fácil de cambiar y mantener.
