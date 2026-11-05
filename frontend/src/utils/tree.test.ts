import { describe, expect, it } from 'vitest'
import type { CommentDto } from '@/api/types'
import { buildReplyTree } from './tree'

function reply(id: number, parentId: number, rootId = 1): CommentDto {
  return { id, parentId, rootId, userName: 'u', email: 'u@example.com', homePage: null, text: 't', createdAt: '2026-10-30T10:00:00Z', attachment: null }
}

describe('buildReplyTree', () => {
  it('возвращает пустой список без ответов', () => {
    expect(buildReplyTree(1, [])).toEqual([])
  })

  it('кладёт ответы на корень на верхний уровень по порядку Id', () => {
    const tree = buildReplyTree(1, [reply(4, 1), reply(2, 1)])

    expect(tree.map((n) => n.comment.id)).toEqual([2, 4])
  })

  it('вкладывает ответы на ответы', () => {
    const tree = buildReplyTree(1, [reply(3, 2), reply(2, 1), reply(4, 3)])

    expect(tree).toHaveLength(1)
    expect(tree[0]?.comment.id).toBe(2)
    expect(tree[0]?.children[0]?.comment.id).toBe(3)
    expect(tree[0]?.children[0]?.children[0]?.comment.id).toBe(4)
  })

  it('не теряет ответ, у которого родителя нет в списке', () => {
    const tree = buildReplyTree(1, [reply(5, 99)])

    expect(tree.map((n) => n.comment.id)).toEqual([5])
  })
})
