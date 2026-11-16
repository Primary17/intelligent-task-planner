# Task Planner

Task Planner helps students organise university work: disciplines, deadlines, priorities, weekly availability, and study sessions. A .NET backend exposes a REST API; **React** is the primary web client for planning; **.NET MAUI** is a mobile companion for tasks, reminders, and read-only upcoming sessions.

Product vision and scope are described in [`CONCEPT.md`](CONCEPT.md). Scheduling rules and algorithm behaviour are in [`docs/scheduling.md`](docs/scheduling.md).

---

## Technology stack

| Component | Technology |
|-----------|------------|
| Runtime | .NET 8 |
| API | ASP.NET Core Web API |
| Web client | React 18, TypeScript, Vite |
| Mobile client | .NET MAUI 8 |
| Data access | Entity Framework Core 8 |
| Database | **SQLite** (local dev) / **PostgreSQL** (server) |
| Validation | FluentValidation |
| Authentication | JWT Bearer, ASP.NET Core Identity password hashing |
| API docs | Swagger / OpenAPI |

---

## Repository layout

```
TaskPlanner/
├── TaskPlanner.sln              # API and backend layers
├── TaskPlanner.Maui.sln         # MAUI app only (requires MAUI workloads)
├── client/                      # React SPA
├── CONCEPT.md
├── docs/
│   └── scheduling.md            # Planner rules and API semantics
└── src/
    ├── TaskPlanner.Domain/
    ├── TaskPlanner.Application/
    ├── TaskPlanner.Infrastructure/
    ├── TaskPlanner.Api/
    └── TaskPlanner.Maui/
```

Backend dependency flow: `Api → Infrastructure → Application → Domain`.

---

## Clients at a glance

| Capability | React (web) | MAUI (mobile) |
|------------|-------------|---------------|
| Login / register | Yes | Yes |
| Disciplines & tasks CRUD | Yes | Yes |
| Weekly availability & exceptions | Yes | No |
| Manual study sessions | Yes | No |
| **Generate / replan schedule** | Yes | No (by design) |
| Calendar view | Yes | No |
| Upcoming sessions (read-only) | Yes (calendar) | Yes (dedicated tab) |
| Local deadline / study reminders | — | Android / iOS |
| Capacity / workload hints | Yes | No |

The web calendar sends `POST /api/schedule/generate` and `POST /api/schedule/replan`. The mobile app consumes sessions produced by the API; it does not trigger automatic planning.

---

## Data model (short)

| Entity | Role |
|--------|------|
| `User` | Account |
| `Discipline` | Course / subject |
| `StudyTask` | Assignment with deadline, estimate, priority, status |
| `AvailabilityWindow` / `AvailabilityException` | When the user can study |
| `StudySession` | Planned or completed block of work on a task |

Switch database provider via `Database:Provider` (`Sqlite` | `PostgreSQL`). EF migrations live under `src/TaskPlanner.Infrastructure/Persistence/Migrations/Sqlite` and `.../PostgreSQL`.

---

## Prerequisites

