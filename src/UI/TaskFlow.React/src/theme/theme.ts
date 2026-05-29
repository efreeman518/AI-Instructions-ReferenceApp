import { createTheme } from '@mui/material/styles'

export type ThemeMode = 'light' | 'dark'

const baseShape = {
  borderRadius: 8,
}

/** Builds create task flow theme values for API or UI code. */
export function createTaskFlowTheme(mode: ThemeMode) {
  const isDark = mode === 'dark'

  return createTheme({
    palette: {
      mode,
      primary: {
        main: isDark ? '#2dd4bf' : '#0f766e',
        light: '#99f6e4',
        dark: '#115e59',
      },
      secondary: {
        main: isDark ? '#fbbf24' : '#b45309',
      },
      success: {
        main: '#16a34a',
      },
      warning: {
        main: '#d97706',
      },
      error: {
        main: '#dc2626',
      },
      background: {
        default: isDark ? '#151614' : '#f5f7f4',
        paper: isDark ? '#20211f' : '#ffffff',
      },
      text: {
        primary: isDark ? '#f7f7f2' : '#172017',
        secondary: isDark ? '#c0c4b8' : '#5d685f',
      },
      divider: isDark ? 'rgba(255,255,255,0.11)' : 'rgba(15,23,42,0.1)',
    },
    shape: baseShape,
    typography: {
      fontFamily:
        '"Aptos", "Segoe UI Variable", "Segoe UI", "Helvetica Neue", Arial, sans-serif',
      h1: { fontSize: '2.1rem', fontWeight: 650, letterSpacing: 0 },
      h2: { fontSize: '1.55rem', fontWeight: 650, letterSpacing: 0 },
      h3: { fontSize: '1.22rem', fontWeight: 650, letterSpacing: 0 },
      h4: { fontSize: '1.05rem', fontWeight: 650, letterSpacing: 0 },
      button: { textTransform: 'none', fontWeight: 650 },
    },
    components: {
      MuiButton: {
        defaultProps: {
          disableElevation: true,
        },
        styleOverrides: {
          root: {
            minHeight: 36,
          },
        },
      },
      MuiPaper: {
        styleOverrides: {
          root: {
            backgroundImage: 'none',
          },
        },
      },
      MuiTableCell: {
        styleOverrides: {
          head: {
            fontSize: '0.76rem',
            fontWeight: 700,
            textTransform: 'uppercase',
            letterSpacing: 0,
          },
        },
      },
      MuiTextField: {
        defaultProps: {
          size: 'small',
        },
      },
      MuiSelect: {
        defaultProps: {
          size: 'small',
        },
      },
    },
  })
}
