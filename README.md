# Task Planner

**STEM-планувальник навчальних завдань** (дисципліни, дедлайни, доступний час, сесії розкладу) у рамках курсу **«Мультиплатформне програмування»**. Репозиторій одночасно закриває лабораторні роботи та проєкт — деталі предметної області в [`CONCEPT.md`](CONCEPT.md).

---

## Технології

| Компонент | Технологія |
|-----------|------------|
| Runtime | .NET 8 |
| API | ASP.NET Core Web API |
| Дані | Entity Framework Core 8 |
| СУБД | **SQLite** (локально) / **PostgreSQL** (сервер) |
| Валідація | FluentValidation |
| Auth | JWT Bearer, ASP.NET Identity PasswordHasher |
| Документація API | Swagger / OpenAPI |

---

## Структура solution

```
TaskPlanner/
├── TaskPlanner.sln
├── CONCEPT.md                 # STEM: сценарії та модулі
└── src/
    ├── TaskPlanner.Domain/       # сутності, enum (без EF)
    ├── TaskPlanner.Application/  # DTO, інтерфейси сервісів, validators
    ├── TaskPlanner.Infrastructure/ # EF Core, міграції, реалізації сервісів, JWT
    └── TaskPlanner.Api/            # HTTP, Swagger, composition root
```

**Залежності:** `Api → Infrastructure → Application → Domain`.

---

## Модель даних (скорочено)

| Сутність | Призначення |
|----------|-------------|
| `User` | обліковий запис (email, hash пароля, роль) |
| `Discipline` | навчальна дисципліна |
| `StudyTask` | завдання (дедлайн, пріоритет, час, статус) |
| `AvailabilityWindow` / `AvailabilityException` | доступний час (STEM) |
| `StudySession` | запланована навчальна сесія (STEM) |

Перемикання СУБД: ключ `Database:Provider` = `Sqlite` | `PostgreSQL`, окремі connection strings і **окремі набори міграцій** у `Persistence/Migrations/Sqlite` та `.../PostgreSQL`.

---

## Вимоги

