# Plan de Pull Requests — HelpDesk

Hoja de ruta de PRs para implementar las mejoras de [HALLAZGOS.md](HALLAZGOS.md) de forma incremental. Cada PR es **pequeño, enfocado y deja la app funcionando**, para que sea fácil de revisar y de revertir.

## Convenciones

- **Rama base:** `develop`. Cada PR sale de una rama propia y se mergea a `develop`.
- **Nombres de rama:** `feature/<área>-<qué>` · `fix/<qué>` · `chore/<qué>`.
- **Commits / títulos de PR:** [Conventional Commits](https://www.conventionalcommits.org) → `feat:`, `fix:`, `refactor:`, `chore:`, `docs:`.
- **Regla de oro:** un PR = un propósito. Si no lo puedes resumir en una línea, pártelo.

### Plantilla de descripción de PR

```markdown
## Qué hace
<una o dos líneas>

## Por qué
<hallazgo que resuelve, ej. HALLAZGOS.md #1>

## Cambios
- ...

## Cómo probar
- ...

## Fuera de alcance
- ...
```

---

## Resumen de PRs

| # | PR | Rama | Hallazgo | Depende de | Riesgo |
|---|---|---|---|---|---|
| 1 | Infraestructura del patrón de servicio | `feature/service-foundation` | #1 #5 | — | 🟢 |
| 2 | Categories → patrón completo (slice de referencia) | `feature/categories-slice` | #1 #3 #5 | PR 1 | 🟡 |
| 3 | Auth en la API + emisión de JWT | `feature/api-jwt-auth` | #4 | — | 🟡 |
| 4 | El Web envía el JWT (cookie claims + handler) | `feature/web-jwt-client` | #4 | PR 3 | 🟡 |
| 5 | Tickets → patrón de servicio | `feature/tickets-slice` | #1 #3 #5 | PR 2 | 🟢 |
| 6 | Platforms → patrón de servicio | `feature/platforms-slice` | #1 #3 #5 | PR 2 | 🟢 |
| 7 | Users → patrón de servicio | `feature/users-slice` | #1 #3 #5 | PR 2 | 🟢 |
| 8 | Layout compartido | `feature/shared-layout` | #2 | — | 🟢 |
| 9 | Limpieza de dependencias y warnings | `chore/deps-cleanup` | — | — | 🟢 |
| 10 | Consistencia CQRS + tests (opcional) | `refactor/api-cqrs` | — | — | 🟡 |

**Orden recomendado:** 1 → 2 → 3 → 4 → (5, 6, 7 en paralelo) → 8 → 9 → 10.

> **Nota de prioridad:** la seguridad (#4, PRs 3-4) es el hallazgo crítico. El orden de arriba hace primero la base del patrón (PR 1-2) porque es autocontenida y de menor riesgo, y sirve de plantilla. Si el entorno está expuesto con datos reales, **sube los PRs 3-4 al frente**.

---

## PR 1 — Infraestructura del patrón de servicio

**Rama:** `feature/service-foundation` · **Hallazgos:** #1, #5 · **Riesgo:** 🟢 bajo

**Objetivo:** sentar las bases reutilizables del patrón, sin cambiar comportamiento aún.

**Incluye:**
- `Common/ServiceResult.cs` — el envoltorio de resultado (`Ok`/`Warn`/`Fail`).
- `Services/BaseService.cs` — `ExecuteAsync` con try/catch centralizado (`Refit.ApiException`, `OperationCanceledException`, `Exception`) y `[CallerMemberName]`.
- `Common/ErrorMessages.cs` — mensajes de usuario centralizados.
- Conectar el SDK: `AddHelpDeskSdk("https+http://api")` en `HelpDesk.Web/Program.cs` + registro de los clientes Refit.
- Añadir Serilog al Web (si aún no está) para el logging del `BaseService`.

**No incluye:** ningún controlador migrado todavía (eso es PR 2+).

**Cómo probar:** la app compila y corre igual que antes (`aspire run`). No hay cambio visible; es andamiaje.

---

## PR 2 — Categories → patrón completo (slice de referencia)

**Rama:** `feature/categories-slice` · **Hallazgos:** #1, #3, #5 · **Depende de:** PR 1 · **Riesgo:** 🟡 medio

**Objetivo:** migrar Categories de punta a punta al patrón. Queda como **plantilla** para el resto.

**Incluye:**
- `ViewModels/CategoryListViewModel.cs` + `CategoryViewModel.cs`.
- `Mapping/CategoryMappings.cs` — `MapToViewModel` (DTO → ViewModel).
- `Services/CategoriesService.cs` — usa `ICategoriesApi` (SDK) + `BaseService`.
- Refactor de `CategoriesController` (Web): inyecta `CategoriesService`, quita el `IHttpClientFactory` crudo.
- Convertir `Views/Categories/Index.cshtml` a **server-render** (`@model` + `@foreach`).

**No incluye:** Tickets/Platforms/Users; el layout compartido.

**Cómo probar:** entrar a Categorías, ver la lista; crear/editar/borrar; verificar el mensaje de "Aún no hay categorías" cuando está vacío; forzar un error de API y ver el mensaje limpio.

**Referencia:** el código completo está en [HALLAZGOS.md #5](HALLAZGOS.md) y el flujo en `categories-flow.html`.

---

## PR 3 — Auth en la API + emisión de JWT

**Rama:** `feature/api-jwt-auth` · **Hallazgo:** #4 · **Riesgo:** 🟡 medio

**Objetivo:** mover la validación de credenciales a la API y que **emita** el JWT. Proteger los endpoints.

**Incluye:**
- `HelpDesk.API/Auth/JwtTokenService.cs` — genera y firma el token.
- `HelpDesk.API/Controllers/AuthController.cs` — `POST /api/v1/auth/login` (`[AllowAnonymous]`), valida con BCrypt.
- Config `Jwt` (Issuer/Audience) en appsettings + **clave en user-secrets** (nunca en el repo).
- `Program.cs` (API): `AddJwtBearer(...)` + `app.UseAuthentication()` (falta hoy) + `[Authorize]` en los controladores.
- Paquete `Microsoft.IdentityModel.JsonWebTokens` si hace falta.

**No incluye:** el lado Web (guardar/enviar el token) → PR 4.

**Cómo probar:** `curl` a `/api/v1/tickets` **sin token → 401**; login → recibe token; con `Authorization: Bearer <token>` → 200; token alterado/expirado → 401.

**Referencia:** [JWT.md](JWT.md) Piezas 1-4.

---

## PR 4 — El Web envía el JWT (cookie claims + handler)

**Rama:** `feature/web-jwt-client` · **Hallazgo:** #4 · **Depende de:** PR 3 · **Riesgo:** 🟡 medio

**Objetivo:** el Web consume el login de la API, guarda el JWT en la cookie y lo reenvía en cada petición.

**Incluye:**
- `Services/AuthenticationService.cs` — cookie claims con `new Claim("jwt", token)` + `IsPersistent = RememberMe`.
- Refactor de `AccountController` (Web): deja de tocar el `DbContext`; llama a `POST /api/v1/auth/login`.
- `Services/JwtHandler.cs` (`DelegatingHandler`) que añade `Authorization: Bearer` leyendo `User.FindFirst("jwt")`.
- Registro: `AddHttpContextAccessor()` + `.AddHttpMessageHandler<JwtHandler>()` en el cliente/SDK.

**No incluye:** refresh tokens (evaluar aparte).

**Cómo probar:** login desde el Web → entra al dashboard; las llamadas a la API funcionan con token; logout borra la cookie; sin login, la API rechaza.

**Referencia:** [JWT.md](JWT.md) Pieza 5 (login + handler).

---

## PR 5-7 — Replicar el patrón (Tickets, Platforms, Users)

**Ramas:** `feature/tickets-slice`, `feature/platforms-slice`, `feature/users-slice` · **Hallazgos:** #1, #3, #5 · **Depende de:** PR 2 · **Riesgo:** 🟢 bajo

**Objetivo:** aplicar el mismo patrón de Categories a cada módulo. Un PR por módulo para mantenerlos revisables.

**Incluye (por cada uno):** su `Service`, `ViewModel(s)`, `Mapper`, refactor del controlador Web y su vista a server-render.

**Cómo probar:** el CRUD de cada módulo sigue funcionando, ahora vía el patrón.

> Una vez hecha la plantilla (PR 2), estos son mecánicos: copiar la estructura y cambiar los tipos.

---

## PR 8 — Layout compartido

**Rama:** `feature/shared-layout` · **Hallazgo:** #2 · **Riesgo:** 🟢 bajo

**Objetivo:** eliminar el HTML duplicado en las vistas.

**Incluye:**
- `Views/Shared/_Layout.cshtml` (páginas internas) + `_AuthLayout.cshtml` (login/registro).
- `Views/_ViewStart.cshtml` → `Layout = "_Layout"`.
- Quitar `Layout = null` y el `<head>`/`<body>` repetido de cada vista, dejando solo su contenido.
- Reparar o borrar `Privacy` y `Error` (hoy generan HTML incompleto).

**Cómo probar:** todas las páginas se ven igual; cambiar el navbar en un solo archivo se refleja en todas.

---

## PR 9 — Limpieza de dependencias y warnings

**Rama:** `chore/deps-cleanup` · **Riesgo:** 🟢 bajo

**Objetivo:** quitar avisos del build.

**Incluye:**
- Quitar `MediatR.Extensions.Microsoft.DependencyInjection` (obsoleto; `AddMediatR` viene en el paquete principal).
- Actualizar paquetes con vulnerabilidad: `Microsoft.OpenApi`, `MessagePack` (transitivos).
- Corregir el `CS8604` (posible null) en `Dashboard.cshtml:35`.

**Cómo probar:** `dotnet build HelpDesk.slnx` con 0 warnings (o los mínimos justificados).

---

## PR 10 — Consistencia CQRS + tests (opcional)

**Rama:** `refactor/api-cqrs` · **Riesgo:** 🟡 medio

**Objetivo:** pagar la deuda del lado servidor.

**Incluye:**
- Unificar los controladores de la API: pasar todo por MediatR (o todo directo), no mezclar.
- Sacar los LINQ joins crudos de los controladores a la capa Application.
- Añadir tests de integración (hoy solo existe `GreetingTests`).

---

## Cómo trabajar cada PR

```bash
git switch develop && git pull
git switch -c feature/service-foundation      # rama del PR
# ... cambios ...
git add . && git commit -m "feat(web): base del patrón de servicio (ServiceResult, BaseService)"
git push -u origin feature/service-foundation
# abrir el PR en GitHub contra develop, usando la plantilla de arriba
```

Marca cada PR aquí conforme lo completes:

- [ ] PR 1 — Infraestructura del patrón de servicio
- [ ] PR 2 — Categories (slice de referencia)
- [ ] PR 3 — Auth en la API + JWT
- [ ] PR 4 — El Web envía el JWT
- [ ] PR 5 — Tickets
- [ ] PR 6 — Platforms
- [ ] PR 7 — Users
- [ ] PR 8 — Layout compartido
- [ ] PR 9 — Limpieza de dependencias
- [ ] PR 10 — CQRS + tests (opcional)
