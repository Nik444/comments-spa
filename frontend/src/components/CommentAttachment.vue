<script setup lang="ts">
import { ref } from 'vue'
import type { AttachmentDto } from '@/api/types'
import Lightbox from '@/components/Lightbox.vue'
import { formatSize } from '@/utils/format'

const props = defineProps<{ attachment: AttachmentDto }>()

const open = ref(false)
const text = ref<string | null>(null)
const loading = ref(false)
const error = ref<string | null>(null)

// Текст загружается один раз при первом открытии. Показывается как обычный текст, не как HTML.
async function openText() {
  open.value = true
  if (text.value !== null) return

  loading.value = true
  error.value = null

  try {
    const response = await fetch(props.attachment.url)
    if (!response.ok) throw new Error()
    text.value = await response.text()
  } catch {
    error.value = 'Не удалось загрузить файл.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="attachment">
    <template v-if="attachment.kind === 'image'">
      <button type="button" class="thumb-btn" :title="`${attachment.name}. Нажмите, чтобы увеличить`" @click="open = true">
        <img :src="attachment.url" :alt="attachment.name" class="thumb" loading="lazy" />
      </button>

      <Lightbox v-if="open" :label="attachment.name" @close="open = false">
        <img :src="attachment.url" :alt="attachment.name" class="lightbox-image" />
      </Lightbox>
    </template>

    <template v-else>
      <button type="button" class="btn" @click="openText">{{ attachment.name }} ({{ formatSize(attachment.size) }})</button>
      <a :href="attachment.url" :download="attachment.name" class="muted">Скачать</a>

      <Lightbox v-if="open" :label="attachment.name" @close="open = false">
        <div class="lightbox-text">
          <p v-if="loading" class="muted">Загрузка…</p>
          <p v-else-if="error" class="error-text">{{ error }}</p>
          <pre v-else>{{ text }}</pre>
        </div>
      </Lightbox>
    </template>
  </div>
</template>