- [.NET SDK 8](https://dotnet.microsoft.com/download)
- Для PostgreSQL: запущений сервер і база (за замовчуванням у `appsettings.json`: `planner_db` / `planner_user`)
- Для міграцій EF: `dotnet tool install -g dotnet-ef` (або локальний tool у репозиторії)

---

## Швидкий старт

```bash
cd TaskPlanner
dotnet restore TaskPlanner.sln
dotnet build TaskPlanner.sln
```

### Запуск API (SQLite, Swagger)

```bash
dotnet run --launch-profile http --project src/TaskPlanner.Api/TaskPlanner.Api.csproj
```

- Swagger: [http://localhost:5109/swagger](http://localhost:5109/swagger)
- У профілі `http` уже задано `JWT_SECRET` і `Database__Provider=Sqlite`.

### PostgreSQL

```bash
dotnet run --launch-profile http-postgres --project src/TaskPlanner.Api/TaskPlanner.Api.csproj
```

Порт **5110**. Переконайтеся, що PostgreSQL доступний і рядок підключення у [`appsettings.json`](src/TaskPlanner.Api/appsettings.json) актуальний.

> **Не змішуйте** `Database__Provider=PostgreSQL` з профілем `http`: у `launchSettings.json` профіль `http` жорстко задає SQLite. Для PG використовуйте `http-postgres`.

### Міграції

```bash
# SQLite
dotnet ef database update \
  --project src/TaskPlanner.Infrastructure/TaskPlanner.Infrastructure.csproj \
  --startup-project src/TaskPlanner.Api/TaskPlanner.Api.csproj \
  --context SqliteAppDbContext

# PostgreSQL (потрібен JWT_SECRET для design-time/hosting)
JWT_SECRET='LocalDev_JwtSecretKey_Min32CharactersLong!' dotnet ef database update \
  --project src/TaskPlanner.Infrastructure/TaskPlanner.Infrastructure.csproj \
  --startup-project src/TaskPlanner.Api/TaskPlanner.Api.csproj \
  --context PostgresAppDbContext
```

При старті API також виконується `MigrateAsync()` для активного провайдера.

---

## Конфігурація

Файл: [`src/TaskPlanner.Api/appsettings.json`](src/TaskPlanner.Api/appsettings.json).

| Ключ | Опис |
|------|------|
| `Database:Provider` | `Sqlite` або `PostgreSQL` |
| `ConnectionStrings:Sqlite` | шлях до файлу БД |
| `ConnectionStrings:PostgreSQL` | Npgsql connection string |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:AccessTokenMinutes` | параметри JWT |
| `Cors:Origins` | дозволені origins (майбутній React-клієнт) |
| `Seed:AdminEmail` | email dev-адміна |

**Секрет JWT не зберігається в репозиторії.** Задайте одне з:

- змінну середовища `JWT_SECRET` (мінімум ~32 символи);
- у локальній розробці — значення в `launchSettings.json` профілів `http` / `http-postgres`;
- для production — User Secrets, змінні CI/CD, секрети контейнера.

---

## Swagger і JWT

1. Виконайте `POST /api/auth/login` (без попереднього Authorize), наприклад:
   ```json
    { "email": "admin@taskplanner.local", "password": "Admin123!" }
   ```

    Dev-адмін створюється seeder’ом у **Development** (`AuthDataSeeder`).
2. Скопіюйте `accessToken` з відповіді.
3. У Swagger натисніть **Authorize** і вставте **лише token** (без префікса `Bearer`).
4. Викликайте захищені endpoint’и (`/api/tasks`, `/api/disciplines`, …).

Публічні: `POST /api/auth/register`, `POST /api/auth/login`.

---

## HTTP API (огляд)

| Група      | Base path          | Авторизація                            |
| ---------- | ------------------ | -------------------------------------- |
| Auth       | `/api/auth`        | register/login — публічно; `/me` — JWT |
| Завдання   | `/api/tasks`       | JWT (власні завдання; admin — усі)     |
| Дисципліни | `/api/disciplines` | JWT (`userId` з token, не з body)      |
| Admin      | `/api/admin/users` | JWT + роль **Admin**                   |
| Розклад    | `/api/schedule`    | JWT (лише дані поточного користувача)  |

**Query для списків:** фільтрація, `search`, `sort` — див. Swagger.

`POST /api/schedule/generate` ставить сесії зі статусом `Planned` у вільні години. Горизонт — `from`/`to` (UTC, до 92 днів) або `days` (1–92 від поточного часу). Повторний виклик замінює незаблоковані `Planned`-сесії в цьому горизонті, а не дублює їх. Відповідь містить створені сесії, роботу, яка не вмістилась, і перетини заморожених сесій.

`POST /api/schedule/replan` перебудовує той самий горизонт: пропущені та майбутні незаблоковані `Planned`-сесії знімаються, завершені й `IsLocked` лишаються, залишок розкладається знову. Відповідь окремо показує, що залишилось, що створено і що знято. Правила — у [`docs/scheduling.md`](docs/scheduling.md).

---

## Тестові облікові записи (Development)

| Email | Пароль | Роль |
|-------|--------|------|
| `admin@taskplanner.local` | `Admin123!` | Admin (seed) |

Студентські акаунти створюйте через `POST /api/auth/register`.

Пароль admin у production **не** використовувати; змініть seed або створюйте admin вручну.

---

## Типові проблеми

| Симптом                        | Що перевірити                                                             |
| ------------------------------ | ------------------------------------------------------------------------- |
| Swagger: 401 на всіх запитах   | Спочатку login; потім Authorize з `accessToken`; login/register без замка |
| Запустилось SQLite замість PG  | Профіль `http-postgres`, не `http` + env                                  |
| `JWT secret is not configured` | `JWT_SECRET` або профіль launch з цією змінною                            |
| Помилка міграції на старій БД  | `dotnet ef database update` або новий файл SQLite у `Data/`               |
| PostgreSQL: auth failed        | `pg_hba.conf`, пароль у connection string                                 |

---

## Ліцензія / авторство

Навчальний проєкт курсу МПП.
