<script setup lang="ts">
import { nextTick, onBeforeUnmount, onMounted, ref } from 'vue'

defineProps<{ label: string }>()
const emit = defineEmits<{ close: [] }>()

// Закрытие идёт через анимацию: родитель убирает окно только после её завершения (after-leave).
const visible = ref(true)
const closeButton = ref<HTMLButtonElement | null>(null)
let previouslyFocused: HTMLElement | null = null

function requestClose() {
  visible.value = false
}

function onKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') requestClose()
}

onMounted(async () => {
  previouslyFocused = document.activeElement as HTMLElement | null
  document.body.style.overflow = 'hidden'
  window.addEventListener('keydown', onKeydown)

  await nextTick()
  closeButton.value?.focus()
})

onBeforeUnmount(() => {
  document.body.style.overflow = ''
  window.removeEventListener('keydown', onKeydown)
  previouslyFocused?.focus()
})
</script>

<template>
  <Teleport to="body">
    <Transition name="lightbox" appear @after-leave="emit('close')">
      <div v-if="visible" class="lightbox" role="dialog" aria-modal="true" :aria-label="label" @click.self="requestClose">
        <button ref="closeButton" type="button" class="btn lightbox-close" aria-label="Закрыть" @click="requestClose">✕</button>
        <slot />
      </div>
    </Transition>
  </Teleport>
</template>
