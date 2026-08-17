import { useEffect, useState, type FormEvent } from 'react'
import { apiFetch, ApiError } from '../../api/client'

type PolicyArticle = {
  policyArticleId: number
  title: string
  summary: string
  body: string
}

export function PolicyHelpPage() {
  const [query, setQuery] = useState('')
  const [results, setResults] = useState<PolicyArticle[]>([])
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    void search('')
  }, [])

  async function search(term: string) {
    setBusy(true)
    setError(null)
    try {
      const params = term.trim() ? `?query=${encodeURIComponent(term.trim())}` : ''
      const data = await apiFetch<PolicyArticle[]>(`/api/assistant/policies${params}`)
      setResults(data)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to search policies.')
    } finally {
      setBusy(false)
    }
  }

  async function onSubmit(event: FormEvent) {
    event.preventDefault()
    await search(query)
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-4xl text-[var(--civic-navy)]">Policy help</h1>
        <p className="mt-1 text-[var(--civic-navy)]/70">
          Search seeded permit guidance. This is keyword search over a curated corpus, not live LLM
          RAG.
        </p>
      </div>

      <form onSubmit={onSubmit} className="flex flex-wrap gap-2">
        <input
          className="min-w-[16rem] flex-1 rounded-lg border border-[var(--civic-line)] px-3 py-2.5"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Search deck, fence, documents…"
        />
        <button
          type="submit"
          disabled={busy}
          className="rounded-lg bg-[var(--civic-navy)] px-4 py-2.5 font-semibold text-white disabled:opacity-60"
        >
          {busy ? 'Searching…' : 'Search'}
        </button>
      </form>

      {error ? <p className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p> : null}

      <div className="space-y-3">
        {results.map((article) => (
          <article
            key={article.policyArticleId}
            className="rounded-2xl border border-[var(--civic-line)] bg-white/80 px-5 py-4"
          >
            <h2 className="text-xl font-semibold text-[var(--civic-navy)]">{article.title}</h2>
            <p className="mt-1 text-sm text-[var(--civic-navy)]/70">{article.summary}</p>
            <p className="mt-3 whitespace-pre-wrap text-[var(--civic-navy)]">{article.body}</p>
          </article>
        ))}
      </div>
    </div>
  )
}
