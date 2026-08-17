import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

type PortalShellProps = {
  title: string
  subtitle: string
  children?: ReactNode
}

export function PortalShell({ title, subtitle, children }: PortalShellProps) {
  const { user, logout } = useAuth()

  return (
    <div className="min-h-screen">
      <header className="border-b border-[var(--civic-line)] bg-white/80 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-6 py-4">
          <div>
            <Link to="/" className="brand text-2xl text-[var(--civic-navy)]">
              CivicFlow
            </Link>
            <p className="text-sm text-[var(--civic-navy)]/70">{subtitle}</p>
          </div>
          <div className="flex items-center gap-3 text-sm">
            <span className="hidden sm:inline text-[var(--civic-navy)]">
              {user?.firstName || user?.email} · {user?.role}
            </span>
            <button
              type="button"
              onClick={logout}
              className="rounded-lg border border-[var(--civic-line)] px-3 py-1.5 hover:bg-[var(--civic-sky)]/40"
            >
              Sign out
            </button>
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-6 py-10">
        <h1 className="mb-2 text-4xl text-[var(--civic-navy)]">{title}</h1>
        {children}
      </main>
    </div>
  )
}
