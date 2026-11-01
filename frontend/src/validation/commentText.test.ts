import { describe, expect, it } from 'vitest'
import { validateCommentText } from './commentText'

describe('validateCommentText', () => {
  it.each([
    '',
    'Просто текст без тегов',
    '<i>курсив</i>',
    '<strong>жирный</strong>',
    '<code>var x = 1;</code>',
    '<a href="https://example.com">ссылка</a>',
    '<a href="http://example.com" title="Подсказка">ссылка</a>',
    '<A HREF="https://example.com"><I>регистр</I></A>',
    '<strong>жирный <i>и курсив</i></strong>',
    'если 2 < 3 и 5 > 4, то всё хорошо',
  ])('принимает допустимый текст: %s', (text) => {
    expect(validateCommentText(text)).toEqual([])
  })

  it.each([
    '<script>alert(1)</script>',
    '<img src=x onerror=alert(1)>',
    '<b>жирный</b>',
    '<!-- комментарий -->',
    '<?xml version="1.0"?>',
  ])('отклоняет запрещённые теги: %s', (text) => {
    expect(validateCommentText(text)).not.toEqual([])
  })

  it.each([
    '<i>не закрыт',
    'закрыт без открытия</i>',
    '<i><strong>неверный порядок</i></strong>',
    '<i>текст</strong>',
    '<a href="https://example.com">ссылка',
    '<i',
  ])('отклоняет битую структуру: %s', (text) => {
    expect(validateCommentText(text)).not.toEqual([])
  })

  it.each([
    '<a>без href</a>',
    '<a href="javascript:alert(1)">xss</a>',
    '<a href="JaVaScRiPt:alert(1)">xss</a>',
    '<a href="data:text/html;base64,PHNjcmlwdD4=">xss</a>',
    '<a href="&#106;avascript:alert(1)">xss</a>',
    '<a href="/относительная">ссылка</a>',
    '<a href="https://example.com" onclick="alert(1)">xss</a>',
    '<a href="https://example.com" href="https://example.org">дубль</a>',
    '<a href=https://example.com>без кавычек</a>',
    '<a href>без значения</a>',
    '<a href="https://example.com"title="слитно">ссылка</a>',
    '<a href="https://example.com"/>самозакрытый</a>',
    "<a href='https://example.com'>одинарные кавычки</a>",
    '<a title="порядок" href="https://example.com">title раньше href</a>',
    '<a href="https://example.com" title="с > внутри">ссылка</a>',
  ])('отклоняет неподдерживаемые ссылки: %s', (text) => {
    expect(validateCommentText(text)).not.toEqual([])
  })

  it.each(['<i class="x">атрибут</i>', '<strong onclick="alert(1)">атрибут</strong>', '<i/>'])(
    'отклоняет атрибуты у простых тегов: %s',
    (text) => {
      expect(validateCommentText(text)).not.toEqual([])
    },
  )

  it('сообщает, какой тег не закрыт', () => {
    expect(validateCommentText('<i>текст')).toEqual(['Тег <i> не закрыт.'])
  })

  it('перечисляет незакрытые теги, начиная с внутреннего', () => {
    expect(validateCommentText('<strong><i>текст')).toEqual(['Тег <i> не закрыт.', 'Тег <strong> не закрыт.'])
  })

  it('ограничивает число ошибок', () => {
    expect(validateCommentText('<script>'.repeat(100))).toHaveLength(10)
  })
})
