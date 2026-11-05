import { HubConnectionBuilder, HttpTransportType, LogLevel, type HubConnection } from '@microsoft/signalr'
import { onBeforeUnmount, ref, watch } from 'vue'
import { fetchByIds } from '@/api/comments'
import type { CommentDto } from '@/api/types'
import type { useCommentList } from '@/composables/useCommentList'
import { confirmDiscardReplyDrafts } from '@/utils/draft'
import { isOwn } from '@/utils/own'

const retryDelayMs = 5000
const chunkSize = 100

// Если новых ответов у ветки больше, чем разрешено запрашивать за раз, Id не копятся, а клиент предлагает обновить страницу.
const maxPendingReplies = 200

type CommentList = ReturnType<typeof useCommentList>

export interface PendingReplies {
  ids: number[]
  extra: number
}

async function fetchAll(ids: number[]): Promise<CommentDto[]> {
  const chunks: number[][] = []
  for (let i = 0; i < ids.length; i += chunkSize) chunks.push(ids.slice(i, i + chunkSize))

  return (await Promise.all(chunks.map(fetchByIds))).flat()
}

// Сервис realtime присылает только сигналы с Id. Список сам не меняется, чтобы не прыгал под руками и не терял набранный текст.
// Новые корни: клиент считает сигналы, по кнопке открывается первая страница.
// Новые ответы: Id копятся по веткам, по кнопке на ветке запрашиваются именно они (by-ids) и вставляются в дерево.
// Если realtime недоступен, страница продолжает работать, просто без живых обновлений.
export function useRealtime(list: CommentList) {
  const connected = ref(false)
  const pendingRoots = ref(0)
  const pendingReplies = ref<Record<number, PendingReplies>>({})
  const needsRefresh = ref(false)
  let stopped = false
  let retried = false

  const connection: HubConnection = new HubConnectionBuilder()
    .withUrl('/hubs/comments', { transport: HttpTransportType.WebSockets, skipNegotiation: true })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build()

  async function subscribe() {
    if (connection.state !== 'Connected') return

    const ids = list.data.value?.items.map((item) => item.id) ?? []

    try {
      await connection.invoke('SubscribeRoots', ids)
    } catch {
      // Не удалось подписаться: живые ответы не придут, но страница работает.
    }
  }

  function onNewRoots(ids: number[]) {
    pendingRoots.value += ids.filter((id) => !isOwn(id)).length
  }

  function onNewReplies(rootId: number, ids: number[]) {
    if (!list.data.value?.items.some((item) => item.id === rootId)) return

    const state = (pendingReplies.value[rootId] ??= { ids: [], extra: 0 })

    for (const id of ids) {
      if (isOwn(id) || state.ids.includes(id)) continue

      if (state.extra > 0 || state.ids.length >= maxPendingReplies) state.extra++
      else state.ids.push(id)
    }

    if (state.ids.length === 0 && state.extra === 0) delete pendingReplies.value[rootId]
  }

  function resetPending() {
    pendingRoots.value = 0
    pendingReplies.value = {}
    needsRefresh.value = false
  }

  // Новые корни: страница 1 с сортировкой «дата по убыванию».
  async function showNewRoots() {
    if (!confirmDiscardReplyDrafts()) return

    resetPending()
    await list.showLatest()
  }

  // Текущая страница целиком: после обрыва связи или когда новых ответов слишком много.
  async function refreshPage() {
    if (!confirmDiscardReplyDrafts()) return

    resetPending()
    await list.load()
  }

  // Ответы одной ветки: запрашиваются только накопленные Id, остальная страница не трогается.
  async function loadReplies(rootId: number) {
    const state = pendingReplies.value[rootId]
    if (!state) return

    try {
      list.addReplies(rootId, await fetchAll(state.ids))
      delete pendingReplies.value[rootId]
    } catch {
      // Не удалось загрузить: счётчик остаётся, кнопку можно нажать ещё раз.
    }
  }

  connection.on('NewRoots', onNewRoots)
  connection.on('NewReplies', onNewReplies)

  connection.onreconnecting(() => {
    connected.value = false
  })

  // За время обрыва сигналы могли потеряться. Страница сама не перезагружается, предлагается кнопка.
  connection.onreconnected(async () => {
    connected.value = true
    needsRefresh.value = true
    await subscribe()
  })

  // Встроенное автопереподключение SignalR сдаётся примерно через минуту. После этого, а также если первое подключение
  // не удалось (сервис ещё не запущен), попытки продолжаются каждые 5 секунд, пока страница открыта.
  connection.onclose(() => {
    connected.value = false
    if (!stopped) void start()
  })

  async function start() {
    while (!stopped) {
      try {
        await connection.start()
        connected.value = true
        await subscribe()
        if (retried) needsRefresh.value = true
        return
      } catch {
        connected.value = false
        retried = true
        await new Promise((resolve) => setTimeout(resolve, retryDelayMs))
      }
    }
  }

  // Набор корней на странице меняется при листании и сортировке, подписка обновляется вслед за ним.
  watch(
    () => list.data.value?.items.map((item) => item.id).join(','),
    () => {
      const onPage = new Set(list.data.value?.items.map((item) => item.id))

      for (const key of Object.keys(pendingReplies.value))
        if (!onPage.has(Number(key))) delete pendingReplies.value[Number(key)]

      void subscribe()
    },
  )

  onBeforeUnmount(() => {
    stopped = true
    void connection.stop()
  })

  return { connected, pendingRoots, pendingReplies, needsRefresh, start, showNewRoots, refreshPage, loadReplies }
}
