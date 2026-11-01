// @vitest-environment jsdom
import { describe, expect, it } from 'vitest'
import { renderCommentHtml } from './sanitize'

describe('renderCommentHtml', () => {
  it('оставляет разрешённые теги', () => {
    const html = renderCommentHtml('<strong>жирный</strong> и <i>курсив</i> и <code>код</code>')

    expect(html).toBe('<strong>жирный</strong> и <i>курсив</i> и <code>код</code>')
  })

  it('удаляет скрипты', () => {
    expect(renderCommentHtml('до<script>alert(1)</script>после')).toBe('допосле')
  })

  it('удаляет обработчики событий', () => {
    const html = renderCommentHtml('<a href="https://example.com" onclick="alert(1)">ссылка</a>')

    expect(html).not.toContain('onclick')
  })

  it('удаляет картинки с обработчиками', () => {
    expect(renderCommentHtml('<img src=x onerror=alert(1)>')).toBe('')
  })

  it('убирает ссылки javascript:', () => {
    expect(renderCommentHtml('<a href="javascript:alert(1)">xss</a>')).not.toContain('javascript')
  })

  it('делает ссылки безопасными', () => {
    const html = renderCommentHtml('<a href="https://example.com" title="т">ссылка</a>')

    expect(html).toContain('target="_blank"')
    expect(html).toContain('rel="noopener noreferrer nofollow"')
  })

  it('показывает одиночный "<" как обычный символ', () => {
    expect(renderCommentHtml('2 < 3')).toBe('2 &lt; 3')
  })
})
