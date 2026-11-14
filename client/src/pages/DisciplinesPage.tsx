import { useState, type FormEvent } from 'react';
import { disciplinesApi } from '../api/endpoints';
import { errorMessage } from '../api/http';
import type { Discipline } from '../api/types';
import { EmptyState, ErrorBanner, Loading } from '../components/Feedback';
import { Modal } from '../components/Modal';
import { useLoad } from '../lib/useLoad';

const DEFAULT_COLOR = '#3b82f6';

export function DisciplinesPage() {
  const { data, loading, error, reload } = useLoad(() => disciplinesApi.list(), []);
  const [editing, setEditing] = useState<Discipline | 'new' | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const remove = async (discipline: Discipline) => {
    const warning = discipline.taskCount > 0
      ? `Дисципліну «${discipline.name}» буде видалено разом із ${discipline.taskCount} завдан. Продовжити?`
      : `Видалити дисципліну «${discipline.name}»?`;
    if (!window.confirm(warning)) return;
    try {
      await disciplinesApi.remove(discipline.id);
      setActionError(null);
      reload();
    } catch (e) {
      setActionError(errorMessage(e));
    }
  };

  return (
    <section>
      <div className="page-header">
        <h1>Дисципліни</h1>
        <button className="btn btn-primary" onClick={() => setEditing('new')}>Нова дисципліна</button>
      </div>
      <ErrorBanner message={error ?? actionError} />
      {loading && !data && <Loading />}
      {data && data.length === 0 && (
        <EmptyState>Дисциплін ще немає. Додайте першу, щоб створювати для неї завдання.</EmptyState>
      )}
      <div className="grid">
        {data?.map((d) => (
          <article key={d.id} className="card discipline-card" style={{ borderTopColor: d.color }}>
            <h3>{d.name}</h3>
            {d.description && <p className="muted">{d.description}</p>}
            <p className="muted small">Завдань: {d.taskCount}</p>
            <div className="row-actions">
              <button className="btn btn-ghost" onClick={() => setEditing(d)}>Редагувати</button>
              <button className="btn btn-danger" onClick={() => remove(d)}>Видалити</button>
            </div>
          </article>
        ))}
      </div>
      {editing && (
        <DisciplineModal
          discipline={editing === 'new' ? null : editing}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null);
            reload();
          }}
        />
      )}
    </section>
  );
}

interface ModalProps {
  discipline: Discipline | null;
  onClose: () => void;
  onSaved: () => void;
}

function DisciplineModal({ discipline, onClose, onSaved }: ModalProps) {
  const [name, setName] = useState(discipline?.name ?? '');
  const [description, setDescription] = useState(discipline?.description ?? '');
  const [color, setColor] = useState(discipline?.color ?? DEFAULT_COLOR);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setBusy(true);
    setError(null);
    const input = { name: name.trim(), description: description.trim() || null, color };
    try {
      if (discipline) await disciplinesApi.update(discipline.id, input);
      else await disciplinesApi.create(input);
      onSaved();
    } catch (err) {
      setError(errorMessage(err));
      setBusy(false);
    }
  };

  return (
    <Modal title={discipline ? 'Редагування дисципліни' : 'Нова дисципліна'} onClose={onClose}>
      <form onSubmit={submit} className="form">
        <ErrorBanner message={error} />
        <label>Назва
          <input value={name} onChange={(e) => setName(e.target.value)} required maxLength={200} autoFocus />
        </label>
        <label>Опис
          <textarea value={description} onChange={(e) => setDescription(e.target.value)} rows={3} maxLength={2000} />
        </label>
        <label>Колір у календарі
          <input type="color" value={color} onChange={(e) => setColor(e.target.value)} />
        </label>
        <div className="modal-actions">
          <button type="button" className="btn btn-ghost" onClick={onClose}>Скасувати</button>
          <button className="btn btn-primary" disabled={busy}>Зберегти</button>
        </div>
      </form>
    </Modal>
  );
}
