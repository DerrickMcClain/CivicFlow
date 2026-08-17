import { useState, type FormEvent } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { ApiError } from '../api/client'
import { homePathForRole, useAuth } from '../auth/AuthContext'
import { isEntraConfigured } from '../auth/msal'

const DEMO_PASSWORD = 'CivicFlow!dev1'

const DEMO_ACCOUNTS = [
  {
    role: 'Citizen',
    email: 'citizen@civicflow.local',
    portal: 'Citizen portal',
    path: '/citizen',
  },
  {
    role: 'Employee',
    email: 'employee@civicflow.local',
    portal: 'Staff work queue',
    path: '/staff',
  },
  {
    role: 'Supervisor',
    email: 'supervisor@civicflow.local',
    portal: 'Staff + approvals',
    path: '/staff',
  },
  {
    role: 'Administrator',
    email: 'admin@civicflow.local',
    portal: 'Admin console',
    path: '/admin',
  },
] as const

export function LoginPage() {
  const { user, login, loginWithMicrosoft } = useAuth()
  const entraEnabled = isEntraConfigured()
  const navigate = useNavigate()
  const [email, setEmail] = useState('citizen@civicflow.local')
  const [password, setPassword] = useState(DEMO_PASSWORD)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  if (user) {
    return <Navigate to={homePathForRole(user.role)} replace />
  }

  async function signIn(nextEmail: string, nextPassword: string) {
    setBusy(true)
    setError(null)
    try {
      const auth = await login(nextEmail.trim(), nextPassword)
      navigate(homePathForRole(auth.role), { replace: true })
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to sign in.')
    } finally {
      setBusy(false)
    }
  }

  async function onSubmit(event: FormEvent) {
    event.preventDefault()
    await signIn(email, password)
  }

  async function onMicrosoftSignIn() {
    setBusy(true)
    setError(null)
    try {
      const auth = await loginWithMicrosoft()
      navigate(homePathForRole(auth.role), { replace: true })
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to sign in with Microsoft.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <main className="min-h-screen grid lg:grid-cols-[1.1fr_0.9fr]">
      <section className="relative overflow-hidden px-8 py-12 lg:px-16 lg:py-16 text-[var(--civic-sand)] bg-[var(--civic-ink)]">
        <div
          className="absolute inset-0 opacity-40"
          style={{
            backgroundImage:
              'linear-gradient(135deg, rgba(31,111,139,0.55), transparent 55%), radial-gradient(circle at 80% 20%, rgba(196,92,38,0.35), transparent 40%)',
          }}
        />
        <div className="relative max-w-xl flex flex-col justify-between min-h-[70vh] lg:min-h-full gap-10">
          <p className="brand text-4xl md:text-5xl text-white">CivicFlow</p>
          <div className="space-y-4">
            <h1 className="text-3xl md:text-5xl leading-tight text-white">
              Case workflow for local government services.
            </h1>
            <p className="text-lg text-[var(--civic-sky)] max-w-md">
              Four roles, three portals. Pick a demo account below to open Citizen,
              Staff, or Admin.
            </p>
          </div>
          <p className="text-sm text-[var(--civic-sky)]">
            Portfolio demo for public-sector Microsoft-stack roles.
          </p>
        </div>
      </section>

      <section className="flex items-center justify-center px-6 py-12">
        <form
          onSubmit={onSubmit}
          className="w-full max-w-md space-y-5 rounded-2xl border border-[var(--civic-line)] bg-white/80 p-8 shadow-[0_20px_60px_rgba(11,31,51,0.08)] backdrop-blur"
        >
          <div className="space-y-1">
            <h2 className="text-3xl text-[var(--civic-navy)]">Sign in</h2>
            <p className="text-sm text-[var(--civic-navy)]/70">
              Each account opens a different portal. Password for all demos:{' '}
              <span className="font-semibold">{DEMO_PASSWORD}</span>
            </p>
          </div>

          {entraEnabled ? (
            <button
              type="button"
              disabled={busy}
              onClick={() => void onMicrosoftSignIn()}
              className="w-full rounded-lg border border-[var(--civic-line)] bg-white px-4 py-3 font-semibold text-[var(--civic-navy)] transition hover:border-[var(--civic-blue)] hover:bg-[var(--civic-sky)]/30 disabled:opacity-60"
            >
              Sign in with Microsoft
            </button>
          ) : null}

          <div className="grid gap-2">
            {DEMO_ACCOUNTS.map((account) => (
              <button
                key={account.email}
                type="button"
                disabled={busy}
                onClick={() => {
                  setEmail(account.email)
                  setPassword(DEMO_PASSWORD)
                  void signIn(account.email, DEMO_PASSWORD)
                }}
                className="flex items-center justify-between gap-3 rounded-lg border border-[var(--civic-line)] bg-white px-3 py-2.5 text-left transition hover:border-[var(--civic-blue)] hover:bg-[var(--civic-sky)]/30 disabled:opacity-60"
              >
                <span>
                  <span className="block text-sm font-semibold text-[var(--civic-navy)]">
                    {account.role}
                  </span>
                  <span className="block text-xs text-[var(--civic-navy)]/70">
                    {account.portal} → {account.path}
                  </span>
                </span>
                <span className="text-xs text-[var(--civic-blue)]">Open</span>
              </button>
            ))}
          </div>

          <div className="border-t border-[var(--civic-line)] pt-4 space-y-4">
            <p className="text-xs font-semibold uppercase tracking-wide text-[var(--civic-navy)]/60">
              Or sign in manually
            </p>

            <label className="block space-y-2">
              <span className="text-sm font-medium text-[var(--civic-navy)]">Email</span>
              <input
                className="w-full rounded-lg border border-[var(--civic-line)] bg-white px-3 py-2.5 outline-none focus:border-[var(--civic-blue)]"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                autoComplete="username"
                required
              />
            </label>

            <label className="block space-y-2">
              <span className="text-sm font-medium text-[var(--civic-navy)]">Password</span>
              <input
                className="w-full rounded-lg border border-[var(--civic-line)] bg-white px-3 py-2.5 outline-none focus:border-[var(--civic-blue)]"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                autoComplete="current-password"
                required
              />
            </label>

            {error ? (
              <p className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>
            ) : null}

            <button
              type="submit"
              disabled={busy}
              className="w-full rounded-lg bg-[var(--civic-navy)] px-4 py-3 font-semibold text-white transition hover:bg-[var(--civic-ink)] disabled:opacity-60"
            >
              {busy ? 'Signing in…' : 'Sign in'}
            </button>
          </div>
        </form>
      </section>
    </main>
  )
}
