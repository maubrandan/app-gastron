# Resto — POS gastronómico

Sistema de punto de venta para restaurantes (.NET 10 + Angular 21).

## Desarrollo local

### Opción A — Docker Compose (recomendado)

Requiere [Docker Desktop](https://www.docker.com/products/docker-desktop/).

```bash
docker compose up --build
```

- API: `http://localhost:5019`
- SQL Server: `localhost:1433` (usuario `sa`, contraseña `Resto_Dev123!`)

La API aplica migraciones y seed al iniciar. Para el frontend, en otra terminal:

```bash
cd frontend
npm install
npx ng serve
```

### Opción B — herramientas locales

#### Backend

```bash
cd src/Resto.Api
dotnet run
```

API: `http://localhost:5019` (requiere SQL Server LocalDB)

#### Frontend

```bash
cd frontend
npm install
npx ng serve
```

App: `http://localhost:4200`

## Tests y CI

```bash
dotnet test Resto.slnx
cd frontend && npm run build
```

Los tests de integración usan SQL Server LocalDB en Windows, o la variable `RESTO_TEST_CONNECTION` en CI.

### E2E (Playwright, modo demo)

```bash
cd frontend
npm install
npx playwright install chromium
npm run e2e
```

Los tests levantan `ng serve --configuration demo` automáticamente (datos en memoria, sin backend).

## Producción

### Variables de entorno requeridas

| Variable | Descripción |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | Cadena SQL Server |
| `Jwt__Key` | Clave secreta JWT (mín. 32 caracteres) |
| `Jwt__Issuer` | Emisor del token (default: `Resto.Api`) |
| `Jwt__Audience` | Audiencia del token (default: `Resto.App`) |
| `Cors__Origins__0` | Origen del frontend (ej. `https://app.tudominio.com`) |

Opcional: `Business__TimeZoneId` (default: `America/Argentina/Buenos_Aires`).

### Health check

```bash
curl http://localhost:5019/health
```

Respuesta esperada: `Healthy` con verificación de base de datos.

### Logging

En `Production` los logs se emiten en **JSON** a consola (con `CorrelationId` por request via header `X-Correlation-Id`).

### Docker

```bash
docker compose up --build
```

Para producción real, reemplazá secretos en `docker-compose.yml` o usá un archivo `.env` (no commitear).

## Usuarios demo (solo desarrollo)

Contraseña común para todos: **`Resto123!`**

| Email | Rol | Vista |
|-------|-----|-------|
| `admin@resto.local` | Admin | Encargado + Personal |
| `encargado@resto.local` | Manager | Encargado |
| `mozo1@resto.local` | Waiter | Mozo |
| `mozo2@resto.local` | Waiter | Mozo |
| `kitchen@resto.local` | Kitchen | Cocina (kiosk) |

Los usuarios se crean automáticamente al iniciar la API (`IdentitySeeder`).

## Autenticación

- Login: `POST /api/auth/login`
- JWT Bearer en header `Authorization` para API y SignalR
- Roles: `Waiter`, `Manager`, `Kitchen`, `Admin`

### Configuración JWT

Ver `src/Resto.Api/appsettings.json` — cambiar `Jwt:Key` en producción.

## Demo en GitHub Pages

Versión interactiva del frontend **sin backend**: datos en memoria en el navegador.

**URL:** https://maubrandan.github.io/app-gastron/

### Usuarios demo

Cualquier contraseña funciona en modo demo. Emails sugeridos:

| Email | Rol | Vista |
|-------|-----|-------|
| `admin@resto.local` | Admin | Encargado + Personal |
| `encargado@resto.local` | Manager | Encargado |
| `mozo1@resto.local` | Waiter | Mozo |
| `kitchen@resto.local` | Kitchen | Cocina |

### Limitaciones

- Sin sincronización entre dispositivos o pestañas
- Los datos se pierden al recargar la página
- Sin concurrencia real entre mozos

### Build y deploy local de la demo

```bash
cd frontend
npm install
npm run build:demo    # compila con modo demo + baseHref /app-gastron/
npm run start:demo    # desarrollo local en modo demo
```

El deploy a GitHub Pages se ejecuta automáticamente con GitHub Actions al pushear a `main` (workflow `.github/workflows/deploy-demo.yml`). En el repo: **Settings → Pages → Source: GitHub Actions**.
