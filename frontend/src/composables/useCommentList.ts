import { ref } from 'vue'
import { fetchPage } from '@/api/comments'
import type { CommentDto, PagedDto, RootCommentDto, SortDirection, SortField } from '@/api/types'

export function useCommentList() {
  const page = ref(1)
  const sortBy = ref<SortField>('Date')
  const sortDir = ref<SortDirection>('Desc')
  const data = ref<PagedDto<RootCommentDto> | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)

  let current: AbortController | null = null

  async function load() {
    current?.abort()
    const controller = new AbortController()
    current = controller

    loading.value = true
    error.value = null

    try {
      const result = await fetchPage(page.value, sortBy.value, sortDir.value, controller.signal)

      if (current === controller) data.value = result
    } catch (e) {
      if (controller.signal.aborted) return
      error.value = e instanceof Error ? e.message : 'Не удалось загрузить комментарии.'
    } finally {
      if (current === controller) loading.value = false
    }
  }

  function setPage(next: number) {
    page.value = next
    return load()
  }

  // Повторный щелчок по тому же столбцу меняет направление. Новый столбец: даты сначала новые, имена и e-mail по алфавиту.
  function toggleSort(field: SortField) {
    if (sortBy.value === field) {
      sortDir.value = sortDir.value === 'Asc' ? 'Desc' : 'Asc'
    } else {
      sortBy.value = field
      sortDir.value = field === 'Date' ? 'Desc' : 'Asc'
    }

    page.value = 1
    return load()
  }

  function showLatest() {
    sortBy.value = 'Date'
    sortDir.value = 'Desc'
    page.value = 1
    return load()
  }

  function isLatestFirstPage() {
    return page.value === 1 && sortBy.value === 'Date' && sortDir.value === 'Desc'
  }

  // Сервер кэширует первые страницы на пару секунд, поэтому свой только что отправленный комментарий
  // добавляется в список сразу, не дожидаясь перезагрузки.
  function addRoot(comment: CommentDto) {
    const list = data.value
    if (!list || !isLatestFirstPage() || list.items.some((item) => item.id === comment.id)) return

    list.items.unshift({ ...comment, replies: [] })
    if (list.items.length > list.pageSize) list.items.pop()
    list.totalItems += 1
  }

  function addReplies(rootId: number, comments: CommentDto[]) {
    const root = data.value?.items.find((item) => item.id === rootId)
    if (!root) return

    for (const comment of comments) {
      if (!root.replies.some((reply) => reply.id === comment.id)) root.replies.push(comment)
    }
  }

  return { page, sortBy, sortDir, data, loading, error, load, setPage, toggleSort, showLatest, isLatestFirstPage, addRoot, addReplies }
}
