import { Link as RouterLink, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Box,
  Button,
  Checkbox,
  Divider,
  IconButton,
  MenuItem,
  Paper,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material'
import { ArrowLeft, ChevronDown, Plus, Save, Trash2 } from 'lucide-react'
import { useMemo, useState } from 'react'
import { taskFlowApi } from '../api/client'
import { queryKeys } from '../api/queryKeys'
import type { ChecklistItem, Comment, Priority, TaskItem, TaskItemStatus } from '../api/types'
import { priorities, taskStatuses } from '../api/types'
import { useNotifications } from '../app/notificationContext'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { PageHeader } from '../components/PageHeader'
import { ErrorState, LoadingState } from '../components/StateViews'
import { formatDate, fromDateInputValue, toDateInputValue } from '../utils/format'

const emptyGuid = '00000000-0000-0000-0000-000000000000'

export function TaskEditorPage() {
  const { id } = useParams()
  const isCreate = !id

  const taskQuery = useQuery({
    enabled: !isCreate && Boolean(id),
    queryKey: queryKeys.task(id),
    queryFn: ({ signal }) => taskFlowApi.getTask(id!, signal),
  })

  if (taskQuery.isLoading) {
    return (
      <>
        <PageHeader title={isCreate ? 'New Task' : 'Edit Task'} eyebrow="Tasks" />
        <LoadingState label="Loading task" />
      </>
    )
  }

  if (taskQuery.isError) {
    return (
      <>
        <PageHeader title={isCreate ? 'New Task' : 'Edit Task'} eyebrow="Tasks" />
        <ErrorState error={taskQuery.error} onRetry={() => void taskQuery.refetch()} />
      </>
    )
  }

  const initialTask = isCreate ? createEmptyTask() : taskQuery.data ? normalizeTask(taskQuery.data) : null

  return initialTask ? (
    <TaskEditorContent initialTask={initialTask} isCreate={isCreate} routeId={id} />
  ) : null
}

interface TaskEditorContentProps {
  initialTask: TaskItem
  isCreate: boolean
  routeId?: string
}

function TaskEditorContent({ initialTask, isCreate, routeId }: TaskEditorContentProps) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { showNotification } = useNotifications()
  const [form, setForm] = useState<TaskItem>(() => initialTask)
  const [startDate, setStartDate] = useState(() => toDateInputValue(initialTask.startDate))
  const [dueDate, setDueDate] = useState(() => toDateInputValue(initialTask.dueDate))
  const [newChecklistTitle, setNewChecklistTitle] = useState('')
  const [newCommentBody, setNewCommentBody] = useState('')
  const [confirmDelete, setConfirmDelete] = useState(false)

  const categoriesQuery = useQuery({
    queryKey: queryKeys.categories({ isActive: true }),
    queryFn: ({ signal }) => taskFlowApi.searchCategories({ isActive: true }, 1, 200, signal),
  })

  const saveMutation = useMutation({
    mutationFn: (item: TaskItem) => (isCreate ? taskFlowApi.createTask(item) : taskFlowApi.updateTask(item)),
    onSuccess: async (saved) => {
      showNotification(isCreate ? 'Task created.' : 'Task saved.', 'success')
      await queryClient.invalidateQueries({ queryKey: ['tasks'] })
      await queryClient.invalidateQueries({ queryKey: queryKeys.dashboard })
      await queryClient.invalidateQueries({ queryKey: queryKeys.task(saved.id ?? routeId) })
      if (isCreate && saved.id) {
        navigate(`/tasks/${saved.id}`, { replace: true })
      } else {
        setForm(normalizeTask(saved))
        setStartDate(toDateInputValue(saved.startDate))
        setDueDate(toDateInputValue(saved.dueDate))
      }
    },
    onError: (error) => showNotification(error instanceof Error ? error.message : 'Task save failed.', 'error'),
  })

  const deleteMutation = useMutation({
    mutationFn: taskFlowApi.deleteTask,
    onSuccess: async () => {
      showNotification('Task deleted.', 'success')
      await queryClient.invalidateQueries({ queryKey: ['tasks'] })
      await queryClient.invalidateQueries({ queryKey: queryKeys.dashboard })
      navigate('/tasks', { replace: true })
    },
    onError: (error) => showNotification(error instanceof Error ? error.message : 'Task delete failed.', 'error'),
  })

  const categories = useMemo(() => categoriesQuery.data?.items ?? [], [categoriesQuery.data])
  const selectedCategory = useMemo(
    () => categories.find((category) => category.id === form.categoryId),
    [categories, form.categoryId],
  )
  const checklist = form.checklistItems ?? []
  const comments = form.comments ?? []
  const isBusy = saveMutation.isPending || deleteMutation.isPending

  // Checklist and comment edits stay local until Save. The API updater syncs them from
  // the parent TaskItem payload, which keeps create/update as one aggregate save.
  function saveTask() {
    if (!form.title.trim()) {
      showNotification('Title is required.', 'warning')
      return
    }

    saveMutation.mutate({
      ...form,
      checklistItems: checklist.map((item, index) => ({
        ...item,
        sortOrder: index,
        taskItemId: item.taskItemId || form.id || emptyGuid,
      })),
      comments: comments.map((comment) => ({
        ...comment,
        taskItemId: comment.taskItemId || form.id || emptyGuid,
      })),
      dueDate: fromDateInputValue(dueDate),
      startDate: fromDateInputValue(startDate),
      title: form.title.trim(),
    })
  }

  function setField<TKey extends keyof TaskItem>(key: TKey, value: TaskItem[TKey]) {
    setForm((current) => ({ ...current, [key]: value }))
  }

  function addChecklistItem() {
    const title = newChecklistTitle.trim()
    if (!title) return
    setForm((current) => ({
      ...current,
      checklistItems: [
        ...(current.checklistItems ?? []),
        { isCompleted: false, sortOrder: current.checklistItems?.length ?? 0, taskItemId: current.id ?? emptyGuid, title },
      ],
    }))
    setNewChecklistTitle('')
  }

  // New child rows use emptyGuid until the task has a server id. The API layer and server
  // updater replace that placeholder from the parent route/body during persistence.
  function updateChecklistItem(index: number, item: ChecklistItem) {
    setForm((current) => ({
      ...current,
      checklistItems: (current.checklistItems ?? []).map((existing, existingIndex) =>
        existingIndex === index ? item : existing,
      ),
    }))
  }

  function removeChecklistItem(index: number) {
    setForm((current) => ({
      ...current,
      checklistItems: (current.checklistItems ?? []).filter((_, existingIndex) => existingIndex !== index),
    }))
  }

  function addComment() {
    const body = newCommentBody.trim()
    if (!body) return
    setForm((current) => ({
      ...current,
      comments: [{ body, taskItemId: current.id ?? emptyGuid }, ...(current.comments ?? [])],
    }))
    setNewCommentBody('')
  }

  function removeComment(comment: Comment) {
    setForm((current) => ({
      ...current,
      comments: (current.comments ?? []).filter((existing) => existing !== comment),
    }))
  }

  return (
    <>
      <PageHeader
        title={isCreate ? 'New Task' : 'Edit Task'}
        eyebrow="Tasks"
        actions={
          <>
            <Button component={RouterLink} startIcon={<ArrowLeft size={18} />} to="/tasks">
              Back
            </Button>
            {!isCreate ? (
              <Button
                color="error"
                disabled={isBusy}
                onClick={() => setConfirmDelete(true)}
                startIcon={<Trash2 size={18} />}
              >
                Delete
              </Button>
            ) : null}
            <Button disabled={isBusy} onClick={saveTask} startIcon={<Save size={18} />} variant="contained">
              Save
            </Button>
          </>
        }
      />

      <Box sx={{ display: 'grid', gap: 2.5, gridTemplateColumns: { xs: '1fr', lg: 'minmax(0, 1fr) 340px' } }}>
          <Stack spacing={2.5}>
            <Paper variant="outlined" sx={{ p: 2.5 }}>
              <Stack spacing={2}>
                <TextField
                  autoFocus
                  fullWidth
                  label="Title"
                  onChange={(event) => setField('title', event.target.value)}
                  required
                  value={form.title}
                />
                <TextField
                  fullWidth
                  label="Description"
                  minRows={4}
                  multiline
                  onChange={(event) => setField('description', event.target.value)}
                  value={form.description ?? ''}
                />
                <Box
                  sx={{
                    display: 'grid',
                    gap: 2,
                    gridTemplateColumns: { xs: '1fr', md: 'repeat(2, minmax(0, 1fr))' },
                  }}
                >
                  <TextField
                    label="Status"
                    onChange={(event) => setField('status', event.target.value as TaskItemStatus)}
                    select
                    value={form.status}
                  >
                    {taskStatuses.map((status) => (
                      <MenuItem key={status} value={status}>
                        {status === 'InProgress' ? 'In Progress' : status}
                      </MenuItem>
                    ))}
                  </TextField>
                  <TextField
                    label="Priority"
                    onChange={(event) => setField('priority', event.target.value as Priority)}
                    select
                    value={form.priority}
                  >
                    {priorities.map((priority) => (
                      <MenuItem key={priority} value={priority}>
                        {priority}
                      </MenuItem>
                    ))}
                  </TextField>
                  <TextField
                    label="Start Date"
                    onChange={(event) => setStartDate(event.target.value)}
                    slotProps={{ inputLabel: { shrink: true } }}
                    type="date"
                    value={startDate}
                  />
                  <TextField
                    label="Due Date"
                    onChange={(event) => setDueDate(event.target.value)}
                    slotProps={{ inputLabel: { shrink: true } }}
                    type="date"
                    value={dueDate}
                  />
                  <TextField
                    label="Category"
                    onChange={(event) => setField('categoryId', event.target.value || null)}
                    select
                    value={form.categoryId ?? ''}
                  >
                    <MenuItem value="">None</MenuItem>
                    {categories.map((category) => (
                      <MenuItem key={category.id ?? category.name} value={category.id ?? ''}>
                        {category.name}
                      </MenuItem>
                    ))}
                  </TextField>
                  <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: 'repeat(2, minmax(0, 1fr))' }}>
                    <TextField
                      label="Est. Effort"
                      onChange={(event) =>
                        setField('estimatedEffort', event.target.value ? Number(event.target.value) : null)
                      }
                      type="number"
                      value={form.estimatedEffort ?? ''}
                    />
                    <TextField
                      label="Actual Effort"
                      onChange={(event) =>
                        setField('actualEffort', event.target.value ? Number(event.target.value) : null)
                      }
                      type="number"
                      value={form.actualEffort ?? ''}
                    />
                  </Box>
                </Box>
              </Stack>
            </Paper>

            <Accordion defaultExpanded={!isCreate}>
              <AccordionSummary expandIcon={<ChevronDown size={18} />}>
                <Typography variant="h3">Checklist ({checklist.length})</Typography>
              </AccordionSummary>
              <AccordionDetails>
                <Stack spacing={1.5}>
                  <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
                    <TextField
                      fullWidth
                      onChange={(event) => setNewChecklistTitle(event.target.value)}
                      onKeyDown={(event) => {
                        if (event.key === 'Enter') {
                          event.preventDefault()
                          addChecklistItem()
                        }
                      }}
                      placeholder="Add item"
                      value={newChecklistTitle}
                    />
                    <Button onClick={addChecklistItem} startIcon={<Plus size={17} />} variant="contained">
                      Add
                    </Button>
                  </Stack>
                  {checklist.map((item, index) => (
                    <Stack
                      direction="row"
                      key={`${item.id ?? 'new'}-${index}`}
                      spacing={1}
                      sx={{ alignItems: 'center' }}
                    >
                      <Checkbox
                        checked={item.isCompleted}
                        onChange={(event) =>
                          updateChecklistItem(index, {
                            ...item,
                            completedDate: event.target.checked ? new Date().toISOString() : null,
                            isCompleted: event.target.checked,
                          })
                        }
                      />
                      <Typography
                        sx={{
                          flex: 1,
                          textDecoration: item.isCompleted ? 'line-through' : 'none',
                        }}
                      >
                        {item.title}
                      </Typography>
                      <Tooltip title="Remove checklist item">
                        <IconButton aria-label="Remove checklist item" color="error" onClick={() => removeChecklistItem(index)}>
                          <Trash2 size={17} />
                        </IconButton>
                      </Tooltip>
                    </Stack>
                  ))}
                  {checklist.length === 0 ? <Typography color="text.secondary">No checklist items yet.</Typography> : null}
                </Stack>
              </AccordionDetails>
            </Accordion>

            <Accordion defaultExpanded={!isCreate}>
              <AccordionSummary expandIcon={<ChevronDown size={18} />}>
                <Typography variant="h3">Comments ({comments.length})</Typography>
              </AccordionSummary>
              <AccordionDetails>
                <Stack spacing={1.5}>
                  <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
                    <TextField
                      fullWidth
                      minRows={2}
                      multiline
                      onChange={(event) => setNewCommentBody(event.target.value)}
                      placeholder="Add a comment"
                      value={newCommentBody}
                    />
                    <Button onClick={addComment} startIcon={<Plus size={17} />} variant="contained">
                      Add
                    </Button>
                  </Stack>
                  {comments.map((comment, index) => (
                    <Paper key={`${comment.id ?? 'new'}-${index}`} variant="outlined" sx={{ p: 1.5 }}>
                      <Stack direction="row" spacing={1} sx={{ alignItems: 'flex-start' }}>
                        <Typography sx={{ flex: 1, whiteSpace: 'pre-wrap' }}>{comment.body}</Typography>
                        <Tooltip title="Remove comment">
                          <IconButton aria-label="Remove comment" color="error" onClick={() => removeComment(comment)}>
                            <Trash2 size={17} />
                          </IconButton>
                        </Tooltip>
                      </Stack>
                    </Paper>
                  ))}
                  {comments.length === 0 ? <Typography color="text.secondary">No comments yet.</Typography> : null}
                </Stack>
              </AccordionDetails>
            </Accordion>
          </Stack>

          <Paper variant="outlined" sx={{ alignSelf: 'start', p: 2.5 }}>
            <Typography variant="h3">Info</Typography>
            <Divider sx={{ my: 1.5 }} />
            <Stack spacing={1}>
              <InfoRow label="ID" value={form.id ?? '(new)'} />
              <InfoRow label="Category" value={selectedCategory?.name ?? '-'} />
              <InfoRow label="Completed" value={formatDate(form.completedDate)} />
            </Stack>
          </Paper>
      </Box>

      <ConfirmDialog
        message={`Delete '${form.title || 'this task'}'? This cannot be undone.`}
        onCancel={() => setConfirmDelete(false)}
        onConfirm={() => form.id && deleteMutation.mutate(form.id)}
        open={confirmDelete}
        title="Delete task"
      />
    </>
  )
}

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <Box sx={{ display: 'grid', gap: 1, gridTemplateColumns: '96px minmax(0, 1fr)' }}>
      <Typography color="text.secondary">{label}</Typography>
      <Typography sx={{ overflowWrap: 'anywhere' }}>{value}</Typography>
    </Box>
  )
}

function createEmptyTask(): TaskItem {
  return {
    checklistItems: [],
    comments: [],
    description: '',
    priority: 'Medium',
    status: 'Open',
    title: '',
  }
}

function normalizeTask(task: TaskItem): TaskItem {
  return {
    ...task,
    checklistItems: [...(task.checklistItems ?? [])].sort((left, right) => left.sortOrder - right.sortOrder),
    comments: task.comments ?? [],
  }
}
