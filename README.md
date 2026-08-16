# Help Desk

A ticketing system built on ASP.NET Core 10, Entity Framework Core, and PostgreSQL, orchestrated locally with .NET Aspire.

> **Warning**
> This repository is **scaffolding only**. No tickets, comments, categories, users, or persistence have been implemented yet. The single working endpoint (`GET /api/v1/greetings`) exists to demonstrate the Clean Architecture layering and to prove the Aspire orchestration boots end-to-end. Treat everything else as a place to grow into — see [What's next](#whats-next).

## Tech stack

| Layer | Choice | Notes |
|---|---|---|
| Runtime | ASP.NET Core 10 (TFM `net10.0`) | `global.json` pins the SDK floor. Preview SDKs roll forward. |
| Language | C# (latest from the .NET 10 SDK) | `Nullable` and `ImplicitUsings` on. |
| ORM | Entity Framework Core 10 + Npgsql provider | Snake-case naming via `EFCore.NamingConventions`. |
| Database | PostgreSQL 16+ | Provisioned by Aspire as a container with a persistent data volume. |
| Auth (identity) | `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Package installed in `HelpDesk.Infrastructure`; no `ApplicationUser` or `DbContext` wired yet. |
| Auth (tokens) | `Microsoft.AspNetCore.Authentication.JwtBearer` | Package installed in `HelpDesk.API`; no scheme registered yet. |
| Web UI | ASP.NET MVC + Bootstrap 5 | Default MVC template, untouched. |
| Validation | FluentValidation 12 (+ DI extensions) | `AddApplication()` scans the Application assembly for validators. |
| Typed HTTP client | Refit 10 (+ `Refit.HttpClientFactory`) | Package installed in `HelpDesk.SDK`; no interfaces yet. |
| Testing | xUnit | One test class today: `GreetingTests`. |
| Local orchestration | .NET Aspire 13 | `HelpDesk.AppHost` wires Postgres, the API, and the Web app. |

## Repository layout

Nine .NET projects under `src/` and `tests/`. Each project's responsibility is fixed; the dependency direction below is enforced by project references.

```
help-desk/
  HelpDesk.slnx
  global.json
  README.md
  src/
    HelpDesk.Domain/            class library  - entities, value objects, business rules
    HelpDesk.Application/       class library  - use cases, service interfaces, DTOs
    HelpDesk.Infrastructure/    class library  - EF Core, Identity persistence (future)
    HelpDesk.API/               web api        - controllers, JWT, middleware
    HelpDesk.SDK/               class library  - Refit interfaces for the API (future)
    HelpDesk.Web/               mvc            - server-rendered UI, consumes the SDK
    HelpDesk.AppHost/           aspire host    - local orchestration
    HelpDesk.ServiceDefaults/   aspire shared  - OpenTelemetry, health checks, discovery
  tests/
    HelpDesk.Domain.Tests/      xunit          - unit tests for the Domain layer
```

### What each project owns today

| Project | Today | Future home for |
|---|---|---|
| `HelpDesk.Domain` | `Greeting` record with a `Create` factory that rejects blank messages. | `Ticket`, `Comment`, `Category`, `Organization`, status enums, business invariants. |
| `HelpDesk.Application` | `IGreetingService`, `GreetingService`, `GreetingDto`, `AddApplication()` DI extension (registers `TimeProvider`, `IGreetingService`, FluentValidation validators). | Use cases per ticket flow, repository interfaces, DTOs, validators. |
| `HelpDesk.Infrastructure` | EF Core, Npgsql, Identity NuGets installed. No code yet. | `HelpDeskDbContext`, Identity tables, repository implementations, EF migrations. |
| `HelpDesk.API` | `GreetingsController` (`GET /api/v1/greetings`). `Program.cs` calls `AddServiceDefaults()`, `AddApplication()`, `MapDefaultEndpoints()`, `MapControllers()`, `MapOpenApi()` (Development only). | Tickets/comments/categories controllers, JWT bearer scheme, CORS, error and logging middleware. |
| `HelpDesk.SDK` | Refit packages installed. No interfaces. | `IGreetingsApi`, `ITicketsApi`, etc. Typed Refit clients with attributes. |
| `HelpDesk.Web` | Default MVC template plus `AddServiceDefaults()` and `MapDefaultEndpoints()`. | A DI extension that registers SDK clients, controllers that consume them, Razor views. |
| `HelpDesk.AppHost` | Wires `postgres` (data volume) → `helpdesk` database → API → Web. | More resources (cache, mail dev, etc.) as the system grows. |
| `HelpDesk.ServiceDefaults` | Aspire defaults: OpenTelemetry, health checks (`/health`, `/alive`), service discovery, HTTP client resilience. | Project-specific cross-cutting defaults if needed. |
| `HelpDesk.Domain.Tests` | `GreetingTests`: blank-message rejection and message trimming. | One unit-test class per Domain aggregate. |

## Dependency rule

Arrows point inward. Outer layers depend on inner layers, never the reverse. The Web app talks to the API only through HTTP via the SDK — it does not reference `HelpDesk.API`, `HelpDesk.Application`, or `HelpDesk.Infrastructure` directly.

```
              +------------------------+
              |       AppHost          |
              +-----------+------------+
                          |
              references for orchestration
                          |
        +-----------------+-----------------+
        |                                   |
        v                                   v
+---------------+                  +---------------+
|     Web       |  --HTTP via-->   |      API      |
+-------+-------+      SDK         +-------+-------+
        |                                  |
        v                                  v
+---------------+                  +---------------+      +-----------------+
|     SDK       |                  |  Application  | <--- | Infrastructure  |
+---------------+                  +-------+-------+      +--------+--------+
   (no internal                            |                       |
       refs)                               v                       v
                                   +---------------+      +-----------------+
                                   |    Domain     |      |     Domain      |
                                   +---------------+      +-----------------+

ServiceDefaults is referenced by API and Web for shared OpenTelemetry/health/discovery.
Domain.Tests references Domain.
```

## Prerequisites

- **.NET 10 SDK** (preview accepted; `global.json` pins the floor). Verify with `dotnet --list-sdks`.
- **Docker Desktop** running. Aspire spawns the Postgres container on demand and mounts a named volume so data survives restarts.
- **Aspire CLI 13+**. Install:
  - Windows: `winget install Microsoft.Aspire`
  - macOS / Linux: `curl -sSL https://aspire.dev/install.sh | bash`
- **`dotnet ef` global tool** (only when you start writing migrations): `dotnet tool install --global dotnet-ef`.

## Quick start

From the repo root:

```powershell
aspire run --project src/HelpDesk.AppHost
```

The CLI prints a dashboard URL similar to `https://localhost:17000/login?t=...`. Open it. You should see four resources running side by side: `postgres`, `helpdesk` (database), `api`, and `web`.

To verify the demo endpoint, click the API row in the dashboard, copy the exposed HTTPS endpoint, and hit:

```bash
curl https://localhost:<api-port>/api/v1/greetings
```

You should see:

```json
{
  "message": "Hello from HelpDesk",
  "timestamp": "2026-05-16T18:42:11.1234567+00:00"
}
```

The `timestamp` is the current UTC time from `TimeProvider.System`, returned through the Application service and serialized by the API controller. If you change the `Greeting.Create` factory's literal, this is the path the request walks.

## Common commands

Run from the repo root unless noted.

```powershell
dotnet build HelpDesk.slnx
dotnet test
```

When you add the `HelpDeskDbContext` (see [What's next](#whats-next)), create migrations with:

```powershell
dotnet ef migrations add InitialCreate `
  --project src/HelpDesk.Infrastructure `
  --startup-project src/HelpDesk.API
```

This command does not work today — no `DbContext` exists yet. It's documented here so the path is obvious when you add one.

## The Greetings vertical slice

`Greetings` walks every layer of the Clean Architecture so you can see the wiring before you start adding real features. Trace it once and the pattern for `Tickets`, `Comments`, and `Categories` falls out the same way.

1. **Domain** — `src/HelpDesk.Domain/Greetings/Greeting.cs` defines `Greeting` as a `record` with a `Create(message, timestamp)` factory that rejects blank input.
2. **Application** — `src/HelpDesk.Application/Greetings/GreetingService.cs` injects `TimeProvider`, calls `Greeting.Create("Hello from HelpDesk", now)`, and returns a `GreetingDto`. `IGreetingService` is the contract the API depends on. The service and `TimeProvider` are registered in `AddApplication()`.
3. **API** — `src/HelpDesk.API/Controllers/GreetingsController.cs` takes `IGreetingService` by constructor injection and exposes `GET /api/v1/greetings`.
4. **Tests** — `tests/HelpDesk.Domain.Tests/Greetings/GreetingTests.cs` asserts that blank and whitespace-only messages throw and that whitespace around a valid message is trimmed.

Both `HelpDesk.SDK` and `HelpDesk.Web` are intentionally untouched by this slice; they are the next two layers to extend.

## What's next

Build the system outward from this scaffold in roughly this order. Each step builds on the previous.

1. **Add the first Domain entities and a `HelpDeskDbContext`.**
   - Place entities under `src/HelpDesk.Domain/`.
   - Create `src/HelpDesk.Infrastructure/Persistence/HelpDeskDbContext.cs` inheriting `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>` if you want Identity integration, or plain `DbContext` otherwise.
   - Add `EFCore.NamingConventions` to the model builder (`optionsBuilder.UseSnakeCaseNamingConvention()`).
2. **Wire the DbContext into Aspire.**
   - Install the Aspire client integration on the API: `dotnet add src/HelpDesk.API package Aspire.Npgsql.EntityFrameworkCore.PostgreSQL`.
   - In `src/HelpDesk.API/Program.cs`, call `builder.AddNpgsqlDbContext<HelpDeskDbContext>("helpdesk")`. Aspire injects the connection string from the AppHost's `helpdesk` database resource.
3. **Generate the first migration.**
   - Run the `dotnet ef migrations add InitialCreate` command above.
   - Apply at startup or via `dotnet ef database update`.
4. **Wire ASP.NET Identity.**
   - Add `ApplicationUser : IdentityUser<Guid>` in Infrastructure.
   - Register Identity in an `AddInfrastructure()` extension: `services.AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole<Guid>>().AddEntityFrameworkStores<HelpDeskDbContext>().AddDefaultTokenProviders()`.
5. **Wire JWT bearer.**
   - Bind options from configuration (`builder.Configuration.GetSection("Jwt")`).
   - Call `builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)`.
6. **Author Refit interfaces in the SDK and consume them from the Web app.**
   - Define `IGreetingsApi` (and later `ITicketsApi`) in `src/HelpDesk.SDK/` with Refit attributes.
   - Add a DI extension `AddHelpDeskSdk(this IServiceCollection services, Uri baseAddress)` that calls `services.AddRefitClient<IGreetingsApi>().ConfigureHttpClient(c => c.BaseAddress = baseAddress)`.
   - From `src/HelpDesk.Web/Program.cs`, register the SDK with the Aspire service discovery scheme (`http+https://api`); `ServiceDefaults` already calls `AddServiceDiscovery()` so the URL resolves at runtime.
7. **Render greetings in the Web app's home page** as a smoke test of the end-to-end path. After that, you have a template for every subsequent feature.

## Troubleshooting

- **`aspire run` fails with "no Docker daemon"** — start Docker Desktop and rerun.
- **`dotnet ef` command not found** — install the tool: `dotnet tool install --global dotnet-ef`.
- **`/api/v1/greetings` returns 404** — confirm `MapControllers()` is called in `src/HelpDesk.API/Program.cs` and that the dashboard shows the API as running, not crashed.
- **The Postgres container keeps restarting** — check Docker disk space. The data volume name is `helpdesk-postgres-data` (Aspire-generated). Remove with `docker volume rm` if you need a clean slate.

## Related

- [.NET Aspire docs](https://learn.microsoft.com/dotnet/aspire/)
- [Entity Framework Core with Npgsql](https://www.npgsql.org/efcore/)
- [Refit](https://github.com/reactiveui/refit)
- [FluentValidation](https://docs.fluentvalidation.net/)
