# Implementación de JWT — HelpDesk

Guía dedicada para asegurar la API con **JWT (JSON Web Token)**. Corresponde al [HALLAZGOS.md #4](HALLAZGOS.md) (la API no tiene autorización). Los conceptos base están en [CONCEPTOS.md #9](CONCEPTOS.md).

> **Por qué un documento aparte:** implementar JWT no es un cambio puntual. Toca **5 piezas** en 3 proyectos (API, Web, configuración) y requiere decisiones de seguridad. Este archivo reúne todo para no perderse.

---

## Estado actual (punto de partida)

| Pieza | Estado hoy |
|---|---|
| Paquete `Microsoft.AspNetCore.Authentication.JwtBearer` | ✅ instalado en `HelpDesk.API` |
| Configuración del esquema JWT en la API | ❌ no existe |
| `app.UseAuthentication()` en la API | ❌ falta (solo hay `UseAuthorization()`) |
| `[Authorize]` en controladores de la API | ❌ ninguno |
| Endpoint que emite el token | ❌ no existe |
| Login | ⚠️ vive en el Web (`AccountController` → `DbContext` directo) |
| Hash de contraseñas | ✅ ya usa BCrypt |
| Auth del navegador ↔ Web | ✅ cookies (se mantiene) |

**Conclusión:** el paquete está pero nada está conectado. Hay que construir el flujo completo.

---

## Anatomía de un JWT

Un JWT son 3 partes separadas por puntos: `header.payload.firma`

```
eyJhbGc...  .  eyJzdWI...  .  dQw4w9WgXcQ
  HEADER         PAYLOAD        FIRMA
(algoritmo)   (los claims)   (garantiza que
                              nadie lo alteró)
```

- **Header:** qué algoritmo firma (ej. HS256).
- **Payload:** los *claims* (id, rol, email, expiración). **Va en Base64, NO cifrado** → cualquiera puede leerlo. Nunca metas secretos aquí.
- **Firma:** el servidor firma con una **clave secreta**. Si alguien cambia el payload (ej. su rol a "Supervisor"), la firma deja de coincidir y la API lo rechaza.

**La clave:** la API valida la firma con su clave secreta **sin consultar la base de datos**. Por eso es *stateless*.

---

## Las 5 piezas a implementar

```
┌─ Pieza 1 ─┐   ┌─ Pieza 2 ─┐   ┌─ Pieza 3 ─┐   ┌─ Pieza 4 ─┐   ┌─ Pieza 5 ─┐
  Mover auth      Generar         Configurar      Validar el      Web guarda
  a la API        el token        secretos        token (API)     y envía token
```

1. **Mover la autenticación a la API** — crear `AuthController` que valide credenciales y emita el JWT.
2. **Generar el token** — un servicio que arma el JWT con claims, clave, issuer, audience y expiración.
3. **Configurar los secretos** — sección `Jwt` en configuración, con la clave fuera de `appsettings.json`.
4. **Validar el token en la API** — registrar el esquema + `UseAuthentication()` + `[Authorize]`.
5. **El Web guarda y envía el token** — tras login, almacenarlo y adjuntarlo en cada llamada.

---

## Flujo completo del login (quién hace qué)

```
Navegador          Web (AccountController)        API (AuthController)         BD
   │                       │                            │                       │
   │ 1. email + password   │                            │                       │
   │──────────────────────▶│                            │                       │
   │                       │ 2. POST /api/v1/auth/login │                       │
   │                       │───────────────────────────▶│                       │
   │                       │                            │ 3. busca user +       │
   │                       │                            │    BCrypt.Verify()    │
   │                       │                            │──────────────────────▶│
   │                       │                            │◀──────────────────────│
   │                       │                            │ 4. GENERA el JWT      │
   │                       │  5. devuelve { token }     │    (firma con clave)  │
   │                       │◀───────────────────────────│                       │
   │                       │ 6. GUARDA el token         │                       │
   │                       │    (en la cookie)          │                       │
   │ 7. cookie de sesión   │                            │                       │
   │◀──────────────────────│                            │                       │
```

**Dos roles distintos — no confundirlos:**

| Acción | Quién | Dónde |
|---|---|---|
| Valida credenciales | La **API** | contra la BD (BCrypt) |
| **Genera / firma** el token | La **API** | `JwtTokenService` (con la clave secreta) |
| **Guarda** el token | El **Web** | dentro de la cookie de sesión |
| **Envía** el token en cada petición | El **Web** | vía `JwtHandler` |

> La API **crea** el token pero **no lo guarda** (es *stateless*, no recuerda nada). El que **guarda** el token es el **Web**, para reenviarlo en cada petición posterior (ver el diagrama del handler en la Pieza 5). El token se genera **una vez** en el login y se reutiliza hasta que expira.

---

## Pieza 1 + 2 — La API emite el token

### Paquete adicional

Para *generar* tokens (no solo validarlos) suele hacer falta:

```bash
dotnet add src/HelpDesk.API package Microsoft.IdentityModel.JsonWebTokens
```

### Servicio generador de tokens

`src/HelpDesk.API/Auth/JwtTokenService.cs`:

```csharp
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using HelpDesk.Domain.Users;

public class JwtTokenService
{
    private readonly IConfiguration _config;
    public JwtTokenService(IConfiguration config) => _config = config;

    public string GenerateToken(User user)
    {
        var jwt = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())   // Usuario / Soporte / Supervisor
        };

        var handler = new JsonWebTokenHandler();
        return handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(8),
            Issuer = jwt["Issuer"],
            Audience = jwt["Audience"],
            SigningCredentials = creds
        });
    }
}
```

### Endpoint de login en la API

`src/HelpDesk.API/Controllers/AuthController.cs`:

```csharp
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly HelpDeskDbContext _db;
    private readonly JwtTokenService _tokens;

    public AuthController(HelpDeskDbContext db, JwtTokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Correo o contraseña incorrectos." });

        var token = _tokens.GenerateToken(user);
        return Ok(new { token, user.FirstName, user.LastName, role = user.Role.ToString() });
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

> Esto **mueve** la lógica de login que hoy está en `HelpDesk.Web/AccountController` (el `// TODO`). El Web dejará de tocar el `DbContext` para autenticar.

---

## Pieza 3 — Configurar los secretos

### En `appsettings.json` (SOLO valores no secretos)

```json
{
  "Jwt": {
    "Issuer": "HelpDesk.API",
    "Audience": "HelpDesk.Web",
    "Key": ""
  }
}
```

### ⚠️ La clave NUNCA en `appsettings.json`

La `Key` firma todos los tokens: si se filtra, cualquiera puede fabricar tokens válidos y hacerse pasar por Supervisor. **No la pongas en el repositorio.** En desarrollo usa *user-secrets*:

```bash
dotnet user-secrets init --project src/HelpDesk.API
dotnet user-secrets set "Jwt:Key" "una-clave-larga-y-aleatoria-de-al-menos-32-caracteres" --project src/HelpDesk.API
```

En producción: variables de entorno o un gestor de secretos (Azure Key Vault, etc.). La clave debe ser **larga y aleatoria** (mínimo 32 bytes para HS256).

---

## Pieza 4 — La API valida el token

### En `HelpDesk.API/Program.cs`

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var jwt = builder.Configuration.GetSection("Jwt");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,          // rechaza tokens expirados
            ValidateIssuerSigningKey = true,  // valida la firma
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!))
        };
    });

