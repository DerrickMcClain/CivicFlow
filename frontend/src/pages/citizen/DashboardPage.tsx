import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { apiFetch, ApiError } from '../../api/client'
import { priorityLabel, type ServiceRequestListItem } from '../../api/types'

export function DashboardPage() {
  const [items, setItems] = useState<ServiceRequestListItem[]>([])
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        const data = await apiFetch<ServiceRequestListItem[]>('/api/requests/my')
        if (!cancelled) {
          setItems(data)
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof ApiError ? err.message : 'Unable to load requests.')
        }
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-4xl text-[var(--civic-navy)]">My requests</h1>
          <p className="mt-1 text-[var(--civic-navy)]/70">
            Track permits and service cases you have submitted.
          </p>
        </div>
        <Link
          to="/citizen/new"
          className="rounded-lg bg-[var(--civic-navy)] px-4 py-2.5 font-semibold text-white hover:bg-[var(--civic-ink)]"
        >
          New request
        </Link>
      </div>

      {loading ? <p className="text-[var(--civic-navy)]/70">Loading…</p> : null}
      {error ? <p className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p> : null}

      {!loading && !error && items.length === 0 ? (
        <div className="rounded-2xl border border-dashed border-[var(--civic-line)] bg-white/70 px-6 py-10 text-center">
          <p className="text-[var(--civic-navy)]">No requests yet.</p>
          <Link to="/citizen/new" className="mt-3 inline-block text-[var(--civic-blue)] underline">
            Submit a Residential Permit
          </Link>
        </div>
      ) : null}

      <div className="space-y-3">
        {items.map((item) => (
          <Link
            key={item.requestId}
            to={`/citizen/requests/${item.requestId}`}
            className="block rounded-2xl border border-[var(--civic-line)] bg-white/80 px-5 py-4 transition hover:border-[var(--civic-blue)]"
          >
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div>
                <p className="font-semibold text-[var(--civic-navy)]">{item.title}</p>
                <p className="text-sm text-[var(--civic-navy)]/70">{item.requestNumber}</p>
              </div>
              <div className="text-right text-sm">
                <p className="font-medium text-[var(--civic-blue)]">{item.status}</p>
                <p className="text-[var(--civic-navy)]/60">{priorityLabel(item.priority)} priority</p>
                {item.isSlaOverdue ? (
                  <p className="font-semibold text-red-600">SLA overdue</p>
                ) : null}
              </div>
            </div>
          </Link>
        ))}
      </div>
    </div>
  )
}
