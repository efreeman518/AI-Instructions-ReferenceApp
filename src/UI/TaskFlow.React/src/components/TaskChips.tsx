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

export function StatusChip({ status }: { status: TaskItemStatus }) {
  return <Chip color={statusColor[status]} label={statusLabel(status)} size="small" />
}

export function PriorityChip({ priority }: { priority: Priority }) {
  return <Chip color={priorityColor[priority]} label={priority} size="small" variant="outlined" />
}

function statusLabel(status: TaskItemStatus) {
  return status === 'InProgress' ? 'In Progress' : status
}
