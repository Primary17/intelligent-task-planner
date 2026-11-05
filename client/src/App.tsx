import { Navigate, Route, Routes } from 'react-router-dom';
import { Layout } from './components/Layout';
import { RequireAuth } from './components/RequireAuth';
import { AvailabilityPage } from './pages/AvailabilityPage';
import { LoginPage, RegisterPage } from './pages/AuthPages';
import { CalendarPage } from './pages/CalendarPage';
import { DisciplinesPage } from './pages/DisciplinesPage';
import { TasksPage } from './pages/TasksPage';

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route element={<RequireAuth><Layout /></RequireAuth>}>
        <Route index element={<Navigate to="/calendar" replace />} />
        <Route path="calendar" element={<CalendarPage />} />
        <Route path="tasks" element={<TasksPage />} />
        <Route path="disciplines" element={<DisciplinesPage />} />
        <Route path="availability" element={<AvailabilityPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
