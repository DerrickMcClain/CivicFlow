import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { apiFetch, ApiError } from '../../api/client'
import { priorityLabel, type ServiceRequestListItem } from '../../api/types'
import { buildQueueQuery, QUEUE_STATUSES } from './staffWorkflow'

export function QueuePage() {
  const [status, setStatus] = useState('')
  const [priority, setPriority] = useState('')
  const [items, setItems] = useState<ServiceRequestListItem[]>([])
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      setLoading(true)
      setError(null)
      try {
        const data = await apiFetch<ServiceRequestListItem[]>(
          `/api/employee/requests${buildQueueQuery(status, priority)}`,
        )
        if (!cancelled) {
          setItems(data)
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof ApiError ? err.message : 'Unable to load queue.')
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
  }, [status, priority])

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-4xl text-[var(--civic-navy)]">Work queue</h1>
        <p className="mt-1 text-[var(--civic-navy)]/70">
          Open service requests awaiting agency action.
        </p>
      </div>

      <div className="flex flex-wrap gap-3 rounded-2xl border border-[var(--civic-line)] bg-white/80 p-4">
        <label className="space-y-1 text-sm">
          <span className="block text-[var(--civic-navy)]/70">Status</span>
          <select
            className="rounded-lg border border-[var(--civic-line)] bg-white px-3 py-2"
            value={status}
            onChange={(e) => setStatus(e.target.value)}
          >
            <option value="">All open</option>
            {QUEUE_STATUSES.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>
        </label>
        <label className="space-y-1 text-sm">
          <span className="block text-[var(--civic-navy)]/70">Priority</span>
          <select
            className="rounded-lg border border-[var(--civic-line)] bg-white px-3 py-2"
            value={priority}
            onChange={(e) => setPriority(e.target.value)}
          >
            <option value="">Any</option>
            <option value="Low">Low</option>
            <option value="Medium">Medium</option>
            <option value="High">High</option>
          </select>
        </label>
      </div>

      {loading ? <p className="text-[var(--civic-navy)]/70">Loading…</p> : null}
      {error ? <p className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p> : null}

      {!loading && !error && items.length === 0 ? (
        <p className="rounded-2xl border border-dashed border-[var(--civic-line)] bg-white/70 px-6 py-8 text-center text-[var(--civic-navy)]">
          No open requests match these filters.
        </p>
      ) : null}

      <div className="space-y-3">
        {items.map((item) => (
          <Link
            key={item.requestId}
            to={`/staff/requests/${item.requestId}`}
            className="block rounded-2xl border border-[var(--civic-line)] bg-white/90 px-5 py-4 transition hover:border-[var(--civic-blue)]"
          >
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div>
                <p className="font-semibold text-[var(--civic-navy)]">{item.title}</p>
                <p className="text-sm text-[var(--civic-navy)]/70">{item.requestNumber}</p>
              </div>
              <div className="text-right text-sm">
                <p className="font-medium text-[var(--civic-blue)]">{item.status}</p>
                <p className="text-[var(--civic-navy)]/60">{priorityLabel(item.priority)}</p>
                {item.isSlaOverdue ? (
                  <p className="font-semibold text-red-600">SLA overdue</p>
                ) : item.slaDueAt ? (
                  <p className="text-[var(--civic-navy)]/60">
                    Due {new Date(item.slaDueAt).toLocaleDateString()}
                  </p>
                ) : null}
              </div>
            </div>
          </Link>
        ))}
      </div>
    </div>
  )
}
