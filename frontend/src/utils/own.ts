// Id комментариев, созданных в этой вкладке. Сигнал realtime о собственном комментарии игнорируется,
// потому что он уже добавлен в список ответом на POST.
const ownIds = new Set<number>()

export function rememberOwn(id: number) {
  ownIds.add(id)
}

export function isOwn(id: number) {
  return ownIds.has(id)
}
