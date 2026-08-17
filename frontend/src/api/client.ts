const TOKEN_KEY = 'civicflow.token'
export const AUTH_SOURCE_KEY = 'civicflow.authSource'

export type AuthSource = 'local' | 'entra'

// Empty for local dev and the Docker/nginx stack, which are same-origin. Split hosting (Azure)
// sets VITE_API_BASE_URL at build time to the API's public origin.
const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/+$/, '')

function resolveUrl(path: string): string {
  if (!API_BASE_URL || /^https?:\/\//i.test(path)) {
    return path
  }

  return path.startsWith('/') ? `${API_BASE_URL}${path}` : `${API_BASE_URL}/${path}`
}

export type AuthUser = {
  token: string
  userId: number
  email: string
  role: string
  firstName: string
  lastName: string
}

export class ApiError extends Error {
  status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

export function getStoredToken(): string | null {
  return localStorage.getItem(TOKEN_KEY)
}

export function setStoredToken(token: string | null) {
  if (token) {
    localStorage.setItem(TOKEN_KEY, token)
  } else {
    localStorage.removeItem(TOKEN_KEY)
  }
}

export function getAuthSource(): AuthSource | null {
  const value = localStorage.getItem(AUTH_SOURCE_KEY)
  if (value === 'local' || value === 'entra') {
    return value
  }
  return null
}

export function setAuthSource(source: AuthSource | null) {
  if (source) {
    localStorage.setItem(AUTH_SOURCE_KEY, source)
  } else {
    localStorage.removeItem(AUTH_SOURCE_KEY)
  }
}

export async function fetchCurrentUser(): Promise<AuthUser> {
  const token = getStoredToken()
  if (!token) {
    throw new ApiError(401, 'Authentication is required.')
  }

  const profile = await apiFetch<Omit<AuthUser, 'token'>>('/api/auth/me')
  return { ...profile, token }
}

export async function apiFetch<T>(
  path: string,
  options: RequestInit = {},
): Promise<T> {
  const headers = new Headers(options.headers)
  if (!headers.has('Content-Type') && options.body) {
    headers.set('Content-Type', 'application/json')
  }

  const token = getStoredToken()
  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  const response = await fetch(resolveUrl(path), { ...options, headers })
  if (!response.ok) {
    let message = `Request failed (${response.status})`
    try {
      const body = await response.json()
      if (typeof body?.message === 'string') {
        message = body.message
      }
    } catch {
      // keep default message
    }
    throw new ApiError(response.status, message)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

export async function login(email: string, password: string): Promise<AuthUser> {
  return apiFetch<AuthUser>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password }),
  })
}
