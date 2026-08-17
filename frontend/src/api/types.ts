export type Priority = 'Low' | 'Medium' | 'High' | number

export type RequestTypeCatalog = {
  serviceRequestTypeId: number
  name: string
  description?: string | null
  departmentName: string
}

export type ServiceRequestListItem = {
  requestId: number
  requestNumber: string
  title: string
  status: string
  priority: Priority
  createdAt: string
  submittedAt?: string | null
  slaDueAt?: string | null
  isSlaOverdue?: boolean
}

export type NoteItem = {
  noteId: number
  noteText: string
  authorName: string
  createdAt: string
  isInternal: boolean
}

export type StatusHistoryItem = {
  oldStatus?: string | null
  newStatus: string
  reason?: string | null
  changedByName: string
  changedAt: string
}

export type DocumentItem = {
  documentId: number
  fileName: string
  contentType: string
  sizeBytes: number
  uploadedByName: string
  uploadedAt: string
  isInternal: boolean
}

export type ServiceRequestDetail = {
  requestId: number
  requestNumber: string
  title: string
  description: string
  status: string
  priority: Priority
  requestTypeName: string
  departmentName: string
  assignedEmployeeName?: string | null
  createdAt: string
  submittedAt?: string | null
  completedAt?: string | null
  slaDueAt?: string | null
  isSlaOverdue?: boolean
  notes: NoteItem[]
  documents: DocumentItem[]
  history: StatusHistoryItem[]
}

export function priorityLabel(priority: Priority): string {
  if (typeof priority === 'number') {
    return ({ 1: 'Low', 2: 'Medium', 3: 'High' } as const)[priority as 1 | 2 | 3] ?? String(priority)
  }
  return priority
}
