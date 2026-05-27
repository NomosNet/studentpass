<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useManagerDiscounts } from '../../composables/useManagerDiscounts'
import { useSession } from '../../composables/useSession'

const route = useRoute()
const router = useRouter()
const { isAdmin } = useSession()
const {
  getById,
  createDiscount,
  updateDiscount,
  categoryNames,
  hasCategories,
  load,
  addCategory,
} = useManagerDiscounts()

const discountId = computed(() => String(route.params.id || ''))
const editingDiscount = computed(() => (discountId.value ? getById(discountId.value) : null))
const isEditMode = computed(() => !!editingDiscount.value)

const title = ref(editingDiscount.value?.title || '15% скидка на все меню')
const description = ref(
  editingDiscount.value?.description || 'Подробное описание скидки и условий получения...',
)
const percent = ref(editingDiscount.value?.percentNumber ?? 15)
const category = ref(editingDiscount.value?.category || '')
const linkUrl = ref(editingDiscount.value?.linkUrl || '')
const emoji = ref(editingDiscount.value?.emoji || '🍔')
const newCategoryName = ref('')
const formError = ref('')
const categoryBusy = ref(false)

const previewTitle = computed(() => title.value.trim() || 'Название скидки')
const previewDesc = computed(() => description.value.trim() || 'Описание скидки...')
const previewEmoji = computed(() => emoji.value.trim() || '📋')
const previewCategory = computed(() => category.value.trim() || 'Категория')

onMounted(async () => {
  await load(true)
  const current = discountId.value ? getById(discountId.value) : null
  if (current) {
    title.value = current.title
    description.value = current.description
    percent.value = current.percentNumber
    category.value = current.category
    linkUrl.value = current.linkUrl
  }
  if (!category.value && categoryNames.value.length) {
    category.value = categoryNames.value[0]
  }
})

async function submitNewCategory() {
  formError.value = ''
  categoryBusy.value = true
  try {
    const created = await addCategory(newCategoryName.value)
    category.value = created.name
    newCategoryName.value = ''
  } catch (error) {
    formError.value = error instanceof Error ? error.message : 'Не удалось создать категорию'
  } finally {
    categoryBusy.value = false
  }
}

async function submitForm() {
  formError.value = ''
  if (!category.value) {
    formError.value = isAdmin.value
      ? 'Создайте категорию или выберите существующую'
      : 'Категории ещё не созданы. Попросите администратора добавить их.'
    return
  }

  const payload = {
    title: title.value,
    description: description.value,
    percentNumber: percent.value,
    category: category.value,
    linkUrl: linkUrl.value,
    emoji: emoji.value,
  }

  try {
    if (isEditMode.value) await updateDiscount(discountId.value, payload)
    else await createDiscount(payload)
    router.push({ name: 'manager-discounts' })
  } catch (error) {
    formError.value = error instanceof Error ? error.message : 'Не удалось сохранить скидку'
  }
}
</script>

