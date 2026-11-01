// Формы, в которых набран неотправленный текст. Нужны, чтобы предупреждать пользователя перед действиями,
// которые этот текст могут потерять: закрытие вкладки и обновление списка.
const drafts = new Map<string, 'root' | 'reply'>()

export function setDraft(key: string, kind: 'root' | 'reply', hasText: boolean) {
  if (hasText) drafts.set(key, kind)
  else drafts.delete(key)
}

export function hasDrafts() {
  return drafts.size > 0
}

export function hasReplyDrafts() {
  return [...drafts.values()].includes('reply')
}

// Форма нового корневого комментария при обновлении списка остаётся на месте, поэтому спрашиваем только про формы ответов.
export function confirmDiscardReplyDrafts() {
  return !hasReplyDrafts() || window.confirm('В форме ответа есть неотправленный текст. Обновить список без него?')
}
