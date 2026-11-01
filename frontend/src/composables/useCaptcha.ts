import { ref } from 'vue'
import { fetchCaptcha } from '@/api/comments'
import type { CaptchaDto } from '@/api/types'

// loading: идёт загрузка. enabled: капча включена, картинка есть. disabled: сервер её не требует. error: не удалось загрузить.
export type CaptchaState = 'loading' | 'enabled' | 'disabled' | 'error'

export function useCaptcha() {
  const state = ref<CaptchaState>('loading')
  const captcha = ref<CaptchaDto | null>(null)

  async function refresh() {
    state.value = 'loading'
    captcha.value = null

    try {
      captcha.value = await fetchCaptcha()
      state.value = captcha.value ? 'enabled' : 'disabled'
    } catch {
      state.value = 'error'
    }
  }

  return { state, captcha, refresh }
}
