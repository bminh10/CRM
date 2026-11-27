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

export interface UserProfile {
  email: string
  fullName: string
  isOtpConfigured: boolean
}

export interface OtpSetup {
  secret: string
  provisioningUri: string
  expiresAt: string
}

export function getUserProfile() {
  return api<UserProfile>('/users/profile')
}

export function setupOtp(email: string) {
  return api<OtpSetup>('/auth/otp/setup', { method: 'POST', body: JSON.stringify({ email }) })
}

export function verifyOtpSetup(email: string, code: string) {
  return api<{ isOtpConfigured: boolean }>('/auth/otp/verify-setup', {
    method: 'POST',
    body: JSON.stringify({ email, code }),
  })
}

export function changePassword(email: string, code: string, newPassword: string) {
  return api<{ message: string }>('/auth/change-password', {
    method: 'POST',
    body: JSON.stringify({ email, code, newPassword }),
  })
}

export { ApiError, logoutSession } from './api'
