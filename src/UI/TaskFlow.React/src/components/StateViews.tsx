import { Alert, Box, Button, CircularProgress, Paper, Stack, Typography } from '@mui/material'
import { RefreshCw } from 'lucide-react'
import type { ReactNode } from 'react'

export function LoadingState({ label = 'Loading' }: { label?: string }) {
  return (
    <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', py: 4 }}>
      <CircularProgress size={22} />
      <Typography color="text.secondary">{label}</Typography>
    </Stack>
  )
}

export function EmptyState({
  title,
  detail,
  action,
}: {
  title: string
  detail?: string
  action?: ReactNode
}) {
  return (
    <Paper
      variant="outlined"
      sx={{
        alignItems: 'center',
        display: 'flex',
        flexDirection: 'column',
        gap: 1,
        justifyContent: 'center',
        minHeight: 180,
        p: 3,
        textAlign: 'center',
      }}
    >
      <Typography variant="h3">{title}</Typography>
      {detail ? (
        <Typography color="text.secondary" sx={{ maxWidth: 460 }}>
          {detail}
        </Typography>
      ) : null}
      {action ? <Box sx={{ mt: 1 }}>{action}</Box> : null}
    </Paper>
  )
}

export function ErrorState({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  const message = error instanceof Error ? error.message : 'Request failed.'
  return (
    <Alert
      severity="error"
      action={
        onRetry ? (
          <Button color="inherit" onClick={onRetry} size="small" startIcon={<RefreshCw size={16} />}>
            Retry
          </Button>
        ) : undefined
      }
    >
      {message}
    </Alert>
  )
}
