import {
  Box,
  Divider,
  FormControlLabel,
  Paper,
  Stack,
  Switch,
  Typography,
} from '@mui/material'
import { apiRuntime } from '../api/client'
import { PageHeader } from '../components/PageHeader'
import { useThemeMode } from '../theme/themeModeContext'

/** Renders the settings page and coordinates its data operations. */
export function SettingsPage() {
  const { mode, setMode } = useThemeMode()
  const isDark = mode === 'dark'

  return (
    <>
      <PageHeader title="Settings" />

      <Box sx={{ display: 'grid', gap: 2.5, maxWidth: 820 }}>
        <Paper variant="outlined" sx={{ p: 2.5 }}>
          <Typography variant="h2">Appearance</Typography>
          <Divider sx={{ my: 2 }} />
          <FormControlLabel
            control={<Switch checked={isDark} onChange={(event) => setMode(event.target.checked ? 'dark' : 'light')} />}
            label="Dark mode"
          />
        </Paper>

        <Paper variant="outlined" sx={{ p: 2.5 }}>
          <Typography variant="h2">Runtime</Typography>
          <Divider sx={{ my: 2 }} />
          <Stack spacing={1.25}>
            <RuntimeRow label="API root" value={apiRuntime.apiRoot} />
            <RuntimeRow label="Dev proxy" value={apiRuntime.devProxyTarget || '-'} />
            <RuntimeRow label="Build" value={import.meta.env.MODE} />
          </Stack>
        </Paper>
      </Box>
    </>
  )
}

/** Renders one runtime setting row on the settings page. */
function RuntimeRow({ label, value }: { label: string; value: string }) {
  return (
    <Box sx={{ display: 'grid', gap: 1, gridTemplateColumns: { xs: '1fr', sm: '160px minmax(0, 1fr)' } }}>
      <Typography color="text.secondary">{label}</Typography>
      <Typography sx={{ fontFamily: 'monospace', overflowWrap: 'anywhere' }}>{value}</Typography>
    </Box>
  )
}
