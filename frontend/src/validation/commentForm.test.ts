import { describe, expect, it } from 'vitest'
import { validateForm, type FormValues } from './commentForm'

const valid: FormValues = { userName: 'Nikolay', email: 'a@b.co', homePage: '', text: 'Привет', captchaAnswer: '' }

describe('validateForm: имя', () => {
  it.each(['Nikolay', 'user123', 'A', '007'])('принимает %s', (userName) => {
    expect(validateForm({ ...valid, userName }, false).userName).toBeUndefined()
  })

  it.each(['Николай', 'john smith', 'john_smith', 'john-smith', 'john@', '<script>', 'Jöhn'])('отклоняет %s', (userName) => {
    expect(validateForm({ ...valid, userName }, false).userName).toContain('латинские буквы и цифры')
  })

  it('пробелы по краям не мешают, как и при отправке', () => {
    expect(validateForm({ ...valid, userName: '  Nikolay  ' }, false).userName).toBeUndefined()
  })

  it('пустое имя даёт понятное сообщение', () => {
    expect(validateForm({ ...valid, userName: '  ' }, false).userName).toBe('Введите имя.')
  })
})

describe('validateForm: остальное', () => {
  it('корректная форма без ошибок', () => {
    expect(validateForm(valid, false)).toEqual({})
  })

  it('капча требуется только когда включена', () => {
    expect(validateForm(valid, true).captchaAnswer).toBeDefined()
    expect(validateForm(valid, false).captchaAnswer).toBeUndefined()
  })
})
