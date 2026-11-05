import { AttachmentLimits } from '@/validation/attachment'

export interface Size {
  width: number
  height: number
}

export interface PreparedImage {
  file: File
  width: number
  height: number
  resized: boolean
}

// Пропорционально вписывает картинку в рамку. Меньшие картинки не увеличиваются.
export function fitIntoBox(width: number, height: number, maxWidth: number, maxHeight: number): Size {
  const scale = Math.min(1, maxWidth / width, maxHeight / height)

  return { width: Math.max(1, Math.round(width * scale)), height: Math.max(1, Math.round(height * scale)) }
}

// Картинка, которая уже подходит по размеру и весу, уходит как есть (у маленького GIF остаётся анимация).
// Остальные уменьшаются в браузере: canvas не умеет писать GIF, поэтому он превращается в PNG.
export async function prepareImage(file: File): Promise<PreparedImage> {
  let bitmap: ImageBitmap

  try {
    bitmap = await createImageBitmap(file)
  } catch {
    throw new Error('Не удалось прочитать изображение. Возможно, файл повреждён.')
  }

  try {
    const target = fitIntoBox(bitmap.width, bitmap.height, AttachmentLimits.imageMaxWidth, AttachmentLimits.imageMaxHeight)
    const resized = target.width !== bitmap.width || target.height !== bitmap.height

    if (!resized && file.size <= AttachmentLimits.imageMaxBytes) return { file, ...target, resized: false }

    const canvas = document.createElement('canvas')
    canvas.width = target.width
    canvas.height = target.height
    canvas.getContext('2d')?.drawImage(bitmap, 0, 0, target.width, target.height)

    const isJpeg = file.type === 'image/jpeg'
    const type = isJpeg ? 'image/jpeg' : 'image/png'
    const blob = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, type, 0.85))

    if (!blob || blob.size > AttachmentLimits.imageMaxBytes) throw new Error('Не удалось уменьшить изображение.')

    const name = file.name.replace(/\.[^.]*$/, '') + (isJpeg ? '.jpg' : '.png')

    return { file: new File([blob], name, { type }), ...target, resized }
  } finally {
    bitmap.close()
  }
}
