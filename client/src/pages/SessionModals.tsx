import { useState, type FormEvent } from 'react';
import { sessionsApi } from '../api/endpoints';
import { errorMessage } from '../api/http';
import type { SessionStatus, StudySession, StudyTask } from '../api/types';
import { ErrorBanner } from '../components/Feedback';
import { Modal } from '../components/Modal';
import { formatMinutes, fromLocalInput, toLocalInput } from '../lib/dates';
import { SESSION_STATUSES, SESSION_STATUS_LABELS } from '../lib/labels';

interface SessionModalProps {
  session: StudySession;
  onClose: () => void;
  onSaved: () => void;
}

export function SessionModal({ session, onClose, onSaved }: SessionModalProps) {
  const [start, setStart] = useState(toLocalInput(session.startAtUtc));
  const [end, setEnd] = useState(toLocalInput(session.endAtUtc));
  const [status, setStatus] = useState<SessionStatus>(session.status);
  const [actual, setActual] = useState(session.actualMinutes === null ? '' : String(session.actualMinutes));
  const [locked, setLocked] = useState(session.isLocked);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const run = async (action: () => Promise<unknown>) => {
    setBusy(true);
    setError(null);
    try {
      await action();
      onSaved();
    } catch (err) {
      setError(errorMessage(err));
      setBusy(false);
    }
  };

  const submit = (e: FormEvent) => {
    e.preventDefault();
    if (new Date(end) <= new Date(start)) {
      setError('Кінець сесії має бути пізніше за початок.');
      return;
    }
    void run(() => sessionsApi.update(session.id, {
      startAtUtc: fromLocalInput(start),
      endAtUtc: fromLocalInput(end),
      status,
      actualMinutes: status === 'Completed' && actual !== '' ? Number(actual) : null,
      isLocked: locked,
    }));
  };

  const remove = () => {
    if (window.confirm('Видалити цю сесію?')) void run(() => sessionsApi.remove(session.id));
  };

  return (
    <Modal title="Навчальна сесія" onClose={onClose}>
      <form onSubmit={submit} className="form">
        <ErrorBanner message={error} />
        <p className="session-title">
          <strong>{session.taskTitle}</strong>
          <span className="muted"> · {session.disciplineName}</span>
        </p>
        <div className="form-row">
          <label>Початок
            <input type="datetime-local" value={start} onChange={(e) => setStart(e.target.value)} required />
          </label>
          <label>Кінець
            <input type="datetime-local" value={end} onChange={(e) => setEnd(e.target.value)} required />
          </label>
        </div>
        <div className="form-row">
          <label>Статус
            <select value={status} onChange={(e) => setStatus(e.target.value as SessionStatus)}>
              {SESSION_STATUSES.map((s) => <option key={s} value={s}>{SESSION_STATUS_LABELS[s]}</option>)}
            </select>
          </label>
          {status === 'Completed' && (
            <label>Фактично, хв
              <input
                type="number" min={0} max={1440} value={actual}
                placeholder={`за планом: ${session.plannedMinutes}`}
                onChange={(e) => setActual(e.target.value)}
              />
            </label>
          )}
        </div>
        <label className="check">
          <input type="checkbox" checked={locked} onChange={(e) => setLocked(e.target.checked)} />
          Закріпити (перепланування не чіпатиме цю сесію)
        </label>
        <p className="muted small">
          Заплановано: {formatMinutes(session.plannedMinutes)}. Витрачений час додається до загального прогресу завдання.
        </p>
        <div className="modal-actions">
          <button type="button" className="btn btn-danger" onClick={remove} disabled={busy}>Видалити</button>
          <button type="button" className="btn btn-ghost" onClick={onClose}>Скасувати</button>
          <button className="btn btn-primary" disabled={busy}>Зберегти</button>
        </div>
      </form>
    </Modal>
  );
}

interface NewSessionModalProps {
  tasks: StudyTask[];
  defaultStart: string;
  onClose: () => void;
  onSaved: () => void;
}

export function NewSessionModal({ tasks, defaultStart, onClose, onSaved }: NewSessionModalProps) {
  const [taskId, setTaskId] = useState(tasks[0]?.id ?? '');
  const [start, setStart] = useState(defaultStart);
  const [minutes, setMinutes] = useState('60');
  const [locked, setLocked] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    const length = Number(minutes);
    if (!(length >= 5 && length <= 720)) {
      setError('Тривалість — від 5 хвилин до 12 годин.');
      return;
    }
    const startDate = new Date(start);
    setBusy(true);
    setError(null);
    try {
      await sessionsApi.create({
        studyTaskId: taskId,
        startAtUtc: startDate.toISOString(),
        endAtUtc: new Date(startDate.getTime() + length * 60000).toISOString(),
        isLocked: locked,
      });
      onSaved();
    } catch (err) {
      setError(errorMessage(err));
      setBusy(false);
    }
  };

  return (
    <Modal title="Нова сесія" onClose={onClose}>
      {tasks.length === 0 ? (
        <p className="muted">Немає незавершених завдань. Спершу додайте завдання на сторінці «Завдання».</p>
      ) : (
        <form onSubmit={submit} className="form">
          <ErrorBanner message={error} />
          <label>Завдання
            <select value={taskId} onChange={(e) => setTaskId(e.target.value)}>
              {tasks.map((t) => <option key={t.id} value={t.id}>{t.disciplineName}: {t.title}</option>)}
            </select>
          </label>
          <div className="form-row">
            <label>Початок
              <input type="datetime-local" value={start} onChange={(e) => setStart(e.target.value)} required />
            </label>
            <label>Тривалість, хв
              <input type="number" min={5} max={720} step={5} value={minutes} onChange={(e) => setMinutes(e.target.value)} required />
            </label>
          </div>
          <label className="check">
            <input type="checkbox" checked={locked} onChange={(e) => setLocked(e.target.checked)} />
            Закріпити (перепланування не чіпатиме цю сесію)
          </label>
          <div className="modal-actions">
            <button type="button" className="btn btn-ghost" onClick={onClose}>Скасувати</button>
            <button className="btn btn-primary" disabled={busy}>Додати</button>
          </div>
        </form>
      )}
    </Modal>
  );
}
