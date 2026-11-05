// Типы повторяют ответы Comments.Api (CommentResponse, RootCommentResponse, PagedResponse, CaptchaResponse).

export interface AttachmentDto {
  kind: 'image' | 'text'
  url: string
  name: string
  size: number
}

export interface CommentDto {
  id: number
  parentId: number | null
  rootId: number | null
  userName: string
  email: string
  homePage: string | null
  text: string
  createdAt: string
  attachment: AttachmentDto | null
}

export interface RootCommentDto {
  id: number
  userName: string
  email: string
  homePage: string | null
  text: string
  createdAt: string
  attachment: AttachmentDto | null
  replies: CommentDto[]
}

export interface PagedDto<T> {
  items: T[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

export interface CaptchaDto {
  id: string
  image: string
}

export type SortField = 'Date' | 'UserName' | 'Email'
export type SortDirection = 'Asc' | 'Desc'

export interface NewComment {
  userName: string
  email: string
  homePage: string
  text: string
  parentId: number | null
  captchaId: string | null
  captchaAnswer: string
  file: File | null
}
