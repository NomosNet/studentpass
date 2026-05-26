# CI/CD: ветка `prod` = сайт на VPS

Краткая настройка. Подробности — в [DEPLOYMENT-VPS.md](./DEPLOYMENT-VPS.md).

## Как это работает

```text
develop  →  CI (сборка, без деплоя)
    │
    └── PR merge ──►  prod  ──push──►  GitHub Actions  ──SSH──►  VPS
```

**На сайте всегда код из `origin/prod`.** Push в `develop` VPS не трогает.

---

## Часть 1. Один раз на VPS (Termius)

Вы уже подключены как `root@161.104.33.28`.

### 1.1. Софт

```bash
apt update && apt upgrade -y
apt install -y git curl nginx
curl -fsSL https://get.docker.com | sh
curl -fsSL https://deb.nodesource.com/setup_20.x | bash -
apt install -y nodejs
```

### 1.2. Клон репозитория

```bash
mkdir -p /opt/studentpass
cd /opt/studentpass
git clone https://github.com/ВАШ_ЛОГИН/studentpass.git .
git checkout prod
```

**Приватный репозиторий** — deploy key:

```bash
ssh-keygen -t ed25519 -f ~/.ssh/github_studentpass -N ""
cat ~/.ssh/github_studentpass.pub
```

GitHub → репозиторий → **Settings → Deploy keys → Add** (read-only). Затем:

```bash
cat >> ~/.ssh/config << 'EOF'
Host github.com
  HostName github.com
  User git
  IdentityFile ~/.ssh/github_studentpass
  IdentitiesOnly yes
EOF
chmod 600 ~/.ssh/config ~/.ssh/github_studentpass
cd /opt/studentpass
git remote set-url origin git@github.com:ВАШ_ЛОГИН/studentpass.git
git fetch origin prod
```

### 1.3. Секреты бэкенда

```bash
cd /opt/studentpass/backend
cp .env.example .env
nano .env
```

Заполните `MYSQL_*`, `JWT_SECRET_KEY`. Опционально:

```env
CORS_ORIGINS=http://161.104.33.28
```

### 1.4. Nginx

```bash
cp /opt/studentpass/deploy/nginx/studentpass.conf /etc/nginx/sites-available/studentpass
# при домене отредактируйте server_name
rm -f /etc/nginx/sites-enabled/default
ln -sf /etc/nginx/sites-available/studentpass /etc/nginx/sites-enabled/
nginx -t && systemctl reload nginx
```

### 1.5. Первый деплой вручную

```bash
chmod +x /opt/studentpass/scripts/deploy-vps.sh
/opt/studentpass/scripts/deploy-vps.sh
```

Проверка: http://161.104.33.28 и http://161.104.33.28/api/v1/health

---

## Часть 2. SSH-ключ для GitHub Actions

**На своём ПК** (PowerShell):

```powershell
ssh-keygen -t ed25519 -C "github-actions-deploy" -f $env:USERPROFILE\.ssh\studentpass_actions -N '""'
```

Публичный ключ на VPS:

```bash
mkdir -p ~/.ssh
nano ~/.ssh/authorized_keys
# вставить содержимое studentpass_actions.pub
chmod 600 ~/.ssh/authorized_keys
```

Проверка с ПК:

```powershell
ssh -i $env:USERPROFILE\.ssh\studentpass_actions root@161.104.33.28 "echo OK"
```

---

## Часть 3. Секреты в GitHub

Репозиторий → **Settings → Secrets and variables → Actions → New repository secret**

| Secret | Значение для вас |
|--------|------------------|
| `VPS_HOST` | `161.104.33.28` |
| `VPS_USER` | `root` (позже лучше отдельный `deploy`) |
| `VPS_SSH_KEY` | весь файл `studentpass_actions` (приватный) |
| `VPS_APP_PATH` | `/opt/studentpass` |
| `VPS_PORT` | `22` (опционально) |

**Settings → Environments → New environment** → имя `production` (как в workflow).

---

## Часть 4. Залить CI/CD в репозиторий

На ПК в проекте:

```bash
git checkout develop
git add .github/ backend/docker-compose.prod.yml frontend/.env.production scripts/ deploy/
git commit -m "Add CI/CD: deploy prod branch to VPS"
git push origin develop
```

PR **develop → prod**, merge.

После merge в `prod` откройте **Actions → Deploy Prod** — должен пройти зелёным.

---

## Ежедневная работа

1. Фичи в `feature/*` → PR в **`develop`** (запускается CI).
2. Готово к выкладке → PR **`develop` → `prod`**.
3. Merge в `prod` → автодеплой на VPS.
4. Синхронизация:

```bash
git checkout develop && git merge origin/prod && git push origin develop
```

---

## Если Deploy упал

- **Actions** → failed run → лог шага SSH.
- На VPS: `docker compose -f backend/docker-compose.prod.yml logs api`
- Ручной повтор: `/opt/studentpass/scripts/deploy-vps.sh`
