// Transport types that mirror TaskFlow.Application.Models. Keep these wire names aligned with
// the API envelopes; component-specific view state should live in page components instead.
export const taskStatuses = ['Open', 'InProgress', 'Blocked', 'Completed', 'Cancelled'] as const
export type TaskItemStatus = (typeof taskStatuses)[number]

export const priorities = ['Low', 'Medium', 'High', 'Critical'] as const
export type Priority = (typeof priorities)[number]

export type Id = string

/** Describes default request data used by the React UI. */
export interface DefaultRequest<T> {
  item: T
}

/** Describes default response data used by the React UI. */
export interface DefaultResponse<T> {
  item: T | null
}

/** Describes search request data used by the React UI. */
export interface SearchRequest<TFilter> {
  filter: TFilter
  pageIndex: number
  pageSize: number
}

/** Describes paged response data used by the React UI. */
export interface PagedResponse<T> {
  items?: T[]
  data?: T[]
  totalCount?: number
  total?: number
  pageNumber?: number
  pageIndex?: number
  pageSize?: number
}

/** Describes paged result data used by the React UI. */
export interface PagedResult<T> {
  items: T[]
  total: number
  pageIndex: number
  pageSize: number
}

/** Describes default search filter data used by the React UI. */
export interface DefaultSearchFilter {
  searchTerm?: string | null
  tenantId?: Id | null
}

/** Describes task item search filter data used by the React UI. */
export interface TaskItemSearchFilter extends DefaultSearchFilter {
  status?: TaskItemStatus | null
  priority?: Priority | null
  categoryId?: Id | null
  parentTaskItemId?: Id | null
  dueBefore?: string | null
  dueAfter?: string | null
  isOverdue?: boolean | null
}

/** Describes category search filter data used by the React UI. */
export interface CategorySearchFilter extends DefaultSearchFilter {
  isActive?: boolean | null
  parentCategoryId?: Id | null
}

export type TagSearchFilter = DefaultSearchFilter

/** Describes comment search filter data used by the React UI. */
export interface CommentSearchFilter {
  taskItemId?: Id | null
}

/** Describes checklist item search filter data used by the React UI. */
export interface ChecklistItemSearchFilter {
  taskItemId?: Id | null
  isCompleted?: boolean | null
}

/** Describes attachment search filter data used by the React UI. */
export interface AttachmentSearchFilter {
  ownerId?: Id | null
  ownerType?: string | null
}

/** Describes entity DTO data used by the React UI. */
export interface EntityDto {
  id?: Id | null
}

/** Describes task item data used by the React UI. */
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

/** Describes category data used by the React UI. */
export interface Category extends EntityDto {
  tenantId?: Id
  name: string
  description?: string | null
  sortOrder: number
  isActive: boolean
  parentCategoryId?: Id | null
}

/** Describes tag data used by the React UI. */
export interface Tag extends EntityDto {
  tenantId?: Id
  name: string
  color?: string | null
}

/** Describes comment data used by the React UI. */
export interface Comment extends EntityDto {
  tenantId?: Id
  body: string
  taskItemId?: Id | null
  attachments?: Attachment[]
}

/** Describes checklist item data used by the React UI. */
export interface ChecklistItem extends EntityDto {
  tenantId?: Id
  title: string
  isCompleted: boolean
  sortOrder: number
  completedDate?: string | null
  taskItemId?: Id | null
}

/** Describes attachment data used by the React UI. */
export interface Attachment extends EntityDto {
  tenantId?: Id
  fileName: string
  contentType: string
  fileSizeBytes: number
  storageUri: string
  ownerType: string
  ownerId: Id
}

/** Describes problem details data used by the React UI. */
export interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  errors?: Record<string, string[]>
  messages?: string[]
}
