const formatter = new Intl.DateTimeFormat('ru-RU', { dateStyle: 'short', timeStyle: 'short' })

export function formatDate(iso: string): string {
  return formatter.format(new Date(iso))
}
