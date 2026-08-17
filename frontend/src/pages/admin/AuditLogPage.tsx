import { useEffect, useState } from 'react'
import { apiFetch, ApiError } from '../../api/client'

type AuditLog = {
  auditLogId: number
  userId?: number | null
  userEmail?: string | null
  action: string
  entityType: string
  entityId: string
  oldValues?: string | null
  newValues?: string | null
  ipAddress?: string | null
  timestamp: string
}

export function AuditLogPage() {
  const [logs, setLogs] = useState<AuditLog[]>([])
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        const data = await apiFetch<AuditLog[]>('/api/admin/audit-logs?take=100')
        if (!cancelled) {
          setLogs(data)
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof ApiError ? err.message : 'Unable to load audit logs.')
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
      <div>
        <h1 className="text-4xl text-[var(--civic-navy)]">Audit log</h1>
        <p className="mt-1 text-[var(--civic-navy)]/70">
          Recent administrative and case actions (latest 100).
        </p>
      </div>

      {loading ? <p className="text-[var(--civic-navy)]/70">Loading…</p> : null}
      {error ? <p className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p> : null}

      {!loading && !error && logs.length === 0 ? (
        <p className="rounded-2xl border border-dashed border-[var(--civic-line)] bg-white/70 px-6 py-8 text-center text-[var(--civic-navy)]">
          No audit entries yet.
        </p>
      ) : null}

      <div className="space-y-3">
        {logs.map((log) => (
          <article
            key={log.auditLogId}
            className="rounded-2xl border border-[var(--civic-line)] bg-white/90 px-5 py-4"
          >
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div>
                <p className="font-semibold text-[var(--civic-navy)]">
                  {log.action} · {log.entityType} #{log.entityId}
                </p>
                <p className="text-sm text-[var(--civic-navy)]/70">
                  {log.userEmail || 'System'}
                  {log.ipAddress ? ` · ${log.ipAddress}` : ''}
                </p>
              </div>
              <p className="text-sm text-[var(--civic-navy)]/60">
                {new Date(log.timestamp).toLocaleString()}
              </p>
            </div>
            {(log.oldValues || log.newValues) && (
              <div className="mt-3 grid gap-2 text-xs text-[var(--civic-navy)]/80 md:grid-cols-2">
                {log.oldValues ? (
                  <pre className="overflow-x-auto rounded-lg bg-[var(--civic-sand)]/60 p-2 whitespace-pre-wrap">
                    {log.oldValues}
                  </pre>
                ) : null}
                {log.newValues ? (
                  <pre className="overflow-x-auto rounded-lg bg-[var(--civic-sky)]/25 p-2 whitespace-pre-wrap">
                    {log.newValues}
                  </pre>
                ) : null}
              </div>
            )}
          </article>
        ))}
      </div>
    </div>
  )
}
