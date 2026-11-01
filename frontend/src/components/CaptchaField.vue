<script setup lang="ts">
import { useId } from 'vue'
import type { CaptchaState } from '@/composables/useCaptcha'
import type { CaptchaDto } from '@/api/types'

defineProps<{
  state: CaptchaState
  captcha: CaptchaDto | null
  error?: string
}>()

const answer = defineModel<string>({ required: true })
const emit = defineEmits<{ refresh: [] }>()
const id = useId()
</script>

<template>
  <div v-if="state !== 'disabled'" class="field">
    <label :for="id">Код с картинки</label>

    <div class="captcha">
      <img v-if="captcha" :src="captcha.image" alt="Капча" width="160" height="60" />
      <span v-else-if="state === 'loading'" class="muted">Загрузка…</span>
      <span v-else class="error-text">Не удалось загрузить капчу.</span>

      <button type="button" class="btn" @click="emit('refresh')">Обновить</button>
    </div>

    <input :id="id" v-model="answer" type="text" maxlength="16" autocomplete="off" :aria-invalid="!!error" />
    <p v-if="error" class="error-text">{{ error }}</p>
  </div>
</template>
