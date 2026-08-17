import { useEffect, useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { apiFetch, ApiError } from '../../api/client'
import type { RequestTypeCatalog, ServiceRequestDetail } from '../../api/types'

export function SubmitRequestPage() {
  const navigate = useNavigate()
  const [types, setTypes] = useState<RequestTypeCatalog[]>([])
  const [requestTypeId, setRequestTypeId] = useState<number | ''>('')
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [priority, setPriority] = useState('Medium')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        const data = await apiFetch<RequestTypeCatalog[]>('/api/catalog/request-types')
        if (!cancelled) {
          setTypes(data)
          const residential = data.find((x) => x.name === 'Residential Permit')
          setRequestTypeId(residential?.serviceRequestTypeId ?? data[0]?.serviceRequestTypeId ?? '')
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof ApiError ? err.message : 'Unable to load request types.')
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  async function onSubmit(event: FormEvent) {
    event.preventDefault()
    if (requestTypeId === '') {
      setError('Select a request type.')
      return
    }
    setBusy(true)
    setError(null)
    try {
      const created = await apiFetch<ServiceRequestDetail>('/api/requests', {
        method: 'POST',
        body: JSON.stringify({
          requestTypeId,
          title: title.trim(),
          description: description.trim(),
          priority,
        }),
      })
      navigate(`/citizen/requests/${created.requestId}`, { replace: true })
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to submit request.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <div>
        <h1 className="text-4xl text-[var(--civic-navy)]">Submit a request</h1>
        <p className="mt-1 text-[var(--civic-navy)]/70">
          Start a service case with Planning &amp; Development.
        </p>
      </div>

      <form
        onSubmit={onSubmit}
        className="space-y-4 rounded-2xl border border-[var(--civic-line)] bg-white/80 p-6"
      >
        <label className="block space-y-2">
          <span className="text-sm font-medium text-[var(--civic-navy)]">Request type</span>
          <select
            className="w-full rounded-lg border border-[var(--civic-line)] bg-white px-3 py-2.5"
            value={requestTypeId}
            onChange={(e) => setRequestTypeId(Number(e.target.value))}
            required
          >
            {types.length === 0 ? <option value="">Loading…</option> : null}
            {types.map((type) => (
              <option key={type.serviceRequestTypeId} value={type.serviceRequestTypeId}>
                {type.name} ({type.departmentName})
              </option>
            ))}
          </select>
        </label>

        <label className="block space-y-2">
          <span className="text-sm font-medium text-[var(--civic-navy)]">Title</span>
          <input
            className="w-full rounded-lg border border-[var(--civic-line)] bg-white px-3 py-2.5"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder="e.g. Deck addition permit"
            required
          />
        </label>

        <label className="block space-y-2">
          <span className="text-sm font-medium text-[var(--civic-navy)]">Description</span>
          <textarea
            className="min-h-32 w-full rounded-lg border border-[var(--civic-line)] bg-white px-3 py-2.5"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Describe the work or request."
            required
          />
        </label>

        <label className="block space-y-2">
          <span className="text-sm font-medium text-[var(--civic-navy)]">Priority</span>
          <select
            className="w-full rounded-lg border border-[var(--civic-line)] bg-white px-3 py-2.5"
            value={priority}
            onChange={(e) => setPriority(e.target.value)}
          >
            <option>Low</option>
            <option>Medium</option>
            <option>High</option>
          </select>
        </label>

        {error ? <p className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p> : null}

        <button
          type="submit"
          disabled={busy || types.length === 0}
          className="rounded-lg bg-[var(--civic-navy)] px-4 py-2.5 font-semibold text-white hover:bg-[var(--civic-ink)] disabled:opacity-60"
        >
          {busy ? 'Submitting…' : 'Submit request'}
        </button>
      </form>
    </div>
  )
}
