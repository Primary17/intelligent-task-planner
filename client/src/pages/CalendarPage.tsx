import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { availabilityApi, disciplinesApi, tasksApi } from '../api/endpoints';
import { DAYS, addDays, formatDayMonth, formatMinutes, parseUtc, startOfWeek, timeToMinutes, toDateKey } from '../lib/dates';
import { ErrorBanner, Loading } from '../components/Feedback';
import { useLoad } from '../lib/useLoad';

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
    return { startHour: start, endHour: end, totalMinutes: total };
  }, [availability.data]);

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
          <button className="btn btn-ghost" onClick={() => setWeekStart(addDays(weekStart, -7))}>←</button>
          <button className="btn btn-ghost" onClick={() => setWeekStart(startOfWeek(new Date()))}>Сьогодні</button>
          <button className="btn btn-ghost" onClick={() => setWeekStart(addDays(weekStart, 7))}>→</button>
        </div>
      </div>
      <p className="muted">
        {formatDayMonth(days[0])} – {formatDayMonth(days[6])}. Доступно для навчання:{' '}
        <strong>{formatMinutes(totalMinutes)}</strong>
        {totalMinutes === 0 && !availability.loading && (
          <> — <Link to="/availability">налаштуйте доступний час</Link></>
        )}
      </p>
      <ErrorBanner message={availability.error ?? tasks.error ?? disciplines.error} />
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
                      >
                        {interval.startTime.slice(0, 5)}–{interval.endTime.slice(0, 5)}
                      </div>
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
      </div>
    </section>
  );
}
