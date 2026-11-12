# Task Planner

Веб-API та клієнти для **планування навчальних завдань**: дисципліни, дедлайни, пріоритети, статуси; у моделі також закладені дані для доступного часу та навчальних сесій (розвиток за [`CONCEPT.md`](CONCEPT.md)).

---

## Технології

| Компонент | Технологія |
|-----------|------------|
| Runtime | .NET 8 |
| API | ASP.NET Core Web API |
| Frontend | React, TypeScript, Vite |
| Дані | Entity Framework Core 8 |
| СУБД | **SQLite** (локально) / **PostgreSQL** (сервер) |
| Валідація | FluentValidation |
| Автентифікація | JWT Bearer, ASP.NET Identity PasswordHasher |
| Документація API | Swagger / OpenAPI |
| Клієнт (GUI) | .NET MAUI 8 |

---

## Структура репозиторію

```
TaskPlanner/
├── TaskPlanner.sln          # API + Domain / Application / Infrastructure
├── TaskPlanner.Maui.sln     # лише проєкт MAUI (потрібні workload’и MAUI)
├── CONCEPT.md
└── src/
    ├── TaskPlanner.Domain/
    ├── TaskPlanner.Application/
    ├── TaskPlanner.Infrastructure/
    ├── TaskPlanner.Api/
    └── TaskPlanner.Maui/
```

**Залежності бекенду:** `Api → Infrastructure → Application → Domain`.

---

## Модель даних (скорочено)

| Сутність | Призначення |
|----------|-------------|
| `User` | обліковий запис |
| `Discipline` | навчальна дисципліна |
| `StudyTask` | завдання |
| `AvailabilityWindow` / `AvailabilityException` | доступний час |
| `StudySession` | навчальна сесія |

Перемикання СУБД: `Database:Provider` = `Sqlite` | `PostgreSQL`; окремі міграції у `Persistence/Migrations/Sqlite` та `.../PostgreSQL`.

---

## Вимоги

