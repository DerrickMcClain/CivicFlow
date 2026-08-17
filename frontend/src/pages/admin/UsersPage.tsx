import { useEffect, useState } from 'react'
import { apiFetch, ApiError } from '../../api/client'

export type AdminUser = {
  userId: number
  firstName: string
  lastName: string
  email: string
  role: string
  departmentId?: number | null
  departmentName?: string | null
  isActive: boolean
  isEntraUser?: boolean
}

export type Department = {
  departmentId: number
  departmentName: string
  description?: string | null
}

const ROLES = ['Citizen', 'Employee', 'Supervisor', 'Administrator'] as const

export function UsersPage() {
  const [users, setUsers] = useState<AdminUser[]>([])
  const [departments, setDepartments] = useState<Department[]>([])
  const [error, setError] = useState<string | null>(null)
  const [busyId, setBusyId] = useState<number | null>(null)
  const [drafts, setDrafts] = useState<
    Record<number, { role: string; departmentId: string }>
  >({})

  async function load() {
    const [userList, deptList] = await Promise.all([
      apiFetch<AdminUser[]>('/api/admin/users'),
      apiFetch<Department[]>('/api/admin/departments'),
    ])
    setUsers(userList)
    setDepartments(deptList)
    const next: Record<number, { role: string; departmentId: string }> = {}
    for (const u of userList) {
      next[u.userId] = {
        role: u.role,
        departmentId: u.departmentId != null ? String(u.departmentId) : '',
      }
    }
    setDrafts(next)
  }

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        await load()
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof ApiError ? err.message : 'Unable to load users.')
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  async function saveUser(userId: number) {
    const draft = drafts[userId]
    if (!draft) {
      return
    }
    setBusyId(userId)
    setError(null)
    try {
      const updated = await apiFetch<AdminUser>(`/api/admin/users/${userId}/role`, {
        method: 'PUT',
        body: JSON.stringify({
          role: draft.role,
          departmentId: draft.departmentId ? Number(draft.departmentId) : null,
        }),
      })
      setUsers((prev) => prev.map((u) => (u.userId === userId ? updated : u)))
      setDrafts((prev) => ({
        ...prev,
        [userId]: {
          role: updated.role,
          departmentId: updated.departmentId != null ? String(updated.departmentId) : '',
        },
      }))
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to update user.')
    } finally {
      setBusyId(null)
    }
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-4xl text-[var(--civic-navy)]">Users</h1>
        <p className="mt-1 text-[var(--civic-navy)]/70">
          Assign roles and optional department membership.
        </p>
      </div>

      {error ? <p className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p> : null}

      <div className="space-y-3">
        {users.map((user) => {
          const draft = drafts[user.userId] ?? {
            role: user.role,
            departmentId: user.departmentId != null ? String(user.departmentId) : '',
          }
          return (
            <article
              key={user.userId}
              className="rounded-2xl border border-[var(--civic-line)] bg-white/90 px-5 py-4"
            >
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <p className="font-semibold text-[var(--civic-navy)]">
                    {user.firstName} {user.lastName}
                  </p>
                  <p className="text-sm text-[var(--civic-navy)]/70">{user.email}</p>
                  <p className="mt-1 text-xs text-[var(--civic-navy)]/50">
                    {user.isActive ? 'Active' : 'Inactive'}
                    {user.departmentName ? ` · ${user.departmentName}` : ''}
                  </p>
                </div>
                <div className="flex flex-wrap items-end gap-2">
                  {user.isEntraUser ? (
                    <p className="text-sm text-[var(--civic-navy)]/70">Managed in Entra ID</p>
                  ) : (
                    <label className="space-y-1 text-sm">
                      <span className="block text-[var(--civic-navy)]/70">Role</span>
                      <select
                        className="rounded-lg border border-[var(--civic-line)] bg-white px-3 py-2"
                        value={draft.role}
                        onChange={(e) =>
                          setDrafts((prev) => ({
                            ...prev,
                            [user.userId]: { ...draft, role: e.target.value },
                          }))
                        }
                      >
                        {ROLES.map((role) => (
                          <option key={role} value={role}>
                            {role}
                          </option>
                        ))}
                      </select>
                    </label>
                  )}
                  <label className="space-y-1 text-sm">
                    <span className="block text-[var(--civic-navy)]/70">Department</span>
                    <select
                      className="rounded-lg border border-[var(--civic-line)] bg-white px-3 py-2"
                      value={draft.departmentId}
                      onChange={(e) =>
                        setDrafts((prev) => ({
                          ...prev,
                          [user.userId]: { ...draft, departmentId: e.target.value },
                        }))
                      }
                    >
                      <option value="">None</option>
                      {departments.map((d) => (
                        <option key={d.departmentId} value={d.departmentId}>
                          {d.departmentName}
                        </option>
                      ))}
                    </select>
                  </label>
                  {user.isEntraUser ? null : (
                    <button
                      type="button"
                      disabled={busyId === user.userId}
                      onClick={() => void saveUser(user.userId)}
                      className="rounded-lg bg-[var(--civic-navy)] px-4 py-2 font-semibold text-white disabled:opacity-60"
                    >
                      Save
                    </button>
                  )}
                </div>
              </div>
            </article>
          )
        })}
      </div>
    </div>
  )
}
