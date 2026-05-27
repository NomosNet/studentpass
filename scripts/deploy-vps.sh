#!/usr/bin/env bash
# Ручной деплой на VPS (то же, что делает GitHub Actions для ветки prod)
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

git fetch origin prod
git checkout prod
git reset --hard origin/prod

echo ">>> Backend secrets"
cd backend
cp -f .env.production .env

echo ">>> Nginx"
if [ "$(id -u)" -eq 0 ]; then
  cp "$ROOT/deploy/nginx/studentpass.conf" /etc/nginx/sites-available/studentpass
  rm -f /etc/nginx/sites-enabled/default
  ln -sf /etc/nginx/sites-available/studentpass /etc/nginx/sites-enabled/studentpass
  nginx -t
  systemctl reload nginx
else
  sudo cp "$ROOT/deploy/nginx/studentpass.conf" /etc/nginx/sites-available/studentpass
  sudo rm -f /etc/nginx/sites-enabled/default
  sudo ln -sf /etc/nginx/sites-available/studentpass /etc/nginx/sites-enabled/studentpass
  sudo nginx -t
  sudo systemctl reload nginx
fi

echo ">>> Backend containers"
docker compose -f docker-compose.prod.yml --env-file .env up -d --build

echo ">>> Frontend"
cd ../frontend
npm ci
npm run build

WEB_ROOT="/var/www/studentpass"
if [ "$(id -u)" -eq 0 ]; then
  mkdir -p "$WEB_ROOT"
  rsync -a --delete dist/ "$WEB_ROOT/"
else
  sudo mkdir -p "$WEB_ROOT"
  sudo rsync -a --delete dist/ "$WEB_ROOT/"
fi

echo ">>> Health check"
sleep 8
curl -fsS http://127.0.0.1:8080/api/v1/health
echo ""
echo "Deploy OK"
