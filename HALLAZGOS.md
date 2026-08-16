# Hallazgos del proyecto HelpDesk

Registro de observaciones sobre la arquitectura y el estado del código, encontradas durante el análisis del proyecto.

---

## 1. El SDK está construido pero desconectado

**Flujo ideal (Clean Architecture):**

```
Web Controller → SDK → API → base de datos
```

**Estado actual:** el `HelpDesk.SDK` (clientes Refit tipados) está **construido pero desconectado**. Hoy el Web se salta el SDK y llama a la API "a mano" con `IHttpClientFactory` / `HttpClient`, escribiendo las URLs y el JSON manualmente.

**Migración recomendada:** pasar del `HttpClient` manual al SDK sería una mejora real (más limpio, tipado y menos propenso a errores). Consta de dos pasos:

1. Llamar `AddHelpDeskSdk(...)` en `Program.cs` del proyecto Web (hoy no se llama en ningún lado).
2. Cambiar los controladores del Web para inyectar las interfaces del SDK (ej. `IUsersApi`, `ITicketsApi`) en lugar de `IHttpClientFactory`.

**Ejemplo del cambio:**

| | Hoy (HttpClient manual) | Ideal (con SDK) |
|---|---|---|
| Qué inyecta | `IHttpClientFactory` | `IUsersApi` |
| La URL | escrita a mano como string | dentro de la interfaz Refit |
| El JSON | se serializa/deserializa a mano | Refit lo hace solo |
| Tipado | strings crudos | trabaja con DTOs (`UserDto`) |
| Registro | `AddHttpClient("api")` ✅ ya está | `AddHelpDeskSdk(...)` ❌ falta |

---

## 2. Las vistas no usan `_Layout` (HTML duplicado en cada página)

**Diagnóstico:** todas las vistas de página completa tienen `Layout = null;` y cada una trae su propio `<!DOCTYPE html>`, `<head>` y `<body>`. No usan el `_Layout.cshtml` que existe.

El layout se apaga en **dos niveles**:

1. **Global** — `Views/_ViewStart.cshtml` define `Layout = null;` (normalmente diría `Layout = "_Layout";`), lo que desactiva el layout para todas las vistas por defecto.
2. **Por vista** — cada `.cshtml` además repite `Layout = null;` y mete la página HTML entera.

**Estado por vista:**

| Vista | `Layout` | Trae HTML completo |
|---|---|---|
| `Home/Index` | `null` | Sí (página entera) |
| `Home/Dashboard` | `null` | Sí |
| `Home/TicketDetail` | `null` | Sí |
| `Categories/Index` | `null` | Sí |
| `Platforms/Index` | `null` | Sí |
| `Account/Login` | `null` | Sí |
| `Account/Register` | `null` | Sí |
| `Home/Privacy` | *(sin definir)* | No — fragmento roto (scaffold) |
| `Shared/Error` | *(sin definir)* | No — fragmento roto (scaffold) |

> Detalle: `Privacy` y `Error` no ponen `Layout = null`, así que *intentan* usar el layout global, pero como `_ViewStart` está en `null` tampoco lo usan → generan HTML incompleto (un `<h1>` suelto sin `<html>`/`<head>`).

**Archivos huérfanos (existen pero nadie los referencia):**

- `Shared/_Layout.cshtml` y su `_Layout.cshtml.css`
- `Shared/_AuthLayout.cshtml` (empezado para las pantallas de auth, sin usar)

**Por qué es un problema (deuda técnica):**

1. **Duplicación masiva** — el `<head>`, la fuente de Google (`Inter`), los `<link>` de CSS y el navbar/sidebar están copiados y pegados en cada vista.
2. **Mantenimiento costoso** — cambiar el menú o un CSS obliga a editar ~7 archivos en vez de 1.
3. **Riesgo de inconsistencia** — indentación y variables (`role`, etc.) ya difieren entre vistas por el copy/paste.
4. **Lógica repetida en las vistas** — el cálculo de iniciales del usuario está duplicado literalmente en Dashboard, TicketDetail, Categories y Platforms:
   ```csharp
   var userInitials = string.Join("", userName.Split(' ').Take(2).Select(w => w[0].ToString().ToUpper()));
   ```

**Solución recomendada (a alto nivel):**

