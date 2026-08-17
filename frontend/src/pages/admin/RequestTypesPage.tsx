import { useEffect, useState, type FormEvent } from 'react'
import { apiFetch, ApiError } from '../../api/client'
import type { Department } from './UsersPage'

type RequestType = {
  serviceRequestTypeId: number
  departmentId: number
  name: string
  description?: string | null
  isActive: boolean
}

export function RequestTypesPage() {
  const [departments, setDepartments] = useState<Department[]>([])
  const [types, setTypes] = useState<RequestType[]>([])
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const [deptName, setDeptName] = useState('')
  const [deptDescription, setDeptDescription] = useState('')

  const [typeName, setTypeName] = useState('')
  const [typeDescription, setTypeDescription] = useState('')
  const [typeDepartmentId, setTypeDepartmentId] = useState<number | ''>('')
  const [typeActive, setTypeActive] = useState(true)
  const [editingId, setEditingId] = useState<number | null>(null)

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        const [deptList, typeList] = await Promise.all([
          apiFetch<Department[]>('/api/admin/departments'),
          apiFetch<RequestType[]>('/api/admin/request-types'),
        ])
        if (!cancelled) {
          setDepartments(deptList)
          setTypes(typeList)
          setTypeDepartmentId((current) =>
            current === '' && deptList[0] ? deptList[0].departmentId : current,
          )
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof ApiError ? err.message : 'Unable to load catalog.')
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  async function refresh() {
    const [deptList, typeList] = await Promise.all([
      apiFetch<Department[]>('/api/admin/departments'),
      apiFetch<RequestType[]>('/api/admin/request-types'),
    ])
    setDepartments(deptList)
    setTypes(typeList)
    setTypeDepartmentId((current) =>
      current === '' && deptList[0] ? deptList[0].departmentId : current,
    )
  }

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

  function startEdit(type: RequestType) {
    setEditingId(type.serviceRequestTypeId)
    setTypeName(type.name)
    setTypeDescription(type.description ?? '')
    setTypeDepartmentId(type.departmentId)
    setTypeActive(type.isActive)
  }

  function resetTypeForm() {
    setEditingId(null)
    setTypeName('')
    setTypeDescription('')
    setTypeActive(true)
    setTypeDepartmentId(departments[0]?.departmentId ?? '')
  }

  function departmentLabel(id: number) {
    return departments.find((d) => d.departmentId === id)?.departmentName ?? `Department ${id}`
  }

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-4xl text-[var(--civic-navy)]">Service catalog</h1>
        <p className="mt-1 text-[var(--civic-navy)]/70">
          Departments and request types available to citizens.
        </p>
      </div>

      {error ? <p className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p> : null}

      <div className="grid gap-4 lg:grid-cols-2">
        <form
          className="space-y-3 rounded-2xl border border-[var(--civic-line)] bg-white p-5"
          onSubmit={(e: FormEvent) => {
            e.preventDefault()
            void run(async () => {
              await apiFetch('/api/admin/departments', {
                method: 'POST',
                body: JSON.stringify({
                  departmentName: deptName.trim(),
                  description: deptDescription.trim() || null,
                }),
              })
              setDeptName('')
              setDeptDescription('')
            })
          }}
        >
          <h2 className="text-xl text-[var(--civic-navy)]">Add department</h2>
          <input
            className="w-full rounded-lg border border-[var(--civic-line)] px-3 py-2"
            placeholder="Department name"
            value={deptName}
            onChange={(e) => setDeptName(e.target.value)}
            required
          />
          <input
            className="w-full rounded-lg border border-[var(--civic-line)] px-3 py-2"
            placeholder="Description (optional)"
            value={deptDescription}
            onChange={(e) => setDeptDescription(e.target.value)}
          />
          <button
            type="submit"
            disabled={busy}
            className="rounded-lg bg-[var(--civic-navy)] px-4 py-2 font-semibold text-white disabled:opacity-60"
          >
            Create department
          </button>
        </form>

        <form
          className="space-y-3 rounded-2xl border border-[var(--civic-line)] bg-white p-5"
          onSubmit={(e: FormEvent) => {
            e.preventDefault()
            if (typeDepartmentId === '') {
              return
            }
            void run(async () => {
              const body = {
                departmentId: typeDepartmentId,
                name: typeName.trim(),
                description: typeDescription.trim() || null,
                isActive: typeActive,
              }
              if (editingId == null) {
                await apiFetch('/api/admin/request-types', {
                  method: 'POST',
                  body: JSON.stringify(body),
                })
              } else {
                await apiFetch(`/api/admin/request-types/${editingId}`, {
                  method: 'PUT',
                  body: JSON.stringify(body),
                })
              }
              resetTypeForm()
            })
          }}
        >
          <h2 className="text-xl text-[var(--civic-navy)]">
            {editingId == null ? 'Add request type' : 'Edit request type'}
          </h2>
          <select
            className="w-full rounded-lg border border-[var(--civic-line)] px-3 py-2"
            value={typeDepartmentId}
            onChange={(e) => setTypeDepartmentId(Number(e.target.value))}
            required
          >
            {departments.length === 0 ? <option value="">No departments yet</option> : null}
            {departments.map((d) => (
              <option key={d.departmentId} value={d.departmentId}>
                {d.departmentName}
              </option>
            ))}
          </select>
          <input
            className="w-full rounded-lg border border-[var(--civic-line)] px-3 py-2"
            placeholder="Request type name"
            value={typeName}
            onChange={(e) => setTypeName(e.target.value)}
            required
          />
          <input
            className="w-full rounded-lg border border-[var(--civic-line)] px-3 py-2"
            placeholder="Description (optional)"
            value={typeDescription}
            onChange={(e) => setTypeDescription(e.target.value)}
          />
          <label className="flex items-center gap-2 text-sm text-[var(--civic-navy)]">
            <input
              type="checkbox"
              checked={typeActive}
              onChange={(e) => setTypeActive(e.target.checked)}
            />
            Active (visible in citizen catalog)
          </label>
          <div className="flex flex-wrap gap-2">
            <button
              type="submit"
              disabled={busy || departments.length === 0}
              className="rounded-lg bg-[var(--civic-navy)] px-4 py-2 font-semibold text-white disabled:opacity-60"
            >
              {editingId == null ? 'Create type' : 'Save changes'}
            </button>
            {editingId != null ? (
              <button
                type="button"
                disabled={busy}
                onClick={resetTypeForm}
                className="rounded-lg border border-[var(--civic-line)] px-4 py-2 text-[var(--civic-navy)]"
              >
                Cancel
              </button>
            ) : null}
          </div>
        </form>
      </div>

      <section className="space-y-3">
        <h2 className="text-2xl text-[var(--civic-navy)]">Departments</h2>
        {departments.length === 0 ? (
          <p className="text-sm text-[var(--civic-navy)]/70">No departments yet.</p>
        ) : (
          departments.map((d) => (
            <article
              key={d.departmentId}
              className="rounded-xl border border-[var(--civic-line)] bg-white/80 px-4 py-3"
            >
              <p className="font-semibold text-[var(--civic-navy)]">{d.departmentName}</p>
              {d.description ? (
                <p className="text-sm text-[var(--civic-navy)]/70">{d.description}</p>
              ) : null}
            </article>
          ))
        )}
      </section>

      <section className="space-y-3">
        <h2 className="text-2xl text-[var(--civic-navy)]">Request types</h2>
        {types.length === 0 ? (
          <p className="text-sm text-[var(--civic-navy)]/70">No request types yet.</p>
        ) : (
          types.map((t) => (
            <article
              key={t.serviceRequestTypeId}
              className="flex flex-wrap items-start justify-between gap-3 rounded-xl border border-[var(--civic-line)] bg-white/80 px-4 py-3"
            >
              <div>
                <p className="font-semibold text-[var(--civic-navy)]">{t.name}</p>
                <p className="text-sm text-[var(--civic-navy)]/70">
                  {departmentLabel(t.departmentId)} · {t.isActive ? 'Active' : 'Inactive'}
                </p>
                {t.description ? (
                  <p className="mt-1 text-sm text-[var(--civic-navy)]/80">{t.description}</p>
                ) : null}
              </div>
              <button
                type="button"
                onClick={() => startEdit(t)}
                className="rounded-lg border border-[var(--civic-line)] px-3 py-1.5 text-sm text-[var(--civic-navy)] hover:bg-[var(--civic-sky)]/30"
              >
                Edit
              </button>
            </article>
          ))
        )}
      </section>
    </div>
  )
}
