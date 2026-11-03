# Task Planner

Бекенд персонального **STEM-планувальника навчальних завдань** (дисципліни, дедлайни, доступний час, сесії розкладу) у рамках курсу **«Мультиплатформне програмування»**. Репозиторій одночасно закриває лабораторні роботи та базу для семестрового проєкту — деталі предметної області в [`Planner.md`](Planner.md).

**Поточний стан:** реалізовано **ЛР1** (EF Core, SQLite/PostgreSQL), **ЛР2** (REST API, DTO, сервіси), **ЛР3** (JWT, ролі, ізоляція даних). Далі — MAUI-клієнт (ЛР4) і Docker (ЛР5).

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
├── Planner.md                 # STEM: сценарії та модулі
├── commands/                  # приклади curl для лабораторних
├── reports/                   # артефакти прогонів (за потреби)
├── МПП_ЛР_*                   # методички та звіти
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
| Demo ЛР1   | `/api/lab1`        | JWT + **Admin**                        |

**Query для списків:** фільтрація, `search`, `sort` — див. Swagger або [`commands/lab2-api-demo.txt`](commands/lab2-api-demo.txt).

**Відхилення від методички ЛР2:** замість шаблонного `/api/resources` використовується `/api/tasks` — зрозуміліше для STEM і майбутнього MAUI-клієнта (обґрунтування — у звітах ЛР2).

---

## Демо-сценарії

| Файл | Зміст |
|------|--------|
| [`commands/lab1-demo.txt`](commands/lab1-demo.txt) | EF demo (`--demo`), SQLite/PG |
| [`commands/lab2-api-demo.txt`](commands/lab2-api-demo.txt) | CRUD через HTTP |
| [`commands/lab3-auth-demo.txt`](commands/lab3-auth-demo.txt) | register, login, Bearer, admin |

Консольний demo ЛР1 (JSON-звіт EF):

```bash
Database__Provider=Sqlite dotnet run --no-launch-profile \
  --project src/TaskPlanner.Api/TaskPlanner.Api.csproj -- --demo
```

---

## Тестові облікові записи (Development)

| Email | Пароль | Роль |
|-------|--------|------|
| `admin@taskplanner.local` | `Admin123!` | Admin (seed) |
| `demo@taskplanner.local` | `Demo1234!` | User (після `POST /api/lab1/scenario` під admin) |

Пароль admin у production **не** використовуйте; змініть seed або створюйте admin вручну.

---

## Звіти та навчальні матеріали

| ЛР | Здача | Для себе |
|----|-------|----------|
| 1 | [`МПП_ЛР_1_Звіт_для_здачі.md`](МПП_ЛР_1_Звіт_для_здачі.md) | [`МПП_ЛР_1_Навчальний_звіт.md`](МПП_ЛР_1_Навчальний_звіт.md) |
| 2 | [`МПП_ЛР_2_Звіт_для_здачі.md`](МПП_ЛР_2_Звіт_для_здачі.md) | [`МПП_ЛР_2_Навчальний_звіт.md`](МПП_ЛР_2_Навчальний_звіт.md) |
| 3 | [`МПП_ЛР_3_Звіт_для_здачі.md`](МПП_ЛР_3_Звіт_для_здачі.md) | [`МПП_ЛР_3_Навчальний_звіт.md`](МПП_ЛР_3_Навчальний_звіт.md) |

---

## Дорожня карта

- [x] ЛР1 — EF Core, multi-DB, migrations, LINQ
- [x] ЛР2 — REST, DTO, сервісний шар, Swagger
- [x] ЛР3 — JWT, User/Admin, policies, ізоляція ресурсів
- [ ] ЛР4 — .NET MAUI клієнт
- [ ] ЛР5 — Docker / Compose
- [ ] STEM — React UI, планувальник, календар (за `Planner.md`)

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
