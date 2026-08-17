# Guía HelpDesk

## Paso 1 — Conectar el SDK

El **SDK** es un cliente *tipado* para hablar con la API **sin escribir HTTP a mano**. En vez de
armar URLs y leer JSON manualmente, llamas un método de C# (`GetAllAsync()`) y te devuelve objetos.
Usamos [Refit](https://github.com/reactiveui/refit), que genera ese cliente a partir de una interfaz.

**Qué mirar:**

- La interfaz que describe los endpoints (un método por endpoint, con su ruta):
  📄 `src/HelpDesk.SDK/Categories/ICategoriesApi.cs`
  Debajo, en el mismo archivo, están los DTOs que viajan por HTTP (`CategoryDto`, `CategoryRequestDto`).

- Dónde se **registra** el cliente para poder inyectarlo:
  📄 `src/HelpDesk.SDK/DependencyInjection.cs:21`

```csharp
// ⚠️ Usa AddRefitGeneratedClient, NO AddRefitClient (ese usa reflection y truena en runtime).
services.AddRefitGeneratedClient<ICategoriesApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseUrl));
```

- Dónde el Web enciende el SDK (una sola línea; Aspire resuelve el host `api`):
  📄 `src/HelpDesk.Web/Program.cs:36` → `builder.Services.AddHelpDeskSdk("https+http://api");`

**Por qué así:** la URL vive en un solo lugar, trabajas con tipos (no strings sueltos que un typo
rompe en runtime) y, si la API responde un error, Refit lanza una excepción que **sí podemos
capturar** (lo verás en el Paso 2).

**Para tu módulo nuevo:** crea `I<Modulo>Api.cs` copiando el de Categories y añade una línea
`AddRefitGeneratedClient<I<Modulo>Api>()` junto a la de la línea 21.

---

## Paso 2 — El Web

### 2.1 El controller es un ORQUESTADOR (no hace el trabajo, lo reparte)

Antes de escribir nada, la idea más importante:

> **El controller NO debe tener lógica, NI hacer llamadas a la base de datos, NI hablar HTTP a
> mano.** Su único trabajo es: llamar al servicio y elegir qué responder (una vista o un status).

Mira lo delgado que es el de Categories:
📄 `src/HelpDesk.Web/Controllers/CategoriesController.cs:19-28` (la acción `Index`)

- Línea **21** → llama al servicio: `var result = await _categoriesService.GetAllCategories();`
- Línea **23** → decide según el resultado (si falló, pasa el mensaje a la vista).
- Línea **27** → devuelve la vista con los datos.

Fíjate en lo que **NO** hay ahí: ni `try/catch`, ni `using HelpDesk.SDK`, ni `HttpClient`, ni
acceso a datos. Todo eso lo hace el servicio. El controller se lee en 5 segundos.

> Cómprobación rápida: si tu controller importa `HelpDesk.SDK` o tiene un `try/catch`, algo se
> te coló que debería estar en el servicio.

### 2.2 ¿Por qué servicios, viewmodels y mappers? (el reparto de trabajo)

Repartimos el trabajo en piezas pequeñas para que **cada cosa falle o cambie en un solo lugar**.
Esto es lo que hace cada una y dónde verla:

| Pieza | Su única responsabilidad | Archivo de referencia |
|---|---|---|
| **Servicio** | Encapsula `try/catch` + logging + mapeo + decide éxito/vacío/error | 📄 `Web/Services/CategoriesService.cs` |
| **`BaseService`** | Escribe el `try/catch` **una sola vez** para todos los servicios | 📄 `Web/Services/BaseService.cs:34` |
| **`ServiceResult<T>`** | Distingue **éxito / vacío / error** (un `500` mudo no puede) | 📄 `Web/Common/ServiceResult.cs:28` |
| **Mapper** | Traduce entre los tipos del SDK y los del Web | 📄 `Web/Mapping/CategoryMappings.cs:19` |
| **ViewModel** | Lleva los datos **ya listos** para que la vista solo los pinte | 📄 `Web/ViewModels/Category/` |
| **ErrorMessages** | Textos limpios para el usuario (el detalle técnico va al log) | 📄 `Web/Common/ErrorMessages.cs:16` |

**El servicio, en concreto.** Cada método envuelve su "lógica feliz" en `ExecuteAsync`, que vive
en `BaseService` y ya trae el `try/catch` + logging. Así **ningún método repite manejo de errores**.
Mira `GetAllCategories`: 📄 `src/HelpDesk.Web/Services/CategoriesService.cs:33`

- El `try/catch` común está una sola vez en 📄 `Web/Services/BaseService.cs:34`
  (captura errores de la API en la línea **49** y del Web en la **55**, con logging distinto).

**Por qué `ServiceResult<T>` y no devolver el dato pelón.** Necesitamos diferenciar tres cosas
que se ven distintas para el usuario:
📄 `src/HelpDesk.Web/Common/ServiceResult.cs` → `Ok` (línea 28), `Warn` (31), `Fail` (34).
Así el controller sabe si mostrar los datos, un "aún no hay nada" o un mensaje de error — y el
usuario **nunca ve un stack trace**.

**Por qué un mapper.** Lo que llega de la API (`CategoryDto`) no es lo que la vista quiere pintar.
Ejemplo: el DTO trae `PlatformId` (un Guid), pero la pantalla quiere el **nombre** de la
plataforma. El mapper resuelve eso **una vez**, en el servicio, para que la vista no calcule nada:
📄 `Web/Mapping/CategoryMappings.cs:19` (DTO→ViewModel), `:38` (Form→Request), `:47` (DTO→Form).

**Por qué un ViewModel.** Es un objeto con los datos **ya cocinados** (nombre de plataforma
resuelto, descripción normalizada…). La vista solo recorre y pinta.
📄 `Web/ViewModels/Category/CategoryViewModel.cs`.

**Registrar el servicio** para poder inyectarlo en el controller:
📄 `src/HelpDesk.Web/Program.cs:39` → `AddScoped<...CategoriesService>();`

### 2.3 Pasos para tu módulo nuevo (en este orden)

1. **ViewModels** en `Web/ViewModels/<Modulo>/` — copia los 3 de Categories (`List`, `View`, `Form`).
2. **Mapper** en `Web/Mapping/<Modulo>Mappings.cs`.
3. **Servicio** en `Web/Services/<Modulo>Service.cs` — hereda de `BaseService`, inyecta el/los
   `I<Modulo>Api` del SDK + `ILogger`, y envuelve cada método en `ExecuteAsync`.
4. **Mensajes** en `Web/Common/ErrorMessages.cs` — un par `Api.../Web...` por operación.
5. **Registro** en `Web/Program.cs` junto a la línea 39.
6. **Controller** — delgado, inyecta solo el servicio.
7. **Vista** — ver Paso 3.

---

## Paso 3 — La API (backend)

Esta es la otra mitad: cómo la petición del Web, viajando por el SDK, termina hablando con la base
de datos. Si tu módulo ya tiene sus endpoints (como Categories), aquí ves cómo están hechos por
dentro; si aún no existen, es la receta para crearlos.

La API está en capas (**Clean Architecture**) y la dependencia apunta siempre hacia adentro:
`Domain ← Application ← Infrastructure ← API`. Y usa **CQRS con MediatR**: cada operación es un
mensaje (`Command` para escribir, `Query` para leer) que atiende un `Handler`.

### 3.1 El controller de la API también es un orquestador

Igual que en el Web: **no tiene lógica ni toca la base de datos.** Solo recibe la petición, la
manda a **MediatR** y traduce lo que vuelve a un status HTTP.

Mira lo delgado que es: 📄 `src/HelpDesk.API/Controllers/CategoriesController.cs`
- Línea **12/14** → lo único que inyecta es `IMediator`.
- Línea **18** → `GetAll`: manda un `Query` y responde `Ok(...)`.
- Línea **24** → `GetById`: si no existe, `NotFound()`; si existe, `Ok(...)`.
- Línea **31** → `Create`: traduce el Request a un `Command`, lo manda y responde `201 Created`.
- Línea **45** → `Delete`: `NoContent()` o `NotFound()` según el resultado.

Lo que **NO** hay: ni `try/catch`, ni EF, ni reglas de negocio. Su trabajo es enrutar y elegir el
código de estado.

### 3.2 ¿Por qué MediatR, Application y Repository? (quién hace qué)

| Pieza | Su única responsabilidad | Archivo de referencia |
|---|---|---|
| **Contrato de entrada** | Recibe el JSON (`CategoryRequest`) y lo traduce a un `Command` | 📄 `API/Mapping/CategoryRequestMappings.cs:6` y `:16` |
| **MediatR** | El "cartero": lleva el `Command`/`Query` al `Handler` correcto | registrado en 📄 `Application/DependencyInjection.cs:11` |
| **Command / Query + Handler** | Un **caso de uso por archivo** (escribir vs leer) | 📄 `Application/Categories/` |
| **Reglas de negocio** | Viven en el `Handler`, no en el controller | 📄 `Application/Categories/CreateCategoryCommand.cs:18` |
| **Errores de negocio → HTTP** | Traduce una excepción de dominio a status (duplicado → 409) | 📄 `API/GlobalExceptionHandler.cs` |
| **DTO de salida + mapper** | Convierte la entidad en el objeto que responde la API | 📄 `Application/Categories/CategoryDto.cs`, `CategoryMappings.cs` |
| **Repository** | Aísla el acceso a datos (filtra `!IsDeleted`, borrado suave) | 📄 `Infrastructure/Repositories/CategoryRepository.cs:14` |
| **Entidad (Domain)** | El modelo del negocio, sin dependencias | 📄 `Domain/Categories/Category.cs` |

**Por qué MediatR / CQRS.** El controller no sabe *cómo* se crea una categoría; solo dice "manda
este `CreateCategoryCommand`" y MediatR encuentra el handler que lo atiende. Cada operación en su
propio archivo (`CreateCategoryCommand.cs`, `GetAllCategoriesQuery.cs`…) = fácil de encontrar,
leer y probar por separado. **MediatR ya está registrado y escanea el assembly**, así que los
handlers se descubren solos (no los registras a mano).

**Por qué la capa Application (los handlers).** Las **reglas de negocio** viven aquí, no en el
controller ni en la vista. Ejemplo: no permitir dos categorías con el mismo nombre — el handler lo
valida y lanza `ConflictException` (📄 `CreateCategoryCommand.cs:18`), que el `GlobalExceptionHandler`
convierte en un **409**. El controller ni se entera.

**Por qué el Repository.** Los handlers **no tocan Entity Framework directamente**: hablan con una
interfaz (`ICategoryRepository`, en Domain) cuya implementación vive en Infrastructure. Así el
acceso a datos está en un solo lugar — incluido el filtro `!IsDeleted` y el **borrado suave**
(📄 `CategoryRepository.cs:37`) — y se puede cambiar o mockear sin tocar la lógica de negocio.

### 3.3 Pasos para tu módulo nuevo (orden backend: de adentro hacia afuera)

1. **Domain** — la entidad (hereda de `BaseEntity`, que ya pone `Id`/`CreatedAt`) y la interfaz
   `I<Modulo>Repository`. 📄 `Domain/Categories/Category.cs`, `Domain/Interfaces/ICategoryRepository.cs`.
2. **Application** — el `DTO` (un `record`), el mapper `ToDto`, y **un archivo por caso de uso**
   (Command/Query + su Handler). 📄 `Application/Categories/`.
3. **Infrastructure** — implementa el repositorio (filtra `!IsDeleted`, borrado suave), configura
   el modelo en `HelpDeskDbContext.OnModelCreating` (claves e índices únicos) y **registra el repo**
   en 📄 `Infrastructure/DependencyInjection.cs:14`. Luego crea la migración:
   ```bash
   dotnet ef migrations add Add<Modulo> --project src/HelpDesk.Infrastructure --startup-project src/HelpDesk.API
   ```
   (No hace falta `database update`: la API corre las migraciones al arrancar.)
4. **API** — el contrato de entrada `<Modulo>Request` + su mapping a los Commands
   (📄 `API/Mapping/CategoryRequestMappings.cs`), y el **controller delgado** con solo `IMediator`.
5. **Pruébalo en Swagger** (servicio `api` con Aspire) antes de pasar al Web.

---

## Paso 4 — La vista y el layout compartido

La vista **solo pinta** (`@model` + `@foreach`); no busca datos ni calcula nada.
📄 `src/HelpDesk.Web/Views/Categories/Index.cshtml` (mira la línea 1: `@model`, la 3: el título,
y la 96: el `@section Scripts` donde va el JS del modal).

Todas las pantallas comparten un mismo "cascarón" para no repetir el `<head>` ni el menú:

- **`_ViewStart.cshtml`** activa el layout para todas las vistas:
  📄 `src/HelpDesk.Web/Views/_ViewStart.cshtml:2` → `Layout = "_Layout";`
- **`_Layout.cshtml`** es el cascarón: `<head>`, incrusta el menú como **partial**, y deja huecos:
  📄 `Views/Shared/_Layout.cshtml:13` (`<partial name="_Sidebar" />`), `:16` (`@RenderBody()`),
  `:24` (`RenderSectionAsync("Scripts")`).
- **`_Sidebar.cshtml`** es la PartialView del menú (un fragmento reutilizable):
  📄 `Views/Shared/_Sidebar.cshtml` — para que tu módulo salga en el menú, añade un `<a>` como el
  de Categorías (línea 42).

**Para tu vista nueva:** no escribas `<html>` ni `<head>` ni el menú. Solo pon
`@{ ViewData["Title"] = "Mi módulo"; }` arriba, escribe el contenido, y el JS en `@section Scripts`.

---

## Antes de decir "listo"

**Checklist rápido**

Backend (API):
- [ ] El controller de la API solo inyecta `IMediator`; sin `try/catch`, sin EF, sin reglas de negocio.
- [ ] Un archivo por caso de uso en `Application/<Modulo>/` (Command/Query + su Handler).
- [ ] Las reglas de negocio están en el handler (no en el controller).
- [ ] Los handlers usan `I<Modulo>Repository`, no EF directamente; el repo filtra `!IsDeleted`.
- [ ] Repositorio registrado en `Infrastructure/DependencyInjection.cs` y migración creada.

Frontend (Web):
- [ ] El controller del Web **no** importa `HelpDesk.SDK` ni tiene `try/catch`.
- [ ] Ningún método del servicio repite `try/catch` (lo pone `ExecuteAsync`).
- [ ] La vista solo pinta; no hace `fetch` para *cargar* la lista.
- [ ] Servicio registrado en `Web/Program.cs`; ítem de menú añadido en `_Sidebar.cshtml`.

