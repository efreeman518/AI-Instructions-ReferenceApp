import { Navigate, Route, Routes } from 'react-router-dom'
import { AppShell } from './app/AppShell'
import { CategoriesPage } from './pages/CategoriesPage'
import { DashboardPage } from './pages/DashboardPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { SettingsPage } from './pages/SettingsPage'
import { TagsPage } from './pages/TagsPage'
import { TaskEditorPage } from './pages/TaskEditorPage'
import { TasksPage } from './pages/TasksPage'

export default function App() {
  return (
    <Routes>
      <Route element={<AppShell />}>
        <Route element={<DashboardPage />} index />
        <Route element={<TasksPage />} path="tasks" />
        <Route element={<TaskEditorPage />} path="tasks/new" />
        <Route element={<TaskEditorPage />} path="tasks/:id" />
        <Route element={<CategoriesPage />} path="categories" />
        <Route element={<TagsPage />} path="tags" />
        <Route element={<SettingsPage />} path="settings" />
        <Route element={<Navigate replace to="/" />} path="dashboard" />
        <Route element={<NotFoundPage />} path="*" />
      </Route>
    </Routes>
  )
}
