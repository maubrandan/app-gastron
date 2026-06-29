# Resto — POS gastronómico

Sistema de punto de venta para restaurantes (.NET 10 + Angular 21).

## Desarrollo local

### Backend

```bash
cd src/Resto.Api
dotnet run
```

API: `http://localhost:5019`

### Frontend

```bash
cd frontend
npm install
npx ng serve
```

App: `http://localhost:4200`

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
