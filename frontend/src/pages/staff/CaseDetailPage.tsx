import { useEffect, useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { apiFetch, ApiError } from '../../api/client'
import { priorityLabel, type ServiceRequestDetail } from '../../api/types'
import { RequestDocumentsSection } from '../../components/RequestDocumentsSection'
import { useAuth } from '../../auth/AuthContext'
import { nextStaffStatuses, type StaffAssignee } from './staffWorkflow'

export function CaseDetailPage() {
  const { id } = useParams()
  const { user } = useAuth()
  const role = user?.role ?? 'Employee'

  const [detail, setDetail] = useState<ServiceRequestDetail | null>(null)
  const [assignees, setAssignees] = useState<StaffAssignee[]>([])
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const [nextStatus, setNextStatus] = useState('')
  const [statusReason, setStatusReason] = useState('')
  const [noteText, setNoteText] = useState('')
  const [isInternal, setIsInternal] = useState(true)
  const [assigneeId, setAssigneeId] = useState<number | ''>('')
  const [assignReason, setAssignReason] = useState('')
  const [rejectReason, setRejectReason] = useState('')

  async function refresh() {
    if (!id) {
      return
    }
    const data = await apiFetch<ServiceRequestDetail>(`/api/requests/${id}`)
    setDetail(data)
    const options = nextStaffStatuses(data.status, role)
    setNextStatus(options[0] ?? '')
  }

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        const [caseDetail, staff] = await Promise.all([
          apiFetch<ServiceRequestDetail>(`/api/requests/${id}`),
          apiFetch<StaffAssignee[]>('/api/employee/requests/assignees'),
        ])
        if (!cancelled) {
          setDetail(caseDetail)
          setAssignees(staff)
          const options = nextStaffStatuses(caseDetail.status, role)
          setNextStatus(options[0] ?? '')
          setAssigneeId(staff[0]?.userId ?? '')
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof ApiError ? err.message : 'Unable to load case.')
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [id, role])

  async function run(action: () => Promise<void>) {
    setBusy(true)
    setError(null)
    try {
      await action()
      await refresh()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Action failed.')
    } finally {
      setBusy(false)
    }
  }

  if (!detail && !error) {
    return <p className="text-[var(--civic-navy)]/70">Loading…</p>
  }

  if (!detail) {
    return (
      <div className="space-y-3">
        <p className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>
        <Link to="/staff" className="text-[var(--civic-blue)] underline">
          Back to queue
        </Link>
      </div>
    )
  }

  const transitions = nextStaffStatuses(detail.status, role)
  const canDecide = role === 'Supervisor' && detail.status === 'SupervisorReview'
  const canUpload =
    detail.status !== 'Completed' &&
    detail.status !== 'Cancelled' &&
    detail.status !== 'Rejected'

  return (
    <div className="space-y-8">
      <div>
        <Link to="/staff" className="text-sm text-[var(--civic-blue)] underline">
          ← Work queue
        </Link>
        <h1 className="mt-3 text-4xl text-[var(--civic-navy)]">{detail.title}</h1>
        <p className="mt-1 text-[var(--civic-navy)]/70">
          {detail.requestNumber} · {detail.requestTypeName} · {detail.departmentName}
        </p>
      </div>

      <section className="grid gap-4 rounded-2xl border border-[var(--civic-line)] bg-white/90 p-5 md:grid-cols-3">
        <div>
          <p className="text-xs uppercase tracking-wide text-[var(--civic-navy)]/60">Status</p>
          <p className="font-semibold text-[var(--civic-blue)]">{detail.status}</p>
        </div>
        <div>
          <p className="text-xs uppercase tracking-wide text-[var(--civic-navy)]/60">Priority</p>
          <p className="font-semibold text-[var(--civic-navy)]">{priorityLabel(detail.priority)}</p>
        </div>
        <div>
          <p className="text-xs uppercase tracking-wide text-[var(--civic-navy)]/60">Assignee</p>
          <p className="font-semibold text-[var(--civic-navy)]">
            {detail.assignedEmployeeName || 'Unassigned'}
          </p>
        </div>
        <div className="md:col-span-3">
          <p className="text-xs uppercase tracking-wide text-[var(--civic-navy)]/60">Description</p>
          <p className="mt-1 whitespace-pre-wrap text-[var(--civic-navy)]">{detail.description}</p>
        </div>
      </section>

      {error ? <p className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p> : null}

      <div className="grid gap-4 lg:grid-cols-2">
        {transitions.length > 0 ? (
          <form
            className="space-y-3 rounded-2xl border border-[var(--civic-line)] bg-white p-5"
            onSubmit={(e: FormEvent) => {
              e.preventDefault()
              void run(async () => {
                await apiFetch(`/api/requests/${id}/status`, {
                  method: 'PUT',
                  body: JSON.stringify({ status: nextStatus, reason: statusReason || null }),
                })
                setStatusReason('')
              })
            }}
          >
            <h2 className="text-xl text-[var(--civic-navy)]">Change status</h2>
            <select
              className="w-full rounded-lg border border-[var(--civic-line)] px-3 py-2"
              value={nextStatus}
              onChange={(e) => setNextStatus(e.target.value)}
            >
              {transitions.map((s) => (
                <option key={s} value={s}>
                  {s}
                </option>
              ))}
            </select>
            <input
              className="w-full rounded-lg border border-[var(--civic-line)] px-3 py-2"
              placeholder="Reason (optional)"
              value={statusReason}
              onChange={(e) => setStatusReason(e.target.value)}
            />
            <button
              type="submit"
              disabled={busy}
              className="rounded-lg bg-[var(--civic-navy)] px-4 py-2 font-semibold text-white disabled:opacity-60"
            >
              Update status
            </button>
          </form>
        ) : null}

        {canDecide ? (
          <div className="space-y-3 rounded-2xl border border-[var(--civic-accent)]/40 bg-white p-5">
            <h2 className="text-xl text-[var(--civic-navy)]">Supervisor decision</h2>
            <button
              type="button"
              disabled={busy}
              className="mr-2 rounded-lg bg-emerald-700 px-4 py-2 font-semibold text-white disabled:opacity-60"
              onClick={() =>
                void run(async () => {
                  await apiFetch(`/api/requests/${id}/approve`, {
                    method: 'POST',
                    body: JSON.stringify({ reason: 'Approved' }),
                  })
                })
              }
            >
              Approve
            </button>
            <div className="space-y-2">
              <input
                className="w-full rounded-lg border border-[var(--civic-line)] px-3 py-2"
                placeholder="Reject reason (required)"
                value={rejectReason}
                onChange={(e) => setRejectReason(e.target.value)}
              />
              <button
                type="button"
                disabled={busy || !rejectReason.trim()}
                className="rounded-lg bg-red-700 px-4 py-2 font-semibold text-white disabled:opacity-60"
                onClick={() =>
                  void run(async () => {
                    await apiFetch(`/api/requests/${id}/reject`, {
                      method: 'POST',
                      body: JSON.stringify({ reason: rejectReason.trim() }),
                    })
                    setRejectReason('')
                  })
                }
              >
                Reject
              </button>
            </div>
          </div>
        ) : null}

        <form
          className="space-y-3 rounded-2xl border border-[var(--civic-line)] bg-white p-5"
          onSubmit={(e: FormEvent) => {
            e.preventDefault()
            void run(async () => {
              await apiFetch(`/api/requests/${id}/notes`, {
                method: 'POST',
                body: JSON.stringify({ noteText, isInternal }),
              })
              setNoteText('')
            })
          }}
        >
          <h2 className="text-xl text-[var(--civic-navy)]">Add note</h2>
          <textarea
            className="min-h-24 w-full rounded-lg border border-[var(--civic-line)] px-3 py-2"
            value={noteText}
            onChange={(e) => setNoteText(e.target.value)}
            required
          />
          <label className="flex items-center gap-2 text-sm text-[var(--civic-navy)]">
            <input
              type="checkbox"
              checked={isInternal}
              onChange={(e) => setIsInternal(e.target.checked)}
            />
            Internal note (hidden from citizen)
          </label>
          <button
            type="submit"
            disabled={busy}
            className="rounded-lg bg-[var(--civic-navy)] px-4 py-2 font-semibold text-white disabled:opacity-60"
          >
            Save note
          </button>
        </form>

        <form
          className="space-y-3 rounded-2xl border border-[var(--civic-line)] bg-white p-5"
          onSubmit={(e: FormEvent) => {
            e.preventDefault()
            if (assigneeId === '') {
              return
            }
            void run(async () => {
              await apiFetch(`/api/requests/${id}/assignment`, {
                method: 'PUT',
                body: JSON.stringify({
                  assignedToUserId: assigneeId,
                  reason: assignReason || null,
                }),
              })
              setAssignReason('')
            })
          }}
        >
          <h2 className="text-xl text-[var(--civic-navy)]">Assign case</h2>
          <select
            className="w-full rounded-lg border border-[var(--civic-line)] px-3 py-2"
            value={assigneeId}
            onChange={(e) => setAssigneeId(Number(e.target.value))}
          >
            {assignees.map((a) => (
              <option key={a.userId} value={a.userId}>
                {a.displayName} ({a.role})
              </option>
            ))}
          </select>
          <input
            className="w-full rounded-lg border border-[var(--civic-line)] px-3 py-2"
            placeholder="Reason (optional)"
            value={assignReason}
            onChange={(e) => setAssignReason(e.target.value)}
          />
          <button
            type="submit"
            disabled={busy || assignees.length === 0}
            className="rounded-lg bg-[var(--civic-navy)] px-4 py-2 font-semibold text-white disabled:opacity-60"
          >
            Assign
          </button>
        </form>
      </div>

      <RequestDocumentsSection
        requestId={detail.requestId}
        documents={detail.documents}
        canUpload={canUpload}
        allowInternal
        onUpdated={refresh}
      />

      <section className="space-y-3">
        <h2 className="text-2xl text-[var(--civic-navy)]">Notes</h2>
        {detail.notes.length === 0 ? (
          <p className="text-sm text-[var(--civic-navy)]/70">No notes yet.</p>
        ) : (
          detail.notes.map((note) => (
            <article
              key={note.noteId}
              className="rounded-xl border border-[var(--civic-line)] bg-white/80 px-4 py-3"
            >
              <p className="text-sm text-[var(--civic-navy)]/60">
                {note.authorName} · {new Date(note.createdAt).toLocaleString()}
                {note.isInternal ? ' · Internal' : ' · Public'}
              </p>
              <p className="mt-1 whitespace-pre-wrap text-[var(--civic-navy)]">{note.noteText}</p>
            </article>
          ))
        )}
      </section>

      <section className="space-y-3">
        <h2 className="text-2xl text-[var(--civic-navy)]">Status history</h2>
        <ol className="space-y-3 border-l-2 border-[var(--civic-sky)] pl-4">
          {detail.history.map((entry, index) => (
            <li key={`${entry.changedAt}-${index}`}>
              <p className="font-semibold text-[var(--civic-navy)]">
                {entry.oldStatus ? `${entry.oldStatus} → ` : ''}
                {entry.newStatus}
              </p>
              <p className="text-sm text-[var(--civic-navy)]/70">
                {entry.changedByName} · {new Date(entry.changedAt).toLocaleString()}
              </p>
              {entry.reason ? <p className="text-sm">{entry.reason}</p> : null}
            </li>
          ))}
        </ol>
      </section>
    </div>
  )
}
