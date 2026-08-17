import { PortalShell } from './PortalShell'

export function CitizenHomePage() {
  return (
    <PortalShell title="Citizen portal" subtitle="Submit and track service requests">
      <p className="max-w-2xl text-[var(--civic-navy)]/80">
        Request screens are next. You are signed in as a citizen and ready to create
        Residential Permit cases once the citizen UI lands.
      </p>
    </PortalShell>
  )
}
