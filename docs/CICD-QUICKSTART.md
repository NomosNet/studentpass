# CI/CD: ветка `prod` = сайт на VPS

## Как это работает

```text
develop  →  CI (сборка, без деплоя)
    │
    └── PR merge ──►  prod  ──push──►  GitHub Actions  ──SSH──►  VPS
```

**На сайте всегда код из `origin/prod`.** Push в `develop` VPS не трогает.

---

## Один раз на VPS

Подключение: `root@161.104.33.28`

### 1. Софт

```bash
apt update && apt upgrade -y
apt install -y git curl nginx rsync
curl -fsSL https://get.docker.com | sh
curl -fsSL https://deb.nodesource.com/setup_20.x | bash -
apt install -y nodejs
```

### 2. Клон репозитория

```bash
mkdir -p /opt/studentpass
cd /opt/studentpass
git clone https://github.com/NomosNet/studentpass.git .
git checkout prod
```

### 3. Секреты бэкенда

```bash
cd /opt/studentpass/backend
cp .env.example .env
nano .env
```

Заполните `POSTGRES_PASSWORD`, `JWT_SECRET_KEY`, SMTP-параметры для отправки кодов регистрации.

### 4. Nginx

```bash
cp /opt/studentpass/deploy/nginx/studentpass.conf /etc/nginx/sites-available/studentpass
rm -f /etc/nginx/sites-enabled/default
ln -sf /etc/nginx/sites-available/studentpass /etc/nginx/sites-enabled/
nginx -t && systemctl reload nginx
```

### 5. Первый деплой

```bash
chmod +x /opt/studentpass/scripts/deploy-vps.sh
/opt/studentpass/scripts/deploy-vps.sh
```

Проверка: http://161.104.33.28 и http://161.104.33.28/api/v1/health

---

## GitHub Actions (секреты)

| Secret | Значение |
|--------|----------|
| `VPS_HOST` | `161.104.33.28` |
| `VPS_USER` | `root` |
| `VPS_SSH_KEY` | приватный SSH-ключ для деплоя |
| `VPS_APP_PATH` | `/opt/studentpass` |
| `VPS_PORT` | `22` |

Environment: `production`

---

## Ежедневная работа

1. Фичи → PR в **`develop`** (CI).
2. Готово к выкладке → PR **`develop` → `prod`**, merge.
3. Merge в `prod` → автодеплой на VPS.

Ручной деплой на VPS:

```bash
/opt/studentpass/scripts/deploy-vps.sh
```
