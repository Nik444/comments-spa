import { describe, expect, it } from 'vitest'
import { formatSize } from './format'

describe('formatSize', () => {
  it.each([
    [0, '0 Б'],
    [1023, '1023 Б'],
    [1024, '1 КБ'],
    [1025, '2 КБ'],
    [100 * 1024, '100 КБ'],
    [1024 * 1024, '1.0 МБ'],
    [1536 * 1024, '1.5 МБ'],
  ])('%i байт это %s', (bytes, expected) => {
    expect(formatSize(bytes)).toBe(expected)
  })
})
