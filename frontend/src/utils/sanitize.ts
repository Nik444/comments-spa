import DOMPurify from 'dompurify'

// Текст комментария показывается через v-html, поэтому перед показом он всегда проходит через DOMPurify,
// даже если сервер уже проверил его: клиент не должен доверять данным.
// Разрешены те же теги, что и на сервере. Ссылки только http и https, и открываются в новой вкладке без передачи opener.
const config = {
  ALLOWED_TAGS: ['a', 'code', 'i', 'strong'],
  ALLOWED_ATTR: ['href', 'title'],
  ALLOWED_URI_REGEXP: /^https?:\/\//i,
}

DOMPurify.addHook('afterSanitizeAttributes', (node) => {
  if (node.tagName === 'A') {
    node.setAttribute('target', '_blank')
    node.setAttribute('rel', 'noopener noreferrer nofollow')
  }
})

export function renderCommentHtml(text: string): string {
  return DOMPurify.sanitize(text, config)
}
