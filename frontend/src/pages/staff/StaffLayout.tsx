import { Link, NavLink, Outlet } from 'react-router-dom'
import { NotificationBell } from '../../components/NotificationBell'
import { useAuth } from '../../auth/AuthContext'

export function StaffLayout() {
  const { user, logout } = useAuth()
  const isSupervisor = user?.role === 'Supervisor'

  return (
    <div className="min-h-screen">
      <header className="border-b border-[var(--civic-line)] bg-[var(--civic-ink)] text-[var(--civic-sand)]">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-4 px-6 py-4">
          <div>
            <Link to="/staff" className="brand text-2xl text-white">
              CivicFlow
            </Link>
            <p className="text-sm text-[var(--civic-sky)]">Agency workbench</p>
          </div>
          <nav className="flex flex-wrap items-center gap-2 text-sm">
            <NavLink
              to="/staff"
              end
              className={({ isActive }) =>
                `rounded-lg px-3 py-1.5 ${isActive ? 'bg-white/15 font-semibold text-white' : 'text-[var(--civic-sky)] hover:bg-white/10'}`
              }
            >
              Work queue
            </NavLink>
            {isSupervisor ? (
              <NavLink
                to="/staff/dashboard"
                className={({ isActive }) =>
                  `rounded-lg px-3 py-1.5 ${isActive ? 'bg-white/15 font-semibold text-white' : 'text-[var(--civic-sky)] hover:bg-white/10'}`
                }
              >
                Supervisor dashboard
              </NavLink>
            ) : null}
            <NotificationBell />
            <span className="hidden sm:inline px-2 text-[var(--civic-sky)]">
              {user?.firstName || user?.email} · {user?.role}
            </span>
            <button
              type="button"
              onClick={logout}
              className="rounded-lg border border-white/25 px-3 py-1.5 hover:bg-white/10"
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
