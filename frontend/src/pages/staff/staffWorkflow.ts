export const QUEUE_STATUSES = [
  'Submitted',
  'UnderReview',
  'AdditionalInfoRequired',
  'EmployeeRecommendation',
  'SupervisorReview',
  'Approved',
  'Rejected',
] as const

/** Next statuses staff can set via PUT /status (approve/reject use dedicated endpoints). */
export function nextStaffStatuses(current: string, _role: string): string[] {
  switch (current) {
    case 'Submitted':
      return ['UnderReview']
    case 'UnderReview':
      return ['AdditionalInfoRequired', 'EmployeeRecommendation']
    case 'AdditionalInfoRequired':
      return ['UnderReview']
    case 'EmployeeRecommendation':
      return ['SupervisorReview']
    case 'Approved':
    case 'Rejected':
      return ['Completed']
    default:
      return []
  }
}

export function buildQueueQuery(status: string, priority: string): string {
  const params = new URLSearchParams()
  if (status) {
    params.set('status', status)
  }
  if (priority) {
    params.set('priority', priority)
  }
  const q = params.toString()
  return q ? `?${q}` : ''
}

export type StaffAssignee = {
  userId: number
  displayName: string
  role: string
}

export type SupervisorDashboard = {
  openCount: number
  completedCount: number
  agingOverSevenDaysCount: number
}
