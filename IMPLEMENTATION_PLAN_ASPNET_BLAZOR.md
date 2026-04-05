# IMPLEMENTATION_PLAN

## Cél
Task / Issue Tracker fullstack megvalósítása `Blazor + ASP.NET Core + EF Core + PostgreSQL` stackkel, teljesen szerveroldali megközelítéssel, Docker alapú adatbázis-futtatással, tiszta architektúrával, erős validációval, és iterációnként ellenőrzött helyes működéssel.

A cél, hogy a backend endpointok és viselkedés ugyanúgy működjenek, mint a meglévő tervben, miközben a projekt frontend oldala is fokozatosan, külön iterációkban épül fel Blazorral.

## Scope
- Root endpoint health-check jellegű válasszal
- Auth: regisztráció, login, refresh, logout, me
- Project létrehozás és kezelés
- Projekt tagságkezelés (member add/remove)
- Task CRUD projekten belül
- Task mezők: `title`, `description`, `status`, `priority`, `assignedUserId`, `dueDate`
- Task szűrés: `status`, `priority`, `assigneeId`, `dueFrom`, `dueTo`
- Task rendezés: `createdAt`, `dueDate`, `priority`, `status`
- Pagination taskokhoz és commentekhez
- Comment modell és endpointok taskokhoz
- Egységes API response shape és globális hibakezelés
- OpenAPI / Swagger dokumentáció
- Blazor alapú frontend oldalak és komponensek ugyanennek a funkcionalitásnak a használatához
- Frontend oldali form validáció, állapotkezelés, route-védelem és API-integráció
- Docker Compose alapú PostgreSQL fejlesztői környezet

## Nem része ennek a tervnek
- Websocket real-time update
- Fájl csatolmányok
- Dashboard/reporting
- Külső frontend framework használata
- Mikroservice architektúra
- Külön deployolt frontend és backend szétválasztás

## Kötelezően megvalósítandó endpointok

| Method | Endpoint | Auth | Leírás |
|---|---|---|---|
| GET | `/` | No | Egyszerű health-check jellegű válasz |
| POST | `/auth/register` | No | Új user regisztráció |
| POST | `/auth/login` | No | Bejelentkezés |
| POST | `/auth/refresh` | No | Token frissítés refresh tokennel |
| POST | `/auth/logout` | Yes | Kijelentkezés + refresh token érvénytelenítés |
| GET | `/auth/me` | Yes | Aktuális bejelentkezett user payload |
| POST | `/projects` | Yes | Projekt létrehozás |
| GET | `/projects` | Yes | Projektek listázása (owner/member) |
| GET | `/projects/{id}` | Yes | Egy projekt lekérdezése |
| PATCH | `/projects/{id}` | Yes | Projekt módosítása (owner) |
| DELETE | `/projects/{id}` | Yes | Projekt törlése (owner) |
| POST | `/projects/{id}/members` | Yes | Tag hozzáadása projekthez (owner) |
| DELETE | `/projects/{id}/members/{memberUserId}` | Yes | Tag eltávolítása projektből (owner) |
| POST | `/projects/{projectId}/tasks` | Yes | Task létrehozása projektben |
| GET | `/projects/{projectId}/tasks` | Yes | Task lista szűréssel/lapozással |
| GET | `/projects/{projectId}/tasks/{taskId}` | Yes | Egy task lekérdezése |
| PATCH | `/projects/{projectId}/tasks/{taskId}` | Yes | Task módosítása |
| DELETE | `/projects/{projectId}/tasks/{taskId}` | Yes | Task törlése |
| POST | `/tasks/{taskId}/comments` | Yes | Komment létrehozása taskhoz |
| GET | `/tasks/{taskId}/comments` | Yes | Komment lista taskhoz (lapozható) |
| GET | `/tasks/{taskId}/comments/{commentId}` | Yes | Egy komment lekérdezése |
| PATCH | `/tasks/{taskId}/comments/{commentId}` | Yes | Komment módosítása (author vagy project owner) |
| DELETE | `/tasks/{taskId}/comments/{commentId}` | Yes | Komment törlése (author vagy project owner) |

## Technológiai döntések

### Frontend
- Blazor Server vagy Blazor Web App szerveres interaktivitással
- Razor komponensek
- `EditForm` alapú formkezelés
- Beépített validáció DataAnnotations alapon
- Oldalankénti és komponensenkénti felépítés

