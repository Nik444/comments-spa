// Ошибка запроса к API. fieldErrors содержит сообщения по полям формы (ключи с маленькой буквы: userName, text, ...).
export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
    public readonly fieldErrors: Record<string, string[]> = {},
  ) {
    super(message)
  }
}

export async function getJson<T>(url: string, signal?: AbortSignal): Promise<T> {
  return send<T>(url, { signal })
}

export async function postForm<T>(url: string, body: FormData): Promise<T> {
  return send<T>(url, { method: 'POST', body })
}

async function send<T>(url: string, init: RequestInit): Promise<T> {
  let response: Response

  try {
    response = await fetch(url, init)
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') throw error
    throw new ApiError(0, 'Нет связи с сервером. Проверьте подключение и повторите попытку.')
  }

  if (!response.ok) throw await toApiError(response)

  return (await response.json()) as T
}

async function toApiError(response: Response): Promise<ApiError> {
  let problem: { title?: string; detail?: string; errors?: Record<string, string[]> } | null = null

  try {
    problem = await response.json()
  } catch {
    // Тело ответа не JSON, останется общее сообщение.
  }

  const fieldErrors: Record<string, string[]> = {}

  for (const [key, messages] of Object.entries(problem?.errors ?? {})) {
    fieldErrors[key.charAt(0).toLowerCase() + key.slice(1)] = messages
  }

  const message = problem?.detail ?? problem?.title ?? `Ошибка сервера (${response.status}).`

  return new ApiError(response.status, message, fieldErrors)
}
