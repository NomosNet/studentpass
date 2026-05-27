<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import SitePublicHeader from '../components/SitePublicHeader.vue'
import { getFavorites, removeFavorite } from '../services/favoritesService'
import { useSession } from '../composables/useSession'

const UNIVERSITY_KEY = 'studentpass_university'

const route = useRoute()
const router = useRouter()
const { user, logout, readStorage } = useSession()

const activeTab = ref('profile')
const favorites = ref([])
const favoritesTotal = ref(0)
const favoritesLoading = ref(false)
const favoritesError = ref('')
const removingId = ref(null)

const displayName = computed(() => user.value?.displayName || 'Студент')
const email = computed(() => user.value?.email || '')
const university = computed(() => {
  const stored = localStorage.getItem(UNIVERSITY_KEY)
  return stored?.trim() || 'Не указан'
})
const isVerified = computed(() => user.value?.isActive !== false)

const favoritesCountLabel = computed(() => {
  const n = favoritesTotal.value
  if (n === 0) return '0 скидок'
  if (n === 1) return '1 скидка'
  if (n >= 2 && n <= 4) return `${n} скидки`
  return `${n} скидок`
})

function formatDate(value) {
  if (!value) return '—'
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? '—' : date.toLocaleDateString('ru-RU')
}

function syncTabFromRoute() {
  activeTab.value = route.query.tab === 'favorites' ? 'favorites' : 'profile'
}

function setTab(tab) {
  activeTab.value = tab
  const query = tab === 'favorites' ? { tab: 'favorites' } : {}
  router.replace({ name: 'account', query })
}

async function loadFavorites() {
  favoritesLoading.value = true
  favoritesError.value = ''
  try {
    const response = await getFavorites({ limit: 100 })
    favorites.value = Array.isArray(response?.items) ? response.items : []
    favoritesTotal.value = Number(response?.total) || favorites.value.length
  } catch (error) {
    favorites.value = []
    favoritesTotal.value = 0
    const msg = error instanceof Error ? error.message : ''
    if (/401|403|unauthorized|авториз/i.test(msg)) return
    favoritesError.value = msg || 'Не удалось загрузить избранное'
  } finally {
    favoritesLoading.value = false
  }
}

async function handleRemoveFavorite(adId) {
  removingId.value = adId
  try {
    await removeFavorite(adId)
    await loadFavorites()
  } catch (error) {
    favoritesError.value = error instanceof Error ? error.message : 'Не удалось удалить'
  } finally {
    removingId.value = null
  }
}

async function handleLogout() {
  await logout()
  router.push({ name: 'home' })
}

watch(
  () => route.query.tab,
  () => {
    syncTabFromRoute()
    if (activeTab.value === 'favorites') void loadFavorites()
  },
)

onMounted(async () => {
  await readStorage()
  syncTabFromRoute()
  await loadFavorites()
})

watch(activeTab, (tab) => {
  if (tab === 'favorites' && !favoritesLoading.value && favorites.value.length === 0 && !favoritesError.value) {
    void loadFavorites()
  }
})
</script>