builder.Services.AddScoped<JwtTokenService>();

// ... más abajo, el ORDEN importa:
app.UseAuthentication();   // ← AGREGAR (hoy falta). Va ANTES de UseAuthorization
app.UseAuthorization();
```

> **Orden crítico:** `UseAuthentication()` debe ir **antes** de `UseAuthorization()`. Primero se identifica *quién eres* (authentication), luego *qué puedes hacer* (authorization).

### Proteger los controladores

```csharp
[Authorize]                          // ← toda la API protegida por defecto
[ApiController]
[Route("api/v1/tickets")]
public class TicketsController : ControllerBase { ... }
```

Y el `AuthController` público (para poder loguearte sin token):

```csharp
[AllowAnonymous]
[HttpPost("login")]
public async Task<IActionResult> Login(...) { ... }
```

### Autorización por rol (bonus)

Como los claims incluyen el rol, puedes restringir por rol:

```csharp
[Authorize(Roles = "Supervisor")]           // solo Supervisor
public async Task<IActionResult> Delete(Guid id) { ... }
```

---

## Pieza 5 — El Web guarda y envía el token

### Al hacer login, el Web pide el token a la API

El `AccountController` del Web llama a `POST /api/v1/auth/login`, recibe el token y lo **guarda dentro de la cookie de sesión** (como un claim más, incluido el JWT).

**Enfoque elegido: cookie claims** (el token vive en la cookie cifrada del navegador). Se prefiere sobre guardarlo en `HttpContext.Session` porque **no requiere infraestructura extra** (Redis/SQL) y encaja con la cookie auth que el Web ya usa.

> **Cookie claims vs Session — cuándo usar cada uno:**
>
> | | Cookie claims (elegido) | Session (`HttpContext.Session`) |
> |---|---|---|
> | Dónde vive el token | Cookie cifrada, en el navegador | Servidor; el navegador solo tiene un ID |
> | Infraestructura extra | Ninguna | Redis/SQL distribuido en producción |
> | Estado en el Web | Stateless | Con estado |
> | Mejor para | JWT de vida corta (nuestro caso) | **RefreshTokens** (más seguros server-side) |
>
> Empezar con **cookie claims**. Migrar a **Session** solo si se agregan refresh tokens o se escala a múltiples instancias.

### Encapsular en un `AuthenticationService` (buena práctica)

En vez de poner `SignInAsync` directo en el controlador, se envuelve en un servicio dedicado (reutilizable, testeable, fuera del controlador). Versión con mejoras aplicadas:

```csharp
using Microsoft.AspNetCore.Authentication;   // evita el nombre completo verboso

