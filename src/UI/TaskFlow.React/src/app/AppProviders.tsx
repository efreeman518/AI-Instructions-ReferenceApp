import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { type ReactNode, useState } from 'react'
import { BrowserRouter } from 'react-router-dom'
import { NotificationProvider } from './NotificationProvider'
import { ThemeModeProvider } from '../theme/ThemeModeProvider'

/** Provides app providers application shell or context behavior for React children. */
export function AppProviders({ children }: { children: ReactNode }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            refetchOnWindowFocus: false,
            retry: 1,
            staleTime: 20_000,
          },
        },
      }),
  )

  return (
    <QueryClientProvider client={queryClient}>
      <ThemeModeProvider>
        <NotificationProvider>
          <BrowserRouter>{children}</BrowserRouter>
        </NotificationProvider>
      </ThemeModeProvider>
    </QueryClientProvider>
  )
}
