import { useEffect, useState } from 'react'
import { Link, Navigate } from 'react-router-dom'
import { apiFetch, ApiError } from '../../api/client'
import { useAuth } from '../../auth/AuthContext'
import type { SupervisorDashboard } from './staffWorkflow'

export function SupervisorDashboardPage() {
  const { user } = useAuth()
  const [data, setData] = useState<SupervisorDashboard | null>(null)
  const [error, setError] = useState<string | null>(null)
  const isSupervisor = user?.role === 'Supervisor'

  useEffect(() => {
    if (!isSupervisor) {
      return
    }
    let cancelled = false
    ;(async () => {
      try {
        const dashboard = await apiFetch<SupervisorDashboard>('/api/supervisor/dashboard')
        if (!cancelled) {
          setData(dashboard)
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof ApiError ? err.message : 'Unable to load dashboard.')
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [isSupervisor])

  if (!isSupervisor) {
    return <Navigate to="/staff" replace />
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-4xl text-[var(--civic-navy)]">Supervisor dashboard</h1>
        <p className="mt-1 text-[var(--civic-navy)]/70">
          Snapshot of open workload and aging cases.
        </p>
      </div>

      {error ? <p className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p> : null}

      {data ? (
        <div className="grid gap-4 sm:grid-cols-3">
          <div className="rounded-2xl border border-[var(--civic-line)] bg-white p-5">
            <p className="text-sm text-[var(--civic-navy)]/60">Open cases</p>
            <p className="mt-2 text-4xl font-semibold text-[var(--civic-navy)]">{data.openCount}</p>
          </div>
          <div className="rounded-2xl border border-[var(--civic-line)] bg-white p-5">
            <p className="text-sm text-[var(--civic-navy)]/60">Completed</p>
            <p className="mt-2 text-4xl font-semibold text-[var(--civic-navy)]">
              {data.completedCount}
            </p>
          </div>
          <div className="rounded-2xl border border-[var(--civic-line)] bg-white p-5">
            <p className="text-sm text-[var(--civic-navy)]/60">Aging &gt; 7 days</p>
            <p className="mt-2 text-4xl font-semibold text-[var(--civic-accent)]">
              {data.agingOverSevenDaysCount}
            </p>
          </div>
        </div>
      ) : !error ? (
        <p className="text-[var(--civic-navy)]/70">Loading…</p>
      ) : null}

      <Link to="/staff" className="inline-block text-[var(--civic-blue)] underline">
        Open work queue
      </Link>
    </div>
  )
}
