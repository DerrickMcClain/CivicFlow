import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import {
  fetchCurrentUser,
  getAuthSource,
  getStoredToken,
  login as loginRequest,
  setAuthSource,
  setStoredToken,
  type AuthUser,
} from '../api/client'
import { signInWithMicrosoft, signOutMicrosoft } from './msal'

type AuthContextValue = {
  user: AuthUser | null
  token: string | null
  login: (email: string, password: string) => Promise<AuthUser>
  loginWithMicrosoft: () => Promise<AuthUser>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

function decodeUserFromToken(token: string): AuthUser | null {
  try {
    const payload = JSON.parse(atob(token.split('.')[1] ?? ''))
    const role =
      payload.role ??
      payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
    const email =
      typeof payload.email === 'string'
        ? payload.email
        : Array.isArray(payload.email)
          ? payload.email[0]
          : ''
    const userId = Number(
      payload.sub ??
        payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'],
    )
    if (!role || Number.isNaN(userId)) {
      return null
    }
    return {
      token,
      userId,
      email,
      role,
      firstName: '',
      lastName: '',
    }
  } catch {
    return null
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => {
    const token = getStoredToken()
    if (!token || getAuthSource() === 'entra') {
      return null
    }
    return decodeUserFromToken(token)
  })

  useEffect(() => {
    const token = getStoredToken()
    if (!token || getAuthSource() !== 'entra') {
      return
    }

    let cancelled = false
    ;(async () => {
      try {
        const profile = await fetchCurrentUser()
        if (!cancelled) {
          setUser(profile)
        }
      } catch {
        setStoredToken(null)
        setAuthSource(null)
        if (!cancelled) {
          setUser(null)
        }
      }
    })()

    return () => {
      cancelled = true
    }
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    const auth = await loginRequest(email, password)
    setStoredToken(auth.token)
    setAuthSource('local')
    setUser(auth)
    return auth
  }, [])

  const loginWithMicrosoft = useCallback(async () => {
    const auth = await signInWithMicrosoft()
    setUser(auth)
    return auth
  }, [])

  const logout = useCallback(() => {
    const clearSession = () => {
      setStoredToken(null)
      setAuthSource(null)
      setUser(null)
    }

    if (getAuthSource() === 'entra') {
      void signOutMicrosoft().finally(clearSession)
      return
    }

    clearSession()
  }, [])

  const value = useMemo(
    () => ({
      user,
      token: user?.token ?? null,
      login,
      loginWithMicrosoft,
      logout,
    }),
    [user, login, loginWithMicrosoft, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider')
  }
  return context
}

export function homePathForRole(role: string): string {
  switch (role) {
    case 'Citizen':
      return '/citizen'
    case 'Employee':
    case 'Supervisor':
      return '/staff'
    case 'Administrator':
      return '/admin'
    default:
      return '/login'
  }
}
