# Logging nativo de .NET y su conexión con Aspire — HelpDesk

Guía de cómo funciona el **logging** en este proyecto: el sistema nativo de .NET
(`Microsoft.Extensions.Logging`), cómo **Aspire** se engancha a él para mostrar los logs
en su dashboard, y cómo lo usa nuestra capa de servicio (`BaseService`). Complementa a
[CONCEPTOS.md](CONCEPTOS.md) y [HALLAZGOS.md](HALLAZGOS.md).

---

## Índice

1. [Qué es "logging" y por qué no usar `Console.WriteLine`](#1-qué-es-logging)
2. [El logging nativo de .NET: la tubería](#2-el-logging-nativo-de-net-la-tubería)
3. [Las piezas: `ILogger`, provider, categoría, nivel](#3-las-piezas)
4. [Structured logging (logs con estructura, no texto plano)](#4-structured-logging)
5. [Cómo se engancha Aspire (OpenTelemetry)](#5-cómo-se-engancha-aspire)
6. [El recorrido completo de un log (del `BaseService` al dashboard)](#6-el-recorrido-completo)
7. [Cómo aplica en HelpDesk](#7-cómo-aplica-en-helpdesk)
8. [Por qué esto en vez de Serilog](#8-por-qué-esto-en-vez-de-serilog)
9. [Resumen](#9-resumen)

---

## 1. Qué es logging

**Logging** = dejar un rastro escrito de lo que pasa en la app mientras corre:
"se cargó la lista de categorías", "falló la llamada a la API con status 500", etc.
Sirve para diagnosticar problemas sin un debugger, sobre todo en producción.

**¿Por qué no `Console.WriteLine`?** Porque `Console.WriteLine`:

- Siempre escribe (no puedes bajar/subir el detalle según el entorno).
- Solo va a un sitio (la consola), no a un archivo, una base de datos o un dashboard.
- No lleva metadata (nivel de gravedad, categoría, marca de tiempo, datos estructurados).

El logging nativo de .NET resuelve las tres cosas.

---

## 2. El logging nativo de .NET: la tubería

.NET trae de fábrica un sistema de logging: **`Microsoft.Extensions.Logging`**.
Su idea central es una **tubería con múltiples salidas**:

```
Tu código escribe UN mensaje
        │
        ▼
┌───────────────────────┐
│  Tubería MS.Logging   │   (decide nivel, categoría, formato)
└───────────────────────┘
        │  reparte el MISMO mensaje a cada salida ("provider")
        ├──────────────► Provider Consola      → lo ves en la terminal
        ├──────────────► Provider Debug         → ventana de depuración
        └──────────────► Provider OpenTelemetry → dashboard de Aspire
```

**La clave:** tú escribes el log **una sola vez** y la tubería lo reparte a **todos** los destinos conectados (los *providers*). No repites código por cada destino. Añadir o quitar un destino es enchufar/desenchufar un provider, sin tocar el código que loguea.

Esto es lo que hace posible que **el mismo `_logger.LogError(...)`** de nuestro `BaseService` aparezca a la vez en la consola **y** en el dashboard de Aspire.

---

## 3. Las piezas

| Pieza | Qué es | Analogía |
|---|---|---|
| **`ILogger`** | El objeto con el que **escribes** logs (`LogInformation`, `LogWarning`, `LogError`). | El micrófono por el que hablas. |
| **`ILoggerProvider`** | Un **destino** de salida (consola, archivo, OTel). | Cada altavoz conectado. |
| **`ILoggerFactory`** | Fábrica que crea los `ILogger` y conoce todos los providers. | La central que conecta micrófono con altavoces. |
| **Categoría** | Etiqueta que dice **quién** logueó (normalmente el nombre de la clase). | El nombre de quien habla. |
| **Nivel** | Gravedad del mensaje. | El tono/urgencia. |

### Niveles (de menos a más grave)

| Nivel | Cuándo | Ejemplo |
|---|---|---|
| `Trace` / `Debug` | Detalle fino, solo en desarrollo | "entrando al método X con id=..." |
| `Information` | Eventos normales del flujo | "usuario inició sesión" |
| `Warning` | Algo raro pero no roto | "la lista vino vacía"; **una cancelación** |
| `Error` | Falló una operación | "la API devolvió 500" |
| `Critical` | La app entera está en peligro | "no hay conexión a la BD" |

Los niveles se filtran por configuración (`appsettings.json` → sección `Logging`), así en
producción puedes mostrar solo `Warning` para arriba y en desarrollo bajar a `Debug`, **sin
tocar el código**.

### La categoría viene del tipo genérico `ILogger<T>`

Cuando inyectas `ILogger<CategoriesService>`, la **categoría** del log es automáticamente
`HelpDesk.Web.Services.CategoriesService`. Así, al leer el log, sabes exactamente qué clase
lo emitió sin escribirlo a mano. Por eso se inyecta `ILogger<TClaseConcreta>`.

---

## 4. Structured logging

El logging de .NET no guarda solo **texto**, guarda **datos con estructura**. Fíjate en los
`{Placeholders}` con nombre:

```csharp
_logger.LogError("API - {Service}.{Method} falló. Status: {Status}",
    ServiceName, method, ex.StatusCode);
```

Esto **no** es interpolación de strings (`$"..."`). Los `{Service}`, `{Method}`, `{Status}`
son **campos con nombre**. La tubería guarda:

- El **texto** legible: `"API - CategoriesService.GetAll falló. Status: 500"`.
- Y por separado los **campos**: `Service=CategoriesService`, `Method=GetAll`, `Status=500`.

**Por qué importa:** en el dashboard de Aspire (o en cualquier sistema que reciba los logs)
puedes **filtrar y buscar por esos campos** — "muéstrame todos los errores donde
`Status = 500`" — cosa imposible si fuera solo texto plano.

> Regla: usa `{PlaceholdersConNombre}` y pasa los valores como argumentos.
> **Nunca** metas los valores con `$"..."` dentro del mensaje, porque pierdes la estructura.

---

## 5. Cómo se engancha Aspire

Aspire no reinventa el logging: **se enchufa a la tubería nativa** como un provider más.
Lo hace en el proyecto compartido `HelpDesk.ServiceDefaults`, que **todos** los servicios
(API y Web) referencian.

En `src/HelpDesk.ServiceDefaults/Extensions.cs`:

```csharp
public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) ...
{
    builder.Logging.AddOpenTelemetry(logging =>   // ← engancha el provider de OTel a la tubería
    {
        logging.IncludeFormattedMessage = true;
        logging.IncludeScopes = true;
    });
    // ... también métricas y trazas ...
}
```

Y esto se activa con la línea que ya está en `Program.cs` (tanto de la API como del Web):

```csharp
builder.AddServiceDefaults();   // dentro llama a ConfigureOpenTelemetry()
```

### ¿Qué es OpenTelemetry (OTel)?

Es un **estándar** para exportar telemetría (logs, métricas y trazas). El provider
`AddOpenTelemetry` recoge cada log que pasa por la tubería y lo **envía por la red** al
recolector que Aspire levanta. El **dashboard de Aspire** es quien recibe y muestra todo eso.

```
App (Web/API)  ──OTLP──►  Recolector de Aspire  ──►  Dashboard de Aspire
   (provider OTel)          (lo levanta AppHost)       (lo que ves en el navegador)
```

**Conclusión clave:** como el provider de OTel ya está enchufado a la tubería por
`AddServiceDefaults()`, **cualquier** cosa que logueemos con `ILogger` llega al dashboard
**sin configuración adicional**. Ese es el gran motivo para usar el logging nativo.

---

## 6. El recorrido completo

Un error dentro de la capa de servicio, de principio a fin:

```
1. CategoriesService (hijo de BaseService) llama a la API vía el SDK y algo falla.

2. BaseService.ExecuteAsync atrapa la excepción y escribe:
       _logger.LogError("API - {Service}.{Method} falló. Status: {Status}",
                        ServiceName, method, ex.StatusCode);
   (ese _logger es un ILogger<CategoriesService>, inyectado por DI)

3. El mensaje entra a la TUBERÍA de Microsoft.Extensions.Logging.

4. La tubería lo reparte a TODOS los providers enchufados:
       ├─► Provider Consola      → aparece en la terminal
       └─► Provider OpenTelemetry → lo manda al recolector de Aspire

5. El dashboard de Aspire lo muestra, con sus campos estructurados
   (Service, Method, Status) filtrables.

   → Y el USUARIO nunca ve nada de esto: ExecuteAsync le devuelve un
     ServiceResult.Fail(mensajeLimpio). El detalle técnico vive solo en el log.
```

Esa última línea es el principio de oro: **el detalle técnico va al log; al usuario solo un
mensaje limpio** (ver [CONCEPTOS.md §6](CONCEPTOS.md) y [HALLAZGOS.md #5](HALLAZGOS.md)).

---

## 7. Cómo aplica en HelpDesk

### `BaseService` recibe un `ILogger` por constructor

En `src/HelpDesk.Web/Services/BaseService.cs`:

```csharp
public abstract class BaseService
{
    private readonly ILogger _logger;

    protected BaseService(ILogger logger) => _logger = logger;

    protected abstract string ServiceName { get; }

    protected async Task<ServiceResult<T>> ExecuteAsync<T>(...)
    {
        try { return await action(); }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("{Service}.{Method} - operación cancelada.", ServiceName, method);
            return ServiceResult<T>.Fail(ErrorMessages.OperationCancelled);
        }
        catch (ApiException ex)
        {
            _logger.LogError("API - {Service}.{Method} falló. Status: {Status}. Detalle: {Detail}",
                ServiceName, method, ex.StatusCode, ex.Content ?? ex.Message);
            return ServiceResult<T>.Fail(apiErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WEB - {Service}.{Method} falló.", ServiceName, method);
            return ServiceResult<T>.Fail(webErrorMessage);
        }
    }
}
```

### Los servicios concretos inyectan `ILogger<TSuClase>` y lo pasan a la base

Así será cada servicio a partir de PR 2 (ejemplo de Categories):

```csharp
public class CategoriesService : BaseService
{
    private readonly ICategoriesApi _categoriesApi;
    protected override string ServiceName => nameof(CategoriesService);

    public CategoriesService(ICategoriesApi categoriesApi, ILogger<CategoriesService> logger)
        : base(logger)                       // ← pasa su logger a BaseService
    {
        _categoriesApi = categoriesApi;
    }
}
```

**El contenedor de DI** crea automáticamente ese `ILogger<CategoriesService>` (no lo registras
tú a mano) y le pone la categoría correcta. `BaseService` solo lo guarda y lo usa.

> Nota sobre `ServiceName`: la categoría del `ILogger<CategoriesService>` **ya** identifica la
> clase, pero mantenemos `ServiceName` en el texto del mensaje para que sea legible de un
> vistazo y por consistencia con el patrón (ver [design-patterns](.claude/skills/design-patterns)).

### Configuración de niveles

En `src/HelpDesk.Web/appsettings.json`, la sección estándar `Logging` controla el detalle:

```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning"
  }
}
```

No hace falta nada más: no hay paquetes extra ni bootstrap en `Program.cs`. Todo el cableado
(la tubería + el provider de OTel) lo pone `AddServiceDefaults()`.

---

## 8. Por qué esto en vez de Serilog

Durante PR 1 se evaluó usar **Serilog** (una librería de logging externa muy popular). Se
descartó a favor del logging nativo por estas razones:

| | Logging nativo (`ILogger`) | Serilog |
|---|---|---|
| Integración con Aspire | **Automática** (OTel ya está en la tubería) | Hay que puentearla a mano (`writeToProviders: true`) |
| Dependencias extra | Ninguna | Un paquete NuGet más |
| Config en `Program.cs` | Cero | Bootstrap del logger + flush al cerrar |
| Testeable / inyectable | Sí (`ILogger<T>` por DI) | El `Log.` estático es global, más difícil de aislar |
| Formato de salida bonito | El estándar | Más plantillas y sinks |

Para este proyecto —que **ya** usa Aspire y su telemetría OTel— el logging nativo da la
integración gratis, sin dependencias ni configuración, y encaja con la inyección de
dependencias del resto del código. Serilog aportaría sobre todo formato/sinks que aquí no
necesitamos.

> Si algún día se quisiera Serilog **y** que sus logs llegaran a Aspire, la vía es
> `builder.Services.AddSerilog(..., writeToProviders: true)` para reenviar sus eventos a los
> providers de la tubería (incluido el de OTel). No es el camino elegido hoy.

---

## 9. Resumen

- .NET tiene un logging nativo (`Microsoft.Extensions.Logging`) que funciona como una
  **tubería con múltiples salidas** (providers): escribes una vez, se reparte a todos.
- Escribes con **`ILogger`**; inyectas **`ILogger<TuClase>`** para que la **categoría** sea
  el nombre de la clase.
- Usa **structured logging** (`{CamposConNombre}`), no interpolación, para poder filtrar.
- **Aspire** se engancha como un provider más (**OpenTelemetry**) dentro de
  `AddServiceDefaults()`. Por eso todo lo que logueas con `ILogger` **aparece en su dashboard
  sin configurar nada**.
- En HelpDesk, **`BaseService`** recibe un `ILogger`, cada servicio concreto pasa su
  `ILogger<TSuClase>`, y el usuario solo ve un `ServiceResult` limpio mientras el detalle
  técnico queda en el log → dashboard de Aspire.
