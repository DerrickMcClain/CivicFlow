import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { apiFetch, ApiError } from '../api/client'

export type NotificationItem = {
  notificationId: number
  title: string
  message: string
  linkPath?: string | null
  isRead: boolean
  createdAt: string
}

export function NotificationBell() {
  const [open, setOpen] = useState(false)
  const [items, setItems] = useState<NotificationItem[]>([])
  const [error, setError] = useState<string | null>(null)

  async function load() {
    const data = await apiFetch<NotificationItem[]>('/api/notifications')
    setItems(data)
  }

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        const data = await apiFetch<NotificationItem[]>('/api/notifications')
        if (!cancelled) {
          setItems(data)
        }
      } catch {
        // ignore polling errors
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  const unread = items.filter((item) => !item.isRead).length

  async function toggleOpen() {
    const next = !open
    setOpen(next)
    if (next) {
      setError(null)
      try {
        await load()
      } catch (err) {
        setError(err instanceof ApiError ? err.message : 'Unable to load notifications.')
      }
    }
  }

  async function markRead(id: number) {
    await apiFetch(`/api/notifications/${id}/read`, { method: 'PUT' })
    await load()
  }

  return (
    <div className="relative">
      <button
        type="button"
        onClick={() => void toggleOpen()}
        className="relative rounded-lg border border-[var(--civic-line)] px-3 py-1.5 hover:bg-[var(--civic-sky)]/40"
      >
        Alerts
        {unread > 0 ? (
          <span className="absolute -right-1 -top-1 rounded-full bg-[var(--civic-accent)] px-1.5 text-xs font-semibold text-white">
            {unread}
          </span>
        ) : null}
      </button>

      {open ? (
        <div className="absolute right-0 z-20 mt-2 w-80 rounded-xl border border-[var(--civic-line)] bg-white p-3 shadow-lg">
          <div className="mb-2 flex items-center justify-between">
            <p className="font-semibold text-[var(--civic-navy)]">Notifications</p>
            <button
              type="button"
              className="text-xs text-[var(--civic-blue)]"
              onClick={() =>
                void apiFetch('/api/notifications/read-all', { method: 'PUT' }).then(load)
              }
            >
              Mark all read
            </button>
          </div>
          {error ? <p className="mb-2 text-sm text-red-700">{error}</p> : null}
          {items.length === 0 ? (
            <p className="text-sm text-[var(--civic-navy)]/70">No notifications yet.</p>
          ) : (
            <ul className="max-h-72 space-y-2 overflow-y-auto">
              {items.map((item) => (
                <li
                  key={item.notificationId}
                  className={`rounded-lg px-3 py-2 text-sm ${item.isRead ? 'bg-white' : 'bg-[var(--civic-sky)]/30'}`}
                >
                  <p className="font-semibold text-[var(--civic-navy)]">{item.title}</p>
                  <p className="text-[var(--civic-navy)]/80">{item.message}</p>
                  <div className="mt-1 flex gap-2">
                    {item.linkPath ? (
                      <Link
                        to={item.linkPath}
                        className="text-[var(--civic-blue)] underline"
                        onClick={() => void markRead(item.notificationId)}
                      >
                        Open
                      </Link>
                    ) : null}
                    {!item.isRead ? (
                      <button
                        type="button"
                        className="text-[var(--civic-navy)]/70"
                        onClick={() => void markRead(item.notificationId)}
                      >
                        Mark read
                      </button>
                    ) : null}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
      ) : null}
    </div>
  )
}
