# Instalación y ejecución — HelpDesk

Guía para dejar el proyecto corriendo desde cero en macOS. Basada en la instalación real hecha en este equipo.

## Requisitos

| Herramienta | Para qué | Versión |
|---|---|---|
| **.NET SDK 10** | Compilar y ejecutar los proyectos (TFM `net10.0`) | 10.0.101+ |
| **Aspire CLI** | Orquestar Postgres + API + Web en local | 13+ |
| **Docker Desktop** | Aspire levanta PostgreSQL como contenedor | cualquiera reciente |

Verifica lo que ya tienes:

```bash
dotnet --list-sdks     # ¿hay un 10.0.x?
aspire --version       # ¿13 o superior?
docker info            # ¿Docker está corriendo?
```

---

## 1. Instalar .NET 10 SDK

Si `dotnet --list-sdks` no muestra ningún `10.0.x`, descárgalo desde:

- https://dotnet.microsoft.com/download/dotnet/10.0

Instala el **SDK** (no solo el runtime) para tu arquitectura (Apple Silicon = arm64). Verifica:

```bash
dotnet --list-sdks
```

> **Nota sobre `global.json`:** el repo fija una versión de SDK en `global.json`. Si la versión que instalaste no coincide, `dotnet` fallará con "A compatible .NET SDK was not found". Dos opciones:
> - Instalar exactamente la versión pedida, **o**
> - Ajustar `global.json` a tu SDK instalado. En este equipo se cambió a la versión estable instalada:
>   ```json
>   { "sdk": { "rollForward": "latestFeature", "version": "10.0.101" } }
>   ```

---

## 2. Instalar la Aspire CLI

En macOS / Linux:

```bash
curl -sSL https://aspire.dev/install.sh | bash
```

Esto instala el binario en `~/.aspire/bin/aspire` y lo agrega al `PATH` en tu `~/.zshrc`.

> ⚠️ **Abre una terminal nueva** (o corre `source ~/.zshrc`) para que el comando `aspire` quede disponible. Si escribes `aspire` en la terminal vieja y dice "command not found", es por esto.

Verifica:

```bash
aspire --version     # debe decir 13.x
```

En Windows sería: `winget install Microsoft.Aspire`.

---

## 3. Instalar / abrir Docker Desktop

- Descárgalo desde https://www.docker.com/products/docker-desktop
- **Ábrelo y espera a que arranque** antes de correr el proyecto (Aspire necesita el demonio de Docker activo para crear el contenedor de PostgreSQL).

Verifica que responde:

```bash
docker info      # si imprime info del sistema, está corriendo
```

---

## 4. Correr el proyecto

Con Docker abierto y desde una terminal nueva en la **raíz del repo**:

```bash
aspire run --project src/HelpDesk.AppHost
```

Aspire imprimirá algo como:

```
Dashboard:  https://localhost:17002/login?t=<token>
```

1. Abre esa **URL completa** (el `?t=<token>` es un acceso de un solo uso; no sirve solo `localhost:17002`).
2. Acepta la advertencia de certificado del navegador (es HTTPS local de desarrollo → *Avanzado → Continuar*).
3. En el dashboard, espera a que los **4 recursos** estén en verde (**Running**):
   - `postgres` → `helpdesk` (base de datos) → `api` → `web`
   - La primera vez tarda más: Docker descarga la imagen de PostgreSQL y la **API aplica las migraciones al arrancar** (crea las tablas solas).
4. Haz clic en el recurso **`web`**, abre su endpoint → verás la pantalla de **login**.

> No hace falta crear la base de datos ni correr migraciones a mano: la API ejecuta `db.Database.Migrate()` al iniciar, y Aspire inyecta el connection string automáticamente.

---

## Comandos útiles

```bash
# Compilar toda la solución (verifica que todo está bien antes de correr)
dotnet build HelpDesk.slnx

# Ejecutar los tests
dotnet test

# Detener la app: Ctrl+C en la terminal donde corre 'aspire run'
```

**Migraciones** (solo si cambias el modelo de datos; requiere `dotnet tool install --global dotnet-ef`):

```bash
dotnet ef migrations add <Nombre> \
  --project src/HelpDesk.Infrastructure \
  --startup-project src/HelpDesk.API
```

---

## Problemas comunes

| Síntoma | Causa / solución |
|---|---|
| `aspire: command not found` | Terminal vieja → abre una nueva o `source ~/.zshrc` |
| `A compatible .NET SDK was not found` | El `global.json` pide una versión que no tienes → instálala o ajusta `global.json` (ver paso 1) |
| `aspire run` falla con "no Docker daemon" | Docker Desktop no está abierto → ábrelo y reintenta |
| Un recurso queda en rojo en el dashboard | Haz clic en él y revisa sus **Logs** en el dashboard |
| El navegador bloquea el dashboard | Acepta el certificado de desarrollo local (HTTPS) |

---

## Consejos de terminal (VS Code)

- Abre la terminal integrada con `Ctrl+ñ`. Debe ser **nueva** (posterior a instalar Aspire) para reconocer el comando.
- La terminal donde corre `aspire run` **se queda ocupada**; abre otra con `+` para más comandos.
- Puedes `Cmd+clic` sobre la URL del dashboard para abrirla directo en el navegador.
