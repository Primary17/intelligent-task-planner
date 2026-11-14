import { NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

const LINKS = [
  { to: '/calendar', label: 'Календар' },
  { to: '/tasks', label: 'Завдання' },
  { to: '/disciplines', label: 'Дисципліни' },
  { to: '/availability', label: 'Доступний час' },
];

export function Layout() {
  const { user, logout } = useAuth();

  return (
    <div className="app">
      <header className="topbar">
        <div className="brand">Task Planner</div>
        <nav>
          {LINKS.map((link) => (
            <NavLink key={link.to} to={link.to} className={({ isActive }) => (isActive ? 'active' : '')}>
              {link.label}
            </NavLink>
          ))}
        </nav>
        <div className="user">
          <NavLink to="/profile" className="user-link" title={user?.email}>{user?.displayName}</NavLink>
          <button type="button" className="btn btn-ghost" onClick={logout}>Вийти</button>
        </div>
      </header>
      <main className="content">
        <Outlet />
      </main>
    </div>
  );
}