public class AuthenticationService
{
    private readonly IHttpContextAccessor _ctx;
    private readonly IConfiguration _config;

    public AuthenticationService(IHttpContextAccessor ctx, IConfiguration config)
    {
        _ctx = ctx;
        _config = config;
    }

    public async Task GenerarClaimsAsync(UserClaimModel model)
    {
        var scheme = _config.GetSection("CookieSettings")["CookieName"]!;

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, model.UserName),
            new Claim(ClaimTypes.NameIdentifier, model.UserId.ToString()),
            new Claim(ClaimTypes.Role, model.Role),        // Usuario / Soporte / Supervisor
            new Claim("jwt", model.Token)                  // ← CLAVE: guarda el JWT en la cookie
        };

        var identity = new ClaimsIdentity(claims, scheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe               // ← respeta el checkbox "Recordarme"
        };

        if (_ctx.HttpContext is not null)
            await _ctx.HttpContext.SignInAsync(scheme, new ClaimsPrincipal(identity), authProperties);
    }

    public async Task SignOutAsync()
    {
        var scheme = _config.GetSection("CookieSettings")["CookieName"]!;
        // SignOutAsync borra la cookie: eso ES el logout. No hace falta remover claims a mano.
        if (_ctx.HttpContext is not null)
            await _ctx.HttpContext.SignOutAsync(scheme);
    }
}
```

**Mejoras aplicadas respecto al ejemplo original:**

| Original | Mejora | Por qué |
|---|---|---|
| Sin claim del token | `new Claim("jwt", model.Token)` | sin esto el `JwtHandler` no encuentra el token |
| `IsPersistent = false` fijo | `IsPersistent = model.RememberMe` | respeta el "Recordarme" del formulario |
| Bucle que remueve claims en `SignOut` | eliminado | era inútil; `SignOutAsync` ya borra la cookie |
| `Task<bool>` que siempre es `true` | `Task` a secas | el `bool` no significaba nada |
| Nombre completo `Microsoft.AspNetCore...` | `using` + `.SignInAsync(...)` | legibilidad |

El `model.Token` es el JWT que la API devolvió en el paso 5 del login. Así el token queda dentro de la cookie cifrada, listo para que el `JwtHandler` lo reenvíe.

### Adjuntar el token en CADA llamada a la API (DelegatingHandler)

En vez de añadir la cabecera a mano en cada petición, un `DelegatingHandler` la inyecta automáticamente. Es un **interceptor** que se mete en medio de cada petición que sale del `HttpClient`, antes de llegar a la red:

```
  Navegador                Web (servidor)                              API
     │                          │                                       │
     │  1. petición + cookie    │                                       │
     │─────────────────────────▶│                                       │
     │      (JWT dentro de       │                                       │
     │       la cookie)          │                                       │
     │                          │  2. tu código: client.GetAsync(...)   │
     │                          │            │                          │
     │                          │            ▼                          │
     │                    ┌─────────────────────────────┐               │
     │                    │      JwtHandler              │               │
     │                    │  a) lee "jwt" del usuario     │               │
     │                    │     (IHttpContextAccessor)    │               │
     │                    │  b) añade la cabecera:        │               │
     │                    │     Authorization: Bearer ... │               │
     │                    │  c) base.SendAsync()          │               │
     │                    └──────────────┬──────────────┘               │
     │                          │         │  3. petición CON token       │
     │                          │         │─────────────────────────────▶│
     │                          │         │                              │ 4. valida
     │                          │         │                              │    la firma
     │                          │         │◀─────────────────────────────│    [Authorize]
     │  6. respuesta            │◀────────┘  5. respuesta                 │
     │◀─────────────────────────│                                       │
