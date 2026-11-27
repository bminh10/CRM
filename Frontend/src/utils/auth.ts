import type { AuthSession } from '@/types/auth'

export function isAuthSession(value: unknown): value is AuthSession {
  if (typeof value !== 'object' || value === null || !('user' in value)) return false
  const { user } = value
  if (typeof user !== 'object' || user === null) return false
  return 'fullName' in user && typeof user.fullName === 'string' && !!user.fullName
}
