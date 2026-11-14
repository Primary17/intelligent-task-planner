import { useState, type FormEvent } from 'react';
import { authApi } from '../api/endpoints';
import { errorMessage } from '../api/http';
import { useAuth } from '../auth/AuthContext';
import { ErrorBanner } from '../components/Feedback';

const ROLE_LABELS = { User: 'Користувач', Admin: 'Адміністратор' } as const;

export function ProfilePage() {
  const { user, setProfile } = useAuth();
  const [displayName, setDisplayName] = useState(user?.displayName ?? '');
  const [nameError, setNameError] = useState<string | null>(null);
  const [nameSaved, setNameSaved] = useState(false);
  const [nameBusy, setNameBusy] = useState(false);

  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [passwordError, setPasswordError] = useState<string | null>(null);
  const [passwordSaved, setPasswordSaved] = useState(false);
  const [passwordBusy, setPasswordBusy] = useState(false);

  if (!user) return null;

  const saveName = async (e: FormEvent) => {
    e.preventDefault();
    setNameBusy(true);
    setNameError(null);
    setNameSaved(false);
    try {
      setProfile(await authApi.updateProfile({ displayName: displayName.trim() }));
      setNameSaved(true);
    } catch (err) {
      setNameError(errorMessage(err));
    } finally {
      setNameBusy(false);
    }
  };

  const savePassword = async (e: FormEvent) => {
    e.preventDefault();
    setPasswordSaved(false);
    if (newPassword !== confirm) {
      setPasswordError('Паролі не збігаються.');
      return;
    }
    setPasswordBusy(true);
    setPasswordError(null);
    try {
      await authApi.changePassword({ currentPassword, newPassword });
      setCurrentPassword('');
      setNewPassword('');
      setConfirm('');
      setPasswordSaved(true);
    } catch (err) {
      setPasswordError(errorMessage(err));
    } finally {
      setPasswordBusy(false);
    }
  };

  return (
    <section>
      <div className="page-header">
        <h1>Профіль</h1>
      </div>

      <form className="card form profile-card" onSubmit={saveName}>
        <h2>Особисті дані</h2>
        <ErrorBanner message={nameError} />
        {nameSaved && <div className="alert alert-ok" role="status">Збережено.</div>}
        <label>Email
          <input value={user.email} disabled />
        </label>
        <label>Роль
          <input value={ROLE_LABELS[user.role]} disabled />
        </label>
        <label>Імʼя
          <input value={displayName} onChange={(e) => setDisplayName(e.target.value)} required maxLength={128} />
        </label>
        <div className="modal-actions">
          <button className="btn btn-primary" disabled={nameBusy || displayName.trim() === ''}>Зберегти</button>
        </div>
      </form>

      <form className="card form profile-card" onSubmit={savePassword}>
        <h2>Зміна пароля</h2>
        <ErrorBanner message={passwordError} />
        {passwordSaved && <div className="alert alert-ok" role="status">Пароль змінено.</div>}
        <label>Поточний пароль
          <input type="password" value={currentPassword} onChange={(e) => setCurrentPassword(e.target.value)}
            autoComplete="current-password" required />
        </label>
        <label>Новий пароль (мінімум 8 символів)
          <input type="password" value={newPassword} onChange={(e) => setNewPassword(e.target.value)}
            autoComplete="new-password" minLength={8} maxLength={128} required />
        </label>
        <label>Повторіть новий пароль
          <input type="password" value={confirm} onChange={(e) => setConfirm(e.target.value)}
            autoComplete="new-password" required />
        </label>
        <div className="modal-actions">
          <button className="btn btn-primary" disabled={passwordBusy}>Змінити пароль</button>
        </div>
      </form>
    </section>
  );
}
