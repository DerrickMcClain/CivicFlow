import { useEffect, useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { apiFetch, ApiError } from '../../api/client'
import { priorityLabel, type ServiceRequestDetail } from '../../api/types'

export function RequestDetailPage() {
  const { id } = useParams()
  const [detail, setDetail] = useState<ServiceRequestDetail | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [message, setMessage] = useState('')
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      if (!id) {
        return
      }
      try {
        const data = await apiFetch<ServiceRequestDetail>(`/api/requests/${id}`)
        if (!cancelled) {
          setDetail(data)
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof ApiError ? err.message : 'Unable to load request.')
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [id])

  async function onRespond(event: FormEvent) {
    event.preventDefault()
    if (!id) {
      return
    }
    setBusy(true)
    setError(null)
    try {
      const updated = await apiFetch<ServiceRequestDetail>(`/api/requests/${id}/responses`, {
        method: 'POST',
        body: JSON.stringify({ message }),
      })
      setDetail(updated)
      setMessage('')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to send response.')
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
        <Link to="/citizen" className="text-[var(--civic-blue)] underline">
          Back to my requests
        </Link>
      </div>
    )
  }

  const canRespond = detail.status === 'AdditionalInfoRequired'

  return (
    <div className="space-y-8">
      <div>
        <Link to="/citizen" className="text-sm text-[var(--civic-blue)] underline">
          ← My requests
        </Link>
        <h1 className="mt-3 text-4xl text-[var(--civic-navy)]">{detail.title}</h1>
        <p className="mt-1 text-[var(--civic-navy)]/70">
          {detail.requestNumber} · {detail.requestTypeName} · {detail.departmentName}
        </p>
      </div>

      <section className="grid gap-4 rounded-2xl border border-[var(--civic-line)] bg-white/80 p-5 md:grid-cols-3">
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

      {canRespond ? (
        <form
          onSubmit={onRespond}
          className="space-y-3 rounded-2xl border border-[var(--civic-accent)]/40 bg-white p-5"
        >
          <h2 className="text-2xl text-[var(--civic-navy)]">Additional information requested</h2>
          <p className="text-sm text-[var(--civic-navy)]/70">
            Agency staff asked for more details. Your reply returns the case to Under Review.
          </p>
          <textarea
            className="min-h-28 w-full rounded-lg border border-[var(--civic-line)] px-3 py-2.5"
            value={message}
            onChange={(e) => setMessage(e.target.value)}
            required
          />
          <button
            type="submit"
            disabled={busy}
            className="rounded-lg bg-[var(--civic-navy)] px-4 py-2.5 font-semibold text-white disabled:opacity-60"
          >
            {busy ? 'Sending…' : 'Send response'}
          </button>
        </form>
      ) : null}

      {error ? <p className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p> : null}

      <section className="space-y-3">
        <h2 className="text-2xl text-[var(--civic-navy)]">Public notes</h2>
        {detail.notes.length === 0 ? (
          <p className="text-sm text-[var(--civic-navy)]/70">No public notes yet.</p>
        ) : (
          detail.notes.map((note) => (
            <article
              key={note.noteId}
              className="rounded-xl border border-[var(--civic-line)] bg-white/70 px-4 py-3"
            >
              <p className="text-sm text-[var(--civic-navy)]/60">
                {note.authorName} · {new Date(note.createdAt).toLocaleString()}
              </p>
              <p className="mt-1 whitespace-pre-wrap text-[var(--civic-navy)]">{note.noteText}</p>
            </article>
          ))
        )}
      </section>

      <section className="space-y-3">
        <h2 className="text-2xl text-[var(--civic-navy)]">Status timeline</h2>
        <ol className="space-y-3 border-l-2 border-[var(--civic-sky)] pl-4">
          {detail.history.map((entry, index) => (
            <li key={`${entry.changedAt}-${index}`} className="relative">
              <span className="absolute -left-[1.4rem] top-1.5 h-2.5 w-2.5 rounded-full bg-[var(--civic-blue)]" />
              <p className="font-semibold text-[var(--civic-navy)]">
                {entry.oldStatus ? `${entry.oldStatus} → ` : ''}
                {entry.newStatus}
              </p>
              <p className="text-sm text-[var(--civic-navy)]/70">
                {entry.changedByName} · {new Date(entry.changedAt).toLocaleString()}
              </p>
              {entry.reason ? (
                <p className="text-sm text-[var(--civic-navy)]/80">{entry.reason}</p>
              ) : null}
            </li>
          ))}
        </ol>
      </section>
    </div>
  )
}