- [.NET SDK 8](https://dotnet.microsoft.com/download) (see `global.json` in projects if present)
- [Node.js](https://nodejs.org/) and npm for the React client
- PostgreSQL — only when using the PostgreSQL provider
- EF tools: `dotnet tool install -g dotnet-ef`
- MAUI (optional): [Install .NET MAUI workload](https://learn.microsoft.com/dotnet/maui/get-started/installation)

---

## Quick start

### 1. Backend

From the repository root:

```bash
dotnet restore TaskPlanner.sln
dotnet build TaskPlanner.sln
dotnet run --launch-profile http --project src/TaskPlanner.Api/TaskPlanner.Api.csproj
```

- API: [http://localhost:5109](http://localhost:5109)
- Swagger: [http://localhost:5109/swagger](http://localhost:5109/swagger)
- The `http` launch profile sets `JWT_SECRET` and `Database__Provider=Sqlite` for local development.

### 2. React client

In a second terminal:

```bash
cd client
npm install
npm run dev
```

- App: [http://localhost:5173](http://localhost:5173)
- Dev server proxies `/api` to `http://localhost:5109` (override with `VITE_API_URL` if needed).

Production build check:

```bash
cd client
npm run build
npm test
```

### 3. MAUI companion (optional)

```bash
dotnet workload restore
dotnet build src/TaskPlanner.Maui/TaskPlanner.Maui.csproj -f net8.0-android
# or -f net8.0-ios / net8.0-windows10.0.19041.0 on supported hosts
```

Start the API first. Set the **API base URL** on the login or settings screen:

| Where the app runs | Base URL |
|--------------------|----------|
| Windows / iOS Simulator | `http://localhost:5109/` |
| Android emulator | `http://10.0.2.2:5109/` |
| Physical phone (same LAN) | `http://<your-pc-ip>:5109/` |

**Reminders (Android / iOS):** configurable in settings (hour before deadline, at deadline, study-window start, morning summary). The app syncs reminders with the server after login, when returning to the app, and when notification preferences change. Grant notification and (on Android 12+) exact-alarm permissions for reliable delivery. Windows stores preferences only; the OS does not show local notifications.

---

## PostgreSQL profile

```bash
dotnet run --launch-profile http-postgres --project src/TaskPlanner.Api/TaskPlanner.Api.csproj
```

Listens on port **5110**. Do not mix `Database__Provider=PostgreSQL` with the `http` profile (that profile forces SQLite).

---

## Database migrations

```bash
# SQLite
dotnet ef database update \
  --project src/TaskPlanner.Infrastructure/TaskPlanner.Infrastructure.csproj \
  --startup-project src/TaskPlanner.Api/TaskPlanner.Api.csproj \
  --context SqliteAppDbContext

# PostgreSQL (example with dev secret)
JWT_SECRET='LocalDev_JwtSecretKey_Min32CharactersLong!' dotnet ef database update \
  --project src/TaskPlanner.Infrastructure/TaskPlanner.Infrastructure.csproj \
  --startup-project src/TaskPlanner.Api/TaskPlanner.Api.csproj \
  --context PostgresAppDbContext
```

On startup, the API runs `MigrateAsync()` for the active provider.

---

## Configuration

Main file: [`src/TaskPlanner.Api/appsettings.json`](src/TaskPlanner.Api/appsettings.json).

| Key | Purpose |
|-----|---------|
| `Database:Provider` | `Sqlite` or `PostgreSQL` |
| `ConnectionStrings:*` | Provider connection strings |
| `Jwt:*` | Issuer, audience, token lifetime |
| `Cors:Origins` | Allowed origins (Vite, local API, etc.) |
| `Seed:AdminEmail` | Development admin seed |

Do not commit production JWT secrets. Use environment variable `JWT_SECRET` (or launch profiles locally).

---

## Swagger and JWT

1. Call `POST /api/auth/login` (or register, then login).
2. Copy `accessToken` from the response.
3. In Swagger, click **Authorize** and paste the token **without** a `Bearer ` prefix.
4. Call protected endpoints (`/api/tasks`, `/api/disciplines`, `/api/schedule/...`, etc.).

Public: `POST /api/auth/register`, `POST /api/auth/login`.

---

## HTTP API overview

| Area | Base path | Auth |
|------|-----------|------|
| Auth | `/api/auth` | Register/login public; `/me` requires JWT |
| Tasks | `/api/tasks` | JWT (own tasks; admin sees all) |
| Disciplines | `/api/disciplines` | JWT |
| Availability | `/api/availability/*` | JWT |
| Sessions | `/api/sessions` | JWT |
| Planning | `/api/planning/*` | JWT |
| Schedule | `/api/schedule` | JWT |
| Admin | `/api/admin/users` | JWT + **Admin** role |

Query parameters and response shapes are documented in Swagger.

### Schedule endpoints

- **`POST /api/schedule/generate`** — places new `Planned` sessions in free availability. Horizon: explicit `from` / `to` (UTC, max 92 days) or `days` (1–92 from now). Re-running **replaces unlocked `Planned` sessions** in that horizon (not locked, not completed). Returns created sessions, work that did not fit, and time conflicts involving existing sessions.

- **`POST /api/schedule/replan`** — same horizon rules; removes **missed** and future **unlocked planned** sessions, keeps **completed** and **locked** sessions, then re-plans remaining work. Response lists stayed, created, removed, unscheduled, and conflicts.

The React calendar builds the request horizon from the **visible week through the latest deadline of open tasks** (capped at 92 days), so tasks due in later weeks receive sessions without regenerating week by week.

The planner treats **all existing sessions** in the request as blocking free time (including planned and missed), so newly generated sessions do not overlap them in time.

Details: [`docs/scheduling.md`](docs/scheduling.md).

---

## Development accounts

| Email | Password | Role |
|-------|----------|------|
| `admin@taskplanner.local` | `Admin123!` | Admin (seeded in Development only) |

Create student accounts with `POST /api/auth/register`. Change or disable the admin seed in production.

---

## Continuous integration

GitHub Actions (`.github/workflows/dotnet.yml`) restores, builds, and tests **`TaskPlanner.sln`** on Ubuntu. It does **not** build MAUI (no mobile workloads on the agent). Build the mobile project locally.

The React client is not run in CI by default; run `npm test` and `npm run build` locally or extend CI as needed.

---

## Troubleshooting

| Symptom | What to check |
|---------|----------------|
| 401 in Swagger | Login first, then Authorize with `accessToken` |
| SQLite DB used instead of PostgreSQL | Use `http-postgres` launch profile |
| `JWT secret is not configured` | Set `JWT_SECRET` or use a launch profile |
| EF migration errors | Run `dotnet ef database update` or remove stale SQLite file |
| PostgreSQL auth failure | Credentials, `pg_hba.conf`, connection string |
| `npm ci` fails | Run `npm install` (lockfile may list optional platform packages) |
| MAUI workload errors | `dotnet workload restore` |
| MAUI cannot reach API on Android emulator | Base URL `http://10.0.2.2:5109/` |
| No mobile notifications | OS notification permission; Android exact-alarm setting |
| Schedule still looks crowded in UI | Refresh; narrow sessions show time only—use tooltip for full title |

---

## Testing

```bash
# Backend
dotnet test TaskPlanner.sln -c Release

# React
cd client && npm test
```

Scheduling behaviour has unit tests in `tests/TaskPlanner.Tests/Scheduling/` and API tests in `tests/TaskPlanner.Tests/Api/ScheduleTests.cs`.

---

## License

Educational project; clarify terms of use with the authors if you redistribute or deploy publicly.