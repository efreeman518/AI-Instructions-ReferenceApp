import { createContext, useContext } from 'react'

export type NotificationSeverity = 'success' | 'info' | 'warning' | 'error'

/** Describes notification context value data used by the React UI. */
export interface NotificationContextValue {
  showNotification: (message: string, severity?: NotificationSeverity) => void
}

export const NotificationContext = createContext<NotificationContextValue | undefined>(undefined)

/** Provides use notifications hook behavior for React components. */
export function useNotifications() {
  const context = useContext(NotificationContext)
  if (!context) {
    throw new Error('useNotifications must be used within NotificationProvider.')
  }
  return context
}
