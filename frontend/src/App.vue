<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted } from 'vue'
import type { CommentDto, SortField } from '@/api/types'
import CommentForm from '@/components/CommentForm.vue'
import Pagination from '@/components/Pagination.vue'
import RootComment from '@/components/RootComment.vue'
import { useCommentList } from '@/composables/useCommentList'
import { useRealtime } from '@/composables/useRealtime'
import { hasDrafts } from '@/utils/draft'

const list = useCommentList()
const realtime = useRealtime(list)

const columns: { field: SortField; title: string }[] = [
  { field: 'UserName', title: 'User Name' },
  { field: 'Email', title: 'E-mail' },
  { field: 'Date', title: 'Дата' },
]

const items = computed(() => list.data.value?.items ?? [])

// Браузер сам покажет своё стандартное предупреждение, если закрыть или обновить вкладку при неотправленном тексте.
function warnAboutDrafts(event: BeforeUnloadEvent) {
  if (!hasDrafts()) return
  event.preventDefault()
  event.returnValue = ''
}

onMounted(async () => {
  window.addEventListener('beforeunload', warnAboutDrafts)
  await list.load()
  await realtime.start()
})

onBeforeUnmount(() => window.removeEventListener('beforeunload', warnAboutDrafts))

function arrow(field: SortField) {
  if (list.sortBy.value !== field) return ''
  return list.sortDir.value === 'Asc' ? '▲' : '▼'
}

function ariaSort(field: SortField) {
  if (list.sortBy.value !== field) return 'none'
  return list.sortDir.value === 'Asc' ? 'ascending' : 'descending'
}

async function changePage(page: number) {
  await list.setPage(page)
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

async function onRootCreated(comment: CommentDto) {
  if (list.isLatestFirstPage()) list.addRoot(comment)
  else await list.showLatest()
}

function onReplyCreated(comment: CommentDto) {
  const rootId = comment.rootId ?? comment.parentId
  if (rootId !== null) list.addReplies(rootId, [comment])
}
</script>

<template>
  <main class="page">
    <h1>
      Комментарии
      <span class="live" :class="{ on: realtime.connected.value }" :title="realtime.connected.value ? 'Обновления в реальном времени включены' : 'Живые обновления недоступны'" />
    </h1>

    <section class="card">
      <h2>Новый комментарий</h2>
      <CommentForm @created="onRootCreated" />
    </section>

    <section aria-live="polite">
      <div class="list-head" role="group" aria-label="Сортировка">
        <button
          v-for="column in columns"
          :key="column.field"
          type="button"
          class="sort-btn"
          :aria-sort="ariaSort(column.field)"
          @click="list.toggleSort(column.field)"
        >
          {{ column.title }} <span class="arrow">{{ arrow(column.field) }}</span>
        </button>
      </div>

      <button v-if="realtime.pendingRoots.value > 0" type="button" class="btn primary new-banner" @click="realtime.showNewRoots()">
        +{{ realtime.pendingRoots.value }} новых. Показать
      </button>

      <button v-if="realtime.needsRefresh.value" type="button" class="btn new-banner" @click="realtime.refreshPage()">
        Связь прерывалась, часть новых комментариев могла не прийти. Обновить страницу
      </button>

      <p v-if="list.error.value" class="error-text" role="alert">
        {{ list.error.value }}
        <button type="button" class="btn" @click="list.load()">Повторить</button>
      </p>

      <p v-else-if="!list.data.value && list.loading.value" class="muted">Загрузка…</p>
      <p v-else-if="list.data.value && items.length === 0" class="muted">Комментариев пока нет. Будьте первым.</p>

      <div :class="{ loading: list.loading.value }">
        <RootComment
          v-for="comment in items"
          :key="comment.id"
          :comment="comment"
          :pending="realtime.pendingReplies.value[comment.id]"
          @created="onReplyCreated"
          @load-replies="realtime.loadReplies(comment.id)"
          @refresh="realtime.refreshPage()"
        />
      </div>

      <Pagination v-if="list.data.value" :page="list.data.value.page" :total-pages="list.data.value.totalPages" @change="changePage" />
    </section>
  </main>
</template>
