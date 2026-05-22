import { Link as RouterLink } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  Box,
  Button,
  Link,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import { ListChecks, Plus } from 'lucide-react'
import { taskFlowApi } from '../api/client'
import { queryKeys } from '../api/queryKeys'
import { PageHeader } from '../components/PageHeader'
import { ErrorState, LoadingState } from '../components/StateViews'
import { PriorityChip, StatusChip } from '../components/TaskChips'
import { formatDate } from '../utils/format'

export function DashboardPage() {
  const dashboardQuery = useQuery({
    queryKey: queryKeys.dashboard,
    queryFn: async ({ signal }) => {
      const [recent, total, open, inProgress, completed, blocked] = await Promise.all([
        taskFlowApi.searchTasks({}, 1, 8, signal),
        taskFlowApi.searchTasks({}, 1, 1, signal),
        taskFlowApi.searchTasks({ status: 'Open' }, 1, 1, signal),
        taskFlowApi.searchTasks({ status: 'InProgress' }, 1, 1, signal),
        taskFlowApi.searchTasks({ status: 'Completed' }, 1, 1, signal),
        taskFlowApi.searchTasks({ status: 'Blocked' }, 1, 1, signal),
      ])

      return { blocked, completed, inProgress, open, recent, total }
    },
  })

  const data = dashboardQuery.data

  return (
    <>
      <PageHeader
        title="Dashboard"
        actions={
          <>
            <Button component={RouterLink} startIcon={<Plus size={18} />} to="/tasks/new" variant="contained">
              New Task
            </Button>
            <Button component={RouterLink} startIcon={<ListChecks size={18} />} to="/tasks">
              Tasks
            </Button>
          </>
        }
      />

      {dashboardQuery.isLoading ? <LoadingState label="Loading dashboard" /> : null}
      {dashboardQuery.isError ? (
        <ErrorState error={dashboardQuery.error} onRetry={() => void dashboardQuery.refetch()} />
      ) : null}

      {data ? (
        <Stack spacing={3}>
          <Box
            sx={{
              display: 'grid',
              gap: 2,
              gridTemplateColumns: {
                xs: '1fr',
                sm: 'repeat(2, minmax(0, 1fr))',
                lg: 'repeat(5, minmax(0, 1fr))',
              },
            }}
          >
            <Metric label="Total" value={data.total.total} />
            <Metric label="Open" tone="info" value={data.open.total} />
            <Metric label="In Progress" tone="warning" value={data.inProgress.total} />
            <Metric label="Blocked" tone="error" value={data.blocked.total} />
            <Metric label="Completed" tone="success" value={data.completed.total} />
          </Box>

          <Paper variant="outlined" sx={{ overflow: 'hidden' }}>
            <Box sx={{ alignItems: 'center', display: 'flex', justifyContent: 'space-between', px: 2.5, py: 2 }}>
              <Typography variant="h2">Recent Tasks</Typography>
              <Link component={RouterLink} sx={{ fontWeight: 650 }} to="/tasks" underline="hover">
                View all
              </Link>
            </Box>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Title</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Priority</TableCell>
                  <TableCell>Due</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {data.recent.items.map((task) => (
                  <TableRow
                    hover
                    key={task.id ?? task.title}
                    component={RouterLink}
                    to={task.id ? `/tasks/${task.id}` : '/tasks'}
                    sx={{ textDecoration: 'none' }}
                  >
                    <TableCell>
                      <Typography sx={{ color: 'text.primary', fontWeight: 650 }}>{task.title}</Typography>
                      {task.categoryName ? (
                        <Typography color="text.secondary" variant="caption">
                          {task.categoryName}
                        </Typography>
                      ) : null}
                    </TableCell>
                    <TableCell>
                      <StatusChip status={task.status} />
                    </TableCell>
                    <TableCell>
                      <PriorityChip priority={task.priority} />
                    </TableCell>
                    <TableCell>{formatDate(task.dueDate)}</TableCell>
                  </TableRow>
                ))}
                {data.recent.items.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={4}>
                      <Typography color="text.secondary" sx={{ py: 4, textAlign: 'center' }}>
                        No tasks found.
                      </Typography>
                    </TableCell>
                  </TableRow>
                ) : null}
              </TableBody>
            </Table>
          </Paper>
        </Stack>
      ) : null}
    </>
  )
}

function Metric({
  label,
  value,
  tone,
}: {
  label: string
  value: number
  tone?: 'info' | 'warning' | 'success' | 'error'
}) {
  const color = tone ? `${tone}.main` : 'text.primary'

  return (
    <Paper
      variant="outlined"
      sx={{
        minHeight: 118,
        p: 2,
      }}
    >
      <Typography color="text.secondary" sx={{ fontWeight: 650 }} variant="body2">
        {label}
      </Typography>
      <Typography sx={{ color, mt: 1 }} variant="h1">
        {value}
      </Typography>
    </Paper>
  )
}
