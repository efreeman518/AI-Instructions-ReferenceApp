import type {
  Category,
  CategorySearchFilter,
  ChecklistItem,
  ChecklistItemSearchFilter,
  Comment,
  CommentSearchFilter,
  DefaultResponse,
  Id,
  PagedResponse,
  PagedResult,
  ProblemDetails,
  SearchRequest,
  Tag,
  TagSearchFilter,
  TaskItem,
  TaskItemSearchFilter,
} from './types'

const apiVersionRoot = '/api/v1'
const configuredBaseUrl = (import.meta.env.PROD ? import.meta.env.VITE_API_BASE_URL : '')?.replace(/\/$/, '') ?? ''

/**
 * Error shape used by React Query callers. It preserves ProblemDetails when the API returns
 * RFC 7807 payloads while still supporting plain text or empty error bodies.
 */
export class ApiError extends Error {
  status: number
  details?: ProblemDetails

  constructor(message: string, status: number, details?: ProblemDetails) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.details = details
  }
}

async function requestJson<T>(path: string, init?: RequestInit): Promise<T> {
  // Production calls the Aspire-provided gateway URL. Development leaves the origin empty so
  // Vite proxying can route API traffic without baking a dynamic port into the bundle.
  const response = await fetch(`${configuredBaseUrl}${path}`, {
    ...init,
    credentials: 'include',
    headers: {
      Accept: 'application/json',
      ...(init?.body ? { 'Content-Type': 'application/json' } : {}),
      ...init?.headers,
    },
  })

  if (response.status === 204) {
    return undefined as T
  }

  const payload = await readJson(response)

  if (!response.ok) {
    const problem = payload as ProblemDetails | undefined
    throw new ApiError(problemMessage(problem, response), response.status, problem)
  }

  return payload as T
}

async function readJson(response: Response): Promise<unknown> {
  const text = await response.text()
  if (!text) {
    return undefined
  }

  try {
    return JSON.parse(text)
  } catch {
    return text
  }
}

function problemMessage(problem: ProblemDetails | undefined, response: Response): string {
  if (problem?.detail) return problem.detail
  if (problem?.title) return problem.title
  if (problem?.messages?.length) return problem.messages.join(', ')
  if (problem?.errors) {
    const first = Object.values(problem.errors).flat()[0]
    if (first) return first
  }
  return `Request failed with ${response.status} ${response.statusText}`
}

function post<TResponse, TBody>(path: string, body: TBody, signal?: AbortSignal): Promise<TResponse> {
  return requestJson<TResponse>(path, {
    method: 'POST',
    body: JSON.stringify(body),
    signal,
  })
}

function put<TResponse, TBody>(path: string, body: TBody, signal?: AbortSignal): Promise<TResponse> {
  return requestJson<TResponse>(path, {
    method: 'PUT',
    body: JSON.stringify(body),
    signal,
  })
}

function del(path: string, signal?: AbortSignal): Promise<void> {
  return requestJson<void>(path, { method: 'DELETE', signal })
}

// The API has used both data/total/pageIndex and items/totalCount/pageNumber names.
// Normalize both shapes so page components do not encode transport compatibility logic.
function normalizePaged<T>(response: PagedResponse<T>): PagedResult<T> {
  return {
    items: response.data ?? response.items ?? [],
    total: response.total ?? response.totalCount ?? 0,
    pageIndex: response.pageIndex ?? response.pageNumber ?? 1,
    pageSize: response.pageSize ?? 25,
  }
}

// The server treats pageIndex as 1-based. Keep that convention here even though MUI's
// TablePagination uses 0-based page indexes at the component boundary.
function searchRequest<TFilter>(filter: TFilter, pageIndex = 1, pageSize = 25): SearchRequest<TFilter> {
  return { filter, pageIndex, pageSize }
}

async function search<TItem, TFilter>(
  resource: string,
  filter: TFilter,
  pageIndex?: number,
  pageSize?: number,
  signal?: AbortSignal,
): Promise<PagedResult<TItem>> {
  const response = await post<PagedResponse<TItem>, SearchRequest<TFilter>>(
    `${apiVersionRoot}/${resource}/search`,
    searchRequest(filter, pageIndex, pageSize),
    signal,
  )
  return normalizePaged(response)
}

// Minimal API write endpoints return DefaultResponse<T>. Treat null item as a contract
// failure because callers need a concrete entity for cache invalidation and navigation.
function unwrap<T>(response: DefaultResponse<T>): T {
  if (!response.item) {
    throw new ApiError('The API returned an empty response.', 500)
  }
  return response.item
}

