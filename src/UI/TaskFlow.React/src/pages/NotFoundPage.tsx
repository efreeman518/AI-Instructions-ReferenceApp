import { Link as RouterLink } from 'react-router-dom'
import { Button } from '@mui/material'
import { Home } from 'lucide-react'
import { EmptyState } from '../components/StateViews'

export function NotFoundPage() {
  return (
    <EmptyState
      title="Page not found"
      action={
        <Button component={RouterLink} startIcon={<Home size={18} />} to="/" variant="contained">
          Dashboard
        </Button>
      }
    />
  )
}
