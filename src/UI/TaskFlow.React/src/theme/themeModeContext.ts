import { createContext, useContext } from 'react'
import type { ThemeMode } from './theme'

/** Describes theme mode context value data used by the React UI. */
export interface ThemeModeContextValue {
  mode: ThemeMode
  setMode: (mode: ThemeMode) => void
}

export const ThemeModeContext = createContext<ThemeModeContextValue | undefined>(undefined)

/** Provides use theme mode hook behavior for React components. */
export function useThemeMode() {
  const context = useContext(ThemeModeContext)
  if (!context) {
    throw new Error('useThemeMode must be used within ThemeModeProvider.')
  }
  return context
}