export const taskFlowApi = {
  searchTasks: (filter: TaskItemSearchFilter = {}, pageIndex?: number, pageSize?: number, signal?: AbortSignal) =>
    search<TaskItem, TaskItemSearchFilter>('task-items', filter, pageIndex, pageSize, signal),

  getTask: async (id: Id, signal?: AbortSignal) =>
    unwrap(await requestJson<DefaultResponse<TaskItem>>(`${apiVersionRoot}/task-items/${id}`, { signal })),

  createTask: async (item: TaskItem) =>
    unwrap(await post<DefaultResponse<TaskItem>, { item: TaskItem }>(`${apiVersionRoot}/task-items`, { item })),

  updateTask: async (item: TaskItem) => {
    if (!item.id) throw new ApiError('Cannot update a task without an id.', 400)
    return unwrap(
      await put<DefaultResponse<TaskItem>, { item: TaskItem }>(`${apiVersionRoot}/task-items/${item.id}`, { item }),
    )
  },

  deleteTask: (id: Id) => del(`${apiVersionRoot}/task-items/${id}`),

  searchCategories: (
    filter: CategorySearchFilter = {},
    pageIndex?: number,
    pageSize?: number,
    signal?: AbortSignal,
  ) => search<Category, CategorySearchFilter>('categories', filter, pageIndex, pageSize, signal),

  createCategory: async (item: Category) =>
    unwrap(await post<DefaultResponse<Category>, { item: Category }>(`${apiVersionRoot}/categories`, { item })),

  updateCategory: async (item: Category) => {
    if (!item.id) throw new ApiError('Cannot update a category without an id.', 400)
    return unwrap(
      await put<DefaultResponse<Category>, { item: Category }>(`${apiVersionRoot}/categories/${item.id}`, { item }),
    )
  },

  deleteCategory: (id: Id) => del(`${apiVersionRoot}/categories/${id}`),

  searchTags: (filter: TagSearchFilter = {}, pageIndex?: number, pageSize?: number, signal?: AbortSignal) =>
    search<Tag, TagSearchFilter>('tags', filter, pageIndex, pageSize, signal),

  createTag: async (item: Tag) =>
    unwrap(await post<DefaultResponse<Tag>, { item: Tag }>(`${apiVersionRoot}/tags`, { item })),

  updateTag: async (item: Tag) => {
    if (!item.id) throw new ApiError('Cannot update a tag without an id.', 400)
    return unwrap(await put<DefaultResponse<Tag>, { item: Tag }>(`${apiVersionRoot}/tags/${item.id}`, { item }))
  },

  deleteTag: (id: Id) => del(`${apiVersionRoot}/tags/${id}`),

  searchComments: (filter: CommentSearchFilter, pageIndex?: number, pageSize?: number, signal?: AbortSignal) =>
    search<Comment, CommentSearchFilter>('comments', filter, pageIndex, pageSize, signal),

  createComment: async (item: Comment) =>
    unwrap(await post<DefaultResponse<Comment>, { item: Comment }>(`${apiVersionRoot}/comments`, { item })),

  deleteComment: (id: Id) => del(`${apiVersionRoot}/comments/${id}`),

  searchChecklistItems: (
    filter: ChecklistItemSearchFilter,
    pageIndex?: number,
    pageSize?: number,
    signal?: AbortSignal,
  ) => search<ChecklistItem, ChecklistItemSearchFilter>('checklist-items', filter, pageIndex, pageSize, signal),

  createChecklistItem: async (item: ChecklistItem) =>
    unwrap(
      await post<DefaultResponse<ChecklistItem>, { item: ChecklistItem }>(`${apiVersionRoot}/checklist-items`, {
        item,
      }),
    ),

  updateChecklistItem: async (item: ChecklistItem) => {
    if (!item.id) throw new ApiError('Cannot update a checklist item without an id.', 400)
    return unwrap(
      await put<DefaultResponse<ChecklistItem>, { item: ChecklistItem }>(
        `${apiVersionRoot}/checklist-items/${item.id}`,
        { item },
      ),
    )
  },

  deleteChecklistItem: (id: Id) => del(`${apiVersionRoot}/checklist-items/${id}`),
}

export const apiRuntime = {
  apiRoot: `${configuredBaseUrl}${apiVersionRoot}`,
  devProxyTarget: import.meta.env.DEV ? import.meta.env.VITE_API_BASE_URL : undefined,
}
