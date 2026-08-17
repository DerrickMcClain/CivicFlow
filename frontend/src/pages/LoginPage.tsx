import { useState, type FormEvent } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { ApiError } from '../api/client'
import { homePathForRole, useAuth } from '../auth/AuthContext'

export function LoginPage() {
  const { user, login } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('citizen@civicflow.local')
  const [password, setPassword] = useState('CivicFlow!dev1')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  if (user) {
    return <Navigate to={homePathForRole(user.role)} replace />
  }

  async function onSubmit(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    setError(null)
    try {
      const auth = await login(email.trim(), password)
      navigate(homePathForRole(auth.role), { replace: true })
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to sign in.')
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
              Submit, review, and approve service requests with enforced status
              transitions, role-based access, and a full audit trail.
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
              Use a seeded demo account or a registered citizen login.
            </p>
          </div>

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

          <div className="rounded-lg bg-[var(--civic-sky)]/50 px-3 py-3 text-xs text-[var(--civic-navy)] space-y-1">
            <p className="font-semibold">Demo users (password: CivicFlow!dev1)</p>
            <p>citizen@civicflow.local · employee@civicflow.local</p>
            <p>supervisor@civicflow.local · admin@civicflow.local</p>
          </div>
        </form>
      </section>
    </main>
  )
}
