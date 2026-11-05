import type { ExceptionKind, Priority, TaskStatus } from '../api/types';

export const PRIORITY_LABELS: Record<Priority, string> = {
  Low: 'Низький',
  Medium: 'Середній',
  High: 'Високий',
  Critical: 'Критичний',
};

export const STATUS_LABELS: Record<TaskStatus, string> = {
  NotStarted: 'Не розпочато',
  InProgress: 'У процесі',
  Completed: 'Виконано',
};

export const EXCEPTION_LABELS: Record<ExceptionKind, string> = {
  Unavailable: 'Недоступний весь день',
  CustomHours: 'Інші години',
};

export const PRIORITIES = Object.keys(PRIORITY_LABELS) as Priority[];
export const STATUSES = Object.keys(STATUS_LABELS) as TaskStatus[];
