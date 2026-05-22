import { useCallback, useMemo, useState, type ReactNode } from 'react'
import { Alert, Snackbar } from '@mui/material'
import { NotificationContext, type NotificationSeverity } from './notificationContext'

interface NotificationState {
  message: string
  severity: NotificationSeverity
}

export function NotificationProvider({ children }: { children: ReactNode }) {
  const [notification, setNotification] = useState<NotificationState | null>(null)

  const showNotification = useCallback((message: string, severity: NotificationSeverity = 'info') => {
    setNotification({ message, severity })
  }, [])

  const value = useMemo(() => ({ showNotification }), [showNotification])

  return (
    <NotificationContext.Provider value={value}>
      {children}
      <Snackbar
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        autoHideDuration={4200}
        open={notification !== null}
        onClose={() => setNotification(null)}
      >
        <Alert
          variant="filled"
          severity={notification?.severity ?? 'info'}
          onClose={() => setNotification(null)}
        >
          {notification?.message}
        </Alert>
      </Snackbar>
    </NotificationContext.Provider>
  )
}
