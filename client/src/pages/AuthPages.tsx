import { useState, type FormEvent } from 'react';
import { Link, Navigate, useNavigate } from 'react-router-dom';
import { errorMessage } from '../api/http';
import { useAuth } from '../auth/AuthContext';
import { ErrorBanner } from '../components/Feedback';

export function LoginPage() {
  const { user, login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  if (user) return <Navigate to="/calendar" replace />;

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      await login(email, password);
      navigate('/calendar');
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="auth-page">
      <form className="card auth-card" onSubmit={submit}>
        <h1>Вхід</h1>
        <ErrorBanner message={error} />
        <label>Email
          <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required autoFocus />
        </label>
        <label>Пароль
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
        </label>
        <button className="btn btn-primary" disabled={busy}>{busy ? 'Вхід…' : 'Увійти'}</button>
        <p className="muted">Немає облікового запису? <Link to="/register">Зареєструватися</Link></p>
      </form>
    </div>
  );
}

export function RegisterPage() {
  const { user, register } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  if (user) return <Navigate to="/calendar" replace />;

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    if (password !== confirm) {
      setError('Паролі не збігаються.');
      return;
    }
    setBusy(true);
    setError(null);
    try {
      await register(email, displayName, password);
      navigate('/calendar');
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="auth-page">
      <form className="card auth-card" onSubmit={submit}>
        <h1>Реєстрація</h1>
        <ErrorBanner message={error} />
        <label>Імʼя
          <input value={displayName} onChange={(e) => setDisplayName(e.target.value)} required maxLength={128} autoFocus />
        </label>
        <label>Email
          <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
        </label>
        <label>Пароль (мінімум 8 символів)
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required minLength={8} />
        </label>
        <label>Повторіть пароль
          <input type="password" value={confirm} onChange={(e) => setConfirm(e.target.value)} required />
        </label>
        <button className="btn btn-primary" disabled={busy}>{busy ? 'Створення…' : 'Створити обліковий запис'}</button>
        <p className="muted">Вже є обліковий запис? <Link to="/login">Увійти</Link></p>
      </form>
    </div>
  );
}