1. Poner `Layout = "_Layout";` en `_ViewStart.cshtml`.
2. Mover el `<head>`, navbar/sidebar y scripts comunes a `_Layout.cshtml`, con `@RenderBody()` para el contenido.
3. Quitar `Layout = null;` y el HTML repetido de cada vista, dejando solo su contenido propio.
4. Usar **dos layouts**: `_AuthLayout` (ya existe a medias) para Login/Register, y `_Layout` para Dashboard/Categories/Platforms/TicketDetail.
5. Reparar o borrar `Privacy` y `Error` (hoy generan HTML incompleto).

---

## 3. Mezcla de mecanismos para pasar datos a las vistas (usar ViewModel)

**Diagnóstico:** las vistas reciben datos por **tres mecanismos distintos a la vez**, en lugar de un ViewModel único por pantalla. Ejemplo en `HomeController.Dashboard`:

```csharp
// 1. Model tipado (bien) ✅
return View(tickets);                 // List<TicketViewModel>

// 2. ViewBag (dinámico, sin tipos) ⚠️
ViewBag.Role = role;
ViewBag.Error = ex.Message;

// 3. La vista calcula por su cuenta leyendo claims (lógica en la vista) ⚠️
// en Dashboard.cshtml:
var userName = User.Identity?.Name ?? "Usuario";
var userInitials = string.Join("", userName.Split(' ').Take(2).Select(w => w[0].ToString().ToUpper()));
var role = ViewBag.Role as string ?? "Usuario";
```

**Por qué preferir ViewModel sobre ViewBag:**

| | ViewModel (`@model`) | ViewBag |
|---|---|---|
| Tipado | Fuerte (el compilador valida) | Dinámico (errores en runtime) |
| Autocompletado | Sí | No |
| Nombre mal escrito | Error de compilación | Falla silenciosa (`null`) |
| Refactor (renombrar) | Seguro | Frágil |
| Documenta el contrato | Sí | No |

**Recomendación:** un ViewModel por vista que agrupe **todo** lo específico de esa pantalla. Para el Dashboard:

```csharp
public class DashboardViewModel
{
    public List<TicketViewModel> Tickets { get; set; } = [];
    public string Role { get; set; } = "Usuario";
    public string UserName { get; set; } = string.Empty;
    public string UserInitials { get; set; } = string.Empty;  // calculado en el controller, no en la vista
    public string? Error { get; set; }
}
```

Así la vista **solo muestra** (`@Model.Role`, `@Model.UserInitials`), no calcula ni adivina.

**Matices (no es dogma):**

1. **Datos de negocio de la pantalla → siempre ViewModel** (tickets, rol, error).
2. **Datos globales (usuario, rol) → mejor en el `_Layout` vía claims**, no repetidos en cada ViewModel. Conecta con el **hallazgo #2**: con un `_Layout` no haría falta meter `UserName`/`UserInitials` en cada ViewModel → otra razón para arreglar el layout primero.
3. **`ViewBag` puntual está bien para trivialidades** (un título, un flag menor); el problema es usarlo como mecanismo *principal* de datos importantes como hoy con `ViewBag.Role`.
4. **No confundir capas:** `TicketViewModel` (Web) es casi idéntico a `TicketDto` (SDK), y está bien que sean distintos (DTO = viaja por HTTP; ViewModel = lo que la vista muestra). Lo que nunca debe pasar es meter la entidad de Dominio (`Ticket`) directo en la vista.

---

## 4. 🚨 CRÍTICO: la API no tiene autorización (endpoints abiertos)

**Diagnóstico:** el `[Authorize]` del Web solo protege las páginas MVC. La **API** (`HelpDesk.API`), que es donde viven los datos, **no tiene ninguna autorización**:

- Ningún controlador de la API (`TicketsController`, `UsersController`, etc.) tiene `[Authorize]`.
- `Program.cs` de la API llama `app.UseAuthorization()` pero **nunca registra un esquema de autenticación** y **falta `app.UseAuthentication()`**.

**Impacto:** cualquiera que llegue directo a la API se salta todo el login:

```bash
curl https://<api>/api/v1/tickets                  # lista TODOS los tickets, sin token
curl -X DELETE https://<api>/api/v1/tickets/{id}   # borra cualquier ticket, sin token
```

