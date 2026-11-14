import type { CapacityReport, TaskCapacity } from '../api/types';

export interface CapacitySummary {
  shortfallMinutes: number;
  firstShortfall: TaskCapacity | null;
  shortfallTasks: TaskCapacity[];
  overdue: TaskCapacity[];
}

export function summarizeCapacity(report: CapacityReport | null): CapacitySummary {
  const tasks = report?.tasks ?? [];
  const shortfallTasks = tasks.filter((t) => t.status === 'Shortfall');
  return {
    shortfallMinutes: report?.totalShortfallMinutes ?? 0,
    firstShortfall: shortfallTasks[0] ?? null,
    shortfallTasks,
    overdue: tasks.filter((t) => t.status === 'Overdue'),
  };
}

export const hasCapacityProblems = (summary: CapacitySummary) =>
  summary.shortfallTasks.length > 0 || summary.overdue.length > 0;
