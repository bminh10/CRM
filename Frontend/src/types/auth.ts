export interface AuthUser {
  fullName: string
}

export interface AuthSession {
  user: AuthUser
  tokens: {
    accessToken: string
    refreshToken: string
    expiresIn: number
  }
}

export function isAuthSession(value: unknown): value is AuthSession {
  if (typeof value !== 'object' || value === null ||
      !('user' in value) || !('tokens' in value)) return false

  const { user, tokens } = value
  if (typeof user !== 'object' || user === null ||
      typeof tokens !== 'object' || tokens === null) return false

  return 'fullName' in user && typeof user.fullName === 'string' && !!user.fullName &&
    'accessToken' in tokens && typeof tokens.accessToken === 'string' && !!tokens.accessToken &&
    'refreshToken' in tokens && typeof tokens.refreshToken === 'string' && !!tokens.refreshToken &&
    'expiresIn' in tokens && typeof tokens.expiresIn === 'number' &&
    Number.isFinite(tokens.expiresIn) && tokens.expiresIn > 0
}
