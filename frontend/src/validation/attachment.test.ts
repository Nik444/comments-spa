import { describe, expect, it } from 'vitest'
import { fitIntoBox } from '@/utils/image'
import { checkAttachment, detectKind } from './attachment'

describe('detectKind', () => {
  it.each([
    ['photo.JPG', 'image/jpeg', 'image'],
    ['a.png', 'image/png', 'image'],
    ['a.gif', 'image/gif', 'image'],
    ['a.jpeg', '', 'image'],
    ['notes.txt', 'text/plain', 'text'],
    ['NOTES.TXT', '', 'text'],
  ])('%s (%s) распознаётся как %s', (name, type, kind) => {
    expect(detectKind({ name, type })).toBe(kind)
  })

  it.each([
    ['a.exe', 'application/octet-stream'],
    ['a.pdf', 'application/pdf'],
    ['a.svg', 'image/svg+xml'],
    ['a.png', 'text/plain'],
    ['a.txt', 'image/png'],
    ['noextension', ''],
  ])('%s (%s) не подходит', (name, type) => {
    expect(detectKind({ name, type })).toBeNull()
  })
})

describe('checkAttachment', () => {
  it('принимает подходящий текст и картинку', () => {
    expect(checkAttachment({ name: 'a.txt', type: 'text/plain', size: 100 * 1024 })).toBeNull()
    expect(checkAttachment({ name: 'a.png', type: 'image/png', size: 5 * 1024 * 1024 })).toBeNull()
  })

  it('отклоняет пустой файл, большой текст и огромное фото', () => {
    expect(checkAttachment({ name: 'a.txt', type: 'text/plain', size: 0 })).toContain('пустой')
    expect(checkAttachment({ name: 'a.txt', type: 'text/plain', size: 100 * 1024 + 1 })).toContain('100 КБ')
    expect(checkAttachment({ name: 'a.jpg', type: 'image/jpeg', size: 21 * 1024 * 1024 })).toContain('20 МБ')
  })

  it('отклоняет неподходящий формат', () => {
    expect(checkAttachment({ name: 'a.exe', type: '', size: 10 })).toContain('JPG, GIF, PNG и TXT')
  })
})

describe('fitIntoBox', () => {
  it('не трогает картинку, которая уже помещается', () => {
    expect(fitIntoBox(320, 240, 320, 240)).toEqual({ width: 320, height: 240 })
    expect(fitIntoBox(10, 10, 320, 240)).toEqual({ width: 10, height: 10 })
  })

  it('уменьшает пропорционально по более тесной стороне', () => {
    expect(fitIntoBox(640, 480, 320, 240)).toEqual({ width: 320, height: 240 })
    expect(fitIntoBox(1920, 1080, 320, 240)).toEqual({ width: 320, height: 180 })
    expect(fitIntoBox(1000, 2000, 320, 240)).toEqual({ width: 120, height: 240 })
  })

  it('не даёт размеру стать нулевым', () => {
    expect(fitIntoBox(10000, 1, 320, 240)).toEqual({ width: 320, height: 1 })
  })
})
