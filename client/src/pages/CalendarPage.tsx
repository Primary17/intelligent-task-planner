import { useMemo, useState, type CSSProperties } from 'react';
import { Link } from 'react-router-dom';
import { availabilityApi, disciplinesApi, planningApi, scheduleApi, sessionsApi, tasksApi } from '../api/endpoints';
import { errorMessage } from '../api/http';
import type { StudySession } from '../api/types';
import {
  DAYS, addDays, formatDayMonth, formatMinutes, localUtcOffsetMinutes, minutesToClock, parseUtc, startOfWeek, timeToMinutes, toDateKey,
} from '../lib/dates';
import { CapacityAlert } from '../components/CapacityAlert';
import { ErrorBanner, Loading } from '../components/Feedback';
import { ScheduleReport } from '../components/ScheduleReport';
import { SESSION_STATUS_LABELS } from '../lib/labels';
import { browserTimeZone, planningHorizon, summarizeGenerate, summarizeReplan, type ScheduleSummary } from '../lib/schedule';
import { spanOnDay } from '../lib/sessions';
import { useLoad } from '../lib/useLoad';
import { NewSessionModal, SessionModal } from './SessionModals';

const HOUR_PX = 44;

export function CalendarPage() {
  const [weekStart, setWeekStart] = useState(() => startOfWeek(new Date()));
  const days = useMemo(() => Array.from({ length: 7 }, (_, i) => addDays(weekStart, i)), [weekStart]);
  const from = toDateKey(days[0]);
  const to = toDateKey(days[6]);
  const todayKey = toDateKey(new Date());

  const availability = useLoad(() => availabilityApi.effective(from, to), [from, to]);
  const tasks = useLoad(() => tasksApi.list(), []);
  const disciplines = useLoad(() => disciplinesApi.list(), []);
  const rangeFrom = days[0].toISOString();
  const rangeTo = addDays(days[6], 1).toISOString();
  const sessions = useLoad(() => sessionsApi.list(rangeFrom, rangeTo), [rangeFrom, rangeTo]);

  const capacity = useLoad(() => planningApi.capacity(localUtcOffsetMinutes()), []);

  const [editing, setEditing] = useState<StudySession | null>(null);
  const [adding, setAdding] = useState(false);
  const [scheduling, setScheduling] = useState<'generate' | 'replan' | null>(null);
  const [scheduleError, setScheduleError] = useState<string | null>(null);
  const [report, setReport] = useState<ScheduleSummary | null>(null);
  const changeWeek = (newWeekStart: Date) => {
    setWeekStart(newWeekStart);
    setReport(null);
    setScheduleError(null);
  };

  const colorOf = (id: string) => disciplines.data?.find((d) => d.id === id)?.color ?? '#94a3b8';

  const { startHour, endHour, totalMinutes } = useMemo(() => {
    let start = 8;
    let end = 21;
    let total = 0;
    for (const day of availability.data ?? []) {
      for (const interval of day.intervals) {
        const s = timeToMinutes(interval.startTime);
        const e = timeToMinutes(interval.endTime);
        start = Math.min(start, Math.floor(s / 60));
        end = Math.max(end, Math.ceil(e / 60));
        total += e - s;
      }
    }
    for (const session of sessions.data ?? []) {
      for (const day of days) {
        const span = spanOnDay(session.startAtUtc, session.endAtUtc, day);
        if (!span) continue;
        start = Math.min(start, Math.floor(span.start / 60));
        end = Math.max(end, Math.ceil(span.end / 60));
      }
    }
    return { startHour: start, endHour: end, totalMinutes: total };
  }, [availability.data, sessions.data, days]);

  const plannedMinutes = (sessions.data ?? [])
    .filter((s) => s.status !== 'Missed')
    .reduce((sum, s) => sum + s.plannedMinutes, 0);

  const openTasks = (tasks.data ?? []).filter((t) => t.status !== 'Completed');
  const openDeadlines = useMemo(
    () => openTasks.map((t) => t.deadlineUtc),
    [tasks.data],
  );
  const defaultStart = `${toDateKey(days.some((d) => toDateKey(d) === todayKey) ? new Date() : days[0])}T18:00`;

  const reloadAll = () => {
    sessions.reload();
    tasks.reload();
    capacity.reload();
    setEditing(null);
    setAdding(false);
  };

  const horizon = useMemo(
    () => planningHorizon(weekStart, new Date(), openDeadlines),
    [weekStart, openDeadlines],
  );

  const runSchedule = async (kind: 'generate' | 'replan') => {
    if (!horizon) return;

    if (kind === 'generate') {
      const confirmed = window.confirm(
        'Повторна генерація замінить незакріплені заплановані сесії '
        + 'на всьому горизонті планування (від обраного тижня до найпізнішого дедлайну '
        + 'незавершених завдань, не більше 92 днів). '
        + 'Закріплені та завершені сесії залишаться без змін. '
        + 'Продовжити?'
      );

      if (!confirmed) return;
    }
    const names = {
      taskTitle: (id: string) => tasks.data?.find((t) => t.id === id)?.title,
      sessionTitle: (id: string) => sessions.data?.find((s) => s.id === id)?.taskTitle,
    };
    setScheduling(kind);
    setScheduleError(null);
    try {
      const request = { ...horizon, timeZoneId: browserTimeZone() };
      setReport(kind === 'generate'
        ? summarizeGenerate(await scheduleApi.generate(request), names)
        : summarizeReplan(await scheduleApi.replan(request), names));
      reloadAll();
    } catch (e) {
      setScheduleError(errorMessage(e));
    } finally {
      setScheduling(null);
    }
  };

  const hours = Array.from({ length: endHour - startHour }, (_, i) => startHour + i);
  const bodyHeight = hours.length * HOUR_PX;
  const offset = (minutes: number) => ((minutes - startHour * 60) / 60) * HOUR_PX;

  const deadlinesByDay = useMemo(() => {
    const map = new Map<string, NonNullable<typeof tasks.data>>();
    for (const task of tasks.data ?? []) {
      const key = toDateKey(parseUtc(task.deadlineUtc));
      map.set(key, [...(map.get(key) ?? []), task]);
    }
    return map;
  }, [tasks.data]);

  return (
    <section>
      <div className="page-header">
        <h1>Календар</h1>
        <div className="week-nav">
          <button
            className="btn btn-primary"
            disabled={!horizon || scheduling !== null}
            title="Розставити сесії у вільний час до дедлайнів відкритих завдань (не лише обраний тиждень)"
            onClick={() => runSchedule('generate')}
          >
            {scheduling === 'generate' ? 'Формуємо…' : 'Сформувати розклад'}
          </button>
          <button
            className="btn btn-ghost"
            disabled={!horizon || scheduling !== null}
            title="Прибрати пропущені й майбутні незакріплені сесії та розставити залишок знову"
            onClick={() => runSchedule('replan')}
          >
            {scheduling === 'replan' ? 'Перепланування…' : 'Перепланувати'}
          </button>
          <button className="btn btn-ghost" onClick={() => setAdding(true)}>+ Сесія</button>
          <button className="btn btn-ghost" onClick={() => changeWeek(addDays(weekStart, -7))}>←</button>
          <button className="btn btn-ghost" onClick={() => changeWeek(startOfWeek(new Date()))}>Сьогодні</button>
          <button className="btn btn-ghost" onClick={() => changeWeek(addDays(weekStart, 7))}>→</button>
        </div>
      </div>
      <p className="muted">
        {formatDayMonth(days[0])} – {formatDayMonth(days[6])}. Доступно для навчання:{' '}
        <strong>{formatMinutes(totalMinutes)}</strong>. Заплановано: <strong>{formatMinutes(plannedMinutes)}</strong>
        {totalMinutes === 0 && !availability.loading && (
          <> — <Link to="/availability">налаштуйте доступний час</Link></>
        )}
      </p>
      <ErrorBanner message={availability.error ?? tasks.error ?? disciplines.error ?? sessions.error} />
      <ErrorBanner message={scheduleError} />
      {report && <ScheduleReport summary={report} onClose={() => setReport(null)} />}
      <CapacityAlert report={capacity.data} />
      {availability.loading && !availability.data && <Loading />}

      {availability.data && (
        <div className="card calendar">
          <div className="cal-grid" style={{ gridTemplateColumns: '52px repeat(7, minmax(110px, 1fr))' }}>
            <div />
            {days.map((day, i) => (
              <div key={i} className={`cal-head ${toDateKey(day) === todayKey ? 'today' : ''}`}>
                <strong>{DAYS[i].short}</strong> {day.getDate()}.{String(day.getMonth() + 1).padStart(2, '0')}
              </div>
            ))}

            <div className="cal-label">Дедлайни</div>
            {days.map((day, i) => (
              <div key={i} className="cal-deadlines">
                {(deadlinesByDay.get(toDateKey(day)) ?? []).map((task) => (
                  <div
                    key={task.id}
                    className={`deadline ${task.status === 'Completed' ? 'done' : ''}`}
                    style={{ borderLeftColor: colorOf(task.disciplineId), background: `${colorOf(task.disciplineId)}22` }}
                    title={`${task.disciplineName}: ${task.title}`}
                  >
                    <span className="deadline-time">
                      {parseUtc(task.deadlineUtc).toLocaleTimeString('uk-UA', { hour: '2-digit', minute: '2-digit' })}
                    </span>{' '}
                    {task.title}
                  </div>
                ))}
              </div>
            ))}

            <div className="cal-hours" style={{ height: bodyHeight }}>
              {hours.map((h) => (
                <div key={h} className="cal-hour" style={{ height: HOUR_PX }}>{String(h).padStart(2, '0')}:00</div>
              ))}
            </div>
            {days.map((day, i) => {
              const info = availability.data!.find((d) => d.date === toDateKey(day));
              return (
                <div
                  key={i}
                  className={`cal-col ${toDateKey(day) === todayKey ? 'today' : ''}`}
                  style={{ height: bodyHeight, backgroundSize: `100% ${HOUR_PX}px` }}
                >
                  {info?.source === 'Unavailable' && <div className="cal-note">Недоступний</div>}
                  {info?.intervals.map((interval, j) => {
                    const s = timeToMinutes(interval.startTime);
                    const e = timeToMinutes(interval.endTime);
                    return (
                      <div
                        key={j}
                        className={`slot ${info.source === 'CustomHours' ? 'slot-custom' : ''}`}
                        style={{ top: offset(s), height: ((e - s) / 60) * HOUR_PX }}
                        title={`${interval.startTime.slice(0, 5)}–${interval.endTime.slice(0, 5)}`}
                      />
                    );
                  })}
                  {(sessions.data ?? []).map((session) => {
                    const span = spanOnDay(session.startAtUtc, session.endAtUtc, day);
                    if (!span) return null;
                    const color = colorOf(session.disciplineId);
                    const pxHeight = Math.max(((span.end - span.start) / 60) * HOUR_PX - 1, 16);
                    const timeLabel = `${minutesToClock(span.start)}–${minutesToClock(span.end)}`;
                    const title = session.taskTitle.trim();
                    const locked = session.isLocked ? '🔒 ' : '';
                    const layout = pxHeight < 26 ? 'tiny' : pxHeight < 46 ? 'short' : 'full';
                    return (
                      <button
                        key={session.id}
                        type="button"
                        className={`session session-${session.status.toLowerCase()} session-${layout}`}
                        style={{
                          top: offset(span.start),
                          height: pxHeight,
                          borderLeftColor: color,
                          backgroundColor: `color-mix(in srgb, ${color} 78%, white)`,
                        } as CSSProperties}
                        title={`${session.disciplineName}: ${title} · ${timeLabel} (${SESSION_STATUS_LABELS[session.status]})`}
                        onClick={() => setEditing(session)}
                      >
                        {layout === 'tiny' && <span className="session-time">{timeLabel}</span>}
                        {layout === 'short' && (
                          <span className="session-line">{timeLabel} · {locked}{title}</span>
                        )}
                        {layout === 'full' && (
                          <>
                            <span className="session-time">{timeLabel}</span>
                            <span className="session-name">{locked}{title}</span>
                          </>
                        )}
                      </button>
                    );
                  })}
                </div>
              );
            })}
          </div>
        </div>
      )}

      <div className="legend muted small">
        <span><i className="legend-box slot" /> Доступний час</span>
        <span><i className="legend-box slot slot-custom" /> Змінені години (виняток)</span>
        <span><i className="legend-box deadline" /> Дедлайн завдання (колір дисципліни)</span>
        <span><i className="legend-box session" /> Навчальна сесія (клік — відмітити виконання)</span>
      </div>

      {editing && <SessionModal session={editing} onClose={() => setEditing(null)} onSaved={reloadAll} />}
      {adding && (
        <NewSessionModal tasks={openTasks} defaultStart={defaultStart} onClose={() => setAdding(false)} onSaved={reloadAll} />
      )}
    </section>
  );
}
