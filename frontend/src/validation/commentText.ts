import { isHttpUrl } from './httpUrl'

// Порт серверной проверки (Comments.Api/Application/Validation/CommentTextValidator.cs): те же правила и сообщения.
// Сервер всё равно проверяет текст сам, здесь это только быстрая подсказка пользователю.

const MAX_ERRORS = 10
const SIMPLE_TAGS = new Set(['code', 'i', 'strong'])
const ALLOWED_TAGS = new Set(['a', 'code', 'i', 'strong'])

// Одиночный "<" (например, "a < b") тегом не считается, а всё, что похоже на тег, обязано быть разрешённым.
const TAG_REGEX = /<(?=[A-Za-z/!?])[^>]*(>)?/g

// Строгий вид ссылки: href обязателен и идёт первым, title необязателен, только двойные кавычки.
const ANCHOR_REGEX = /^a\s+href="([^"<>]*)"(\s+title="[^"<>]*")?$/i

export function validateCommentText(text: string): string[] {
  const errors: string[] = []

  if (!text) return errors

  const openTags: string[] = []

  for (const match of text.matchAll(TAG_REGEX)) {
    if (errors.length >= MAX_ERRORS) break

    if (match[1] === undefined) {
      errors.push("Тег не завершён: не найден закрывающий символ '>'.")
      break
    }

    readTag(match[0].slice(1, -1).trim(), openTags, errors)
  }

  for (const name of [...openTags].reverse().slice(0, MAX_ERRORS - errors.length)) {
    errors.push(`Тег <${name}> не закрыт.`)
  }

  return errors
}

function readTag(body: string, openTags: string[], errors: string[]) {
  if (body.startsWith('/')) {
    readClosingTag(body.slice(1).trim().toLowerCase(), openTags, errors)
    return
  }

  const lowered = body.toLowerCase()

  if (SIMPLE_TAGS.has(lowered)) {
    openTags.push(lowered)
    return
  }

  const anchor = ANCHOR_REGEX.exec(body)

  if (anchor) {
    if (!isHttpUrl(anchor[1] ?? '')) errors.push('Ссылка в href должна быть полным адресом http или https.')

    openTags.push('a')
    return
  }

  const nameEnd = lowered.search(/[ \t\r\n/]/)
  const name = nameEnd < 0 ? lowered : lowered.slice(0, nameEnd)

  if (!ALLOWED_TAGS.has(name)) {
    errors.push(`Тег <${name}> не разрешён.`)
    return
  }

  errors.push(
    name === 'a'
      ? 'Ссылка должна иметь вид <a href="https://..." title="...">, title необязателен.'
      : `Тег <${name}> не должен содержать атрибуты или '/'.`,
  )

  openTags.push(name)
}

function readClosingTag(name: string, openTags: string[], errors: string[]) {
  if (!ALLOWED_TAGS.has(name)) {
    errors.push(`Закрывающий тег </${name}> не разрешён.`)
  } else if (openTags.length === 0) {
    errors.push(`Лишний закрывающий тег </${name}>: он ничего не закрывает.`)
  } else if (openTags[openTags.length - 1] !== name) {
    errors.push(`Теги закрыты в неправильном порядке: ожидался </${openTags[openTags.length - 1]}>, найден </${name}>.`)
  } else {
    openTags.pop()
  }
}
