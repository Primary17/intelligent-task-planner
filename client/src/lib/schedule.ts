import type { GenerateScheduleResponse, GeneratedSession, ReplanScheduleResponse, ScheduleConflict, UnscheduledWork } from '../api/types';
import { addDays } from './dates';

export interface Horizon {
  from: string;
  to: string;
}

export interface Names {
  taskTitle: (taskId: string) => string | undefined;
  sessionTitle: (sessionId: string) => string | undefined;
}

export interface UnscheduledItem {
  taskId: string;
  title: string;
  remainingMinutes: number;
  scheduledMinutes: number;
  missingMinutes: number;
}

export interface ConflictItem {
  firstTitle: string;
  secondTitle: string;
  startUtc: string;
  endUtc: string;
}

export interface ScheduleSummary {
  kind: 'generate' | 'replan';
  createdCount: number;
  createdMinutes: number;
  removedCount: number;
  stayedCount: number;
  unscheduled: UnscheduledItem[];
  conflicts: ConflictItem[];
}

// The planner does not skip the past, so the horizon starts at "now" for the current week.
export function weekHorizon(weekStart: Date, now: Date): Horizon | null {
  const start = new Date(Math.max(weekStart.getTime(), now.getTime()));
  const end = addDays(weekStart, 7);
  return end > start ? { from: start.toISOString(), to: end.toISOString() } : null;
}

const sumMinutes = (sessions: GeneratedSession[]) => sessions.reduce((sum, s) => sum + s.plannedMinutes, 0);

function toUnscheduled(items: UnscheduledWork[], names: Names): UnscheduledItem[] {
  return items
    .map((item) => ({
      taskId: item.studyTaskId,
      title: names.taskTitle(item.studyTaskId) ?? 'Невідоме завдання',
      remainingMinutes: item.remainingMinutes,
      scheduledMinutes: item.scheduledMinutes,
      missingMinutes: item.remainingMinutes - item.scheduledMinutes,
    }))
    .filter((item) => item.missingMinutes > 0)
    .sort((a, b) => b.missingMinutes - a.missingMinutes);
}

function toConflicts(items: ScheduleConflict[], names: Names): ConflictItem[] {
  return items.map((item) => ({
    firstTitle: names.sessionTitle(item.firstSessionId) ?? 'Сесія',
    secondTitle: names.sessionTitle(item.secondSessionId) ?? 'Сесія',
    startUtc: item.overlapStartUtc,
    endUtc: item.overlapEndUtc,
  }));
}

export function summarizeGenerate(response: GenerateScheduleResponse, names: Names): ScheduleSummary {
  return {
    kind: 'generate',
    createdCount: response.sessions.length,
    createdMinutes: sumMinutes(response.sessions),
    removedCount: 0,
    stayedCount: 0,
    unscheduled: toUnscheduled(response.unscheduled, names),
    conflicts: toConflicts(response.conflicts, names),
  };
}

export function summarizeReplan(response: ReplanScheduleResponse, names: Names): ScheduleSummary {
  return {
    kind: 'replan',
    createdCount: response.created.length,
    createdMinutes: sumMinutes(response.created),
    removedCount: response.removed.length,
    stayedCount: response.stayed.length,
    unscheduled: toUnscheduled(response.unscheduled, names),
    conflicts: toConflicts(response.conflicts, names),
  };
}

export const scheduleFits = (summary: ScheduleSummary) =>
  summary.unscheduled.length === 0 && summary.conflicts.length === 0;

export const browserTimeZone = () => Intl.DateTimeFormat().resolvedOptions().timeZone || undefined;
