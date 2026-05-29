import { useMemo, useState, type ReactNode } from 'react'
import { CssBaseline, ThemeProvider as MuiThemeProvider } from '@mui/material'
import type { ThemeMode } from './theme'
import { createTaskFlowTheme } from './theme'
import { ThemeModeContext, type ThemeModeContextValue } from './themeModeContext'

const storageKey = 'taskflow.react.theme'

/** Provides theme mode state to React children. */
export function ThemeModeProvider({ children }: { children: ReactNode }) {
  const [mode, setModeState] = useState<ThemeMode>(() => {
    const stored = window.localStorage.getItem(storageKey)
    return stored === 'light' || stored === 'dark' ? stored : 'dark'
  })

  const value = useMemo<ThemeModeContextValue>(
    () => ({
      mode,
      setMode: (nextMode) => {
        window.localStorage.setItem(storageKey, nextMode)
        setModeState(nextMode)
      },
    }),
    [mode],
  )

  const theme = useMemo(() => createTaskFlowTheme(mode), [mode])

  return (
    <ThemeModeContext.Provider value={value}>
      <MuiThemeProvider theme={theme}>
        <CssBaseline />
        {children}
      </MuiThemeProvider>
    </ThemeModeContext.Provider>
  )
}
