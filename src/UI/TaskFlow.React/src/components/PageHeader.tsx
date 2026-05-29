import { Box, Stack, Typography, type SxProps, type Theme } from '@mui/material'
import type { ReactNode } from 'react'

/** Renders the page header component with consistent TaskFlow UI state. */
export function PageHeader({
  title,
  eyebrow,
  actions,
  sx,
}: {
  title: string
  eyebrow?: string
  actions?: ReactNode
  sx?: SxProps<Theme>
}) {
  return (
    <Box
      sx={{
        alignItems: { xs: 'flex-start', sm: 'center' },
        display: 'flex',
        flexDirection: { xs: 'column', sm: 'row' },
        gap: 2,
        justifyContent: 'space-between',
        mb: 3,
        ...sx,
      }}
    >
      <Box>
        {eyebrow ? (
          <Typography color="text.secondary" sx={{ fontWeight: 650 }} variant="body2">
            {eyebrow}
          </Typography>
        ) : null}
        <Typography component="h1" variant="h1">
          {title}
        </Typography>
      </Box>
      {actions ? (
        <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
          {actions}
        </Stack>
      ) : null}
    </Box>
  )
}
