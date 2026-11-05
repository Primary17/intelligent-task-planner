import { useEffect, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { disciplinesApi, tasksApi } from '../api/endpoints';
import { errorMessage } from '../api/http';
import type { Discipline, Priority, StudyTask, TaskFilters, TaskStatus } from '../api/types';
import { EmptyState, ErrorBanner, Loading } from '../components/Feedback';
import { Modal } from '../components/Modal';
import {
  defaultDeadlineInput, formatDateTime, formatMinutes, fromLocalInput, hoursToMinutes, minutesToHours,
  parseUtc, toLocalInput,
} from '../lib/dates';
import { PRIORITIES, PRIORITY_LABELS, STATUSES, STATUS_LABELS } from '../lib/labels';
import { useLoad } from '../lib/useLoad';

function useDebounced<T>(value: T, delay = 300): T {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const id = setTimeout(() => setDebounced(value), delay);
    return () => clearTimeout(id);
  }, [value, delay]);
  return debounced;
}

export function TasksPage() {
  const [search, setSearch] = useState('');
  const [disciplineId, setDisciplineId] = useState('');
  const [status, setStatus] = useState<TaskStatus | ''>('');
  const [minPriority, setMinPriority] = useState<Priority | ''>('');
  const [sort, setSort] = useState<NonNullable<TaskFilters['sort']>>('deadline');
  const debouncedSearch = useDebounced(search);

  const disciplines = useLoad(() => disciplinesApi.list(), []);
  const tasks = useLoad(
    () => tasksApi.list({
      search: debouncedSearch || undefined,
      disciplineId: disciplineId || undefined,
      status: status || undefined,
      minPriority: minPriority || undefined,
      sort,
    }),
    [debouncedSearch, disciplineId, status, minPriority, sort],
  );

  const [editing, setEditing] = useState<StudyTask | 'new' | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const colorOf = (id: string) => disciplines.data?.find((d) => d.id === id)?.color ?? '#94a3b8';

  const complete = async (task: StudyTask) => {
    try {
      await tasksApi.update(task.id, {
        title: task.title,
        deadlineUtc: task.deadlineUtc,
        complexity: task.complexity,
        priority: task.priority,
        estimatedMinutes: task.estimatedMinutes,
        actualMinutes: task.actualMinutes,
        status: 'Completed',
      });
      setActionError(null);
      tasks.reload();
    } catch (e) {
      setActionError(errorMessage(e));
    }
  };

  const remove = async (task: StudyTask) => {
    if (!window.confirm(`Видалити завдання «${task.title}»?`)) return;
    try {
      await tasksApi.remove(task.id);
      setActionError(null);
      tasks.reload();
    } catch (e) {
      setActionError(errorMessage(e));
    }
  };

  const noDisciplines = disciplines.data !== null && disciplines.data.length === 0;

  return (
    <section>
      <div className="page-header">
        <h1>Завдання</h1>
        <button className="btn btn-primary" onClick={() => setEditing('new')} disabled={noDisciplines}>
          Нове завдання
        </button>
      </div>

      {noDisciplines && (
        <EmptyState>Спочатку <Link to="/disciplines">створіть дисципліну</Link>, до якої належатимуть завдання.</EmptyState>
      )}

      <div className="filters card">
        <input placeholder="Пошук за назвою" value={search} onChange={(e) => setSearch(e.target.value)} />
        <select value={disciplineId} onChange={(e) => setDisciplineId(e.target.value)}>
          <option value="">Усі дисципліни</option>
          {disciplines.data?.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}
        </select>
        <select value={status} onChange={(e) => setStatus(e.target.value as TaskStatus | '')}>
          <option value="">Будь-який статус</option>
          {STATUSES.map((s) => <option key={s} value={s}>{STATUS_LABELS[s]}</option>)}
        </select>
        <select value={minPriority} onChange={(e) => setMinPriority(e.target.value as Priority | '')}>
          <option value="">Будь-який пріоритет</option>
          {PRIORITIES.map((p) => <option key={p} value={p}>Від: {PRIORITY_LABELS[p]}</option>)}
        </select>
        <select value={sort} onChange={(e) => setSort(e.target.value as NonNullable<TaskFilters['sort']>)}>
          <option value="deadline">За дедлайном</option>
          <option value="priority">За пріоритетом</option>
          <option value="title">За назвою</option>
        </select>
      </div>

      <ErrorBanner message={tasks.error ?? disciplines.error ?? actionError} />
      {tasks.loading && !tasks.data && <Loading />}
      {tasks.data && tasks.data.length === 0 && !noDisciplines && <EmptyState>Завдань за цими умовами немає.</EmptyState>}

      {tasks.data && tasks.data.length > 0 && (
        <div className="card table-wrap">
          <table>
            <thead>
              <tr>
                <th>Завдання</th><th>Дедлайн</th><th>Пріоритет</th><th>Склад.</th><th>Прогрес</th><th>Статус</th><th />
              </tr>
            </thead>
            <tbody>
              {tasks.data.map((task) => {
                const overdue = task.status !== 'Completed' && parseUtc(task.deadlineUtc) < new Date();
                const percent = Math.min(100, Math.round((task.actualMinutes / task.estimatedMinutes) * 100));
                return (
                  <tr key={task.id} className={task.status === 'Completed' ? 'done' : ''}>
                    <td>
                      <div className="task-title">{task.title}</div>
                      <div className="small muted">
                        <span className="dot" style={{ background: colorOf(task.disciplineId) }} />
                        {task.disciplineName}
                      </div>
                    </td>
                    <td className={overdue ? 'overdue' : ''}>{formatDateTime(task.deadlineUtc)}</td>
                    <td><span className={`badge badge-${task.priority}`}>{PRIORITY_LABELS[task.priority]}</span></td>
                    <td>{task.complexity}/5</td>
                    <td className="progress-cell">
                      <div className="progress"><div style={{ width: `${percent}%` }} /></div>
                      <span className="small muted">
                        {formatMinutes(task.actualMinutes)} / {formatMinutes(task.estimatedMinutes)}
                      </span>
                    </td>
                    <td>{STATUS_LABELS[task.status]}</td>
                    <td className="row-actions">
                      {task.status !== 'Completed' && (
                        <button className="btn btn-ghost" onClick={() => complete(task)} title="Позначити виконаним">✓</button>
                      )}
                      <button className="btn btn-ghost" onClick={() => setEditing(task)}>Ред.</button>
                      <button className="btn btn-danger" onClick={() => remove(task)}>Вид.</button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {editing && disciplines.data && (
        <TaskModal
          task={editing === 'new' ? null : editing}
          disciplines={disciplines.data}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null);
            tasks.reload();
          }}
        />
      )}
    </section>
  );
}

interface TaskModalProps {
  task: StudyTask | null;
  disciplines: Discipline[];
  onClose: () => void;
  onSaved: () => void;
}

function TaskModal({ task, disciplines, onClose, onSaved }: TaskModalProps) {
  const [title, setTitle] = useState(task?.title ?? '');
  const [disciplineId, setDisciplineId] = useState(task?.disciplineId ?? disciplines[0]?.id ?? '');
  const [deadline, setDeadline] = useState(task ? toLocalInput(task.deadlineUtc) : defaultDeadlineInput());
  const [complexity, setComplexity] = useState(task?.complexity ?? 3);
  const [priority, setPriority] = useState<Priority>(task?.priority ?? 'Medium');
  const [estimatedHours, setEstimatedHours] = useState(String(task ? minutesToHours(task.estimatedMinutes) : 2));
  const [actualHours, setActualHours] = useState(String(task ? minutesToHours(task.actualMinutes) : 0));
  const [status, setStatus] = useState<TaskStatus>(task?.status ?? 'NotStarted');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    const estimatedMinutes = hoursToMinutes(Number(estimatedHours));
    if (!(estimatedMinutes > 0)) {
      setError('Орієнтовний час має бути більшим за нуль.');
      return;
    }
    setBusy(true);
    setError(null);
    const common = {
      title: title.trim(),
      deadlineUtc: fromLocalInput(deadline),
      complexity,
      priority,
      estimatedMinutes,
      status,
    };
    try {
      if (task) {
        await tasksApi.update(task.id, { ...common, actualMinutes: hoursToMinutes(Number(actualHours) || 0) });
      } else {
        await tasksApi.create({ ...common, disciplineId });
      }
      onSaved();
    } catch (err) {
      setError(errorMessage(err));
      setBusy(false);
    }
  };

  return (
    <Modal title={task ? 'Редагування завдання' : 'Нове завдання'} onClose={onClose}>
      <form onSubmit={submit} className="form">
        <ErrorBanner message={error} />
        <label>Назва
          <input value={title} onChange={(e) => setTitle(e.target.value)} required maxLength={300} autoFocus />
        </label>
        <label>Дисципліна
          <select value={disciplineId} onChange={(e) => setDisciplineId(e.target.value)} disabled={task !== null}>
            {disciplines.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}
          </select>
        </label>
        <label>Дедлайн
          <input type="datetime-local" value={deadline} onChange={(e) => setDeadline(e.target.value)} required />
        </label>
        <div className="form-row">
          <label>Пріоритет
            <select value={priority} onChange={(e) => setPriority(e.target.value as Priority)}>
              {PRIORITIES.map((p) => <option key={p} value={p}>{PRIORITY_LABELS[p]}</option>)}
            </select>
          </label>
          <label>Складність (1–5)
            <input type="number" min={1} max={5} value={complexity} onChange={(e) => setComplexity(Number(e.target.value))} required />
          </label>
        </div>
        <div className="form-row">
          <label>Орієнтовний час, год
            <input type="number" min={0.25} step={0.25} value={estimatedHours} onChange={(e) => setEstimatedHours(e.target.value)} required />
          </label>
          {task && (
            <label>Фактично витрачено, год
              <input type="number" min={0} step={0.25} value={actualHours} onChange={(e) => setActualHours(e.target.value)} />
            </label>
          )}
        </div>
        <label>Статус
          <select value={status} onChange={(e) => setStatus(e.target.value as TaskStatus)}>
            {STATUSES.map((s) => <option key={s} value={s}>{STATUS_LABELS[s]}</option>)}
          </select>
        </label>
        <div className="modal-actions">
          <button type="button" className="btn btn-ghost" onClick={onClose}>Скасувати</button>
          <button className="btn btn-primary" disabled={busy}>Зберегти</button>
        </div>
      </form>
    </Modal>
  );
}
