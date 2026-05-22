import { createContext, useContext } from 'react'
import type { ThemeMode } from './theme'

export interface ThemeModeContextValue {
  mode: ThemeMode
  setMode: (mode: ThemeMode) => void
}

export const ThemeModeContext = createContext<ThemeModeContextValue | undefined>(undefined)

export function useThemeMode() {
  const context = useContext(ThemeModeContext)
  if (!context) {
    throw new Error('useThemeMode must be used within ThemeModeProvider.')
  }
  return context
}
