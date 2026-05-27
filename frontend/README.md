# StudentPass Frontend (Vue 3 + Vite)

Фронтенд для проекта **StudentPass** на **Vue 3** + **Vite**.

## Требования

- **Node.js**: рекомендуется **LTS** (18/20/22)
- **npm**: идет вместе с Node.js

Проверка:

```bash
node -v
npm -v
```

## Установка

```bash
cd frontend
npm install
```

## Запуск (dev)

Бэкенд (Docker в `backend/`):

```bash
cd backend
docker compose up -d
```

В `.env.development` (или по умолчанию в `apiClient.js`):

```env
VITE_API_BASE_URL=http://localhost:8080
```

Фронтенд:

```bash
npm run dev
```

Vite выведет URL (обычно `http://localhost:5173`).

## Сборка (prod)

```bash
npm run build
```

## Предпросмотр сборки

```bash
npm run preview
```

## Зависимости

Основные:

- **vue** (Vue 3)
- **vue-router** (маршрутизация)

Dev:

- **vite**
- **@vitejs/plugin-vue**

## Тестовая авторизация (для проверки всех страниц)

В dev-режиме можно включить автоматическую сессию без ручного логина:

- Файл: `.env.development`
- Переменная: `VITE_DEV_AUTH_ROLE`

Варианты:

- `admin` — админка `/admin/*`
- `manager` — кабинет компании `/manager/*`
- `user` — обычный пользователь
- `off` — выключить авто-авторизацию (только через модалку «Войти» + `localStorage`)

После изменения `.env.development` перезапустите `npm run dev`.

Дополнительно: подробный чек-лист визуального прогона лежит в `TESTING.md`.

## Полезные маршруты

Публичные:

- `/` — главная
- `/catalog` — каталог скидок
- `/how` — как это работает
- `/product/:id` — карточка скидки
- `/account` — личный кабинет студента (вкладки «Профиль» и «Избранное»)

Админка (роль `admin`):

- `/admin`
- `/admin/applications`
- `/admin/managers`
- `/admin/statistics`

Кабинет компании (роль `manager`):

- `/manager/discounts`
- `/manager/discounts/create`
- `/manager/statistics`
