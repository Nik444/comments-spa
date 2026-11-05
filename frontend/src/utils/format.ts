const formatter = new Intl.DateTimeFormat('ru-RU', { dateStyle: 'short', timeStyle: 'short' })

export function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} Б`
  if (bytes < 1024 * 1024) return `${Math.ceil(bytes / 1024)} КБ`

  return `${(bytes / 1024 / 1024).toFixed(1)} МБ`
}

export function formatDate(iso: string): string {
  return formatter.format(new Date(iso))
}
