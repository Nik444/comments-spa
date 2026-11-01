<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{ page: number; totalPages: number }>()
const emit = defineEmits<{ change: [page: number] }>()

// Первая, последняя и по две страницы вокруг текущей. Пропуски обозначаются многоточием (null).
const items = computed(() => {
  const pages = new Set([1, props.totalPages])
  for (let p = props.page - 2; p <= props.page + 2; p++) if (p >= 1 && p <= props.totalPages) pages.add(p)

  const sorted = [...pages].sort((a, b) => a - b)
  const result: (number | null)[] = []

  sorted.forEach((p, i) => {
    const prev = sorted[i - 1]
    if (prev !== undefined && p - prev > 1) result.push(null)
    result.push(p)
  })

  return result
})
</script>

<template>
  <nav v-if="totalPages > 1" class="pagination" aria-label="Страницы">
    <button type="button" class="btn" :disabled="page <= 1" @click="emit('change', page - 1)">←</button>

    <template v-for="(item, i) in items" :key="i">
      <span v-if="item === null" class="muted">…</span>
      <button v-else type="button" class="btn" :class="{ primary: item === page }" :aria-current="item === page ? 'page' : undefined" @click="emit('change', item)">
        {{ item }}
      </button>
    </template>

    <button type="button" class="btn" :disabled="page >= totalPages" @click="emit('change', page + 1)">→</button>
  </nav>
</template>
