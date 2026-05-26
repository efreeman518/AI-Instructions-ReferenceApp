// Transport types that mirror TaskFlow.Application.Models. Keep these wire names aligned with
// the API envelopes; component-specific view state should live in page components instead.
export const taskStatuses = ['Open', 'InProgress', 'Blocked', 'Completed', 'Cancelled'] as const
export type TaskItemStatus = (typeof taskStatuses)[number]

export const priorities = ['Low', 'Medium', 'High', 'Critical'] as const
export type Priority = (typeof priorities)[number]

export type Id = string

export interface DefaultRequest<T> {
  item: T
}

export interface DefaultResponse<T> {
  item: T | null
}

export interface SearchRequest<TFilter> {
  filter: TFilter
  pageIndex: number
  pageSize: number
}

export interface PagedResponse<T> {
  items?: T[]
  data?: T[]
  totalCount?: number
  total?: number
  pageNumber?: number
  pageIndex?: number
  pageSize?: number
}

export interface PagedResult<T> {
  items: T[]
  total: number
  pageIndex: number
  pageSize: number
}

export interface DefaultSearchFilter {
  searchTerm?: string | null
  tenantId?: Id | null
}

export interface TaskItemSearchFilter extends DefaultSearchFilter {
  status?: TaskItemStatus | null
  priority?: Priority | null
  categoryId?: Id | null
  parentTaskItemId?: Id | null
  dueBefore?: string | null
  dueAfter?: string | null
  isOverdue?: boolean | null
}

export interface CategorySearchFilter extends DefaultSearchFilter {
  isActive?: boolean | null
  parentCategoryId?: Id | null
}

export type TagSearchFilter = DefaultSearchFilter

export interface CommentSearchFilter {
  taskItemId?: Id | null
}

export interface ChecklistItemSearchFilter {
  taskItemId?: Id | null
  isCompleted?: boolean | null
}

export interface AttachmentSearchFilter {
  ownerId?: Id | null
  ownerType?: string | null
}

export interface EntityDto {
  id?: Id | null
}

export interface TaskItem extends EntityDto {
  tenantId?: Id
  title: string
  description?: string | null
  priority: Priority
  status: TaskItemStatus
  features?: string | number | null
  estimatedEffort?: number | null
  actualEffort?: number | null
  completedDate?: string | null
  categoryId?: Id | null
  parentTaskItemId?: Id | null
  startDate?: string | null
  dueDate?: string | null
  recurrenceInterval?: number | null
  recurrenceFrequency?: string | null
  recurrenceEndDate?: string | null
  comments?: Comment[]
  checklistItems?: ChecklistItem[]
  tags?: Tag[]
  attachments?: Attachment[]
  subTasks?: TaskItem[]
  categoryName?: string | null
}

export interface Category extends EntityDto {
  tenantId?: Id
  name: string
  description?: string | null
  sortOrder: number
  isActive: boolean
  parentCategoryId?: Id | null
}

export interface Tag extends EntityDto {
  tenantId?: Id
  name: string
  color?: string | null
}

export interface Comment extends EntityDto {
  tenantId?: Id
  body: string
  taskItemId?: Id | null
  attachments?: Attachment[]
}

export interface ChecklistItem extends EntityDto {
  tenantId?: Id
  title: string
  isCompleted: boolean
  sortOrder: number
  completedDate?: string | null
  taskItemId?: Id | null
}

export interface Attachment extends EntityDto {
  tenantId?: Id
  fileName: string
  contentType: string
  fileSizeBytes: number
  storageUri: string
  ownerType: string
  ownerId: Id
}

export interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  errors?: Record<string, string[]>
  messages?: string[]
}
