import { ApiError, getJson, postForm } from './http'
import type { CaptchaDto, CommentDto, NewComment, PagedDto, RootCommentDto, SortDirection, SortField } from './types'

export function fetchPage(page: number, sortBy: SortField, sortDir: SortDirection, signal?: AbortSignal) {
  const query = new URLSearchParams({ page: String(page), sortBy, sortDir })

  return getJson<PagedDto<RootCommentDto>>(`/api/comments?${query}`, signal)
}

export function fetchByIds(ids: number[]) {
  const query = new URLSearchParams()
  ids.forEach((id) => query.append('ids', String(id)))

  return getJson<CommentDto[]>(`/api/comments/by-ids?${query}`)
}

export function createComment(comment: NewComment) {
  const form = new FormData()

  form.append('UserName', comment.userName.trim())
  form.append('Email', comment.email.trim())
  form.append('Text', comment.text)

  if (comment.homePage.trim()) form.append('HomePage', comment.homePage.trim())
  if (comment.parentId !== null) form.append('ParentId', String(comment.parentId))
  if (comment.captchaId) form.append('CaptchaId', comment.captchaId)
  if (comment.captchaAnswer.trim()) form.append('CaptchaAnswer', comment.captchaAnswer.trim())
  if (comment.file) form.append('File', comment.file)

  return postForm<CommentDto>('/api/comments', form)
}

// null значит, что капча на сервере выключена (404), и поле для кода показывать не нужно.
export async function fetchCaptcha(): Promise<CaptchaDto | null> {
  try {
    return await getJson<CaptchaDto>('/api/captcha')
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) return null
    throw error
  }
}
