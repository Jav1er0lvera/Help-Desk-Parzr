# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A help-desk ticketing system on ASP.NET Core 10 (TFM `net10.0`), EF Core 10 + Npgsql/PostgreSQL, orchestrated locally with .NET Aspire 13. Nine projects under `src/` and `tests/`, wired as Clean Architecture layers.

> Note: `README.md` is stale — it describes the repo as "scaffolding only" with a single `Greetings` demo endpoint. That is no longer true. Tickets, Comments, Categories, Platforms, and Users are fully implemented with EF migrations, cookie auth, and a working MVC UI. Trust the code over the README. The `Greetings` slice still exists as the reference example (and is what the one test class covers).

## Commands

Run from the repo root.

```bash
# Run everything (Postgres container + API + Web) via Aspire — requires Docker Desktop running
aspire run --project src/HelpDesk.AppHost

# Build / test
dotnet build HelpDesk.slnx
dotnet test
dotnet test --filter "FullyQualifiedName~GreetingTests"   # single test class

# EF migrations — API is the startup project, Infrastructure holds the DbContext & migrations
dotnet ef migrations add <Name> --project src/HelpDesk.Infrastructure --startup-project src/HelpDesk.API
dotnet ef database update      --project src/HelpDesk.Infrastructure --startup-project src/HelpDesk.API
```

The SDK floor is pinned in `global.json` (`10.0.300-preview…`, `rollForward: latestFeature`). The solution file is `HelpDesk.slnx` (XML slnx format, not `.sln`).

Migrations are also applied automatically at API startup: `HelpDesk.API/Program.cs` calls `db.Database.Migrate()` in a startup scope.

## Architecture

Two ASP.NET hosts fronted by Aspire. `AppHost/AppHost.cs` provisions `postgres` → `helpdesk` database, then starts `api` and `web`, both given a reference to Postgres. Aspire injects the connection string as `ConnectionStrings:helpdesk`.

**Layer dependency direction** (enforced by project references):
`Domain` ← `Application` ← `Infrastructure` ← `API`; `SDK` and `Web` are the outer client side; `ServiceDefaults` is shared cross-cutting (OpenTelemetry, `/health` + `/alive`, service discovery, HTTP resilience).

- **HelpDesk.Domain** — POCO entities (`Ticket`, `Comment`, `Category`, `Platform`, `User`, `Greeting`) + repository interfaces (`Domain/Interfaces/*`). All entities derive from `Common/BaseEntity`, which auto-assigns `Guid Id` and UTC `CreatedAt`/`UpdatedAt` in its constructor.
- **HelpDesk.Application** — MediatR command/handler use cases (e.g. `Tickets/CreateTicketUseCase.cs`, `AssignTicketUseCase.cs`), each file holds both the `Command` and its `Handler`. `AddApplication()` registers MediatR by scanning the Application assembly.
- **HelpDesk.Infrastructure** — `Persistence/HelpDeskDbContext.cs` (plain `DbContext`, model configured via fluent `OnModelCreating`), repository implementations, EF migrations, `Identity/ApplicationUser.cs`. `AddInfrastructure()` registers the repositories.
- **HelpDesk.API** — versioned controllers under `api/v1/*`. OpenAPI mapped in Development only.
- **HelpDesk.SDK** — Refit typed clients + DTOs (`ITicketsApi`, `ICategoriesApi`, etc.), registered by `AddHelpDeskSdk(baseUrl)`.
- **HelpDesk.Web** — server-rendered MVC UI, cookie auth. Views live under `Views/**`, some with `.cshtml.cs` code-behind.

### Conventions and quirks worth knowing before you edit

- **The API is not pure CQRS.** Controllers mix two styles: some actions go through MediatR (`_mediator.Send(new CreateTicketCommand …)`), others hit `HelpDeskDbContext` directly in the controller (e.g. `TicketsController.GetAll` runs a hand-written LINQ join across Users/Categories/Platforms; `UpdateStatus`/`Delete` use `_db` directly). When adding an endpoint, match the pattern already used by the surrounding actions in that controller.
- **Web does not use the Refit SDK yet.** `HelpDesk.Web` calls the API through a named `IHttpClientFactory` client `"api"` (`BaseAddress = https+http://api`, resolved by Aspire service discovery) with hand-built JSON payloads — not the `HelpDesk.SDK` Refit interfaces. The SDK exists and compiles but is unused by Web; `AddHelpDeskSdk` is not called anywhere. Prefer the existing `_httpClientFactory.CreateClient("api")` pattern for new Web→API calls unless deliberately migrating to the SDK.
- **Auth lives in Web, not API.** `Web/Controllers/AccountController.cs` talks directly to `HelpDeskDbContext` (bypassing the API) to log in / register, hashes passwords with **BCrypt** (`BCrypt.Net-Next`), and issues a `"Cookies"` auth cookie with `NameIdentifier`/`Name`/`Email`/`Role` claims. There is a `// TODO` to move this into the API + consume via the SDK; it hasn't happened. The JWT bearer package is referenced by the API but no JWT scheme is registered — auth today is cookie-only, and both hosts register their own `HelpDeskDbContext`.
- **Domain string values are Spanish literals**, not enums (except `UserRole`). Ticket `Status`: `"Abierto"`, `"En progreso"`, `"Resuelto"`; `Priority` defaults to `"Media"`. `ResolvedAt` is set when status becomes `"Resuelto"`. `UserRole` enum: `Usuario`, `Soporte`, `Supervisor`. Keep these literals consistent across API, SDK DTOs, and Web when adding flows.
- **Category soft-delete:** `Category.IsDeleted` — filter it out in queries rather than hard-deleting.
- Model configuration (keys, required props, unique indexes on `User.Email`, `Category.Name`, `Platform.Name`) is done fluently in `HelpDeskDbContext.OnModelCreating`, using string-based property references.

## Testing

xUnit, one project: `tests/HelpDesk.Domain.Tests` (currently only `GreetingTests`). Tests cover the Domain layer; there is no integration/API test harness yet.
