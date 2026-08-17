import { Link, NavLink, Outlet } from 'react-router-dom'
import { NotificationBell } from '../../components/NotificationBell'
import { downloadAuthenticatedFile } from '../../api/client'
import { useAuth } from '../../auth/AuthContext'

export function AdminLayout() {
  const { user, logout } = useAuth()

  return (
    <div className="min-h-screen">
      <header className="border-b border-[var(--civic-line)] bg-[var(--civic-navy)] text-[var(--civic-sand)]">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-4 px-6 py-4">
          <div>
            <Link to="/admin" className="brand text-2xl text-white">
              CivicFlow
            </Link>
            <p className="text-sm text-[var(--civic-sky)]">Administration</p>
          </div>
          <nav className="flex flex-wrap items-center gap-2 text-sm">
            <NavLink
              to="/admin"
              end
              className={({ isActive }) =>
                `rounded-lg px-3 py-1.5 ${isActive ? 'bg-white/15 font-semibold text-white' : 'text-[var(--civic-sky)] hover:bg-white/10'}`
              }
            >
              Users
            </NavLink>
            <NavLink
              to="/admin/catalog"
              className={({ isActive }) =>
                `rounded-lg px-3 py-1.5 ${isActive ? 'bg-white/15 font-semibold text-white' : 'text-[var(--civic-sky)] hover:bg-white/10'}`
              }
            >
              Catalog
            </NavLink>
            <NavLink
              to="/admin/audit"
              className={({ isActive }) =>
                `rounded-lg px-3 py-1.5 ${isActive ? 'bg-white/15 font-semibold text-white' : 'text-[var(--civic-sky)] hover:bg-white/10'}`
              }
            >
              Audit log
            </NavLink>
            <button
              type="button"
              className="rounded-lg px-3 py-1.5 text-[var(--civic-sky)] hover:bg-white/10"
              onClick={() => void downloadAuthenticatedFile('/api/admin/reports/cases.csv', 'civicflow-cases.csv')}
            >
              Export CSV
            </button>
            <NotificationBell />
            <span className="hidden sm:inline px-2 text-[var(--civic-sky)]">
              {user?.firstName || user?.email} · Admin
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
