# SportsPredictor v3

Base MVP para una app de pronóstico NBA con:
- ASP.NET Core Web API
- EF Core + SQLite
- ingestión automática desde BALDONTLIE
- UI web simple servida por la propia API
- predicción simple basada en forma reciente y localía

## Proyectos
- `Sports.Api`: API principal, DbContext, servicios, background jobs y dashboard web
- `Sports.Domain`: entidades de dominio
- `Sports.Infrastructure`: cliente externo y contratos para proveedores
- `Sports.Jobs`: reservado para separar jobs más adelante

## Requisitos
- .NET 8 SDK

## Qué hace esta versión
- sincroniza equipos, partidos del día y predicciones automáticamente al arrancar
- repite la sync cada `Ingestion:IntervalMinutes`
- expone endpoints para debug y consumo JSON
- sirve una UI en `/` para ver estado, partidos y predicciones
- permite lanzar una sync manual con `POST /api/admin/sync/now`

## Configuración
Edita `src/Sports.Api/appsettings.json`:
- `ConnectionStrings:DefaultConnection`
- `ExternalApis:Balldontlie:ApiKey`
- `Ingestion:Enabled`
- `Ingestion:RunOnStartup`
- `Ingestion:IntervalMinutes`

La config viene preparada para la doc actual de BALDONTLIE:
- Base URL: `https://api.balldontlie.io`
- Auth header: `Authorization: YOUR_API_KEY`
- Games endpoint: `GET /v1/games?dates[]=YYYY-MM-DD`

## Arranque
```bash
cd src/Sports.Api
dotnet restore
dotnet run
```

La app levantará Swagger y además una UI web en la raíz.

## Endpoints útiles
- `GET /api/status`
- `GET /api/games/today`
- `GET /api/predictions/today`
- `GET /api/admin/config/balldontlie`
- `POST /api/admin/sync/now`

También se mantienen estos endpoints manuales:
- `POST /api/admin/seed/teams`
- `POST /api/admin/seed/games/today`
- `POST /api/admin/predictions/today`

## Notas
- El dashboard usa una UI estática simple servida por `Sports.Api/wwwroot/index.html`.
- La predicción actual es deliberadamente básica.
- Si falla la llamada externa y `UseMockFallbackData = true`, se cargan datos demo.
- La ventana de “hoy” se calcula con zona horaria `Europe/Madrid`.

## Siguientes mejoras
- jugadores y stats por partido reales
- injuries
- odds API
- comparación con cuotas
- auth
- PostgreSQL
- jobs con Hangfire/Quartz
