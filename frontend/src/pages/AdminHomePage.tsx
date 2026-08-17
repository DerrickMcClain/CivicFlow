import { PortalShell } from './PortalShell'

export function AdminHomePage() {
  return (
    <PortalShell title="Administration" subtitle="Users, catalog, and audit history">
      <p className="max-w-2xl text-[var(--civic-navy)]/80">
        Admin UI screens are next. User role, department, request-type, and audit-log
        APIs are already available.
      </p>
    </PortalShell>
  )
}
