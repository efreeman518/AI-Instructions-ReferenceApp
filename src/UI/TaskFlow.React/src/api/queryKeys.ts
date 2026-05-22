export const queryKeys = {
  dashboard: ['dashboard'] as const,
  tasks: (filters: unknown, pageIndex: number, pageSize: number) => ['tasks', filters, pageIndex, pageSize] as const,
  task: (id: string | undefined) => ['task', id] as const,
  categories: (filters?: unknown) => ['categories', filters ?? {}] as const,
  tags: (filters?: unknown) => ['tags', filters ?? {}] as const,
}
