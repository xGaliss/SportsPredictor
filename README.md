# SportsPredictor MVP

Base MVP para una app de pronóstico NBA con:
- ASP.NET Core Web API
- EF Core + SQLite
- ingestión básica desde balldontlie
- endpoints propios
- predicción simple basada en forma reciente y localía

## Proyectos
- `Sports.Api`: API principal, DbContext, servicios, controllers y background jobs
- `Sports.Domain`: entidades de dominio
- `Sports.Infrastructure`: cliente externo y contratos para proveedores
- `Sports.Jobs`: reservado para separar jobs más adelante

## Requisitos
- .NET 8 SDK

## Arranque
```bash
cd src/Sports.Api
dotnet restore
dotnet ef database update
dotnet run
```

La API quedará disponible en `https://localhost:7128` o `http://localhost:5128` según tu entorno.

## Configuración
Edita `src/Sports.Api/appsettings.json`:
- `ConnectionStrings:DefaultConnection`
- `ExternalApis:Balldontlie:ApiKey`
- `Ingestion:Enabled`
- `Ingestion:RunOnStartup`

## Flujo recomendado
1. Arranca la API.
2. Llama a `POST /api/admin/seed/teams` para traer equipos NBA.
3. Llama a `POST /api/admin/seed/games/today` para traer partidos del día.
4. Llama a `POST /api/admin/predictions/today` para calcular predicciones.
5. Consume `GET /api/games/today` y `GET /api/predictions/today`.

## Notas
- La ingesta usa una implementación preparada para balldontlie, pero el mapeo puede variar si cambian campos del proveedor.
- La predicción actual es intencionalmente simple: forma reciente + media de puntos anotados/recibidos + bonus local.
- Hay datos demo de fallback si la llamada externa falla y tienes activado `UseMockFallbackData`.

## Siguientes mejoras
- Injuries
- Odds API
- Props de jugadores
- Hangfire / Quartz
- autenticación
- caché
- PostgreSQL


## BALLDONTLIE notes

This project is wired to the current BALLDONTLIE docs:
- Base URL: `https://api.balldontlie.io`
- Auth header: `Authorization: YOUR_API_KEY`
- Games endpoint: `GET /v1/games?dates[]=YYYY-MM-DD`
- The app reads config from `ExternalApis:Balldontlie` in `appsettings.json`.

Quick checks in Swagger:
- `GET /api/admin/config/balldontlie` to verify the API key is being loaded
- `POST /api/admin/seed/teams`
- `POST /api/admin/seed/games/today`
