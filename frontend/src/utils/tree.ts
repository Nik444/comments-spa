import type { CommentDto } from '@/api/types'

export interface ReplyNode {
  comment: CommentDto
  children: ReplyNode[]
}

// Сервер отдаёт ответы плоским списком (у каждого есть parentId), а здесь из них строится дерево.
// Родитель всегда создан раньше ребёнка, поэтому порядок по Id гарантирует, что родитель уже есть в словаре.
export function buildReplyTree(rootId: number, replies: CommentDto[]): ReplyNode[] {
  const nodes = new Map<number, ReplyNode>()
  const top: ReplyNode[] = []

  for (const comment of [...replies].sort((a, b) => a.id - b.id)) {
    const node: ReplyNode = { comment, children: [] }
    nodes.set(comment.id, node)

    const parent = comment.parentId === null || comment.parentId === rootId ? undefined : nodes.get(comment.parentId)

    // Ответ на корень или ответ, чей родитель не найден, показывается на верхнем уровне ветки.
    if (parent) parent.children.push(node)
    else top.push(node)
  }

  return top
}