```

**La idea:** tu código de negocio solo hace `client.GetAsync("/api/v1/tickets")`. El handler, en el paso 2, intercepta esa salida y le pega el token **sin que tú lo toques**. Cada usuario envía su propio token (el que guardó en su cookie al loguearse).

El código del handler:

```csharp
public class JwtHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _ctx;
    public JwtHandler(IHttpContextAccessor ctx) => _ctx = ctx;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = _ctx.HttpContext?.User.FindFirst("jwt")?.Value;
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new("Bearer", token);   // ← Authorization: Bearer <token>
        return await base.SendAsync(request, ct);
    }
}
```

Registro en `HelpDesk.Web/Program.cs`:

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<JwtHandler>();

builder.Services.AddHttpClient("api", c => c.BaseAddress = new Uri("https+http://api"))
    .AddHttpMessageHandler<JwtHandler>()   // ← engancha el handler
    .AddServiceDiscovery();
```

> **Cómo llega el token de la cookie a `User` (importante):** el handler **no lee la cookie a mano** — lee `User.FindFirst("jwt")`. El token pasa de la cookie a `User` automáticamente:
>
> ```
> cookie (con claim "jwt") → app.UseAuthentication() la descifra
>   → reconstruye User con todos sus claims → JwtHandler lee User.FindFirst("jwt")
>   → Authorization: Bearer → API
> ```
>
> Por eso `AuthenticationService.GenerarClaimsAsync` guardó `new Claim("jwt", token)`: para que el middleware lo reconstruya en `User` y el handler lo pueda leer. Se deja que `UseAuthentication()` descifre la cookie (más limpio y seguro que parsearla a mano).

> Si migras al SDK (hallazgo #1), el `AddHttpMessageHandler<JwtHandler>()` se engancha igual sobre el cliente Refit. Así **cada llamada del Web a la API lleva el token sin código repetido**.

---

## Checklist de implementación

- [ ] Pieza 1: `AuthController` en la API con `POST /api/v1/auth/login`
- [ ] Pieza 2: `JwtTokenService` que genera el token (paquete `Microsoft.IdentityModel.JsonWebTokens`)
- [ ] Pieza 3: sección `Jwt` + clave en user-secrets (NO en appsettings)
- [ ] Pieza 4: `AddJwtBearer(...)` + `app.UseAuthentication()` + `[Authorize]` en controladores
- [ ] Pieza 5: Web guarda el token y lo adjunta con `JwtHandler`
- [ ] Migrar el login del `AccountController` (Web) para que consuma la API en vez del `DbContext`
- [ ] Registrar `Registro`/logout equivalentes si se mueven a la API
- [ ] Probar: sin token → 401; con token válido → 200; token alterado → 401; token expirado → 401

---

## Mejores prácticas de seguridad

- **La clave secreta fuera del repo** (user-secrets / variables de entorno / Key Vault). Larga y aleatoria (≥32 bytes).
- **Siempre HTTPS** — el token viaja en la cabecera; sobre HTTP se puede interceptar.
- **Expiración corta** — 8h aquí como el cookie; entre más corta, menor la ventana si se roba.
- **Nunca datos sensibles en el payload** — es Base64, no cifrado. Solo id, rol, email.
- **Validar issuer, audience, lifetime y firma** — las 4 validaciones activas (no las desactives "para que funcione").
- **Considerar refresh tokens (avanzado, opcional)** — para renovar sin re-loguear. Añade complejidad; evaluar si se necesita.

---

## Recordatorio: coexisten DOS autenticaciones

| | Cookie (ya existe) | JWT (esta guía) |
|---|---|---|
| Entre quién | Navegador ↔ Web | Web ↔ API |
| Protege | Páginas MVC del Web | Endpoints de la API |
| Estado | Con estado (sesión) | Sin estado (stateless) |

No se contradicen: el navegador entra al Web con la **cookie**, y el Web (servidor) usa el **JWT** para hablar con la API. Ambas puertas deben estar cerradas.
