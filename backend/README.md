# StudentPass API (C# + MySQL)

Альтернативный бэкенд на **ASP.NET Core 10** и **MySQL 8**. Совместим с Vue-фронтендом по путям `/api/v1/...` (те же контракты, что у Python `service_users`).

Папка **не заменяет** `backend/` (FastAPI) — это отдельный вариант для разработки и деплоя на .NET.

## Стек

| Компонент | Технология |
|-----------|------------|
| API | ASP.NET Core Web API |
| БД | MySQL 8 (Pomelo EF Core) |
| Auth | JWT в cookie `access_token` + Bearer |
| Пароли | BCrypt |
| Письма (код регистрации) | SMTP через `SMTP_*` в `.env`; без SMTP — код в логах API |

## Быстрый старт (Docker)

```bash
cd backend-csharp
docker compose up --build
```

API: **http://localhost:8080**

Проверка:

```bash
curl http://localhost:8080/api/v1/health
```

Код подтверждения при регистрации отправляется по SMTP (`SMTP_*` в `.env`). Без SMTP — только в лог контейнера `api`.

## Локально без Docker

1. Установите [.NET SDK 10](https://dotnet.microsoft.com/download) и MySQL 8.
2. Создайте БД и пользователя (или используйте строку из `appsettings.json`).
3. Запуск:

```bash
cd backend-csharp/StudentPass.Api
dotnet run
```

По умолчанию: **http://localhost:5199** (см. `Properties/launchSettings.json`).

## Связь с фронтендом

В `frontend/.env.development`:

```env
VITE_API_BASE_URL=http://localhost:8080
```

(или порт `dotnet run`, если без Docker).

Перезапустите `npm run dev` после смены URL.

## Эндпоинты

Реализованы те же маршруты, что в `backend/service_users`:

- `POST /api/v1/auth/send_code`, `register`, `login`, `logout`, `me`, …
- `GET/POST /api/v1/ads`, `categories`, клики
- `GET/POST/DELETE /api/v1/favorites`
- `GET /api/v1/partners`, `partners/{id}/ads`
- `GET/POST/PUT/DELETE /api/v1/partner/ads`
- `GET/POST/DELETE /api/v1/admin/...`

## Переменные окружения (Docker)

Скопируйте `.env.example` в `.env` при необходимости. В `docker-compose.yml` используются:

- `MYSQL_PASSWORD`, `MYSQL_ROOT_PASSWORD`
- `JWT_SECRET_KEY`, `JWT_ALGORITHM`, `ACC_TOKEN_EXP_MIN`

## Структура

```
backend-csharp/
  StudentPass.slnx
  StudentPass.Api/
    Controllers/     # HTTP API
    Data/            # EF Core DbContext
    Entities/        # Модели таблиц
    Dtos/            # JSON-контракты
    Services/        # JWT, email, маппинг
  docker-compose.yml
```

## Отличия от Python-бэкенда

- Один сервис вместо gateway + users + notify + RabbitMQ
- MySQL вместо PostgreSQL
- Хеш паролей BCrypt (в Python — Argon2); базы не совместимы без миграции данных
- Письма: `SmtpEmailSender` (MailKit) при заданном `SMTP_HOST`, иначе `ConsoleEmailSender`
