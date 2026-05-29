import { useState } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router-dom'
import {
  AppBar,
  Box,
  Divider,
  Drawer,
  IconButton,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  Tooltip,
  Typography,
} from '@mui/material'
import {
  CheckSquare,
  FolderTree,
  Home,
  Menu,
  Moon,
  Settings,
  Sun,
  Tag,
  X,
  type LucideIcon,
} from 'lucide-react'
import { useThemeMode } from '../theme/themeModeContext'

const drawerWidth = 248

const navItems: Array<{ label: string; path: string; icon: LucideIcon }> = [
  { label: 'Dashboard', path: '/', icon: Home },
  { label: 'Tasks', path: '/tasks', icon: CheckSquare },
  { label: 'Categories', path: '/categories', icon: FolderTree },
  { label: 'Tags', path: '/tags', icon: Tag },
  { label: 'Settings', path: '/settings', icon: Settings },
]

/** Provides app shell application shell or context behavior for React children. */
export function AppShell() {
  const [mobileOpen, setMobileOpen] = useState(false)
  const { mode, setMode } = useThemeMode()
  const location = useLocation()

  const drawer = (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      <Toolbar sx={{ alignItems: 'center', gap: 1.25, minHeight: 68 }}>
        <Box
          aria-hidden="true"
          sx={{
            bgcolor: 'primary.main',
            borderRadius: 1,
            height: 34,
            width: 34,
          }}
        />
        <Box>
          <Typography sx={{ fontWeight: 750, lineHeight: 1.1 }} variant="h3">
            TaskFlow
          </Typography>
          <Typography color="text.secondary" variant="caption">
            React
          </Typography>
        </Box>
      </Toolbar>
      <Divider />
      <List sx={{ flex: 1, px: 1.5, py: 2 }}>
        {navItems.map(({ icon: Icon, label, path }) => (
          <ListItemButton
            key={path}
            component={NavLink}
            end={path === '/'}
            onClick={() => setMobileOpen(false)}
            selected={path === '/' ? location.pathname === '/' : location.pathname.startsWith(path)}
            to={path}
            sx={{
              borderRadius: 1,
              mb: 0.5,
              minHeight: 44,
              '&.active': {
                bgcolor: 'action.selected',
              },
            }}
          >
            <ListItemIcon sx={{ minWidth: 38 }}>
              <Icon size={19} />
            </ListItemIcon>
            <ListItemText primary={<Typography sx={{ fontWeight: 650 }}>{label}</Typography>} />
          </ListItemButton>
        ))}
      </List>
      <Divider />
      <Box sx={{ p: 1.5 }}>
        <Tooltip title={mode === 'dark' ? 'Switch to light theme' : 'Switch to dark theme'}>
          <IconButton
            aria-label={mode === 'dark' ? 'Switch to light theme' : 'Switch to dark theme'}
            onClick={() => setMode(mode === 'dark' ? 'light' : 'dark')}
          >
            {mode === 'dark' ? <Sun size={20} /> : <Moon size={20} />}
          </IconButton>
        </Tooltip>
      </Box>
    </Box>
  )

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      <AppBar
        color="default"
        elevation={0}
        position="fixed"
        sx={{
          borderBottom: 1,
          borderColor: 'divider',
          display: { md: 'none' },
        }}
      >
        <Toolbar>
          <IconButton aria-label="Open navigation" edge="start" onClick={() => setMobileOpen(true)}>
            <Menu />
          </IconButton>
          <Typography sx={{ fontWeight: 700, ml: 1 }} variant="h3">
            TaskFlow
          </Typography>
        </Toolbar>
      </AppBar>

      <Box component="nav" sx={{ flexShrink: { md: 0 }, width: { md: drawerWidth } }}>
        <Drawer
          ModalProps={{ keepMounted: true }}
          onClose={() => setMobileOpen(false)}
          open={mobileOpen}
          sx={{
            display: { xs: 'block', md: 'none' },
            '& .MuiDrawer-paper': { width: drawerWidth },
          }}
          variant="temporary"
        >
          <Box sx={{ alignItems: 'center', display: 'flex', justifyContent: 'flex-end', p: 1 }}>
            <IconButton aria-label="Close navigation" onClick={() => setMobileOpen(false)}>
              <X />
            </IconButton>
          </Box>
          {drawer}
        </Drawer>
        <Drawer
          open
          sx={{
            display: { xs: 'none', md: 'block' },
            '& .MuiDrawer-paper': {
              borderRight: 1,
              borderColor: 'divider',
              boxSizing: 'border-box',
              width: drawerWidth,
            },
          }}
          variant="permanent"
        >
          {drawer}
        </Drawer>
      </Box>

      <Box
        component="main"
        sx={{
          flex: 1,
          minWidth: 0,
          px: { xs: 2, md: 3.5 },
          py: { xs: 9, md: 3.5 },
        }}
      >
        <Outlet />
      </Box>
    </Box>
  )
}