El `[Authorize]` del Web es cosmético mientras la puerta trasera (la API) esté abierta.

> **Nota sobre el estado de `[Authorize]` en el Web (para contexto):** Categories, Platforms y Tickets ya tienen `[Authorize]` a nivel de clase ✅. `AccountController` no lo tiene a propósito (login/registro público) ✅. `HomeController` lo tiene solo por acción (`Dashboard`, `TicketDetail`), y está bien que `Index` (landing) y `Error` sean públicas. Mejora menor: usar el patrón "seguro por defecto" → `[Authorize]` en la clase + `[AllowAnonymous]` en las acciones públicas, para que las acciones nuevas nazcan protegidas.

**Solución — autenticación con JWT (el paquete ya está instalado pero sin configurar):**

```xml
<!-- ya presente en HelpDesk.API.csproj -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.8" />
```

Flujo objetivo: el token se genera **una vez en el login** y se envía en **cada** petición a la API en la cabecera `Authorization: Bearer <token>`. La API valida la firma en cada llamada (stateless).

Tres piezas por conectar:

1. **La API debe EMITIR el token** — falta un endpoint `POST /api/v1/auth/login` que valide credenciales y devuelva el JWT. **Bloqueante:** hoy el login vive en el Web (`AccountController` pega directo a la BD), así que primero hay que **mover la autenticación a la API**.
2. **La API debe VALIDAR el token** — registrar `AddAuthentication(JwtBearerDefaults...).AddJwtBearer(...)`, agregar `app.UseAuthentication()` (falta) y poner `[Authorize]` en los controladores de la API.
3. **El cliente (Web) debe GUARDAR y ENVIAR el token** — al hacer login recibe el JWT, lo guarda (p. ej. en la cookie de sesión) y lo adjunta en cada llamada, idealmente con un `DelegatingHandler` en el `HttpClient`/cliente Refit para no repetirlo en cada método.

**Matiz — coexisten DOS autenticaciones (no se contradicen):**

| | Cookie (ya existe) | JWT (por implementar) |
|---|---|---|
| Entre quién | Navegador ↔ Web | Web ↔ API |
| Protege | Páginas MVC del Web | Endpoints de la API |
| Estado | Con estado (sesión) | Sin estado (stateless) |

---

## 5. Patrón de implementación a seguir: Vista → ViewModel → Controlador → Servicio → SDK

**Objetivo:** estandarizar cómo el Web consume la API, reemplazando el `HttpClient` crudo + `StatusCode(500)` mudo por un flujo con capa de servicio, manejo de errores centralizado, mapeo a ViewModel y un envoltorio `ServiceResult<T>`. Conecta y resuelve los hallazgos **#1 (SDK)** y **#3 (ViewModel)**.

### Reparto de responsabilidades (quién hace qué)

```
Vista  ◀──  Controlador  ◀──  Servicio  ◀──  SDK (Refit)  ◀──  API
(muestra)   (orquesta)      (try/catch      (llama HTTP)
                            + mapper)
```

| Capa | Su ÚNICA responsabilidad | Qué NO hace |
|---|---|---|
| **SDK (Refit)** | Llamada HTTP → devuelve el DTO | No maneja errores ni mapea |
| **Servicio** | `try/catch` + logging + **mapper DTO→ViewModel** + envolver en `ServiceResult` | No sabe de HTTP ni de vistas |
| **Controlador** | Llamar al servicio y decidir la respuesta/vista | No tiene lógica ni try/catch |
| **ViewModel** | Transportar datos listos para mostrar | No tiene lógica |
| **Vista** | Solo pintar (`@model` + `@foreach`) | No calcula, no busca datos, no maneja errores |

**Recorrido de un dato (BD → pantalla):**

1. API → entidad → JSON
2. SDK (Refit) → deserializa a `CategoryDto`
3. Servicio → `try { MapToViewModel() }` / `catch → ServiceResult.Fail() + Log` → `ServiceResult<CategoryListViewModel>`
4. Controlador → `View(result.Data)` (server-render) **o** `Json(result)` (JS fetch)
5. Vista → `@foreach (Model.Categories)` → HTML

### `ServiceResult<T>` (versión pulida)

