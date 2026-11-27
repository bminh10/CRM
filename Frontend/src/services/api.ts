export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message)
  }
}

let refreshPromise: Promise<void> | null = null
let sessionEnded = false

export function request(path: string, options: RequestInit = {}) {
  const headers = new Headers(options.headers)
  headers.set('X-CRM-Request', '1')
  if (options.body) headers.set('Content-Type', 'application/json')
  return fetch(`/api${path}`, { ...options, headers, credentials: 'same-origin', cache: 'no-store' })
}

export async function ensureSuccess(response: Response) {
  if (response.ok) return
  let message = `Yêu cầu thất bại (HTTP ${response.status}).`
  const body: unknown = await response.json().catch(() => null)
  if (typeof body === 'object' && body !== null && 'message' in body && typeof body.message === 'string') message = body.message
  throw new ApiError(response.status, message)
}

function refreshSession() {
  if (!refreshPromise) {
    refreshPromise = request('/auth/session/refresh', { method: 'POST' })
      .then(ensureSuccess)
      .finally(() => { refreshPromise = null })
  }
  return refreshPromise
}

export async function api<T>(path: string, options: RequestInit = {}): Promise<T> {
  let response = await request(path, options)
  if (response.status === 401 && !sessionEnded) {
    try {
      await refreshSession()
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) window.dispatchEvent(new Event('crm:session-expired'))
      throw error
    }
    if (!sessionEnded) response = await request(path, options)
  }
  if (response.status === 401 && !sessionEnded) window.dispatchEvent(new Event('crm:session-expired'))
  await ensureSuccess(response)
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}

export function startSession() {
  sessionEnded = false
}

export async function logoutSession() {
  sessionEnded = true
  try {
    if (refreshPromise) await Promise.allSettled([refreshPromise])
    await ensureSuccess(await request('/auth/session/logout', { method: 'POST' }))
  } catch (error) {
    sessionEnded = false
    throw error
  }
}
