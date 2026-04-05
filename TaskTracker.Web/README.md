# Task Tracker Web (ASP.NET Core + Blazor + EF Core)

Backend API + Blazor UI egy task/issue tracker rendszerhez.

Fobb modulok:
- Auth (register/login/refresh/logout/me)
- Projects (CRUD + tagsagkezeles)
- Tasks (CRUD + szures + lapozas)
- Comments (task kommentek CRUD)

## Technologia
- .NET 10
- ASP.NET Core (Controller API)
- Blazor (server-side interaktiv komponensek)
- EF Core
- PostgreSQL
- JWT auth
- Docker Compose (lokalis adatbazishoz)

## Teljes Setup (lepesrol lepesre)

## 1. Elofeltetelek
- .NET SDK 10
- Docker Desktop + Docker Compose
- (Opcionalis) `dotnet-ef` CLI tool

`dotnet-ef` telepites (ha meg nincs):
```bash
dotnet tool install --global dotnet-ef
```

## 2. Projekt megnyitasa
```bash
cd TaskTracker.Web
```

## 3. Kornyezeti valtozok beallitasa
Az app alapvetoen `appsettings*.json` fajlokbol dolgozik, de environment valtozokkal felulirhato.

Kotelezoen javasolt beallitasok:
- `ConnectionStrings__DefaultConnection`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__SigningKey`
- `Jwt__AccessTokenExpirationMinutes`
- `Jwt__RefreshTokenExpirationDays`
- `Cors__AllowedOrigins__0`

Minimalis pelda (PowerShell):
```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
$env:ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=tasktracker_db;Username=tasktracker;Password=tasktracker_dev_password;"
$env:Jwt__Issuer="TaskTracker.Web"
$env:Jwt__Audience="TaskTracker.Client"
$env:Jwt__SigningKey="THIS_IS_A_DEVELOPMENT_ONLY_SIGNING_KEY_CHANGE_IN_PRODUCTION_2026"
```

## 4. PostgreSQL inditasa Dockerrel
```bash
docker compose up -d
```

Ellenorzes:
```bash
docker compose ps
```

## 5. Adatbazis migraciok lefuttatasa
A projekt indulaskor automatikusan migral (`Database.Migrate()`), de manualisan is lefuttathato:

```bash
dotnet ef database update
```

## 6. Alkalmazas inditasa
Fejlesztoi mod:
```bash
dotnet run
```

Production build + futtatas:
```bash
dotnet build
dotnet run --no-build
```

Default lokalis URL-ek (`launchSettings.json` alapjan):
- `https://localhost:7028`
- `http://localhost:5271`

## 7. Dokumentacio ellenorzese
- Swagger UI: `https://localhost:7028/swagger`
- OpenAPI JSON: `https://localhost:7028/swagger/v1/swagger.json`

## 8. Minosegi ellenorzesek
```bash
dotnet build
dotnet test Tests/TaskTracker.Web.IntegrationTests/TaskTracker.Web.IntegrationTests.csproj
```

## 9. Docker profil (opcionalis)
Dockeres appsettinggel futtatas:

```bash
set ASPNETCORE_ENVIRONMENT=Docker
dotnet run
```

## 10. Release checklist
Release elott futtasd vegig:

- Docker inditas (`docker compose up -d`)
- Migracio (`dotnet ef database update` vagy app startup auto-migrate)
- Seed ellenorzes (demo userek letrejottek-e)
- Env/config ellenorzes (`ConnectionStrings`, `Jwt`, `Cors`, `RateLimiting`)
- Auth config ellenorzes (Bearer token flow)
- Health-check endpointok ellenorzese (`GET /`, `GET /health/db`)
- Fobb felhasznaloi flow-k vegigtesztelese
- Regresszios kor (manualis vagy feleautomata)

## Auth hasznalat roviden
1. `POST /auth/register` vagy `POST /auth/login`
2. `accessToken` megy `Authorization: Bearer <token>` headerben
3. Lejart access token eseten `POST /auth/refresh`
4. Kijelentkezes: `POST /auth/logout`

## Seedelt demo userek
Alap seed (ures DB eseten):

- `admin@example.com` / `admin123`
- `user@example.com` / `user123`

## API Dokumentacio

## Base URL
- `https://localhost:7028`
- `http://localhost:5271`

## Hitelesites
- A vedett endpointok `Authorization: Bearer <accessToken>` headert varnak.

## Endpoint lista (minden vegpont)

