import { validateCommentText } from './commentText'
import { isHttpUrl } from './httpUrl'
import { Limits } from './limits'

export interface FormValues {
  userName: string
  email: string
  homePage: string
  text: string
  captchaAnswer: string
}

export type FormField = keyof FormValues
export type FormErrors = Partial<Record<FormField, string>>

const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

// Те же правила, что на сервере. Сервер проверяет всё повторно, здесь это быстрая подсказка до отправки.
export function validateForm(values: FormValues, captchaRequired: boolean): FormErrors {
  const errors: FormErrors = {}

  const userName = values.userName.trim()
  if (!userName) errors.userName = 'Введите имя.'
  else if (userName.length > Limits.userNameMax) errors.userName = `Не больше ${Limits.userNameMax} символов.`

  const email = values.email.trim()
  if (!email) errors.email = 'Введите e-mail.'
  else if (email.length > Limits.emailMax) errors.email = `Не больше ${Limits.emailMax} символов.`
  else if (!EMAIL_REGEX.test(email)) errors.email = 'Некорректный e-mail.'

  const homePage = values.homePage.trim()
  if (homePage && homePage.length > Limits.homePageMax) errors.homePage = `Не больше ${Limits.homePageMax} символов.`
  else if (homePage && !isHttpUrl(homePage)) errors.homePage = 'Адрес должен начинаться с http:// или https://.'

  if (!values.text.trim()) errors.text = 'Введите текст сообщения.'
  else if (values.text.length > Limits.textMax) errors.text = `Не больше ${Limits.textMax} символов.`
  else {
    const textErrors = validateCommentText(values.text)
    if (textErrors.length > 0) errors.text = textErrors.join(' ')
  }

  if (captchaRequired && !values.captchaAnswer.trim()) errors.captchaAnswer = 'Введите код с картинки.'

  return errors
}
