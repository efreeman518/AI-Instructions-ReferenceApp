import { Chip } from '@mui/material'
import type { Priority, TaskItemStatus } from '../api/types'

const statusColor: Record<TaskItemStatus, 'default' | 'primary' | 'secondary' | 'success' | 'warning' | 'error'> = {
  Open: 'primary',
  InProgress: 'warning',
  Blocked: 'error',
  Completed: 'success',
  Cancelled: 'default',
}

const priorityColor: Record<Priority, 'default' | 'primary' | 'secondary' | 'success' | 'warning' | 'error'> = {
  Low: 'default',
  Medium: 'primary',
  High: 'warning',
  Critical: 'error',
}

/** Renders the status chip component with consistent TaskFlow UI state. */
export function StatusChip({ status }: { status: TaskItemStatus }) {
  return <Chip color={statusColor[status]} label={statusLabel(status)} size="small" />
}

/** Renders the priority chip component with consistent TaskFlow UI state. */
export function PriorityChip({ priority }: { priority: Priority }) {
  return <Chip color={priorityColor[priority]} label={priority} size="small" variant="outlined" />
}

/** Renders the status label component with consistent TaskFlow UI state. */
function statusLabel(status: TaskItemStatus) {
  return status === 'InProgress' ? 'In Progress' : status
}
