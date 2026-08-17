import { Link, NavLink, Outlet } from 'react-router-dom'
import { NotificationBell } from '../../components/NotificationBell'
import { useAuth } from '../../auth/AuthContext'

export function CitizenLayout() {
  const { user, logout } = useAuth()

  return (
    <div className="min-h-screen">
      <header className="border-b border-[var(--civic-line)] bg-white/90 backdrop-blur">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-4 px-6 py-4">
          <div>
            <Link to="/citizen" className="brand text-2xl text-[var(--civic-navy)]">
              CivicFlow
            </Link>
            <p className="text-sm text-[var(--civic-navy)]/70">Citizen services</p>
          </div>
          <nav className="flex items-center gap-2 text-sm">
            <NavLink
              to="/citizen"
              end
              className={({ isActive }) =>
                `rounded-lg px-3 py-1.5 ${isActive ? 'bg-[var(--civic-sky)] text-[var(--civic-navy)] font-semibold' : 'text-[var(--civic-navy)]/80 hover:bg-[var(--civic-sky)]/40'}`
              }
            >
              My requests
            </NavLink>
            <NavLink
              to="/citizen/new"
              className={({ isActive }) =>
                `rounded-lg px-3 py-1.5 ${isActive ? 'bg-[var(--civic-sky)] text-[var(--civic-navy)] font-semibold' : 'text-[var(--civic-navy)]/80 hover:bg-[var(--civic-sky)]/40'}`
              }
            >
              Submit request
            </NavLink>
            <NavLink
              to="/citizen/policy-help"
              className={({ isActive }) =>
                `rounded-lg px-3 py-1.5 ${isActive ? 'bg-[var(--civic-sky)] text-[var(--civic-navy)] font-semibold' : 'text-[var(--civic-navy)]/80 hover:bg-[var(--civic-sky)]/40'}`
              }
            >
              Policy help
            </NavLink>
            <NotificationBell />
            <span className="hidden sm:inline px-2 text-[var(--civic-navy)]/70">
              {user?.firstName || user?.email}
            </span>
            <button
              type="button"
              onClick={logout}
              className="rounded-lg border border-[var(--civic-line)] px-3 py-1.5 hover:bg-[var(--civic-sky)]/40"
            >
              Sign out
            </button>
          </nav>
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-6 py-8">
        <Outlet />
      </main>
    </div>
  )
}
