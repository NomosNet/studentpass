#!/usr/bin/env bash
# Ручной деплой на VPS (то же, что делает GitHub Actions для ветки prod)
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

git fetch origin prod
git checkout prod
git reset --hard origin/prod

cd backend
docker compose -f docker-compose.prod.yml --env-file .env up -d --build

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

sleep 5
curl -fsS http://127.0.0.1:8080/api/v1/health
echo ""
echo "Deploy OK"
