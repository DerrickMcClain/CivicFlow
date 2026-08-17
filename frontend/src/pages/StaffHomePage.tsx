import { PortalShell } from './PortalShell'

export function StaffHomePage() {
  return (
    <PortalShell title="Staff work queue" subtitle="Review, assign, and advance cases">
      <p className="max-w-2xl text-[var(--civic-navy)]/80">
        Queue and case-detail screens are next. Staff APIs for status, notes,
        assignment, and supervisor approval are already live on the backend.
      </p>
    </PortalShell>
  )
}