```csharp
public class ServiceResult<T>
{
    public T? Data { get; set; }                          // nullable
    public bool Success { get; set; }
    public bool Warning { get; set; }                     // OK pero sin datos / advertencia
    public string Message { get; set; } = string.Empty;   // mensaje principal (usuario)
    public List<string> Messages { get; set; } = new();   // varios (ej. validaciones)

    public static ServiceResult<T> Ok(T data)     => new() { Success = true, Data = data };
    public static ServiceResult<T> Warn(string m) => new() { Warning = true, Message = m };
    public static ServiceResult<T> Fail(string m) => new() { Success = false, Message = m };
}
```

### `BaseService` (centraliza try/catch — elimina boilerplate y el bug de copy/paste)

```csharp
public abstract class BaseService
{
    protected abstract string ServiceName { get; }

    protected async Task<ServiceResult<T>> ExecuteAsync<T>(
        Func<Task<ServiceResult<T>>> action,
        string apiErrorMessage,
        string webErrorMessage,
        [CallerMemberName] string method = "")     // rellena el nombre del método solo
    {
        try { return await action(); }
        catch (OperationCanceledException)          // cancelación NO es error
        {
            Log.Warning("{Service}.{Method} - cancelada.", ServiceName, method);
            return ServiceResult<T>.Fail("La operación fue cancelada.");
        }
        catch (Refit.ApiException ex)               // error del lado de la API
        {
            Log.Error("API - {Service}.{Method} falló. Status: {Status}. Detalle: {Detail}",
                ServiceName, method, ex.StatusCode, ex.Content ?? ex.Message);
            return ServiceResult<T>.Fail(apiErrorMessage);
        }
        catch (Exception ex)                        // error del lado del Web
        {
            Log.Error(ex, "WEB - {Service}.{Method} falló.", ServiceName, method);
            return ServiceResult<T>.Fail(webErrorMessage);
        }
    }
}
```

### Por qué centralizar el `try/catch` en `BaseService.ExecuteAsync`

**Problema:** sin centralizar, cada método del servicio repite el mismo `try/catch` (~15-20 líneas idénticas); solo cambia el bloque de "lógica feliz" en medio. Repetirlo en 30 métodos duplica código e invita a bugs de copy/paste (ej. un log que dice "Documentos" dentro de un método de "Puestos").

**Cómo funciona:** `ExecuteAsync` recibe la "lógica feliz" como parámetro (`Func<Task<ServiceResult<T>>> action`) y la ejecuta **dentro** de su try/catch (`return await action();`). Así el manejo de errores se escribe **una sola vez** y cada método concreto queda en ~5 líneas.

- **`Func<Task<ServiceResult<T>>>`** = "una función async que devuelve un `ServiceResult<T>`", pasada como dato (higher-order function).
- **`[CallerMemberName]`** = el compilador rellena solo el nombre del método que llamó → elimina de raíz el bug de copy/paste del nombre en los logs.

**Dónde vive:** `src/HelpDesk.Web/Services/BaseService.cs` (clase `abstract`). Cada servicio la hereda con `: BaseService` y rellena `protected override string ServiceName` con su nombre (para los logs). No se puede instanciar sola; solo sirve para heredar.

**Patrones que combina (nombres formales):**
- **Execute-Around Method** — envolver un bloque de código con comportamiento común (el `ExecuteAsync` con `Func`).
- **Template Method** — clase base abstracta que define el esqueleto y obliga a los hijos a rellenar huecos (`ServiceName`).
- **Result Pattern** — devolver un objeto de resultado (`ServiceResult<T>`) en vez de lanzar excepciones o devolver el dato pelón.

