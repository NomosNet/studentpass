# StudentPass Backend

Python-микросервисы: API Gateway, Users, Notify, PostgreSQL, Redis, RabbitMQ.

## Локальная разработка

```bash
cd backend
cp .env.example .env
# отредактируйте .env
docker compose up --build
```

API Gateway: http://localhost:8080  
Health: http://localhost:8080/api/v1/health

## Production (VPS)

```bash
cd backend
docker compose -f docker-compose.prod.yml --env-file .env up -d --build
```

Порт API только на localhost (`127.0.0.1:8080`), снаружи — через nginx из `deploy/nginx/studentpass.conf`.
