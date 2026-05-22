import { Link as RouterLink, useNavigate } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Box,
  Button,
  IconButton,
  MenuItem,
  Paper,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material'
import { CheckCircle2, Edit, Plus, Search, Trash2, XCircle } from 'lucide-react'
import { useMemo, useState } from 'react'
import { taskFlowApi } from '../api/client'
import { queryKeys } from '../api/queryKeys'
import type { Priority, TaskItem, TaskItemSearchFilter, TaskItemStatus } from '../api/types'
import { priorities, taskStatuses } from '../api/types'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { PageHeader } from '../components/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../components/StateViews'
import { PriorityChip, StatusChip } from '../components/TaskChips'
import { useNotifications } from '../app/notificationContext'
import { formatDate } from '../utils/format'

interface TaskFilterState {
  searchTerm: string
  status: TaskItemStatus | ''
  priority: Priority | ''
  categoryId: string
}

const initialFilters: TaskFilterState = {
  categoryId: '',
  priority: '',
  searchTerm: '',
  status: '',
}

export function TasksPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { showNotification } = useNotifications()
  const [draftFilters, setDraftFilters] = useState(initialFilters)
  const [filters, setFilters] = useState(initialFilters)
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = useState(10)
  const [deleteTarget, setDeleteTarget] = useState<TaskItem | null>(null)

  const apiFilters = useMemo<TaskItemSearchFilter>(
    () => ({
      categoryId: filters.categoryId || null,
      priority: filters.priority || null,
      searchTerm: filters.searchTerm.trim() || null,
      status: filters.status || null,
    }),
    [filters],
  )

  const tasksQuery = useQuery({
    queryKey: queryKeys.tasks(apiFilters, page + 1, pageSize),
    queryFn: ({ signal }) => taskFlowApi.searchTasks(apiFilters, page + 1, pageSize, signal),
  })

  const categoriesQuery = useQuery({
    queryKey: queryKeys.categories({ isActive: true }),
    queryFn: ({ signal }) => taskFlowApi.searchCategories({ isActive: true }, 1, 200, signal),
  })

  const categoryNameById = useMemo(() => {
    const map = new Map<string, string>()
    categoriesQuery.data?.items.forEach((category) => {
      if (category.id) map.set(category.id, category.name)
    })
    return map
  }, [categoriesQuery.data])

  const updateMutation = useMutation({
    mutationFn: taskFlowApi.updateTask,
    onSuccess: async (_, task) => {
      showNotification(`Task marked ${task.status}.`, 'success')
      await queryClient.invalidateQueries({ queryKey: ['tasks'] })
      await queryClient.invalidateQueries({ queryKey: queryKeys.dashboard })
    },
    onError: (error) => showNotification(error instanceof Error ? error.message : 'Task update failed.', 'error'),
  })

  const deleteMutation = useMutation({
    mutationFn: taskFlowApi.deleteTask,
    onSuccess: async () => {
      showNotification('Task deleted.', 'success')
      setDeleteTarget(null)
      await queryClient.invalidateQueries({ queryKey: ['tasks'] })
      await queryClient.invalidateQueries({ queryKey: queryKeys.dashboard })
    },
    onError: (error) => showNotification(error instanceof Error ? error.message : 'Task delete failed.', 'error'),
  })

  function applyFilters() {
    setPage(0)
    setFilters(draftFilters)
  }

  function clearFilters() {
    setDraftFilters(initialFilters)
    setFilters(initialFilters)
    setPage(0)
  }

  function toggleStatus(task: TaskItem) {
    const nextStatus: TaskItemStatus = task.status === 'Completed' ? 'Open' : 'Completed'
    updateMutation.mutate({ ...task, status: nextStatus })
  }

  const categories = categoriesQuery.data?.items ?? []
  const tasks = tasksQuery.data?.items ?? []

  return (
    <>
      <PageHeader
        title="Tasks"
        actions={
          <Button component={RouterLink} startIcon={<Plus size={18} />} to="/tasks/new" variant="contained">
            New Task
          </Button>
        }
      />

      <Paper variant="outlined" sx={{ mb: 2.5, p: 2 }}>
        <Box
          component="form"
          onSubmit={(event) => {
            event.preventDefault()
            applyFilters()
          }}
          sx={{
            display: 'grid',
            gap: 1.5,
            gridTemplateColumns: { xs: '1fr', md: '1.4fr 0.8fr 0.8fr 1fr auto' },
          }}
        >
          <TextField
            label="Search"
            onChange={(event) => setDraftFilters((current) => ({ ...current, searchTerm: event.target.value }))}
            placeholder="Title or description"
            value={draftFilters.searchTerm}
          />
          <Select
            displayEmpty
            onChange={(event) =>
              setDraftFilters((current) => ({ ...current, status: event.target.value as TaskItemStatus | '' }))
            }
            value={draftFilters.status}
          >
            <MenuItem value="">All statuses</MenuItem>
            {taskStatuses.map((status) => (
              <MenuItem key={status} value={status}>
                {status === 'InProgress' ? 'In Progress' : status}
              </MenuItem>
            ))}
          </Select>
          <Select
            displayEmpty
            onChange={(event) =>
              setDraftFilters((current) => ({ ...current, priority: event.target.value as Priority | '' }))
            }
            value={draftFilters.priority}
          >
            <MenuItem value="">All priorities</MenuItem>
            {priorities.map((priority) => (
              <MenuItem key={priority} value={priority}>
                {priority}
              </MenuItem>
            ))}
          </Select>
          <Select
            displayEmpty
            onChange={(event) => setDraftFilters((current) => ({ ...current, categoryId: event.target.value }))}
            value={draftFilters.categoryId}
          >
            <MenuItem value="">All categories</MenuItem>
            {categories.map((category) => (
              <MenuItem key={category.id ?? category.name} value={category.id ?? ''}>
                {category.name}
              </MenuItem>
            ))}
          </Select>
          <Stack direction="row" spacing={1}>
            <Button startIcon={<Search size={17} />} type="submit" variant="contained">
              Search
            </Button>
            <Tooltip title="Clear filters">
              <IconButton aria-label="Clear filters" onClick={clearFilters}>
                <XCircle size={19} />
              </IconButton>
            </Tooltip>
          </Stack>
        </Box>
      </Paper>

      {tasksQuery.isLoading ? <LoadingState label="Loading tasks" /> : null}
      {tasksQuery.isError ? <ErrorState error={tasksQuery.error} onRetry={() => void tasksQuery.refetch()} /> : null}

      {tasksQuery.isSuccess && tasks.length === 0 ? (
        <EmptyState
          title="No tasks found"
          action={
            <Button component={RouterLink} startIcon={<Plus size={18} />} to="/tasks/new" variant="contained">
              New Task
            </Button>
          }
        />
      ) : null}

      {tasks.length > 0 ? (
        <Paper variant="outlined" sx={{ overflow: 'hidden' }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Title</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Priority</TableCell>
                <TableCell>Category</TableCell>
                <TableCell>Due</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {tasks.map((task) => (
                <TableRow hover key={task.id ?? task.title}>
                  <TableCell onClick={() => task.id && navigate(`/tasks/${task.id}`)} sx={{ cursor: 'pointer' }}>
                    <Typography sx={{ fontWeight: 650 }}>{task.title}</Typography>
                    {task.description ? (
                      <Typography color="text.secondary" noWrap sx={{ maxWidth: 420 }} variant="caption">
                        {task.description}
                      </Typography>
                    ) : null}
                  </TableCell>
                  <TableCell>
                    <StatusChip status={task.status} />
                  </TableCell>
                  <TableCell>
                    <PriorityChip priority={task.priority} />
                  </TableCell>
                  <TableCell>{task.categoryName ?? categoryLabel(task.categoryId, categoryNameById)}</TableCell>
                  <TableCell>{formatDate(task.dueDate)}</TableCell>
                  <TableCell align="right">
                    <Tooltip title={task.status === 'Completed' ? 'Reopen task' : 'Complete task'}>
                      <IconButton
                        aria-label={task.status === 'Completed' ? 'Reopen task' : 'Complete task'}
                        color={task.status === 'Completed' ? 'success' : 'default'}
                        disabled={updateMutation.isPending}
                        onClick={() => toggleStatus(task)}
                        size="small"
                      >
                        <CheckCircle2 size={18} />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Edit task">
                      <IconButton
                        aria-label="Edit task"
                        component={RouterLink}
                        size="small"
                        to={task.id ? `/tasks/${task.id}` : '/tasks'}
                      >
                        <Edit size={18} />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Delete task">
                      <IconButton
                        aria-label="Delete task"
                        color="error"
                        onClick={() => setDeleteTarget(task)}
                        size="small"
                      >
                        <Trash2 size={18} />
                      </IconButton>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
          <TablePagination
            component="div"
            count={tasksQuery.data?.total ?? 0}
            onPageChange={(_, nextPage) => setPage(nextPage)}
            onRowsPerPageChange={(event) => {
              setPage(0)
              setPageSize(Number(event.target.value))
            }}
            page={page}
            rowsPerPage={pageSize}
            rowsPerPageOptions={[10, 25, 50, 100]}
          />
        </Paper>
      ) : null}

      <ConfirmDialog
        message={`Delete '${deleteTarget?.title ?? 'this task'}'? This cannot be undone.`}
        onCancel={() => setDeleteTarget(null)}
        onConfirm={() => deleteTarget?.id && deleteMutation.mutate(deleteTarget.id)}
        open={deleteTarget !== null}
        title="Delete task"
      />
    </>
  )
}

function categoryLabel(categoryId: string | null | undefined, categories: Map<string, string>) {
  if (!categoryId) return '-'
  return categories.get(categoryId) ?? '-'
}
