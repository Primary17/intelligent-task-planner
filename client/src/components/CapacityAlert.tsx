import { Link } from 'react-router-dom';
import type { CapacityReport } from '../api/types';
import { formatDateTime, formatMinutes } from '../lib/dates';
import { hasCapacityProblems, summarizeCapacity } from '../lib/capacity';

export function CapacityAlert({ report }: { report: CapacityReport | null }) {
  const summary = summarizeCapacity(report);
  if (!hasCapacityProblems(summary)) return null;

  return (
    <div className="alert alert-warning" role="status">
      {summary.firstShortfall && (
        <p>
          <strong>Не вистачає {formatMinutes(summary.shortfallMinutes)} доступного часу.</strong>{' '}
          Найраніший дедлайн, що під загрозою: «{summary.firstShortfall.title}»
          {' '}({formatDateTime(summary.firstShortfall.deadlineUtc)}). Додайте години в{' '}
          <Link to="/availability">доступному часі</Link>, змініть дедлайни або обсяг завдань.
        </p>
      )}
      {summary.overdue.length > 0 && (
        <p>
          <strong>Прострочені завдання:</strong> {summary.overdue.map((t) => `«${t.title}»`).join(', ')}.
        </p>
      )}
    </div>
  );
}