### Backend
- ASP.NET Core ugyanazon alkalmazáson belül
- Controller alapú Web API
- Service réteg az üzleti logikához
- Egységes exception mapping és API response shape
- Policy / authorization alapú hozzáférés-védelem

### Adatkezelés
- EF Core
- PostgreSQL adatbázis
- Docker Compose alapú adatbázis-futtatás fejlesztői környezetben
- Npgsql provider használata EF Core-hoz
- Migrations alapú schema kezelés
- Seedelés fejlesztői környezethez

## Javasolt solution szerkezet
- `src/TaskTracker.Web` – Blazor és host alkalmazás
- `src/TaskTracker.Api` – API controllerek, ha külön projektbe szervezed ugyanazon solutionön belül
- `src/TaskTracker.Application` – service-ek, DTO-k, use-case logika
- `src/TaskTracker.Domain` – entitások, enumok, interfészek
- `src/TaskTracker.Infrastructure` – EF Core, auth, persistence
- `docker-compose.yml`
- `.env` vagy `.env.example`
- `tests/TaskTracker.UnitTests`
- `tests/TaskTracker.IntegrationTests`

Egyszerűbb indulásnál ez redukálható is, például:
- `TaskTracker`
  - `Components`
  - `Pages`
  - `Controllers`
  - `Data`
  - `Models`
  - `Services`
  - `Auth`
  - `Shared`

## Iterációs terv

### Iteráció 0: Alapozás (solution + architektúra + minőség)
- .NET solution inicializálása
- Blazor szerveroldali projekt létrehozása
- ASP.NET Core API alapstruktúra kialakítása ugyanazon appon belül
- Könyvtárstruktúra kialakítása:
  - `Controllers`
  - `Services`
  - `Data`
  - `Models`
  - `Dtos`
  - `Mappings`
  - `Auth`
  - `Components`
  - `Pages`
  - `Exceptions`
- Egységes API response shape kialakítása (`success`, `data`, illetve globális hibaforma)
- Globális exception handling middleware vagy exception filter kialakítása
- FluentValidation vagy DataAnnotations stratégia eldöntése
- Swagger bekötése
- Logging és konfigurációs alapok
- Docker Compose alapfájl előkészítése PostgreSQL-hez
- Fejlesztői konfigurációs alapok:
  - connection string
  - environment változók
  - secret kezelés
- Code formatting és quality gate:
  - `dotnet format`
  - analyzers
  - unit/integration test projekt előkészítés

Elfogadási feltétel:
- Projekt indul, a solution szerkezete rendezett, API és frontend alapok előkészítve, minőségkapuk aktívak, és a Dockeres adatbázis-stratégia elő van készítve.

### Iteráció 1: Docker + PostgreSQL + domain + health-check
- `docker-compose.yml` létrehozása PostgreSQL szolgáltatással
- PostgreSQL konténer konfigurálása:
  - adatbázisnév
  - user
  - password
  - port mapping
  - volume
