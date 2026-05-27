<script setup>
import { RouterLink, useRouter } from 'vue-router'
import { useAuthModal } from '../composables/useAuthModal'
import { useSession } from '../composables/useSession'

const router = useRouter()
const { openRegister, openLogin } = useAuthModal()
const { user, isLoggedIn, isAdmin, isManager, canManageDiscounts, isStudent, logout } = useSession()

async function handleLogout() {
  const path = router.currentRoute.value.path
  const leaveCabinet = path.startsWith('/admin') || path.startsWith('/manager') || path.startsWith('/account')
  await logout()
  if (leaveCabinet) router.push({ name: 'home' })
}
</script>

<template>
  <div v-if="!isLoggedIn" class="auth-header-actions">
    <button type="button" class="auth-header-btn auth-header-btn--ghost" @click="openLogin">
      Войти
    </button>
    <button type="button" class="auth-header-btn auth-header-btn--primary" @click="openRegister">
      Регистрация
    </button>
  </div>
  <div v-else class="auth-header-actions auth-header-actions--logged">
    <RouterLink
      v-if="isAdmin"
      class="auth-header-btn auth-header-btn--ghost"
      :to="{ name: 'admin-dashboard' }"
    >
      Админ-панель
    </RouterLink>
    <RouterLink
      v-if="canManageDiscounts"
      class="auth-header-btn auth-header-btn--ghost"
      :to="{ name: 'manager-discounts' }"
    >
      {{ isAdmin ? 'Скидки' : 'Кабинет компании' }}
    </RouterLink>
    <RouterLink
      v-if="isStudent"
      class="auth-header-account"
      :to="{ name: 'account' }"
      :title="user.email"
    >
      <span class="auth-header-account__icon" aria-hidden="true">
        <svg viewBox="0 0 24 24" width="16" height="16" fill="none">
          <path d="M12 12a4 4 0 100-8 4 4 0 000 8zM4 20a8 8 0 1116 0H4z" stroke="currentColor" stroke-width="2" />
        </svg>
      </span>
      <span>{{ user.displayName }}</span>
    </RouterLink>
    <template v-if="!isStudent">
      <span class="auth-header-email" :title="user.email">{{ user.displayName }}</span>
      <button type="button" class="auth-header-btn auth-header-btn--ghost" @click="handleLogout">
        Выйти
      </button>
    </template>
  </div>
</template>
