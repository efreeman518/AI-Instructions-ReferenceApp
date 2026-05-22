export function formatDate(value?: string | null) {
  if (!value) return '-'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '-'
  return date.toLocaleDateString()
}

export function toDateInputValue(value?: string | null) {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  return date.toISOString().slice(0, 10)
}

export function fromDateInputValue(value: string) {
  return value ? new Date(`${value}T00:00:00`).toISOString() : null
}

export function formatNumber(value?: number | null) {
  return value === null || value === undefined ? '-' : new Intl.NumberFormat().format(value)
}

export function pluralize(count: number, singular: string, plural = `${singular}s`) {
  return `${count} ${count === 1 ? singular : plural}`
}