| Method | Endpoint | Auth | Leiras |
|---|---|---|---|
| GET | `/` | No | Egyszeru health-check jellegu valasz |
| GET | `/health/db` | No | DB connectivity health-check |
| POST | `/auth/register` | No | Uj user regisztracio |
| POST | `/auth/login` | No | Bejelentkezes |
| POST | `/auth/refresh` | No | Token frissites refresh tokennel |
| POST | `/auth/logout` | Yes | Kijelentkezes + refresh token hash torles |
| GET | `/auth/me` | Yes | Aktualis bejelentkezett user payload |
| GET | `/auth/protected` | Yes | Egyszeru vedett endpoint teszteleshez |
| POST | `/projects` | Yes | Projekt letrehozas |
| GET | `/projects` | Yes | Projektek listazasa (owner/member) |
| GET | `/projects/{id}` | Yes | Egy projekt lekerdezese |
| PATCH | `/projects/{id}` | Yes | Projekt modositasa (owner) |
| DELETE | `/projects/{id}` | Yes | Projekt torlese (owner) |
| POST | `/projects/{id}/members` | Yes | Tag hozzaadasa projekthez (owner) |
| DELETE | `/projects/{id}/members/{memberUserId}` | Yes | Tag eltavolitasa projektbol (owner) |
| POST | `/projects/{projectId}/tasks` | Yes | Task letrehozas projektben |
| GET | `/projects/{projectId}/tasks` | Yes | Task lista szuressel/lapozassal |
| GET | `/projects/{projectId}/tasks/{taskId}` | Yes | Egy task lekerdezese |
| PATCH | `/projects/{projectId}/tasks/{taskId}` | Yes | Task modositasa |
| DELETE | `/projects/{projectId}/tasks/{taskId}` | Yes | Task torlese |
| POST | `/tasks/{taskId}/comments` | Yes | Komment letrehozas taskhoz |
| GET | `/tasks/{taskId}/comments` | Yes | Komment lista taskhoz (lapozhato) |
| GET | `/tasks/{taskId}/comments/{commentId}` | Yes | Egy komment lekerdezese |
| PATCH | `/tasks/{taskId}/comments/{commentId}` | Yes | Komment modositasa (author vagy project owner) |
| DELETE | `/tasks/{taskId}/comments/{commentId}` | Yes | Komment torlese (author vagy project owner) |

## Request body referencia

### POST /auth/register
```json
{
  "email": "user@example.com",
  "fullName": "John Doe",
  "password": "Password123"
}
```

### POST /auth/login
```json
{
  "email": "user@example.com",
  "password": "Password123"
}
```

### POST /auth/refresh
```json
{
  "refreshToken": "<refresh_token>"
}
```

### POST /projects
```json
{
  "name": "My Project",
  "description": "Optional project description"
}
```

### PATCH /projects/{id}
```json
{
  "name": "Updated name",
  "description": "Updated description"
}
```

### POST /projects/{id}/members
```json
{
  "memberUserId": "<user-uuid>"
}
```

### POST /projects/{projectId}/tasks
```json
{
  "title": "Implement endpoint",
  "description": "Optional",
  "status": "TODO",
  "priority": "HIGH",
  "assignedUserId": "<user-uuid>",
  "dueDate": "2031-01-01T00:00:00.000Z"
}
```

### PATCH /projects/{projectId}/tasks/{taskId}
```json
{
  "title": "Updated task",
  "status": "IN_PROGRESS",
  "priority": "MEDIUM"
}
```

### GET /projects/{projectId}/tasks query paramok
- `status`: `TODO` | `IN_PROGRESS` | `DONE`
- `priority`: `LOW` | `MEDIUM` | `HIGH`
- `assigneeId`: UUID
- `dueFrom`: ISO date string
- `dueTo`: ISO date string
- `sortBy`: `CreatedAt` | `DueDate` | `Priority` | `Status`
- `sortOrder`: `Asc` | `Desc`
- `page`: integer (min: 1)
- `limit`: integer (1-100)

### POST /tasks/{taskId}/comments
```json
{
  "content": "This task needs clarification"
}
```

### PATCH /tasks/{taskId}/comments/{commentId}
```json
{
  "content": "Updated comment text"
}
```

### GET /tasks/{taskId}/comments query paramok
- `page`: integer (min: 1)
- `limit`: integer (1-100)

## Response forma

Tobb endpoint ezt hasznalja:
```json
{
  "success": true,
  "data": {}
}
```

Hibak globalisan:
```json
{
  "success": false,
  "statusCode": 400,
  "message": "...",
  "path": "/requested/path",
  "timestamp": "2026-04-05T12:00:00.000Z"
}
```

## Hasznos parancsok
- `dotnet run` - fejlesztoi futtatas
- `dotnet build` - build
- `dotnet test Tests/TaskTracker.Web.IntegrationTests/TaskTracker.Web.IntegrationTests.csproj` - integration tesztek
- `dotnet ef migrations list` - migraciok listazasa
- `dotnet ef database update` - migraciok alkalmazasa
- `docker compose up -d` - PostgreSQL inditas
- `docker compose down` - PostgreSQL leallitas

## Postman
Predefined Postman collection/env fajlok a `postman/` mappaban talalhatok iteracionkent.