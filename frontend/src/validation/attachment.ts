// Те же правила, что на сервере (AttachmentValidator). Сервер проверяет заново по содержимому файла.
export const AttachmentLimits = {
  imageMaxWidth: 320,
  imageMaxHeight: 240,
  imageMaxBytes: 1024 * 1024,
  textMaxBytes: 100 * 1024,
  // Исходное фото до уменьшения. Больше не читаем, чтобы не вешать вкладку.
  imageSourceMaxBytes: 20 * 1024 * 1024,
} as const

export type AttachmentKind = 'image' | 'text'

const imageExtensions = ['jpg', 'jpeg', 'png', 'gif']

function extensionOf(name: string): string {
  const dot = name.lastIndexOf('.')
  return dot < 0 ? '' : name.slice(dot + 1).toLowerCase()
}

export function detectKind(file: Pick<File, 'name' | 'type'>): AttachmentKind | null {
  const extension = extensionOf(file.name)

  if (imageExtensions.includes(extension) && (file.type === '' || file.type.startsWith('image/'))) return 'image'
  if (extension === 'txt' && (file.type === '' || file.type === 'text/plain')) return 'text'

  return null
}

export function checkAttachment(file: Pick<File, 'name' | 'type' | 'size'>): string | null {
  const kind = detectKind(file)

  if (kind === null) return 'Допустимы файлы JPG, GIF, PNG и TXT.'
  if (file.size === 0) return 'Файл пустой.'
  if (kind === 'text' && file.size > AttachmentLimits.textMaxBytes) return `Текстовый файл не больше ${AttachmentLimits.textMaxBytes / 1024} КБ.`
  if (kind === 'image' && file.size > AttachmentLimits.imageSourceMaxBytes) return `Изображение не больше ${AttachmentLimits.imageSourceMaxBytes / 1024 / 1024} МБ.`

  return null
}