<template>
  <div class="account-page">
    <SitePublicHeader />

    <main class="account-container account-main">
      <header class="account-hero">
        <div class="account-hero__left">
          <span class="account-hero__avatar" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" width="32" height="32">
              <path
                d="M12 14a5 5 0 100-10 5 5 0 000 10zM4 20a8 8 0 0116 0H4z"
                stroke="currentColor"
                stroke-width="1.8"
                stroke-linecap="round"
              />
              <path d="M5 9l2 2 4-4" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
            </svg>
          </span>
          <div class="account-hero__info">
            <h1>{{ displayName }}</h1>
            <p class="account-hero__email">{{ email }}</p>
            <span v-if="isVerified" class="account-verified">
              <svg viewBox="0 0 20 20" width="16" height="16" aria-hidden="true">
                <circle cx="10" cy="10" r="9" stroke="currentColor" stroke-width="1.5" fill="none" />
                <path d="M6 10l2.5 2.5L14 7" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" />
              </svg>
              Верифицирован
            </span>
          </div>
        </div>
        <button type="button" class="account-logout" @click="handleLogout">
          <svg viewBox="0 0 24 24" width="18" height="18" aria-hidden="true" fill="none">
            <path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4M16 17l5-5-5-5M21 12H9" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
          </svg>
          Выйти
        </button>
      </header>

      <nav class="account-tabs" aria-label="Разделы личного кабинета">
        <button
          type="button"
          class="account-tabs__btn"
          :class="{ 'is-active': activeTab === 'profile' }"
          @click="setTab('profile')"
        >
          Профиль
        </button>
        <button
          type="button"
          class="account-tabs__btn"
          :class="{ 'is-active': activeTab === 'favorites' }"
          @click="setTab('favorites')"
        >
          Избранное
        </button>
      </nav>

      <div v-if="activeTab === 'profile'" class="account-profile">
        <div class="account-grid account-grid--2">
          <section class="account-card">
            <h2 class="account-card__title">
              <span class="account-card__icon account-card__icon--user" aria-hidden="true" />
              Личные данные
            </h2>
            <dl class="account-fields">
              <div>
                <dt>Имя</dt>
                <dd>{{ displayName }}</dd>
              </div>
              <div>
                <dt>Email</dt>
                <dd>{{ email }}</dd>
              </div>
              <div>
                <dt>Университет</dt>
                <dd>{{ university }}</dd>
              </div>
            </dl>
          </section>

          <section class="account-card">
            <h2 class="account-card__title">
              <span class="account-card__icon account-card__icon--status" aria-hidden="true" />
              Статус аккаунта
            </h2>
            <div v-if="isVerified" class="account-status-ok">
              <span class="account-status-ok__icon" aria-hidden="true">✓</span>
              <div>
                <strong>Аккаунт верифицирован</strong>
                <p>Ваш студенческий статус подтверждён. Все скидки доступны.</p>
              </div>
            </div>
            <div v-else class="account-status-warn">
              <strong>Ожидает подтверждения</strong>
              <p>Завершите верификацию студенческого статуса, чтобы открыть все скидки.</p>
            </div>
            <div class="account-fav-summary">
              <span>Сохранено в избранном</span>
              <button type="button" class="account-fav-summary__link" @click="setTab('favorites')">
                {{ favoritesCountLabel }}
              </button>
            </div>
          </section>
        </div>

        <section class="account-card account-card--wide">
          <h2 class="account-card__title">
            <span class="account-card__icon account-card__icon--star" aria-hidden="true" />
            Быстрый доступ
          </h2>
          <div class="account-quick">
            <RouterLink :to="{ name: 'catalog' }" class="account-quick__btn">Каталог скидок</RouterLink>
            <button type="button" class="account-quick__btn" @click="setTab('favorites')">Избранное</button>
          </div>
        </section>
      </div>

      <div v-else class="account-favorites-panel">
        <p v-if="favoritesLoading" class="account-muted">Загрузка избранного…</p>
        <p v-else-if="favoritesError" class="account-error">{{ favoritesError }}</p>
        <div v-else-if="favorites.length === 0" class="account-empty">
          <span class="account-empty__icon" aria-hidden="true">★</span>
          <h2>Пока пусто</h2>
          <p>Добавляйте скидки в избранное из каталога — они появятся здесь.</p>
          <RouterLink :to="{ name: 'catalog' }" class="sp-btn sp-btn--small">Открыть каталог</RouterLink>
        </div>
        <ul v-else class="account-fav-list">
          <li v-for="item in favorites" :key="item.ad_id" class="account-fav-item">
            <div class="account-fav-item__body">
              <h3>{{ item.title }}</h3>
              <p>{{ item.partner_name }} · −{{ item.discount_percent }}% · до {{ formatDate(item.end_date) }}</p>
            </div>
            <div class="account-fav-item__actions">
              <RouterLink :to="`/product/${item.ad_id}`" class="account-fav-item__open">Открыть</RouterLink>
              <button
                type="button"
                class="account-fav-item__remove"
                :disabled="removingId === item.ad_id"
                @click="handleRemoveFavorite(item.ad_id)"
              >
                {{ removingId === item.ad_id ? '…' : 'Убрать' }}
              </button>
            </div>
          </li>
        </ul>
      </div>
    </main>
  </div>
</template>
