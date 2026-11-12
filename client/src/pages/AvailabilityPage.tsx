import { useState, type FormEvent } from 'react';
import { availabilityApi } from '../api/endpoints';
import { errorMessage } from '../api/http';
import type { AvailabilityException, AvailabilityWindow, DayName, ExceptionKind } from '../api/types';
import { EmptyState, ErrorBanner, Loading } from '../components/Feedback';
import { Modal } from '../components/Modal';
import { DAYS, formatDateKey, shortTime, toApiTime, toDateKey } from '../lib/dates';
import { EXCEPTION_LABELS } from '../lib/labels';
import { useLoad } from '../lib/useLoad';

type WindowEdit = { day: DayName; window: AvailabilityWindow | null };

export function AvailabilityPage() {
  const today = toDateKey(new Date());
  const windows = useLoad(() => availabilityApi.windows(), []);
  const exceptions = useLoad(() => availabilityApi.exceptions(today), [today]);
  const [windowEdit, setWindowEdit] = useState<WindowEdit | null>(null);
  const [exceptionEdit, setExceptionEdit] = useState<AvailabilityException | 'new' | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const run = async (action: () => Promise<unknown>, reload: () => void) => {
    try {
      await action();
      setActionError(null);
      reload();
    } catch (e) {
      setActionError(errorMessage(e));
    }
  };

  return (
    <section>
      <h1>Доступний час</h1>
      <ErrorBanner message={windows.error ?? exceptions.error ?? actionError} />

      <h2>Регулярний тижневий графік</h2>
      <p className="muted">Години, коли ви зазвичай можете вчитися. На один день можна додати кілька проміжків.</p>
      {windows.loading && !windows.data && <Loading />}
      {windows.data && (
        <div className="card week-list">
          {DAYS.map((day) => {
            const items = windows.data!.filter((w) => w.dayOfWeek === day.key);
            return (
              <div key={day.key} className="week-row">
                <div className="week-day">{day.label}</div>
                <div className="week-slots">
                  {items.length === 0 && <span className="muted">Вихідний</span>}
                  {items.map((w) => (
                    <span key={w.id} className="chip">
                      {shortTime(w.startTime)}–{shortTime(w.endTime)}
                      <button className="chip-btn" onClick={() => setWindowEdit({ day: day.key, window: w })} title="Змінити">✎</button>
                      <button
                        className="chip-btn"
                        onClick={() => run(() => availabilityApi.removeWindow(w.id), windows.reload)}
                        title="Видалити"
                      >×</button>
                    </span>
                  ))}
                  <button className="btn btn-ghost" onClick={() => setWindowEdit({ day: day.key, window: null })}>+ Додати</button>
                </div>
              </div>
            );
          })}
        </div>
      )}

      <div className="page-header">
        <h2>Винятки на конкретні дні</h2>
        <button className="btn btn-primary" onClick={() => setExceptionEdit('new')}>Додати виняток</button>
      </div>
      <p className="muted">Наприклад, додаткові заняття або вільний день. Виняток замінює регулярний графік на цю дату.</p>
      {exceptions.loading && !exceptions.data && <Loading />}
      {exceptions.data && exceptions.data.length === 0 && <EmptyState>Майбутніх винятків немає.</EmptyState>}
      {exceptions.data && exceptions.data.length > 0 && (
        <div className="card table-wrap">
          <table>
            <thead><tr><th>Дата</th><th>Тип</th><th>Години</th><th>Причина</th><th /></tr></thead>
            <tbody>
              {exceptions.data.map((ex) => (
                <tr key={ex.id}>
                  <td>{formatDateKey(ex.date)}</td>
                  <td>{EXCEPTION_LABELS[ex.kind]}</td>
                  <td>{ex.startTime && ex.endTime ? `${shortTime(ex.startTime)}–${shortTime(ex.endTime)}` : '—'}</td>
                  <td>{ex.reason ?? '—'}</td>
                  <td className="row-actions">
                    <button className="btn btn-ghost" onClick={() => setExceptionEdit(ex)}>Ред.</button>
                    <button className="btn btn-danger" onClick={() => run(() => availabilityApi.removeException(ex.id), exceptions.reload)}>Вид.</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {windowEdit && (
        <WindowModal
          edit={windowEdit}
          onClose={() => setWindowEdit(null)}
          onSaved={() => {
            setWindowEdit(null);
            windows.reload();
          }}
        />
      )}
      {exceptionEdit && (
        <ExceptionModal
          exception={exceptionEdit === 'new' ? null : exceptionEdit}
          minDate={today}
          onClose={() => setExceptionEdit(null)}
          onSaved={() => {
            setExceptionEdit(null);
            exceptions.reload();
          }}
        />
      )}
    </section>
  );
}

function WindowModal({ edit, onClose, onSaved }: { edit: WindowEdit; onClose: () => void; onSaved: () => void }) {
  const [start, setStart] = useState(edit.window ? shortTime(edit.window.startTime) : '17:00');
  const [end, setEnd] = useState(edit.window ? shortTime(edit.window.endTime) : '20:00');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const dayLabel = DAYS.find((d) => d.key === edit.day)!.label;

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setBusy(true);
    setError(null);
    const input = { dayOfWeek: edit.day, startTime: toApiTime(start), endTime: toApiTime(end) };
    try {
      if (edit.window) await availabilityApi.updateWindow(edit.window.id, input);
      else await availabilityApi.createWindow(input);
      onSaved();
    } catch (err) {
      setError(errorMessage(err));
      setBusy(false);
    }
  };

  return (
    <Modal title={`${dayLabel}: ${edit.window ? 'зміна проміжку' : 'новий проміжок'}`} onClose={onClose}>
      <form onSubmit={submit} className="form">
        <ErrorBanner message={error} />
        <div className="form-row">
          <label>Початок
            <input type="time" value={start} onChange={(e) => setStart(e.target.value)} required />
          </label>
          <label>Кінець
            <input type="time" value={end} onChange={(e) => setEnd(e.target.value)} required />
          </label>
        </div>
        <div className="modal-actions">
          <button type="button" className="btn btn-ghost" onClick={onClose}>Скасувати</button>
          <button className="btn btn-primary" disabled={busy}>Зберегти</button>
        </div>
      </form>
    </Modal>
  );
}

interface ExceptionModalProps {
  exception: AvailabilityException | null;
  minDate: string;
  onClose: () => void;
  onSaved: () => void;
}

function ExceptionModal({ exception, minDate, onClose, onSaved }: ExceptionModalProps) {
  const [date, setDate] = useState(exception?.date ?? minDate);
  const [kind, setKind] = useState<ExceptionKind>(exception?.kind ?? 'Unavailable');
  const [start, setStart] = useState(exception?.startTime ? shortTime(exception.startTime) : '10:00');
  const [end, setEnd] = useState(exception?.endTime ? shortTime(exception.endTime) : '14:00');
  const [reason, setReason] = useState(exception?.reason ?? '');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setBusy(true);
    setError(null);
    const custom = kind === 'CustomHours';
    const input = {
      date,
      kind,
      startTime: custom ? toApiTime(start) : null,
      endTime: custom ? toApiTime(end) : null,
      reason: reason.trim() || null,
    };
    try {
      if (exception) await availabilityApi.updateException(exception.id, input);
      else await availabilityApi.createException(input);
      onSaved();
    } catch (err) {
      setError(errorMessage(err));
      setBusy(false);
    }
  };

  return (
    <Modal title={exception ? 'Редагування винятку' : 'Новий виняток'} onClose={onClose}>
      <form onSubmit={submit} className="form">
        <ErrorBanner message={error} />
        <label>Дата
          <input type="date" value={date} onChange={(e) => setDate(e.target.value)} required />
        </label>
        <label>Тип
          <select value={kind} onChange={(e) => setKind(e.target.value as ExceptionKind)}>
            {(Object.keys(EXCEPTION_LABELS) as ExceptionKind[]).map((k) => (
              <option key={k} value={k}>{EXCEPTION_LABELS[k]}</option>
            ))}
          </select>
        </label>
        {kind === 'CustomHours' && (
          <div className="form-row">
            <label>Початок
              <input type="time" value={start} onChange={(e) => setStart(e.target.value)} required />
            </label>
            <label>Кінець
              <input type="time" value={end} onChange={(e) => setEnd(e.target.value)} required />
            </label>
          </div>
        )}
        <label>Причина (необовʼязково)
          <input value={reason} onChange={(e) => setReason(e.target.value)} maxLength={500} />
        </label>
        <div className="modal-actions">
          <button type="button" className="btn btn-ghost" onClick={onClose}>Скасувати</button>
          <button className="btn btn-primary" disabled={busy}>Зберегти</button>
        </div>
      </form>
    </Modal>
  );
}