**Beneficios:** try/catch en un solo lugar (si mejora, todos los servicios lo reciben gratis); menos código por método; aplica **DRY** (Don't Repeat Yourself).

### Servicio concreto (`CategoriesService`)

```csharp
public class CategoriesService : BaseService
{
    private readonly ICategoriesApi _categoriesApi;   // el SDK, NO HttpClient
    protected override string ServiceName => nameof(CategoriesService);

    public CategoriesService(ICategoriesApi categoriesApi) => _categoriesApi = categoriesApi;

    public Task<ServiceResult<CategoryListViewModel>> GetAllCategories() =>
        ExecuteAsync(async () =>
        {
            var categories = await _categoriesApi.GetAllAsync();
            if (categories is null || categories.Count == 0)          // null-safe
                return ServiceResult<CategoryListViewModel>.Warn("Aún no hay categorías.");

            var vm = new CategoryListViewModel { Categories = categories.MapToViewModel() };
            return ServiceResult<CategoryListViewModel>.Ok(vm);
        },
        apiErrorMessage: ErrorMessages.ApiCategoriesListError,
        webErrorMessage: ErrorMessages.WebCategoriesListError);
}
```

### ViewModel + Mapper

```csharp
public class CategoryListViewModel { public List<CategoryViewModel> Categories { get; set; } = new(); }

public class CategoryViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PlatformName { get; set; } = "—";   // resuelto en el mapper, no en la vista
    public string Description { get; set; } = "—";
    public DateTime CreatedAt { get; set; }
}

public static class CategoryMappings
{
    public static List<CategoryViewModel> MapToViewModel(this List<CategoryDto> dtos) =>
        dtos.Select(d => new CategoryViewModel
        {
            Id = d.Id,
            Name = d.Name,
            Description = string.IsNullOrWhiteSpace(d.Description) ? "—" : d.Description,
            PlatformId = d.PlatformId,
            CreatedAt = d.CreatedAt
        }).ToList();
}
```

### Controlador delgado

```csharp
[HttpGet]
public async Task<IActionResult> Index()
{
    var result = await _categoriesService.GetAllCategories();
    if (!result.Success && !result.Warning) ViewBag.Error = result.Message;
    return View(result.Data);   // server-render: pasa el ViewModel a la vista
}
```

### Vista (server-render con `@model` + `@foreach`)

```razor
@model HelpDesk.Web.ViewModels.CategoryListViewModel

@if (ViewBag.Error != null) { <div class="alert alert-error show">@ViewBag.Error</div> }

<table class="table display shadow-sm mb-4 rounded" style="width:100%">
    <thead>
        <tr class="bg-primary text-white">
            <th class="text-center">Categoría</th>
            <th class="text-center">Plataforma</th>
            <th class="text-center">Descripción</th>
            <th class="text-center">Fecha</th>
            <th class="text-center" style="width:180px">Acciones</th>
        </tr>
    </thead>
    <tbody>
    @if (Model.Categories.Count == 0)
    {
        <tr><td colspan="5" class="text-center text-muted">Aún no hay categorías.</td></tr>
    }
    else
    {
        @foreach (var item in Model.Categories)
        {
            <tr>
                <td>@item.Name</td>
                <td class="text-center">@item.PlatformName</td>
                <td>@item.Description</td>
                <td>@item.CreatedAt.ToString("dd/MM/yyyy")</td>
                <td>
                    <div class="text-center">
                        <a class="fw-bold mx-2 tooltiplink-actions"
                           onclick="editCategory('@item.Id', '@item.Name', '@item.Description')"
                           data-title="Editar"><i class="fas fa-pencil text-orange"></i></a>
                        <a class="fw-bold mx-2 tooltiplink-actions btn-action"
                           onclick="deleteCategory('@item.Id')"
                           data-title="Eliminar"><i class="fas fa-trash text-red"></i></a>
                    </div>
                </td>
            </tr>
        }
    }
    </tbody>
</table>
```

### Registro en DI (`Program.cs`)

```csharp
builder.Services.AddHelpDeskSdk("https+http://api");     // registra ICategoriesApi (hallazgo #1)
builder.Services.AddScoped<CategoriesService>();
```

### Notas / decisiones

- **Server-render vs JS fetch:** con server-render, tras crear/editar/borrar hay que recargar (`location.reload()` o redirigir a `Index`), porque el HTML lo arma el servidor. Para paneles administrativos internos es lo más limpio (menos JS que mantener).
- **El `ServiceResult` no llega a la vista** en server-render: el controlador lo desenvuelve (`result.Data`) y pasa solo el ViewModel. El `ServiceResult` vive entre servicio y controlador.
- **Requiere migrar al SDK** (hallazgo #1) para poder capturar `Refit.ApiException`.
- **`Categories` es el slice de referencia**: replicar este patrón para Tickets, Platforms, Users.
