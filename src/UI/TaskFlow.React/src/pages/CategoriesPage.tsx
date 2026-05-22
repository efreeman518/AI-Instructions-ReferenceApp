import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Box,
  Button,
  Checkbox,
  IconButton,
  MenuItem,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material'
import { Edit, Plus, Trash2 } from 'lucide-react'
import { useMemo, useState } from 'react'
import { taskFlowApi } from '../api/client'
import { queryKeys } from '../api/queryKeys'
import type { Category } from '../api/types'
import { useNotifications } from '../app/notificationContext'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { PageHeader } from '../components/PageHeader'
import { ErrorState, LoadingState } from '../components/StateViews'

export function CategoriesPage() {
  const queryClient = useQueryClient()
  const { showNotification } = useNotifications()
  const [editing, setEditing] = useState<Category>(() => emptyCategory())
  const [deleteTarget, setDeleteTarget] = useState<Category | null>(null)

  const categoriesQuery = useQuery({
    queryKey: queryKeys.categories(),
    queryFn: ({ signal }) => taskFlowApi.searchCategories({}, 1, 500, signal),
  })

  const categories = useMemo(() => orderCategories(categoriesQuery.data?.items ?? []), [categoriesQuery.data])

  const saveMutation = useMutation({
    mutationFn: (category: Category) =>
      category.id ? taskFlowApi.updateCategory(category) : taskFlowApi.createCategory(category),
    onSuccess: async () => {
      showNotification(editing.id ? 'Category saved.' : 'Category created.', 'success')
      setEditing(emptyCategory())
      await queryClient.invalidateQueries({ queryKey: ['categories'] })
    },
    onError: (error) => showNotification(error instanceof Error ? error.message : 'Category save failed.', 'error'),
  })

  const deleteMutation = useMutation({
    mutationFn: taskFlowApi.deleteCategory,
    onSuccess: async () => {
      showNotification('Category deleted.', 'success')
      setDeleteTarget(null)
      await queryClient.invalidateQueries({ queryKey: ['categories'] })
    },
    onError: (error) => showNotification(error instanceof Error ? error.message : 'Category delete failed.', 'error'),
  })

  function saveCategory() {
    if (!editing.name.trim()) {
      showNotification('Name is required.', 'warning')
      return
    }
    saveMutation.mutate({ ...editing, name: editing.name.trim() })
  }

  return (
    <>
      <PageHeader
        title="Categories"
        actions={
          <Button onClick={() => setEditing(emptyCategory())} startIcon={<Plus size={18} />} variant="contained">
            New Category
          </Button>
        }
      />

      <Box sx={{ display: 'grid', gap: 2.5, gridTemplateColumns: { xs: '1fr', lg: 'minmax(0, 1fr) 360px' } }}>
        <Stack spacing={2}>
          {categoriesQuery.isLoading ? <LoadingState label="Loading categories" /> : null}
          {categoriesQuery.isError ? (
            <ErrorState error={categoriesQuery.error} onRetry={() => void categoriesQuery.refetch()} />
          ) : null}

          {categories.length > 0 ? (
            <Paper variant="outlined" sx={{ overflow: 'hidden' }}>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Name</TableCell>
                    <TableCell>Parent</TableCell>
                    <TableCell>Sort</TableCell>
                    <TableCell>Active</TableCell>
                    <TableCell align="right">Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {categories.map((category) => (
                    <TableRow hover key={category.id ?? category.name}>
                      <TableCell>
                        <Typography sx={{ fontWeight: 650, pl: category.depth * 2 }}>{category.name}</Typography>
                        {category.description ? (
                          <Typography color="text.secondary" variant="caption">
                            {category.description}
                          </Typography>
                        ) : null}
                      </TableCell>
                      <TableCell>{parentName(category.parentCategoryId, categories)}</TableCell>
                      <TableCell>{category.sortOrder}</TableCell>
                      <TableCell>{category.isActive ? 'Yes' : 'No'}</TableCell>
                      <TableCell align="right">
                        <Tooltip title="Edit category">
                          <IconButton aria-label="Edit category" onClick={() => setEditing(stripDepth(category))} size="small">
                            <Edit size={18} />
                          </IconButton>
                        </Tooltip>
                        <Tooltip title="Delete category">
                          <IconButton
                            aria-label="Delete category"
                            color="error"
                            onClick={() => setDeleteTarget(stripDepth(category))}
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
            </Paper>
          ) : null}
        </Stack>

        <Paper variant="outlined" sx={{ alignSelf: 'start', p: 2.5 }}>
          <Typography variant="h2">{editing.id ? 'Edit Category' : 'New Category'}</Typography>
          <Stack spacing={2} sx={{ mt: 2 }}>
            <TextField
              label="Name"
              onChange={(event) => setEditing((current) => ({ ...current, name: event.target.value }))}
              required
              value={editing.name}
            />
            <TextField
              label="Description"
              minRows={3}
              multiline
              onChange={(event) => setEditing((current) => ({ ...current, description: event.target.value }))}
              value={editing.description ?? ''}
            />
            <TextField
              label="Parent"
              onChange={(event) =>
                setEditing((current) => ({ ...current, parentCategoryId: event.target.value || null }))
              }
              select
              value={editing.parentCategoryId ?? ''}
            >
              <MenuItem value="">None</MenuItem>
              {categories
                .filter((category) => category.id !== editing.id)
                .map((category) => (
                  <MenuItem key={category.id ?? category.name} value={category.id ?? ''}>
                    {category.name}
                  </MenuItem>
                ))}
            </TextField>
            <TextField
              label="Sort Order"
              onChange={(event) => setEditing((current) => ({ ...current, sortOrder: Number(event.target.value) }))}
              type="number"
              value={editing.sortOrder}
            />
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
              <Checkbox
                checked={editing.isActive}
                onChange={(event) => setEditing((current) => ({ ...current, isActive: event.target.checked }))}
              />
              <Typography>Active</Typography>
            </Stack>
            <Stack direction="row" spacing={1}>
              <Button disabled={saveMutation.isPending} onClick={saveCategory} variant="contained">
                Save
              </Button>
              <Button onClick={() => setEditing(emptyCategory())}>Reset</Button>
            </Stack>
          </Stack>
        </Paper>
      </Box>

      <ConfirmDialog
        message={`Delete '${deleteTarget?.name ?? 'this category'}'? This cannot be undone.`}
        onCancel={() => setDeleteTarget(null)}
        onConfirm={() => deleteTarget?.id && deleteMutation.mutate(deleteTarget.id)}
        open={deleteTarget !== null}
        title="Delete category"
      />
    </>
  )
}

interface CategoryWithDepth extends Category {
  depth: number
}

function emptyCategory(): Category {
  return {
    description: '',
    isActive: true,
    name: '',
    sortOrder: 0,
  }
}

function orderCategories(categories: Category[]): CategoryWithDepth[] {
  const children = new Map<string | null, Category[]>()
  categories.forEach((category) => {
    const key = category.parentCategoryId ?? null
    children.set(key, [...(children.get(key) ?? []), category])
  })

  const result: CategoryWithDepth[] = []
  const walk = (parentId: string | null, depth: number) => {
    for (const category of [...(children.get(parentId) ?? [])].sort(compareCategories)) {
      result.push({ ...category, depth })
      if (category.id) walk(category.id, depth + 1)
    }
  }

  walk(null, 0)
  return result
}

function compareCategories(left: Category, right: Category) {
  return left.sortOrder - right.sortOrder || left.name.localeCompare(right.name)
}

function parentName(parentId: string | null | undefined, categories: CategoryWithDepth[]) {
  if (!parentId) return '-'
  return categories.find((category) => category.id === parentId)?.name ?? '-'
}

function stripDepth(category: CategoryWithDepth): Category {
  return {
    description: category.description,
    id: category.id,
    isActive: category.isActive,
    name: category.name,
    parentCategoryId: category.parentCategoryId,
    sortOrder: category.sortOrder,
    tenantId: category.tenantId,
  }
}