<template>
  <div class="admin-page admin-container">
    <header class="admin-page-head">
      <div class="admin-page-title">
        <span class="mgr-create-icon" aria-hidden="true">+</span>
        <div>
          <h1>{{ isEditMode ? 'Редактировать скидку' : 'Создать скидку' }}</h1>
          <p>
            {{
              isEditMode
                ? 'Обновите условия предложения и сохраните изменения'
                : 'Добавьте новое предложение для студентов'
            }}
          </p>
        </div>
      </div>
    </header>

    <div class="mgr-form-card">
      <form class="mgr-form" @submit.prevent="submitForm">
        <p v-if="formError" class="mgr-form-error">{{ formError }}</p>

        <div class="mgr-form__row">
          <label class="mgr-form-label" for="offer-title">📄 Название предложения</label>
          <input
            id="offer-title"
            v-model="title"
            type="text"
            class="mgr-form-input"
            placeholder="15% скидка на все меню"
          />
        </div>

        <div class="mgr-form__row">
          <label class="mgr-form-label" for="offer-desc">Описание</label>
          <textarea
            id="offer-desc"
            v-model="description"
            class="mgr-form-textarea"
            rows="4"
            placeholder="Подробное описание скидки и условий получения..."
          />
        </div>

        <div class="mgr-form__grid2">
          <div class="mgr-form__row">
            <label class="mgr-form-label" for="offer-pct">Размер скидки (%)</label>
            <input id="offer-pct" v-model.number="percent" type="number" min="0" max="100" class="mgr-form-input" />
          </div>
          <div class="mgr-form__row">
            <label class="mgr-form-label" for="offer-cat">🏷 Категория</label>
            <div class="mgr-form-select-wrap">
              <select
                id="offer-cat"
                v-model="category"
                class="mgr-form-select"
                :disabled="!hasCategories"
              >
                <option v-if="!hasCategories" value="">Нет категорий</option>
                <option v-for="c in categoryNames" :key="c" :value="c">{{ c }}</option>
              </select>
            </div>
            <p v-if="!hasCategories && !isAdmin" class="mgr-form-hint">
              Категории ещё не созданы. Обратитесь к администратору.
            </p>
          </div>
        </div>

        <div v-if="isAdmin" class="mgr-form__row mgr-category-add">
          <label class="mgr-form-label" for="new-category">➕ Новая категория</label>
          <div class="mgr-category-add__row">
            <input
              id="new-category"
              v-model="newCategoryName"
              type="text"
              class="mgr-form-input"
              placeholder="Например: Софт, Еда, Транспорт"
            />
            <button
              type="button"
              class="admin-btn admin-btn--ghost"
              :disabled="categoryBusy || !newCategoryName.trim()"
              @click="submitNewCategory"
            >
              {{ categoryBusy ? 'Добавление…' : 'Добавить' }}
            </button>
          </div>
          <p class="mgr-form-hint">
            Сначала создайте категорию, затем выберите её в списке выше.
          </p>
        </div>

        <div class="mgr-form__row">
          <label class="mgr-form-label" for="offer-link">🔗 Ссылка для получения скидки</label>
          <input
            id="offer-link"
            v-model="linkUrl"
            type="url"
            class="mgr-form-input"
            placeholder="https://yourwebsite.com/student-discount"
          />
          <p class="mgr-form-hint">
            Студенты будут переходить по этой ссылке при нажатии «Получить»
          </p>
        </div>

        <div class="mgr-form__row mgr-form__row--emoji">
          <label class="mgr-form-label" for="offer-emoji">🖼 Эмодзи (иконка)</label>
          <input id="offer-emoji" v-model="emoji" type="text" class="mgr-form-input mgr-form-input--emoji" maxlength="4" />
        </div>

        <button type="submit" class="admin-btn admin-btn--primary mgr-form-submit">
          {{ isEditMode ? 'Сохранить изменения' : 'Опубликовать скидку' }}
        </button>
      </form>

      <div class="mgr-preview-block">
        <h3>Предпросмотр:</h3>
        <div class="mgr-preview-card">
          <span class="mgr-preview-emoji">{{ previewEmoji }}</span>
          <strong>{{ previewTitle }}</strong>
          <p>{{ previewDesc }}</p>
          <span class="mgr-preview-cat">{{ previewCategory }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.mgr-form-error {
  margin: 0 0 1rem;
  padding: 0.75rem 1rem;
  border-radius: 8px;
  background: #fee;
  color: #b42318;
  font-size: 0.95rem;
}

.mgr-category-add__row {
  display: flex;
  gap: 0.75rem;
  align-items: center;
}

.mgr-category-add__row .mgr-form-input {
  flex: 1;
}

@media (max-width: 640px) {
  .mgr-category-add__row {
    flex-direction: column;
    align-items: stretch;
  }
}
</style>
