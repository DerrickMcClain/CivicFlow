import { useState, type FormEvent } from 'react'
import { ApiError, apiUpload, downloadDocument } from '../api/client'
import type { DocumentItem } from '../api/types'

type RequestDocumentsProps = {
  requestId: number
  documents: DocumentItem[]
  canUpload: boolean
  allowInternal?: boolean
  onUpdated: () => Promise<void>
}

function formatSize(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`
  }
  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} KB`
  }
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

export function RequestDocumentsSection({
  requestId,
  documents,
  canUpload,
  allowInternal = false,
  onUpdated,
}: RequestDocumentsProps) {
  const [file, setFile] = useState<File | null>(null)
  const [isInternal, setIsInternal] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function onUpload(event: FormEvent) {
    event.preventDefault()
    if (!file) {
      setError('Choose a file to upload.')
      return
    }

    setBusy(true)
    setError(null)
    try {
      const form = new FormData()
      form.append('file', file)
      if (allowInternal) {
        form.append('isInternal', String(isInternal))
      }
      await apiUpload(`/api/requests/${requestId}/documents`, form)
      setFile(null)
      await onUpdated()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to upload document.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <section className="space-y-3">
      <h2 className="text-2xl text-[var(--civic-navy)]">Documents</h2>

      {documents.length === 0 ? (
        <p className="text-sm text-[var(--civic-navy)]/70">No documents attached yet.</p>
      ) : (
        <ul className="space-y-2">
          {documents.map((doc) => (
            <li
              key={doc.documentId}
              className="flex flex-wrap items-center justify-between gap-2 rounded-xl border border-[var(--civic-line)] bg-white/70 px-4 py-3"
            >
              <div>
                <p className="font-medium text-[var(--civic-navy)]">{doc.fileName}</p>
                <p className="text-sm text-[var(--civic-navy)]/70">
                  {doc.uploadedByName} · {new Date(doc.uploadedAt).toLocaleString()} ·{' '}
                  {formatSize(doc.sizeBytes)}
                  {doc.isInternal ? ' · Internal' : ''}
                </p>
              </div>
              <button
                type="button"
                onClick={() => void downloadDocument(requestId, doc.documentId, doc.fileName)}
                className="rounded-lg border border-[var(--civic-line)] px-3 py-1.5 text-sm font-semibold text-[var(--civic-blue)] hover:bg-[var(--civic-sky)]/30"
              >
                Download
              </button>
            </li>
          ))}
        </ul>
      )}

      {canUpload ? (
        <form onSubmit={onUpload} className="space-y-3 rounded-xl border border-[var(--civic-line)] bg-white/80 p-4">
          <label className="block space-y-2 text-sm">
            <span className="font-medium text-[var(--civic-navy)]">Attach a file</span>
            <input
              type="file"
              accept=".pdf,.jpg,.jpeg,.png,.txt,.doc,.docx"
              onChange={(e) => setFile(e.target.files?.[0] ?? null)}
              className="block w-full text-sm"
            />
            <span className="text-[var(--civic-navy)]/60">PDF, images, Word, or text up to 10 MB.</span>
          </label>

          {allowInternal ? (
            <label className="flex items-center gap-2 text-sm text-[var(--civic-navy)]">
              <input
                type="checkbox"
                checked={isInternal}
                onChange={(e) => setIsInternal(e.target.checked)}
              />
              Internal document (hidden from citizens)
            </label>
          ) : null}

          {error ? <p className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p> : null}

          <button
            type="submit"
            disabled={busy || !file}
            className="rounded-lg bg-[var(--civic-navy)] px-4 py-2 font-semibold text-white disabled:opacity-60"
          >
            {busy ? 'Uploading…' : 'Upload document'}
          </button>
        </form>
      ) : null}
    </section>
  )
}
