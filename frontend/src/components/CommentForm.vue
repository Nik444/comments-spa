<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref, useId, watch } from 'vue'
import { createComment } from '@/api/comments'
import { ApiError } from '@/api/http'
import type { CommentDto } from '@/api/types'
import CaptchaField from '@/components/CaptchaField.vue'
import { useCaptcha } from '@/composables/useCaptcha'
import { validateForm, type FormErrors, type FormField, type FormValues } from '@/validation/commentForm'
import { Limits } from '@/validation/limits'
import { setDraft } from '@/utils/draft'
import { rememberOwn } from '@/utils/own'
import { renderCommentHtml } from '@/utils/sanitize'

const props = defineProps<{ parentId?: number | null }>()

const emit = defineEmits<{
  created: [comment: CommentDto]
  cancel: []
}>()

// Поле адреса сразу содержит начало ссылки. Если его не трогали, считается, что адрес не указан.
const homePagePrefix = 'https://www.'

const id = useId()
const values = reactive<FormValues>({ userName: '', email: '', homePage: homePagePrefix, text: '', captchaAnswer: '' })
const errors = ref<FormErrors>({})
const formError = ref<string | null>(null)
const submitting = ref(false)
const showPreview = ref(false)
const textarea = ref<HTMLTextAreaElement | null>(null)
const { state: captchaState, captcha, refresh: refreshCaptcha } = useCaptcha()

const preview = computed(() => renderCommentHtml(values.text))
const isReply = computed(() => props.parentId != null)

onMounted(refreshCaptcha)

watch(
  () => values.text,
  (text) => setDraft(id, isReply.value ? 'reply' : 'root', text.trim().length > 0),
)

onBeforeUnmount(() => setDraft(id, 'root', false))

async function wrapSelection(open: string, close: string) {
  const el = textarea.value
  if (!el) return

  const start = el.selectionStart
  const end = el.selectionEnd
  const selected = values.text.slice(start, end)

  values.text = values.text.slice(0, start) + open + selected + close + values.text.slice(end)

  await nextTick()
  el.focus()
  el.setSelectionRange(start + open.length, start + open.length + selected.length)
}

function applyServerErrors(error: ApiError) {
  const fields: FormField[] = ['userName', 'email', 'homePage', 'text', 'captchaAnswer']
  const mapped: FormErrors = {}

  for (const field of fields) {
    const messages = error.fieldErrors[field]
    if (messages?.length) mapped[field] = messages.join(' ')
  }

  errors.value = mapped
  if (Object.keys(mapped).length === 0) formError.value = error.message
}

async function submit() {
  formError.value = null
  const homePage = values.homePage.trim() === homePagePrefix ? '' : values.homePage

  errors.value = validateForm({ ...values, homePage }, captchaState.value === 'enabled')
  if (Object.keys(errors.value).length > 0) return

  submitting.value = true

  try {
    const comment = await createComment({
      userName: values.userName,
      email: values.email,
      homePage,
      text: values.text,
      parentId: props.parentId ?? null,
      captchaId: captcha.value?.id ?? null,
      captchaAnswer: values.captchaAnswer,
    })

    rememberOwn(comment.id)
    values.text = ''
    showPreview.value = false
    emit('created', comment)
  } catch (e) {
    if (e instanceof ApiError) applyServerErrors(e)
    else formError.value = 'Не удалось отправить комментарий.'
  } finally {
    values.captchaAnswer = ''
    submitting.value = false
    await refreshCaptcha()
  }
}
</script>

<template>
  <form class="comment-form" novalidate @submit.prevent="submit">
    <div class="form-row">
      <div class="field">
        <label :for="`${id}-name`">Имя *</label>
        <input :id="`${id}-name`" v-model="values.userName" type="text" :maxlength="Limits.userNameMax" :aria-invalid="!!errors.userName" />
        <p v-if="errors.userName" class="error-text">{{ errors.userName }}</p>
      </div>

      <div class="field">
        <label :for="`${id}-email`">E-mail *</label>
        <input :id="`${id}-email`" v-model="values.email" type="email" :maxlength="Limits.emailMax" :aria-invalid="!!errors.email" />
        <p v-if="errors.email" class="error-text">{{ errors.email }}</p>
      </div>

      <div class="field">
        <label :for="`${id}-home`">Домашняя страница</label>
        <input :id="`${id}-home`" v-model="values.homePage" type="url" placeholder="https://" :maxlength="Limits.homePageMax" :aria-invalid="!!errors.homePage" />
        <p v-if="errors.homePage" class="error-text">{{ errors.homePage }}</p>
      </div>
    </div>

    <div class="field">
      <label :for="`${id}-text`">Сообщение *</label>

      <div class="tag-buttons">
        <button type="button" class="btn" title="Курсив" @click="wrapSelection('<i>', '</i>')">[i]</button>
        <button type="button" class="btn" title="Жирный" @click="wrapSelection('<strong>', '</strong>')">[strong]</button>
        <button type="button" class="btn" title="Код" @click="wrapSelection('<code>', '</code>')">[code]</button>
        <button type="button" class="btn" title="Ссылка" @click="wrapSelection('<a href=&quot;https://&quot;>', '</a>')">[a]</button>
      </div>

      <textarea :id="`${id}-text`" ref="textarea" v-model="values.text" rows="5" :maxlength="Limits.textMax" :aria-invalid="!!errors.text" />
      <p v-if="errors.text" class="error-text">{{ errors.text }}</p>
    </div>

    <div v-if="showPreview" class="preview">
      <div class="preview-title">Предпросмотр</div>
      <div v-if="values.text.trim()" class="comment-text" v-html="preview" />
      <p v-else class="muted">Сообщение пустое.</p>
    </div>

    <CaptchaField v-model="values.captchaAnswer" :state="captchaState" :captcha="captcha" :error="errors.captchaAnswer" @refresh="refreshCaptcha" />

    <p v-if="formError" class="error-text" role="alert">{{ formError }}</p>

    <div class="actions">
      <button type="submit" class="btn primary" :disabled="submitting">{{ submitting ? 'Отправка…' : isReply ? 'Ответить' : 'Отправить' }}</button>
      <button type="button" class="btn" @click="showPreview = !showPreview">{{ showPreview ? 'Скрыть предпросмотр' : 'Предпросмотр' }}</button>
      <button v-if="isReply" type="button" class="btn" @click="emit('cancel')">Отмена</button>
    </div>
  </form>
</template>