- EF Core DbContext kialakítása
- PostgreSQL provider konfiguráció (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- Connection string beállítása Dockeres PostgreSQL-hez
- Első migrációs workflow beállítása
- Entitások kezdeti kialakítása:
  - `User`
- `GET /` root endpoint létrehozása health-check jellegű válasszal
- App elindítása úgy, hogy adatbázis kapcsolat működjön
- Seedelési stratégia alapjainak kialakítása
- Alap adatbázis-indítási dokumentáció elkészítése

Elfogadási feltétel:
- A PostgreSQL konténer Dockerben elindul, az app csatlakozik hozzá, a migráció lefut, a health-check endpoint működik, a migrációs folyamat kipróbált.

### Iteráció 2: Auth alapok backend oldalon
- `User` entitás véglegesítése
- Auth DTO-k létrehozása
- `POST /auth/register`, `POST /auth/login`
- Password hashing implementálása
- Access token kezelés
- Refresh token kezeléshez szükséges mezők és tárolási stratégia
- Auth middleware / authentication beállítása
- `Authorize` alapú védelem működésbe hozása
- Request validáció auth végpontokra
- Auth endpoint feature/integration tesztek

Elfogadási feltétel:
- Felhasználó regisztrál és be tud jelentkezni, védett endpointok ténylegesen védettek.

### Iteráció 3: Refresh / logout / me backend oldalon
- `POST /auth/refresh`
- `POST /auth/logout`
- `GET /auth/me`
- Refresh token flow stabilizálása
- Token invalidálási stratégia véglegesítése
- Auth hibakezelés egységesítése
- Auth flow integration tesztek:
  - register
  - login
  - me
  - refresh
  - logout

Elfogadási feltétel:
- Access és refresh token flow végig működik, logout után a refresh token nem használható, a `/auth/me` helyes payloadot ad.

### Iteráció 4: Frontend auth oldalak és session kezelés
- Blazor layout és alap navigáció kialakítása
- Login oldal
- Register oldal
- Kijelentkezési folyamat
- Auth state kezelés
- Route védelem az authot igénylő oldalakhoz
- API kliens auth header kezeléssel
- Validációs hibák megjelenítése UI-n
- Sikeres login utáni átirányítás
- Egyszerű user session betöltés `me` endpoint alapján

Elfogadási feltétel:
- A felhasználó a UI-n keresztül tud regisztrálni, belépni, kijelentkezni, és a védett oldalak csak autentikált állapotban érhetők el.

### Iteráció 5: Projects backend + jogosultság
- `Project` entitás és konfiguráció
- Project membership kapcsolat kialakítása
- `POST /projects`
- `GET /projects`
- `GET /projects/{id}`
- `PATCH /projects/{id}`
- `DELETE /projects/{id}`
- `POST /projects/{id}/members`
- `DELETE /projects/{id}/members/{memberUserId}`
- Owner/member authorization szabályok implementálása
- Service réteg a projektekhez
- Project endpoint integration tesztek

Elfogadási feltétel:
- Owner és member szabályok működnek, idegen felhasználó nem fér hozzá tiltott műveletekhez.

### Iteráció 6: Projects frontend
- Projektlista oldal
- Projekt részletező oldal
- Új projekt létrehozó form
- Projekt szerkesztő form
- Tagkezelő UI:
  - member hozzáadás
  - member eltávolítás
- Jogosultságfüggő gombok és műveletek elrejtése vagy tiltása
- Hibák és üres állapotok megjelenítése
- Egyszerű loading state kezelés

Elfogadási feltétel:
- A felhasználó a felületen keresztül tud projektet létrehozni, megtekinteni, szerkeszteni, és a tagokat kezelni a jogosultsági szabályoknak megfelelően.

### Iteráció 7: Tasks backend CRUD + alap query
- `Task` entitás és konfiguráció:
  - `title`
  - `description`
  - `status`
  - `priority`
  - `assignedUserId`
  - `dueDate`
  - `projectId`
- Enumok bevezetése a `status` és `priority` mezőkhöz
- `POST /projects/{projectId}/tasks`
- `GET /projects/{projectId}/tasks`
- `GET /projects/{projectId}/tasks/{taskId}`
- `PATCH /projects/{projectId}/tasks/{taskId}`
- `DELETE /projects/{projectId}/tasks/{taskId}`
- Query paramok:
  - `status`
  - `priority`
  - `assigneeId`
  - `page`
  - `limit`
- Pagination response kialakítása
- Authorization project membership alapján
- Task endpoint integration tesztek

Elfogadási feltétel:
- A task CRUD működik, a lista szűrhető és lapozható, és csak jogosult felhasználók férnek hozzá.

### Iteráció 8: Tasks frontend
- Task lista oldal projekten belül
- Task részletező oldal
- Új task létrehozó form
- Task szerkesztő form
- Szűrő UI:
  - státusz
  - prioritás
  - hozzárendelt felhasználó
- Lapozás UI
- Törlés megerősítéssel
- Üres és loading állapotok
- Egyszerű státusz badge-ek és vizuális elkülönítés

Elfogadási feltétel:
- Taskok a UI-ról teljesen kezelhetők, a lista használható, a szűrés és lapozás működik.

### Iteráció 9: Haladó task query + teljesítmény
- További query paramok:
  - `dueFrom`
  - `dueTo`
  - `sortBy`
  - `sortOrder`
- `sortBy` támogatott mezők:
  - `createdAt`
  - `dueDate`
  - `priority`
  - `status`
- Kombinált szűrések implementálása
- Query validáció
- Limit cap beállítása
- PostgreSQL indexek a releváns mezőkre
- Lekérdezések ellenőrzése és finomítása
- Haladó task query integration tesztek

Elfogadási feltétel:
- A kombinált szűrések és rendezések stabilan működnek, invalid query esetén kontrollált hiba jön vissza.

### Iteráció 10: Haladó task frontend élmény
- Rendezési vezérlők a task listán
- Dátumszűrés UI
- Query string alapú szűrőállapot megőrzés
- Reset filter lehetőség
- Felhasználóbarát állapotkezelés üres találat esetén
- Egyszerű komponensbontás:
  - `TaskFilters`
  - `TaskTable` vagy `TaskList`
  - `PaginationControls`

Elfogadási feltétel:
- A task lista UI már kényelmesen használható, a szűrési és rendezési állapotok jól követhetők és stabilak.

### Iteráció 11: Comments backend
- `Comment` entitás és konfiguráció
- `POST /tasks/{taskId}/comments`
- `GET /tasks/{taskId}/comments`
- `GET /tasks/{taskId}/comments/{commentId}`
- `PATCH /tasks/{taskId}/comments/{commentId}`
- `DELETE /tasks/{taskId}/comments/{commentId}`
- Pagination a komment listához
- Author vagy project owner alapú jogosultsági szabályok
- Request validáció
- Comment endpoint integration tesztek

Elfogadási feltétel:
- A kommentek létrehozása, olvasása, módosítása és törlése megfelelő jogosultságokkal működik, a lista lapozható.

### Iteráció 12: Comments frontend
- Kommentlista task részletező oldalon
- Új komment beküldő form
- Komment szerkesztés
- Komment törlés
- Jogosultságfüggő szerkesztési és törlési lehetőségek
- Lapozható kommentnézet
- Hibák megjelenítése

Elfogadási feltétel:
- A kommentek UI-ról teljesen kezelhetők, és a szerzői / owner jogosultságoknak megfelelő műveletek jelennek meg.

### Iteráció 13: Frontend finomítás és általános UX
- Közös komponensek kialakítása:
  - visszajelző üzenetek
  - validációs hiba megjelenítés
  - megerősítő dialógus
  - loading indicator
- Layout finomítás
- Navigáció átgondolása
- Hibakezelési oldalak:
  - 401
  - 403
  - 404
- Egységes form stílus és input komponensek
- Egységes API error mapping a frontendben

Elfogadási feltétel:
- A felület egységes, a hibák kulturáltan jelennek meg, a flow végig konzisztens.

### Iteráció 14: Security hardening + best practice véglegesítés
- Rate limiting auth endpointokra
- CORS és cookie / token stratégia véglegesítése
- Secret és konfigurációkezelés rendbetétele
- Production jellegű auth és environment beállítások
- Input validációs stratégia véglegesítése
- Naplózás és auditálhatóság alapjainak javítása
- Biztonsági edge case tesztek
- Dockeres és lokális konfigurációk tiszta szétválasztása
- PostgreSQL connection resilience és startup hibák kezelése

Elfogadási feltétel:
- Az auth és a hibakezelés stabil, a konfigurációk rendezettek, az app kontrolláltan viselkedik rossz input vagy hibás auth állapot esetén.

### Iteráció 15: Dokumentáció + release readiness
- Swagger / OpenAPI dokumentáció a teljes backendhez
- Dockeres környezetindítás dokumentálása
- Fejlesztői README elkészítése
- Release checklist:
  - Docker indítás
  - migráció
  - seed
  - env
  - auth config
  - health-check
- Fő felhasználói flow-k ellenőrzése
- Teljes regressziós kör manuális vagy félautomata módon

Elfogadási feltétel:
- Az alkalmazás dokumentált, tesztelt, bemutatható és újraindítható más környezetben is.

## Adatmodell (EF Core)
- `Users`
- `Projects`
- `ProjectMembers` vagy join tábla
- `Tasks`
- `Comments`

Tervezett relációk:
- User 1-N Projects (owner)
- User N-N Projects (member)
- Project 1-N Tasks
- User 1-N Tasks (assignee)
- Task 1-N Comments
- User 1-N Comments (author)

## API szerződéshez igazodó validációs és formátum szabályok
- Request validáció minden write endpointon
- `status` csak: `TODO` | `IN_PROGRESS` | `DONE`
- `priority` csak: `LOW` | `MEDIUM` | `HIGH`
- `dueDate`, `dueFrom`, `dueTo` ISO date stringként kezelve
- `page` minimum 1
- `limit` 1-100

### Egységes idő formátum használata
- ISO 8601
- Az API UTC-ben vagy offsettel együtt adja vissza az időpontokat
- A frontend a megjelenítéskor alakíthatja át lokális, például magyar formátumra

Egységes sikeres response forma:

```json
{
  "success": true,
  "data": {}
}

Egységes hiba response forma:
{
  "success": false,
  "statusCode": 400,
  "message": "...",
  "path": "/requested/path",
  "timestamp": "2026-03-31T12:00:00.000Z"
}