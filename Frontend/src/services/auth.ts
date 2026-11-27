import type { AuthSession } from '@/types/auth'
import { isAuthSession } from '@/utils/auth'
import { api, ensureSuccess, request, startSession } from './api'

async function authenticate(path: string, body: object): Promise<AuthSession> {
  const response = await request(path, { method: 'POST', body: JSON.stringify(body) })
  await ensureSuccess(response)
  const result: unknown = await response.json()
  if (!isAuthSession(result)) throw new Error('Invalid authentication response')
  startSession()
  return result
}

export function login(email: string, password: string) {
  return authenticate('/auth/session/login', { email, password })
}

export function register(fullName: string, email: string, password: string) {
  return authenticate('/auth/session/register', { fullName, email, password })
}

export async function getSession(): Promise<AuthSession> {
  const result = { user: await api<unknown>('/auth/me') }
  if (!isAuthSession(result)) throw new Error('Invalid user response')
  return result
}

export { ApiError, logoutSession } from './api'
