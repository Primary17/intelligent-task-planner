import { Link } from 'react-router-dom';
import { formatDateTime, formatMinutes } from '../lib/dates';
import { scheduleFits, type ScheduleSummary } from '../lib/schedule';

export function ScheduleReport({ summary, onClose }: { summary: ScheduleSummary; onClose: () => void }) {
  const fits = scheduleFits(summary);
  const heading = summary.kind === 'generate'
    ? `Розклад сформовано: ${summary.createdCount} сесій, ${formatMinutes(summary.createdMinutes)}.`
    : `Перепланування виконано: створено ${summary.createdCount} сесій (${formatMinutes(summary.createdMinutes)}), `
      + `знято ${summary.removedCount}, залишено ${summary.stayedCount}.`;

  return (
    <div className={`alert ${fits ? 'alert-ok' : 'alert-warning'} schedule-report`} role="status">
      <div className="schedule-report-head">
        <strong>{heading}</strong>
        <button type="button" className="btn btn-ghost" onClick={onClose}>Закрити</button>
      </div>

      {fits && <p>Усі завдання вмістилися в розклад.</p>}

      {summary.unscheduled.length > 0 && (
        <>
          <p>
            <strong>Не вмістилися в розклад:</strong> для цих завдань бракує вільного часу до дедлайну.
            Додайте години в <Link to="/availability">доступному часі</Link>, змініть дедлайни або обсяг.
          </p>
          <ul className="report-list">
            {summary.unscheduled.map((item) => (
              <li key={item.taskId}>
                «{item.title}»: не вистачило {formatMinutes(item.missingMinutes)}
                {' '}(заплановано {formatMinutes(item.scheduledMinutes)} із {formatMinutes(item.remainingMinutes)})
              </li>
            ))}
          </ul>
        </>
      )}

      {summary.conflicts.length > 0 && (
        <>
          <p><strong>Конфлікти:</strong> закріплені або завершені сесії перетинаються. Їх можна змінити, клікнувши на сесію в календарі.</p>
          <ul className="report-list">
            {summary.conflicts.map((item, i) => (
              <li key={i}>
                «{item.firstTitle}» і «{item.secondTitle}»: {formatDateTime(item.startUtc)} – {formatDateTime(item.endUtc)}
              </li>
            ))}
          </ul>
        </>
      )}
    </div>
  );
}