- [.NET SDK 8](https://dotnet.microsoft.com/download)
- PostgreSQL — якщо обрано цей провайдер
- Для міграцій EF: `dotnet tool install -g dotnet-ef`
- Для MAUI-клієнта: [workload’и .NET MAUI](https://learn.microsoft.com/dotnet/maui/get-started/installation)

---

## Швидкий старт

### Бекенд

```bash
cd TaskPlanner
dotnet restore TaskPlanner.sln
dotnet build TaskPlanner.sln
```

### MAUI-клієнт

```bash
dotnet workload restore
dotnet build TaskPlanner.Maui.sln
```

Перед запуском клієнта підніміть API (наприклад профіль `http`, порт **5109**). У застосунку на екрані **Налаштування** вкажіть базову URL, наприклад `http://localhost:5109/` (Windows) або `http://10.0.2.2:5109/` (Android Emulator).

Адресу API можна змінити на екрані входу, ще до авторизації. Дедлайн завдання має дату й час. Нагадування: за годину до дедлайну, у момент дедлайну, на початку вікна навчання і вранці (план на день або вихідний). Час ранкового повідомлення задається в налаштуваннях. На Android і iOS вони ставляться після відкриття списку завдань; на Windows вибір лише зберігається.

---

## Запуск API

### SQLite + Swagger

```bash
dotnet run --launch-profile http --project src/TaskPlanner.Api/TaskPlanner.Api.csproj
```

- Swagger: [http://localhost:5109/swagger](http://localhost:5109/swagger)
- У профілі `http` задано `JWT_SECRET` і `Database__Provider=Sqlite`.

### Запуск React-клієнта

Клієнтська частина застосунку реалізована на React, TypeScript і Vite та розташована в каталозі `client/`.

Для запуску необхідно встановити Node.js і npm та запустити ASP.NET Core API за інструкцією з попереднього розділу.

**1. Запуск frontend**

Відкрийте другий термінал із кореня репозиторію та виконайте:

```bash
cd client
npm install
npm run dev
```

React-клієнт буде доступний за адресою `http://localhost:5173`.

**2. Взаємодія з API**

У режимі розробки Vite використовує proxy. Запити до `/api` автоматично перенаправляються на `http://localhost:5109`.

За потреби адресу backend можна змінити через змінну середовища `VITE_API_URL` перед запуском Vite.

**3. Production-збірка**

Для перевірки збірки клієнта виконайте в каталозі `client/`:

```bash
npm run build
```

### PostgreSQL

```bash
dotnet run --launch-profile http-postgres --project src/TaskPlanner.Api/TaskPlanner.Api.csproj
```

Порт **5110**. Не змішуйте `Database__Provider=PostgreSQL` з профілем `http` (там зафіксовано SQLite).

---

## Міграції

```bash
# SQLite
dotnet ef database update \
  --project src/TaskPlanner.Infrastructure/TaskPlanner.Infrastructure.csproj \
  --startup-project src/TaskPlanner.Api/TaskPlanner.Api.csproj \
  --context SqliteAppDbContext

# PostgreSQL
JWT_SECRET='LocalDev_JwtSecretKey_Min32CharactersLong!' dotnet ef database update \
  --project src/TaskPlanner.Infrastructure/TaskPlanner.Infrastructure.csproj \
  --startup-project src/TaskPlanner.Api/TaskPlanner.Api.csproj \
  --context PostgresAppDbContext
```

При старті API виконується `MigrateAsync()` для активного провайдера.

---

## Конфігурація

Файл: [`src/TaskPlanner.Api/appsettings.json`](src/TaskPlanner.Api/appsettings.json).

| Ключ | Опис |
|------|------|
| `Database:Provider` | `Sqlite` або `PostgreSQL` |
| `ConnectionStrings:*` | рядки підключення |
| `Jwt:*` | issuer, audience, термін токена |
| `Cors:Origins` | дозволені origins (у т. ч. для локальних клієнтів) |
| `Seed:AdminEmail` | email dev-адміна |

Секрет JWT не комітиться: змінна `JWT_SECRET` (або профілі в `launchSettings.json` для локальної розробки).

---

## Swagger і JWT

1. `POST /api/auth/login` з обліковими даними (наприклад dev seed у **Development**).
2. Скопіювати `accessToken`.
3. У Swagger: **Authorize** → вставити токен **без** префікса `Bearer`.
4. Викликати `/api/tasks`, `/api/disciplines` тощо.

Публічні: `POST /api/auth/register`, `POST /api/auth/login`.

---

## HTTP API (огляд)

| Група | Base path | Авторизація |
|-------|-----------|-------------|
| Auth | `/api/auth` | register/login — публічно; `/me` — JWT |
| Завдання | `/api/tasks` | JWT |
| Дисципліни | `/api/disciplines` | JWT |
| Admin | `/api/admin/users` | JWT + роль **Admin** |

Деталі query-параметрів — у Swagger.

Замість узагальненого шляху для завдань використано **`/api/tasks`** (назва відповідає доменній сутності `StudyTask`).

---

## Облікові записи (Development)

| Email | Пароль | Роль |
|-------|--------|------|
| `admin@taskplanner.local` | `Admin123!` | Admin (seed у Development) |

Студентські акаунти створюйте через `POST /api/auth/register`.

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
- [x] STEM — React UI, управління завданнями, навчальні сесії, календар, перевірка навантаження та профіль користувача

---

## Типові проблеми

| Симптом | Що перевірити |
|---------|---------------|
| 401 у Swagger | Спочатку login, потім Authorize з `accessToken` |
| Запущено SQLite замість PG | Профіль `http-postgres` |
| `JWT secret is not configured` | `JWT_SECRET` або launch-профіль |
| Помилка міграції | `dotnet ef database update` або чистий файл SQLite |
| PostgreSQL: помилка автентифікації | `pg_hba.conf`, пароль у рядку підключення |
| MAUI: помилка workload | `dotnet workload restore` |
| MAUI + Android + HTTP | URL `http://10.0.2.2:5109/` до хоста з API |

---

## Ліцензія

Проєкт з освітньою метою; умови використання за потреби уточнюйте у автора.
